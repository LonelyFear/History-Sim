using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

public class State : PopObject
{
    public string displayName = "Nation";
    public Color color;
    public Color displayColor;
    public Color capitalColor;
    public List<Army> armies = new List<Army>();
    public GovernmentType government = GovernmentType.MONARCHY;
    public List<Region> realmRegions = new List<Region>(); 
    public List<Region> regions = new List<Region>();
    public Region capital;
    public long manpower = 0;
    public int occupiedLand = 0;
    public float totalWealth = 0;
    public float mobilizationRate = 0.3f;
    public float taxRate = 0.1f;
    public float tribute = 0.1f;
    public List<State> vassals = new List<State>();
    public State liege = null;
    public Dictionary<State, Relation> relations = new Dictionary<State, Relation>();
    public Dictionary<War, bool> wars = new Dictionary<War, bool>();
    public List<State> enemies = new List<State>();
    public List<State> borderingStates = new List<State>();
    public int borderingRegions = 0;
    public int externalBorderingRegions = 0;
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
        switch (sovereignty)
        {
            case Sovereignty.INDEPENDENT:
                capitalColor = new Color(1, 0, 0);
                break;
            case Sovereignty.PUPPET:
                capitalColor = new Color(0.25f, 0, 0);
                break;
            case Sovereignty.PROVINCE:
                capitalColor = new Color(0.5f, 0, 0.5f);
                break;
            case Sovereignty.COLONY:
                capitalColor = new Color(1, 0, 1);
                break;
        }
    }
    #region Diplomacy
    public void UpdateEnemies()
    {
        List<State> atWarWith = new List<State>();
        foreach (var pair in wars)
        {
            War war = pair.Key;
            bool attacker = pair.Value;
            if (attacker)
            {
                foreach (State state in war.defenders)
                {
                    atWarWith.Add(state);
                }
            }
            else
            {
                foreach (State state in war.attackers)
                {
                    atWarWith.Add(state);
                }
            }
        }
        enemies = atWarWith;
    }
    public List<War> GetWarsWithState(State state)
    {
        List<War> ws = new List<War>();
        foreach (var pair in wars)
        {
            War war = pair.Key;
            bool attacker = pair.Value;
            if (attacker)
            {
                if (war.defenders.Contains(state))
                {
                    ws.Add(war);
                }
            }
            else
            {
                if (war.attackers.Contains(state))
                {
                    ws.Add(war);
                }
            }
        }
        return ws;
    }
    public void RelationsUpdate()
    {
        foreach (var pair in relations)
        {
            if (!borderingStates.Contains(pair.Key) && !enemies.Contains(pair.Key))
            {
                relations.Remove(pair.Key);
                continue;
            }
            if (enemies.Contains(pair.Key))
            {
                pair.Value.truce += (int)(TimeManager.ticksPerMonth / 2f);
            }
            else
            {
                pair.Value.truce -= (int)TimeManager.ticksPerMonth;
            }
        }
        foreach (State state in borderingStates)
        {
            if (!relations.ContainsKey(state))
            {
                EstablishRelations(state);
            }
        }
    }
    public void EstablishRelations(State state, int opinion = 0)
    {
        if (state == this)
        {
            return;
        }

        if (!relations.Keys.Contains(state))
        {
            relations.Add(state, new Relation()
            {
                opinion = opinion
            });
        }
        else
        {
            relations[state].opinion = opinion;
        }
    }
    public void StartWars()
    {
        if (sovereignty == Sovereignty.INDEPENDENT)
        {
            foreach (var pair in relations)
            {
                State state = pair.Key;
                Relation relation = pair.Value;
                if (relation.opinion < 0 && !enemies.Contains(state) && relation.truce <= 0 && state.sovereignty == Sovereignty.INDEPENDENT)
                {
                    float warDeclarationChance = Mathf.Lerp(0.001f, 0.005f, relation.opinion / (float)Relation.minOpinionValue);
                    if (liege == state || vassals.Contains(state))
                    {
                        warDeclarationChance = 0f;
                    }
                    if (rng.NextSingle() < warDeclarationChance)
                    {
                        StartWar(state, WarType.CONQUEST);
                        relation.opinion = Relation.minOpinionValue;
                        return;
                    }
                }
            }            
        }
    }
    public void GetRealmBorders()
    {
        if (vassals.Count > 0)
        {
            foreach (State vassal in vassals)
            {
                foreach (State state in vassal.borderingStates)
                {
                    if (!borderingStates.Contains(state))
                    {
                        borderingStates.Add(state);
                    }
                }
            }
        }
        foreach (State state in borderingStates.ToArray())
        {
            if (state.liege != null && !borderingStates.Contains(state.liege))
            {
                borderingStates.Add(state.liege);
            }
        }
    }
    public void EndWars()
    {
        if (sovereignty == Sovereignty.INDEPENDENT)
        {
            foreach (var pair in relations.ToArray())
            {
                State state = pair.Key;
                Relation relation = pair.Value;
                
                if (relation.opinion >= 0 && enemies.Contains(state))
                {
                    if (rng.NextSingle() < 0.01f * (relation.opinion + 1))
                    {
                        EndWar(GetWarsWithState(state).PickRandom(rng));
                    }
                }
                if (capital.occupier == state && liege == null)
                {
                    foreach (War war in GetWarsWithState(state))
                    {
                        EndWar(war);
                    }
                    state.AddVassal(this);
                }
            }
        }
    }
    public void Capitualate()
    {
        if (capital.GetController() != GetHighestLiege())
        {
            foreach (Region region in regions)
            {
                if (capital.occupier == this || capital.occupier == null)
                {
                    region.occupier = capital.occupier;
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
                float relationChangeChance = 0.5f;
                float relationDamageChance = 0.5f;
                if (enemies.Contains(state))
                {
                    relationChangeChance *= 0.75f;
                }
                if (rng.NextSingle() < relationChangeChance)
                {
                    if (rng.NextSingle() < relationDamageChance)
                    {
                        relations.ChangeOpinion(-1);
                    }
                    else
                    {
                        relations.ChangeOpinion(1);
                    }
                }
            }
        }
    }
    #endregion
    #region Government
    public void UpdateDisplayName()
    {
        string govtName;
        switch (government)
        {
            case GovernmentType.REPUBLIC:
                switch (sovereignty)
                {
                    case Sovereignty.COLONY:
                        govtName = "Colony";
                        break;
                    case Sovereignty.PUPPET:
                        govtName = "Mandate";
                        break;
                    case Sovereignty.PROVINCE:
                        govtName = "Department";
                        break;
                    default:
                        govtName = "Free State";
                        if (vassals.Count > 0)
                        {
                            govtName = "Republic";
                        }
                        else if (vassals.Count > 3)
                        {
                            govtName = "Commonwealth";
                        }
                        break;
                }
                break;
            case GovernmentType.MONARCHY:
                switch (sovereignty)
                {
                    case Sovereignty.COLONY:
                        govtName = "Crown Colony";
                        break;
                    case Sovereignty.PUPPET:
                        govtName = "Protectorate";
                        break;
                    case Sovereignty.PROVINCE:
                        govtName = "Duchy";
                        break;
                    default:
                        govtName = "Principality";
                        if (vassals.Count > 0)
                        {
                            govtName = "Kingdom";
                        }
                        else if (vassals.Count > 3)
                        {
                            govtName = "Empire";
                        }
                        break;
                }
                break;
            case GovernmentType.AUTOCRACY:
                switch (sovereignty)
                {
                    case Sovereignty.COLONY:
                        govtName = "Territory";
                        break;
                    case Sovereignty.PUPPET:
                        govtName = "Client State";
                        break;
                    case Sovereignty.PROVINCE:
                        govtName = "Province";
                        break;
                    default:
                        govtName = "State";
                        if (vassals.Count > 0)
                        {
                            govtName = "Autocracy";
                        }
                        else if (vassals.Count > 3)
                        {
                            govtName = "Imperium";
                        }
                        break;
                }
                break;
            default:
                govtName = "State";
                break;
        }
        displayName = govtName + " of " + name;
    }
    public void UpdateDisplayColor()
    {
        displayColor = color;
        switch (sovereignty)
        {
            case Sovereignty.COLONY:
                displayColor = liege.color;
                break;
            case Sovereignty.PROVINCE:
                displayColor = liege.color;
                break;
            case Sovereignty.PUPPET:
                displayColor = liege.color;
                break;
        }
    }
    public void CountStatePopulation()
    {
        realmRegions = GetRealmRegions();
        long countedP = 0;
        long countedW = 0;

        List<Pop> countedPops = new List<Pop>();
        List<State> borders = new List<State>();
        Dictionary<Profession, long> countedProfessions = new Dictionary<Profession, long>();
        foreach (Profession profession in Enum.GetValues(typeof(Profession)))
        {
            countedProfessions.Add(profession, 0);
        }
        
        Dictionary<Culture, long> cCultures = new Dictionary<Culture, long>();
        borderingRegions = 0;
        float countedWealth = 0;
        int occRegions = 0;
        foreach (Region region in realmRegions)
        {
            countedP += region.population;
            countedW += region.workforce;
            countedWealth += region.wealth;
            countedPops.AddRange(region.pops);
            if (region.frontier || region.border)
            {
                if (region.occupier != null && regions.Contains(region))
                {
                    occRegions++;
                }
                borderingRegions++;
                bool bordersOtherState = false;
                // Gets the borders of our state
                foreach (Region border in region.borderingRegions)
                {
                    if (border.owner != null && border.owner != this)
                    {
                        if (!borders.Contains(border.owner))
                        {
                            borders.Add(border.owner);
                        }
                        bordersOtherState = !IsStateInRealm(border.owner);
                    }
                }
                // External Bordering Regions is the borders on the outside of the realm
                // (NO INTERIOR BORDERS)
                if (bordersOtherState)
                {
                    externalBorderingRegions++;
                }
            }

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
        occupiedLand = occRegions;
        borderingStates = borders;
        totalWealth = countedWealth;
        professions = countedProfessions;
        cultures = cCultures;
        population = countedP;
        workforce = countedW;
        pops = countedPops;
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
    public int GetMaxRegionsCount()
    {
        return 6 + (tech.societyLevel * 2);
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
            if (state.sovereignty == Sovereignty.INDEPENDENT)
            {
                foreach (War war in state.wars.Keys)
                {
                    war.RemoveParticipants(state.GetAllVassals());
                }  
                                
            }

            foreach (State vassal in state.vassals.ToArray())
            {
                state.RemoveVassal(vassal);
            }          
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
    #region Military
    public long GetArmyPower()
    {
        float interiorArmyPower = GetRealmManpower() / (float)realmRegions.Count;
        return (long)interiorArmyPower;
    }
    public long GetRealmManpower()
    {
        long mp = manpower;
        foreach (State state in vassals)
        {
            mp += state.manpower;
        }
        return mp;
    }
    public List<Region> GetRealmRegions()
    {
        List<Region> realmRegions = [.. regions];
        foreach (State state in vassals)
        {
            realmRegions.AddRange(state.regions);
        }
        return realmRegions;
    }
    public int GetRealmBorderLength() {
        int size = externalBorderingRegions;
        foreach (State state in vassals)
        {
            size += state.externalBorderingRegions;
        }
        return size;
    }
    #endregion
    #region Utility
    public State GetHighestLiege()
    {
        State state = this;
        while (state != null && state.liege != null)
        {
            state = state.liege;
        }
        return state;
    }
    public bool IsStateInRealm(State state) {
        return GetHighestLiege() == state.GetHighestLiege();
    }   
    public List<State> GetAllVassals()
    {
        List<State> collectedVassals = new List<State>();
        foreach (State vassal in vassals)
        {
            collectedVassals.Add(vassal);
            if (vassal.vassals.Count() > 0)
            {
                foreach (State vassalVassal in vassal.vassals)
                {
                    collectedVassals.Add(vassalVassal);
                }
            }
        }
        return collectedVassals;
    }
    public void StartWar(State state, WarType type)
    {
        EstablishRelations(state, Relation.minOpinionValue);
        state.EstablishRelations(this, Relation.minOpinionValue);
        War war = new War(GetAllVassals(), state.GetAllVassals(), type);
    }
    public void RemoveOccupationFromState(State state)
    {
        foreach (Region region in regions)
        {
            if (region.occupier == state)
            {
                region.occupier = null;
            }
        }
    }
    public void EndWar(War war)
    {
        war.EndWar();       
    }
    #endregion
}   

public enum GovernmentType {
    REPUBLIC,
    MONARCHY,
    AUTOCRACY,
}

public enum Sovereignty
{
    INDEPENDENT,
    PUPPET,
    COLONY,
    PROVINCE
}

public enum WarType
{
    CONQUEST,
    CIVIL_WAR,
    REVOLT
}