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

        if (type == AllianceType.REALM)
        {
            ap = leadState.armyPower;
        } else
        {
            foreach (State state in memberStates)
            {
                ap += state.armyPower;
            }            
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
        long countedP = 0;
        long countedW = 0;
        Dictionary<string, long> countedProfessions = [];
        Dictionary<Culture, long> cCultures = [];

        HashSet<State> borders = [];
    
        float countedWealth = 0;
        float countedBaseWealth = 0;
        int occRegions = 0;

        foreach (State state in memberStates)
        {
            // Adds up population to state total
            countedP += state.population;
            countedW += state.workforce;
            countedWealth += state.totalWealth;
            countedBaseWealth += state.baseWealth;

            foreach (State border in state.borderingStates)
            {
                // Extends our border with the state
                State[] statesToEvaluate = [border, border.GetLiege()];
                foreach (State evaluatedState in statesToEvaluate)
                {
                    if (evaluatedState == null) continue;
                    borders.Add(evaluatedState);
                }                
            }
            // Counts up socialClasses
            CountClasses(state, countedProfessions);
            CountCultures(state, cCultures);
        }
        
        // Updates values
        occupiedLand = occRegions;
        borderingStates = borders;
        
        totalWealth = countedWealth;
        baseWealth = countedBaseWealth;
    
        professions = countedProfessions;
        cultures = cCultures;
        population = countedP;
        workforce = countedW;
        dependents = population - workforce;

        manpower = GetManpower();
        armyPower = GetArmyPower();
    }

    public override int GetSize()
    {
        int size = regions.Count;
        if (type == AllianceType.REALM)
        {
            size = 0;
            foreach (State state in memberStates)
            {
                if (state.sovereignty != Sovereignty.REBELLIOUS)
                {
                    size += state.GetSize();
                }
            }
        }
        return size;
    }

    public override string GenerateDescription()
    {
        if (type == AllianceType.REALM) return leadState.GenerateDescription();
        return base.GenerateDescription();
    }
    public override string GenerateHistoryText()
    {
        if (type == AllianceType.REALM) return leadState.GenerateHistoryText();
        return base.GenerateHistoryText();
    }

}
public enum AllianceType
{
    // Political Types
    REALM,
    ALLIANCE,
    UNION
}