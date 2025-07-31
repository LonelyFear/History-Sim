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
    State liege;
    public Dictionary<State, Relation> relations = new Dictionary<State, Relation>();
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
        foreach (var pair in relations)
        {
            State state = pair.Key;
            Relation relation = pair.Value;
            if (relation.opinion < 0 && !enemies.Contains(state))
            {
                float warDeclarationChance = Mathf.Lerp(0.005f, 0.05f, relation.opinion / (float)Relation.minOpinionValue);
                if (liege == state || vassals.Contains(state))
                {
                    warDeclarationChance = 0f;
                }
                if (warDeclarationChance <= rng.NextSingle())
                {
                    StartWar(state);
                    relation.opinion = Relation.minOpinionValue;
                    GD.Print("War");
                    return;
                }
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
                    if (0.001f < rng.NextSingle())
                    {
                        EndWar(state);
                    }
                }
                if (capital.occupier == state)
                {
                    EndWar(state);
                    state.AddVassal(this);
                }
            }
        }
    }
    public void Capitualate()
    {
        if (capital.occupier != this || capital.occupier != null)
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
                if (relationImproveChance < rng.NextSingle())
                {
                    relations.ChangeOpinion(1);
                }
                else
                {
                    if (badOutcomeChance < rng.NextSingle())
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
        State attacker = GetHighestLiege(this);
        State defender = GetHighestLiege(state);
        attacker.EstablishRelations(defender, Relation.minOpinionValue);
        defender.EstablishRelations(attacker, Relation.minOpinionValue);
        if (!attacker.enemies.Contains(defender))
        {
            attacker.enemies.Add(defender);
        }
        if (!defender.enemies.Contains(attacker))
        {
            defender.enemies.Add(attacker);
        }
    }
    public void EndWar(State state)
    {
        State attacker = GetHighestLiege(this);
        State defender = GetHighestLiege(state);

        if (attacker.enemies.Contains(defender))
        {
            attacker.enemies.Add(defender);
        }
        if (defender.enemies.Contains(attacker))
        {
            defender.enemies.Add(attacker);
        }

        foreach (Region region in attacker.regions)
        {
            if (region.occupier == defender)
            {
                region.occupier = null;
            }
        }  
        foreach (Region region in defender.regions)
        {
            if (region.occupier == attacker)
            {
                region.occupier = null;
            }
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