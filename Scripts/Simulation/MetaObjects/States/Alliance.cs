using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

[MessagePackObject(AllowPrivate = true)]
// Alliances
// Versatile, can represent unions and realms. Use this class for anything involving collections of states
public partial class Alliance : Polity
{
    [Key(25)] public AllianceType type;
    [Key(26)] ulong? leadStateId;
    [Key(27)] public bool exclusive = true;

    // References
    [Key(28)] List<ulong> memberStateIds = [];
    [IgnoreMember] public List<State> memberStates = [];

    // Reference Variables
    [IgnoreMember] State _leadState;
    [IgnoreMember] public State leadState { 
        get
        {
            if (_leadState == null && leadStateId != null) 
                _leadState = ObjectManager.GetState(leadStateId);
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
        base.PrepareForSave();
    }
    public override void LoadFromSave()
    {
        memberStates = [..memberStateIds.Select(i => ObjectManager.GetState(i))];
        base.LoadFromSave();
    }
    public void SetLeader(State newLeader)
    {
        leadState = newLeader;
        AddMember(newLeader);
    }
    public void AddMember(State newMember)
    {
        if (memberStates.Contains(newMember)) return;

        if (exclusive)
        {
            newMember.GetAllianceOfType(type)?.RemoveMember(newMember);
        }

        newMember.alliances.Add(this);
        memberStates.Add(newMember);

        NameGenerator.UpdateAllianceName(this);
    }
    public void RemoveMember(State member)
    {
        if (!memberStates.Contains(member)) return;

        member.alliances.Remove(this);
        memberStates.Remove(member);  

        if (memberStates.Count < 2 || member == leadState)
        {
            Die();
            ObjectManager.DeleteAlliance(this);
        }
    }
    public override int GetArmyPower()
    {
        int ap = 0;
        foreach (State state in memberStates)
        {
            ap += state.armyPower;
        }
        return ap;
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
        if (type == AllianceType.REALM)
        {
            foreach (State member in memberStates.ToArray())
            {
               if (member.sovereignty != Sovereignty.REBELLIOUS) mp += member.manpower;
            }            
        } 
        else
        {
            return memberStates.Sum(st => st.manpower);               
        }
        return mp;
    }
    public override void CountPopulation()
    {        
        regions = [..memberStates.SelectMany(state => state.regions)];
        base.CountPopulation();
    }

}
public enum AllianceType
{
    // Political Types
    REALM,
    ALLIANCE,
    UNION
}