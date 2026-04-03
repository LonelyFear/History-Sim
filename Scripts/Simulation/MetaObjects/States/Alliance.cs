using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Godot;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
// Alliances
// Versatile, can represent unions and realms. Use this class for anything involving collections of states
public partial class Alliance : Polity
{
    [Key(0)] public AllianceType type;
    [Key(2)] ulong? leadStateId;
    [Key(1)] HashSet<ulong> memberStateIds = [];
    [Key(3)] public bool exclusive = true;

    // References
    [IgnoreMember] public HashSet<State> memberStates = [];

    // Reference Variables
    [IgnoreMember] State _leadState;
    [IgnoreMember] public State leadState { 
        get
        {
            if (_leadState == null && leadStateId != null) 
                _leadState = objectManager.GetState(leadStateId);
            return _leadState;
        } 
        set
        {
            leadStateId = value?.id;
            _leadState = value;
        } 
    } 

    public override void PrepareForSave()
    {
        memberStateIds = [..memberStates.Select(s => s.id)];
        PopObjectSave();
        PolitySave();
    }
    public override void LoadFromSave()
    {
        memberStates = [..memberStateIds.Select(i => objectManager.GetState(i))];
        PopObjectSave();
        PolityLoad();
    }
    public void SetLeader(State newLeader)
    {
        if (memberStates.Contains(newLeader))
        {
            leadState = newLeader;
        }
    }
    public void AddMember(State newMember)
    {
        if (memberStates.Contains(newMember)) return;

        if (exclusive)
        {
            newMember.diplomacy.GetAllianceOfType(type)?.RemoveMember(newMember);
        }

        newMember.diplomacy.allianceIds.Add(id);
        memberStates.Add(newMember);
    }
    public void RemoveMember(State member)
    {
        if (!memberStates.Contains(member)) return;

        member.diplomacy.allianceIds.Remove(id);
        memberStates.Remove(member);  

        if (memberStates.Count < 2 || member == leadState)
        {
            Die();
            objectManager.DeleteAlliance(this);
        }
    }
    public void UpdateRegions()
    {
        HashSet<Region> countedRegions = [];
        foreach (State member in memberStates)
        {
            foreach (Region region in member.regions)
            {
                countedRegions.Add(region);
            }
        }
        regions = countedRegions;
    }
    public bool HasMember(State state)
    {
        return memberStates.Contains(state);
    }
    public override void Die()
    {
        dead = true;
        tickDestroyed = simManager.timeManager.ticks;
        foreach (State member in memberStates.ToArray())
        {
            RemoveMember(member);
        }
        leadState = null;
    }
    public override int GetManpower()
    {
        int mp = 0;
        foreach (State member in memberStates.ToArray())
        {
            mp += member.manpower;
        }
        return mp;
    }

}
public enum AllianceType
{
    // Political Types
    REALM,
    ALLIANCE,
    UNION
}