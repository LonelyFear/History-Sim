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
        base.PrepareForSave();
    }
    public override void LoadFromSave()
    {
        memberStates = [..memberStateIds.Select(i => objectManager.GetState(i))];
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
            objectManager.DeleteAlliance(this);
        }
    }
    public override int GetArmyPower()
    {
        return memberStates.Sum(state => state.armyPower);
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
        long countedP = 0;
        long countedW = 0;
        Dictionary<string, long> countedProfessions = [];
        Dictionary<ulong, long> cCultures = [];

        HashSet<State> borders = [];
    
        float countedWealth = 0;
        float countedBaseWealth = 0;
        int occRegions = 0;
        Tech newAvg = new();
        foreach (State state in memberStates)
        {
            newAvg.militaryLevel += state.tech.militaryLevel;
            newAvg.societyLevel += state.tech.societyLevel;
            newAvg.industryLevel += state.tech.industryLevel;        
            occRegions += state.occupiedLand;

            // Adds up population to state total
            countedP += state.population;
            countedW += state.workforce;
            countedWealth += state.totalWealth;
            countedBaseWealth += state.baseWealth;

            foreach (State border in borderingStates)
            {
                if (!memberStates.Contains(border))
                {
                    borders.Add(border);
                }
            }    

            CountClasses(state, countedProfessions);
            CountCultures(state, cCultures);
        }
        
        // Updates values
        occupiedLand = occRegions;
        borderingStates = borders;
        totalWealth = countedWealth;
        baseWealth = countedBaseWealth;
        
        foreach (var pair in countedProfessions)
        {
            professions[pair.Key] = pair.Value;
        }
        
        cultureIds = cCultures;
        population = countedP;
        workforce = countedW;
        dependents = population - workforce;

        manpower = GetManpower();
        armyPower = GetArmyPower();

        // Tech
        newAvg.militaryLevel /= Mathf.Max(memberStates.Count, 1);
        newAvg.societyLevel /= Mathf.Max(memberStates.Count, 1);
        newAvg.industryLevel /= Mathf.Max(memberStates.Count, 1);
        averageTech = newAvg;
    }

}
public enum AllianceType
{
    // Political Types
    REALM,
    ALLIANCE,
    UNION
}