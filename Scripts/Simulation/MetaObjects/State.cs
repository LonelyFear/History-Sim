using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class State : PopObject
{
    public string displayName = "Nation";
    public Color color;
    public Color displayColor;
    public List<Army> armies = new List<Army>();
    public GovernmentTypes government = GovernmentTypes.MONARCHY;
    public List<Region> regions = new List<Region>();
    public Region capital;
    public long manpower;
    public float mobilizationRate = 0.3f;
    public float taxRate = 0.1f;
    public float tribute = 0.1f;
    public List<State> vassals = new List<State>();
    State liege;
    public Dictionary<State, Relation> relations;
    public List<War> wars = new List<War>();
    public List<State> enemies = new List<State>();
    public List<State> borderingStates = new List<State>();
    public Sovereignty sovereignty = Sovereignty.INDEPENDENT;


    public List<Character> characters = new List<Character>();
    int monthsSinceElection = 0;
    public Tech tech;

    public int maxSize = 1;

    // Government
    public long wealth;
    public Character leader;
    public Pop rulingPop;
    public Character heir;
    public void UpdateCapital()
    {
        if (capital == null)
        {
            capital = regions[0];
        }
    }
    #region Diplomacy
    public void EstablishRelations(State state)
    {
        if (!relations.Keys.Contains(state))
        {
            relations.Add(state, new Relation());
        }
    }
    public void UpdateEnemies()
    {
        enemies = new List<State>();
        foreach (War war in wars)
        {
            if (war.agressors.Contains(this))
            {
                // We are attacking
                foreach (State defender in war.defenders)
                {
                    enemies.Add(defender);
                }
            }
            else
            {
                // We are defending
                foreach (State attacker in war.agressors)
                {
                    enemies.Add(attacker);
                }
            }
        }
    }
    public void UpdateDiplomacy()
    {
        foreach (var pair in relations)
        {
            State state = pair.Key;
            Relation relations = pair.Value;
            if (liege != state && !vassals.Contains(state))
            {
                float relationImproveChance = 0.5f;
                switch (leader.agression)
                {
                    case TraitLevel.VERY_LOW:
                        relationImproveChance = 0.8f;
                        break;
                    case TraitLevel.LOW:
                        relationImproveChance = 0.6f;
                        break;
                    case TraitLevel.HIGH:
                        relationImproveChance = 0.4f;
                        break;
                    case TraitLevel.VERY_HIGH:
                        relationImproveChance = 0.2f;
                        break;
                }
                if (relationImproveChance < rng.NextSingle())
                {
                    relations.ChangeOpinion(1);
                }
                else
                {
                    relations.ChangeOpinion(-1);
                }
            }
        }
    }
    #endregion
    #region Government
    public void RulersCheck()
    {
        tech = rulingPop.tech;
        monthsSinceElection++;
        switch (government)
        {
            case GovernmentTypes.MONARCHY:
                if (leader != null)
                {
                    heir = leader.GetHeir();
                }
                if (leader == null)
                {
                    SetLeader(heir);
                }
                break;
            case GovernmentTypes.REPUBLIC:
                if (monthsSinceElection > 48 || leader == null)
                {
                    RunElection();
                }
                break;
        }
    }
    public void RunElection()
    {
        monthsSinceElection = 0;
        Character newLeader = simManager.CreateCharacter(rulingPop, 20, 50);
        if (characters.Count > 0)
        {
            newLeader = characters[rng.Next(0, characters.Count - 1)];
        }
        SetLeader(newLeader);
    }
    public void UpdateDisplayName()
    {
        string govtName;
        switch (government)
        {
            case GovernmentTypes.REPUBLIC:
                govtName = "Republic";
                break;
            case GovernmentTypes.MONARCHY:
                govtName = "Kingdom";
                break;
            case GovernmentTypes.AUTOCRACY:
                govtName = "Dictatorship";
                break;
            default:
                govtName = "State";
                break;
        }
        displayName = govtName + " of " + name;
    }
    public void CountStatePopulation()
    {
        long countedP = 0;
        long countedW = 0;

        List<Pop> countedPops = new List<Pop>();
        Dictionary<Profession, long> countedProfessions = new Dictionary<Profession, long>();
        foreach (Profession profession in Enum.GetValues(typeof(Profession)))
        {
            countedProfessions.Add(profession, 0);
        }
        Dictionary<Culture, long> cCultures = new Dictionary<Culture, long>();

        foreach (Region region in regions.ToArray())
        {
            countedP += region.population;
            countedW += region.workforce;
            countedPops.AddRange(region.pops);

            foreach (Profession profession in region.professions.Keys)
            {
                countedProfessions[profession] += region.professions[profession];
            }
            foreach (Culture culture in region.cultures.Keys)
            {
                if (cCultures.ContainsKey(culture))
                {
                    cCultures[culture] += region.cultures[culture];
                }
                else
                {
                    cCultures.Add(culture, region.cultures[culture]);
                }
            }
        }
        professions = countedProfessions;
        cultures = cCultures;
        population = countedP;
        workforce = countedW;
        pops = countedPops;
    }

    public void SetLeader(Character newLeader)
    {
        if (newLeader != null)
        {
            if (!characters.Contains(newLeader))
            {
                AddCharacter(newLeader);
            }
        }
        leader = newLeader;
        // Removes our heir if the new leader was the heir.
        if (newLeader == heir && newLeader != null)
        {
            heir = null;
        }
    }
    #endregion
    #region Regions
    public void AddRegion(Region region)
    {
        if (!regions.Contains(region))
        {
            if (region.owner != null)
            {
                region.owner.RemoveRegion(region);
            }
            region.owner = this;
            regions.Add(region);
            pops.AddRange(region.pops);
        }
    }
    public void RemoveRegion(Region region)
    {
        if (regions.Contains(region))
        {
            region.owner = null;
            regions.Remove(region);
        }
    }
    #endregion
    #region Vassals
    public void SetStateSovereignty(State state, Sovereignty sovereignty)
    {
        if (state != this)
        {
            if (sovereignty == Sovereignty.INDEPENDENT)
            {
                if (vassals.Contains(state))
                {
                    state.sovereignty = Sovereignty.INDEPENDENT;
                    vassals.Remove(state);
                }
            }
            else
            {
                if (vassals.Contains(state))
                {
                    state.sovereignty = sovereignty;
                }
                else
                {
                    AddVassal(state, sovereignty);
                }
            }
        }
    }

    public void AddVassal(State state, Sovereignty sovereignty = Sovereignty.PUPPET)
    {
        if (sovereignty != Sovereignty.INDEPENDENT)
        {
            if (state.liege != null)
            {
                state.liege.RemoveVassal(state);
            }
            state.liege = this;
            state.sovereignty = sovereignty;
            vassals.Add(state);
        }
    }

    public void RemoveVassal(State state)
    {
        if (vassals.Contains(state))
        {
            state.liege = null;
            state.sovereignty = Sovereignty.INDEPENDENT;
            vassals.Remove(state);
        }
    }
    #endregion
    #region Characters
    public void AddCharacter(Character character)
    {
        if (!characters.Contains(character))
        {
            if (character.state != null)
            {
                character.state.RemoveCharacter(character);
            }
            character.state = this;
            characters.Add(character);
        }
    }
    public void RemoveCharacter(Character character)
    {
        if (characters.Contains(character))
        {
            if (leader == character)
            {
                SetLeader(null);
            }
            if (heir == character)
            {
                heir = null;
            }
            characters.Remove(character);
            character.state = null;
        }
    }
    #endregion
    #region Military
    public long GetArmyPower()
    {
        return (long)(manpower / (float)regions.Count);
    }
    #endregion
    #region Utility
    public static State GetHighestLiege(State state)
    {
        while (state.liege != null)
        {
            state = state.liege;
        }
        return state;
    }
    #endregion
}   

public enum GovernmentTypes {
    REPUBLIC,
    MONARCHY,
    AUTOCRACY,
}

public enum Sovereignty {
    INDEPENDENT,
    PUPPET,
    COLONY, 
    PROVINCE
}