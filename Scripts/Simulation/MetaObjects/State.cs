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
    public Color capitalColor;
    public List<Army> armies = new List<Army>();
    public GovernmentTypes government = GovernmentTypes.MONARCHY;
    public List<Region> regions = new List<Region>();
    public Region capital;
    public long manpower;
    public float mobilizationRate = 0.3f;
    public float taxRate = 0.1f;
    public float tribute = 0.1f;
    public List<State> vassals = new List<State>();
    public State liege = null;
    public Dictionary<State, Relation> relations = new Dictionary<State, Relation>();
    public List<State> enemies = new List<State>();
    public List<State> borderingStates = new List<State>();
    public uint borderingRegions = 0;
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
                pair.Value.truce -= TimeManager.ticksPerMonth;
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
    public void UpdateEnemies()
    {
        if (sovereignty != Sovereignty.INDEPENDENT)
        {
            enemies = liege.enemies;
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
                    float warDeclarationChance = 1f;//Mathf.Lerp(0.1f, 0.5f, relation.opinion / (float)Relation.minOpinionValue);
                    if (liege == state || vassals.Contains(state))
                    {
                        warDeclarationChance = 0f;
                    }
                    if (rng.NextSingle() < warDeclarationChance)
                    {
                        StartWar(state);
                        relation.opinion = Relation.minOpinionValue;
                        GD.Print("War");
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

                if (relation.opinion > 0 && enemies.Contains(state))
                {
                    if (rng.NextSingle() < 0.0001f)
                    {
                        EndWar(state);
                    }
                }
                if (capital.occupier == state && liege == null)
                {
                    EndWar(state, true);
                    state.AddVassal(this);
                }
            }
        }
    }
    public void Capitualate()
    {
        if (capital.occupier != this && capital.occupier != null)
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
                float relationImproveChance = 0.5f;
                float badOutcomeChance = 0.5f;
                if (leader != null)
                {
                    switch (leader.agression)
                    {
                        case TraitLevel.VERY_LOW:
                            relationImproveChance = 0.8f;
                            badOutcomeChance = 0.2f;
                            break;
                        case TraitLevel.LOW:
                            relationImproveChance = 0.6f;
                            badOutcomeChance = 0.4f;
                            break;
                        case TraitLevel.HIGH:
                            relationImproveChance = 0.4f;
                            badOutcomeChance = 0.8f;
                            break;
                        case TraitLevel.VERY_HIGH:
                            relationImproveChance = 0.2f;
                            badOutcomeChance = 1f;
                            break;
                    }
                    switch (leader.culture.agression)
                    {
                        case TraitLevel.VERY_LOW:
                            relationImproveChance += 0.1f;
                            badOutcomeChance -= 0.05f;
                            break;
                        case TraitLevel.LOW:
                            relationImproveChance += 0.05f;
                            badOutcomeChance -= 0.025f;
                            break;
                        case TraitLevel.HIGH:
                            relationImproveChance -= 0.1f;
                            badOutcomeChance += 0.05f;
                            break;
                        case TraitLevel.VERY_HIGH:
                            relationImproveChance -= 0.15f;
                            badOutcomeChance += 0.1f;
                            break;
                    }
                }

                if (rulingPop != null && state.rulingPop != null && state.rulingPop.culture != rulingPop.culture)
                {
                    badOutcomeChance += 0.05f;
                    relationImproveChance -= 0.1f;
                }
                if (enemies.Contains(state))
                {
                    relationImproveChance *= 0.5f;
                }

                relationImproveChance = Mathf.Clamp(relationImproveChance, 0, 1);
                badOutcomeChance = Mathf.Clamp(badOutcomeChance, 0, 1);
                if (rng.NextSingle() < badOutcomeChance)
                {
                    relations.ChangeOpinion(1);
                }
                else
                {
                    if (rng.NextSingle() < badOutcomeChance)
                    {
                        relations.ChangeOpinion(-1);
                    }
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
            case GovernmentTypes.MONARCHY:
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
                        govtName = "Grand Duchy";
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
            case GovernmentTypes.AUTOCRACY:
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
                displayColor = Utility.MultiColourLerp([liege.color, color], 0.25f);
                break;
        }
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
            foreach (State vassal in state.vassals.ToArray())
            {
                state.RemoveVassal(vassal);
            }
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
        return (long)(manpower / (float)borderingRegions);
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
    public State[] GetAllVassals()
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
        return collectedVassals.ToArray();
    }
    public void StartWar(State state)
    {
        EstablishRelations(state, Relation.minOpinionValue);
        state.EstablishRelations(this, Relation.minOpinionValue);
        if (!enemies.Contains(state))
        {
            enemies.Add(state);
        }
        if (!state.enemies.Contains(this))
        {
            state.enemies.Add(this);
        }
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
    public void EndWar(State state, bool returnTerritories = false)
    {
        if (enemies.Contains(state))
        {
            enemies.Remove(state);
        }
        if (state.enemies.Contains(this))
        {
            state.enemies.Remove(this);
        }
        if (returnTerritories)
        {
            state.RemoveOccupationFromState(this);
            RemoveOccupationFromState(state);            
        }

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