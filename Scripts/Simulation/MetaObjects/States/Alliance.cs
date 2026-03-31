using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

[MessagePackObject]
// Alliances
// Versatile, can represent unions and realms. Use this class for anything involving collections of states
public class Alliance : Polity
{
    [Key(0)] public OrgType type;
    [Key(2)] public ulong? leadStateId;
    [Key(1)] public List<ulong> memberStateIds = new List<ulong>();

    //[IgnoreMember] List<Region> regions = new List<Region>();

    public void SetLeader(ulong? leaderId)
    {
        if (memberStateIds.Contains((ulong)leaderId))
        {
            leadStateId = leaderId;
        }
    }
    public State GetAllianceLeader()
    {
        return objectManager.GetState(leadStateId);
    }
    public void AddMember(State newMember)
    {
        if (memberStateIds.Contains(newMember.id)) return;

        if (type == OrgType.REALM)
        {
            newMember.diplomacy.GetRealm()?.RemoveMember(newMember);
        }

        newMember.diplomacy.allianceIds.Add(id);
        memberStateIds.Add(newMember.id);
    }
    public void RemoveMember(State member)
    {
        if (!memberStateIds.Contains(member.id)) return;

        member.diplomacy.allianceIds.Remove(id);
        memberStateIds.Remove(member.id);  

        if (memberStateIds.Count < 1 || member.id == leadStateId)
        {
            Die();
            objectManager.DeleteAlliance(this);
        }
    }
    public void UpdateRegions()
    {
        HashSet<ulong> countedIds = [];
        foreach (ulong stateId in memberStateIds)
        {
            State memberState = objectManager.GetState(stateId);
            foreach (ulong regionId in memberState.regionIds)
            {
                countedIds.Add(regionId);
            }
        }
        regionIds = countedIds;
        //GD.Print(regionIds.Count);
    }
    public bool HasMember(State state)
    {
        return memberStateIds.Contains(state.id);
    }
    public override void Die()
    {
        dead = true;
        tickDestroyed = simManager.timeManager.ticks;
        foreach (ulong memberId in memberStateIds.ToArray())
        {
            RemoveMember(objectManager.GetState(memberId));
        }
        leadStateId = null;
    }
    public override long GetManpower()
    {
        long mp = 0;
        foreach (ulong stateId in memberStateIds)
        {
            State memberState = objectManager.GetState(stateId);
            mp += memberState.GetManpower();
        }
        return mp;
    }

}
public enum OrgType
{
    // Political Types
    REALM,
    ALLIANCE,
}