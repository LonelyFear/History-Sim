using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class State : PopObject
{
    public string displayName { get; set; } = "Nation";
    public Color color { get; set; }
    public Color displayColor;
    public Color capitalColor;
    public bool capitualated = false;

    public GovernmentType government { get; set; } = GovernmentType.MONARCHY;
    [IgnoreMember]
    public List<Region> realmRegions = new List<Region>(); 
    [IgnoreMember]
    public List<Region> regions { get; set; } = new List<Region>();
    public List<ulong> regionsIDs { get; set; }
    [IgnoreMember]
    public Region capital { get; set; }
    public ulong capitalID;

    public long manpower { get; set; } = 0;
    public int occupiedLand { get; set; } = 0;
    public float totalWealth { get; set; } = 0;
    public float mobilizationRate { get; set; } = 0.3f;
    public float poorTaxRate { get; set; } = 0.3f;
    public float middleTaxRate { get; set; } = 0.1f;
    public float richTaxRate { get; set; } = 0.05f;
    public float tributeRate { get; set; } = 0.1f;
    [IgnoreMember]
    public List<State> vassals { get; set; } = new List<State>();
    public List<ulong> vassalsIDs { get; set; }
    [IgnoreMember]
    public State liege { get; set; } = null;
    public ulong liegeID;
    [IgnoreMember]
    public Dictionary<State, Relation> relations { get; set; } = new Dictionary<State, Relation>();
    public Dictionary<ulong, Relation> relationsIDs { get; set; }
    [IgnoreMember]
    public Dictionary<War, bool> wars { get; set; } = new Dictionary<War, bool>();
    public Dictionary<ulong, bool> warsIDs { get; set; }
    [IgnoreMember]
    public List<State> enemies { get; set; } = new List<State>();
    public List<ulong> enemiesIDs { get; set; }
    [IgnoreMember]
    public List<State> borderingStates { get; set; } = new List<State>();
    public List<ulong> borderingStatesIDs { get; set; }
    public int borderingRegions { get; set; } = 0;
    public int externalBorderingRegions { get; set; }= 0;
    public Sovereignty sovereignty { get; set; } = Sovereignty.INDEPENDENT;


    public List<Character> characters = new List<Character>();
    int monthsSinceElection = 0;
    public Tech tech = new Tech();

    public int maxSize = 1;
    public bool bugged;
    public bool buggedTarget;

    // Government
    public long wealth;
    public Character leader;
    public Pop rulingPop;
    public Character heir;
    public double stability = 1;
    public double loyalty = 1;
    [IgnoreMember] public const double minRebellionLoyalty = 0.75;
    [IgnoreMember] public const double minCollapseStability = 0.75;
    public uint timeAsVassal = 0;
    public void PrepareForSave()
    {
        PreparePopObjectForSave();
        capitalID = capital.id;
        regionsIDs = regions.Select(r => r.id).ToList();
        vassalsIDs = vassals.Count > 0 ?vassals.Select(r => r.id).ToList() : null;
        liegeID = liege != null ? liege.id : 0;
        relationsIDs = relations.Count > 0 ? relations.ToDictionary(kv => kv.Key.id, kv => kv.Value) : null;
        warsIDs = wars.Count > 0 ? wars.ToDictionary(kv => kv.Key.id, kv => kv.Value) : null;
        enemiesIDs = enemies.Count > 0 ? enemies.Select(r => r.id).ToList() : null;
        borderingStatesIDs = borderingStates.Count > 0 ? borderingStates.Select(r => r.id).ToList() : null;
    }
    public void LoadFromSave()
    {
        LoadPopObjectFromSave();
        capital = simManager.regionsIds[capitalID];
        regions = regionsIDs.Select(r => simManager.regionsIds[r]).ToList();
        vassals = vassalsIDs == null ? new List<State>() : vassalsIDs.Select(r => simManager.statesIds[r]).ToList();
        liege = liegeID == 0 ? null : simManager.statesIds[liegeID];
        relations = relationsIDs == null ? new Dictionary<State, Relation>() : relationsIDs.ToDictionary(kv => simManager.statesIds[kv.Key], kv => kv.Value);
        wars = warsIDs == null ? new Dictionary<War, bool>() : warsIDs.ToDictionary(kv => simManager.warsIds[kv.Key], kv => kv.Value);
        enemies = enemiesIDs == null ? new List<State>() : enemiesIDs.Select(r => simManager.statesIds[r]).ToList();
        borderingStates = borderingStatesIDs == null ? new List<State>() : borderingStatesIDs.Select(r => simManager.statesIds[r]).ToList();
    }
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
                atWarWith.AddRange(war.defenders);
            }
            else
            {
                atWarWith.AddRange(war.attackers);
            }
        }
        enemies = atWarWith;
    }
    public void RelationsUpdate()
    {
        foreach (var pair in relations)
        {
            if (!borderingStates.Concat(enemies).Contains(pair.Key))
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
        foreach (State state in borderingStates.Concat(enemies))
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
    
    #region Wars
    public void Capitualate()
    {
        if (capital.GetController() != GetHighestLiege())
        {
            if (!capitualated)
            {
                foreach (Region region in regions)
                {
                    if (capital.occupier != this && capital.occupier != null)
                    {
                        region.occupier = capital.occupier;
                    }
                }
            }
            capitualated = true;
        }
        else
        {
            capitualated = false;
        }
    }    
    public void StartWars()
    {
        try
        {
            foreach (var pair in relations.ToArray())
            {
                State state = pair.Key;
                Relation relation = pair.Value;
                bool cantStartWar = state == this && enemies.Contains(state) || relation.truce >= 0 || state.GetHighestLiege() == this || state.sovereignty != Sovereignty.INDEPENDENT;
                if (cantStartWar)
                {
                    continue;
                }
                // Sovereign Wars
                if (state.sovereignty == Sovereignty.INDEPENDENT && relation.opinion < 0 && liege != state)
                {
                    float warDeclarationChance = Mathf.Lerp(0.001f, 0.005f, relation.opinion / (float)Relation.minOpinionValue);
                    if (rng.NextSingle() < warDeclarationChance)
                    {
                        //GD.Print("war");
                        //GD.Print("State in realm: " + GetRealmStates().Contains(this));
                        _ = new War(GetRealmStates(), state.GetRealmStates(), WarType.CONQUEST, this, state);
                        relation.opinion = Relation.minOpinionValue;
                        return;
                    }
                }
                // Rebellions
                if (loyalty < minRebellionLoyalty && state == liege)
                {
                    if (rng.NextSingle() < Mathf.Lerp(loyalty, 0, 0.001))
                    {
                        List<State> fellowRebels = GatherRebels();
                        State formerLiege = liege;
                        foreach (State rebel in fellowRebels)
                        {
                            formerLiege.RemoveVassal(rebel);
                        }
                        //_ = new War(fellowRebels, formerLiege.GetRealmStates(), WarType.REVOLT, this, formerLiege);
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }

    }    
    public void EndWars()
    {
        if (sovereignty != Sovereignty.INDEPENDENT)
        {
            return;
        }
        foreach (var warPair in wars)
        {
            War war = warPair.Key;
            bool isAttacker = warPair.Value;

            if (war.primaryAgressor != this && war.primaryDefender != this)
            {
                continue;
            }
            // Below is if state has authority to end wars
            double warEndChance = 0;
            switch (war.warType)
            {
                case WarType.CONQUEST:
                    try
                    {
                        if (war.primaryAgressor == this)
                        {
                            bugged = !relations.ContainsKey(war.primaryDefender);
                            if (bugged)
                            {
                                //GD.Print(displayName);
                                //GD.Print(war.primaryDefender.displayName);
                                //GD.Print(enemies.Contains(war.primaryDefender));
                                //GD.Print(war.primaryDefender.wars.ContainsKey(war));
                                war.primaryDefender.buggedTarget = true;
                            }
                            else
                            {
                                war.primaryDefender.buggedTarget = false;
                            }
                            warEndChance = Mathf.Max(relations[war.primaryDefender].opinion, 0) * 0.01;
                            // Attacker
                            if (rng.NextDouble() < warEndChance || war.primaryDefender.capitualated)
                            {
                                EndWar(war);
                                foreach (State defender in war.defenders)
                                {
                                    if (!defender.capitualated)
                                    {
                                        return;
                                    }
                                    if (defender.capital.occupier == null)
                                    {
                                        GD.Print(defender.capitualated);
                                        GD.Print(defender.capital.occupier);
                                    }
                                    defender.capital.occupier.AddVassal(defender);
                                }
                            }
                        }
                        else
                        {
                            bugged = !relations.ContainsKey(war.primaryAgressor);
                            if (bugged)
                            {
                                //GD.Print(displayName);
                                //GD.Print(war.primaryAgressor.displayName);
                                //GD.Print(enemies.Contains(war.primaryAgressor));
                                //GD.Print(war.primaryAgressor.wars.ContainsKey(war));
                                war.primaryAgressor.buggedTarget = true;
                            }
                            else
                            {
                                war.primaryAgressor.buggedTarget = false;
                            }
                            warEndChance = Mathf.Max(relations[war.primaryAgressor].opinion, 0) * 0.01;
                            // Defender
                            if (rng.NextDouble() < warEndChance || war.primaryAgressor.capitualated)
                            {
                                EndWar(war);
                                foreach (State attacker in war.attackers)
                                {
                                    if (!attacker.capitualated)
                                    {
                                        return;
                                    }
                                    attacker.capital.occupier.AddVassal(attacker);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        GD.PushError(e);
                    }

                    break;
                case WarType.REVOLT:
                    try
                    {
                        if (isAttacker)
                        {
                            warEndChance = Mathf.Max(relations[war.primaryDefender].opinion, 0) * 0.01;
                            // Rebel Leader
                            if (rng.NextDouble() < warEndChance || war.primaryDefender.capitualated)
                            {
                                EndWar(war);
                            }
                        }
                        else
                        {
                            warEndChance = Mathf.Max(relations[war.primaryAgressor].opinion, 0) * 0.01;
                            // State
                            if (rng.NextDouble() < warEndChance || war.primaryAgressor.capitualated)
                            {
                                EndWar(war);
                                foreach (State rebel in war.attackers)
                                {
                                    AddVassal(rebel);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        GD.PushError(e);
                    }

                    break;
            }
        }
    }    
    public void EndWar(War war)
    {
        war.EndWar();
    }
    public bool StateCollapse()
    {
        if (stability > minCollapseStability)
        {
            return false;
        }
        double stabilityFactor = 1d - (stability / minCollapseStability);
        if (rng.NextDouble() < stabilityFactor * 0.01)
        {
            simManager.DeleteState(this);
            return true;
        }
        return false;
    }
    #endregion
    #endregion
    #region Government
    public void UpdateDisplayName()
    {
        bool useDemonym = true;
        string govtName;
        switch (government)
        {
            case GovernmentType.REPUBLIC:
                switch (sovereignty)
                {
                    case Sovereignty.COLONY:
                        govtName = "Colony";
                        useDemonym = false;
                        break;
                    case Sovereignty.PUPPET:
                        govtName = "Mandate";
                        useDemonym = false;
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
                        useDemonym = false;
                        break;
                    case Sovereignty.PUPPET:
                        govtName = "Protectorate";
                        useDemonym = false;
                        break;
                    case Sovereignty.PROVINCE:
                        govtName = "Duchy";
                        useDemonym = false;
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
                        else
                        {
                            useDemonym = false;
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
                        useDemonym = false;
                        break;
                    case Sovereignty.PROVINCE:
                        govtName = "Province";
                        useDemonym = false;
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
        string demonym = NameGenerator.GetDemonym(name);
        if (useDemonym)
        {
            displayName = $"{demonym} {govtName}";
        }
        else
        {
            displayName = $"{govtName} of {name}";
        }

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
    #region Count Stats
    public void CountStatePopulation()
    {
        realmRegions = GetRealmRegions();
        long countedP = 0;
        long countedW = 0;

        List<Pop> countedPops = new List<Pop>();
        List<State> borders = new List<State>();
        Dictionary<SocialClass, long> countedSocialClasss = new Dictionary<SocialClass, long>();
        foreach (SocialClass profession in Enum.GetValues(typeof(SocialClass)))
        {
            countedSocialClasss.Add(profession, 0);
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
                        if (!borders.Contains(border.owner.GetHighestLiege()))
                        {
                            borders.Add(border.owner.GetHighestLiege());
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

            foreach (SocialClass profession in region.professions.Keys)
            {
                countedSocialClasss[profession] += region.professions[profession];
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
        professions = countedSocialClasss;
        cultures = cCultures;
        population = countedP;
        workforce = countedW;
        pops = countedPops;
    }
    #endregion
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
        return 10 + (tech.societyLevel * 10);
    }
    public void UpdateStability()
    {
        double stabilityTarget = 1;
        stabilityTarget -= wars.Count * 0.05;

        if (largestCulture != GetRulingCulture())
        {
            stabilityTarget -= 0.25;
        }

        stabilityTarget += totalWealth * 0.0001;

        if (rulingPop == null)
        {
            stabilityTarget *= 0.1;
        }
        if (vassals.Count > GetMaxVassals())
        {
            stabilityTarget += (vassals.Count - GetMaxVassals()) * 0.05;
        }

        stabilityTarget = Mathf.Clamp(stabilityTarget, 0, 1);
        stability = Mathf.Lerp(stability, stabilityTarget, 0.05);
    }
    #endregion
    #region Vassals
    public void UpdateLoyalty()
    {
        double loyaltyTarget = 1;
        
        if (largestCulture != liege.GetRulingCulture())
        {
            loyaltyTarget -= 0.1;
        }
        if (largestCulture != liege.largestCulture)
        {
            loyaltyTarget -= 0.25;
        }
        if (regions.Count > liege.regions.Count)
        {
            loyaltyTarget -= ((regions.Count - liege.regions.Count)/liege.regions.Count) * 0.5;
        }
        if (!borderingStates.Contains(liege))
        {
            loyaltyTarget *= 0.5f;
        }
        loyaltyTarget -= liege.tributeRate;
        loyaltyTarget -= Mathf.Min(timeManager.GetYear(timeAsVassal) * 0.001, 0.5);

        loyaltyTarget = Mathf.Clamp(loyaltyTarget, 0, 1);
        loyalty = Mathf.Lerp(loyalty, loyaltyTarget, 0.05);
    }
    public int GetMaxVassals() {
        return 5 + tech.societyLevel;
    } 
    public List<State> GatherRebels()
    {
        List<State> rebels = [this];
        foreach (State vassal in liege.vassals)
        {
            if (vassal.loyalty > minRebellionLoyalty)
            {
                continue;
            }
            double joinChance = (1 - vassal.loyalty) * 0.5;
            if (vassal.GetRulingCulture() != GetRulingCulture())
            {
                joinChance -= 0.2;
            }
            if (rng.NextDouble() < joinChance && !rebels.Contains(vassal))
            {
                rebels.Add(vassal);
            }
        }
        return rebels;
    }

    public void SetStateSovereignty(State state, Sovereignty sovereignty)
    {
        if (state != this)
        {
            if (sovereignty == Sovereignty.INDEPENDENT)
            {
                RemoveVassal(state);
            }
            else
            {
                if (vassals.Contains(state))
                {
                    if (sovereignty < state.sovereignty)
                    {
                        state.loyalty -= 15;
                    }
                    else
                    {
                        state.loyalty += 10;
                    }
                    state.sovereignty = sovereignty;
                    
                }
            }
        }
    }

    public void AddVassal(State state, Sovereignty sovereignty = Sovereignty.PUPPET)
    {
        foreach (State vassal in state.vassals.ToArray())
        {
            state.RemoveVassal(vassal);
        }
        if (state.liege != null)
        {
            state.liege.RemoveVassal(state);
        }
        foreach (War war in state.wars.Keys)
        {
            war.RemoveParticipant(state);
        }
        state.liege = this;
        state.sovereignty = sovereignty;
        vassals.Add(state);
        state.timeAsVassal = 0;
        state.loyalty = 1;
    }

    public void RemoveVassal(State state)
    {
        if (vassals.Contains(state))
        {
            state.liege = null;
            state.sovereignty = Sovereignty.INDEPENDENT;
            vassals.Remove(state);
            state.timeAsVassal = 0;
            state.loyalty = 1;
        }
    }
    #endregion
    #region Military
    public long GetArmyPower(bool realm = true)
    {
        float interiorArmyPower = GetRealmManpower() / (float)realmRegions.Count;
        if (!realm)
        {
            interiorArmyPower = manpower / (float)regions.Count;
        }
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
        if (vassals.Count > 0)
        {
            foreach (State state in vassals)
            {
                realmRegions.AddRange(state.GetRealmRegions());
            }            
        }
        return realmRegions;
    }
    #endregion
    #region Utility
    public Culture GetRulingCulture()
    {
        if (rulingPop != null)
        {
            return rulingPop.culture;
        }
        return null;
    }
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
    public List<State> GetRealmStates()
    {
        State state = GetHighestLiege();
        List<State> collectedVassals = [state];
        foreach (State vassal in state.vassals)
        {
            if (collectedVassals.Contains(vassal))
            {
                continue;
            }
            collectedVassals.Add(vassal);
            if (vassal.vassals.Count() > 0)
            {
                foreach (State vassalVassal in vassal.vassals)
                {
                    if (collectedVassals.Contains(vassal))
                    {
                        continue;
                    }
                    collectedVassals.Add(vassalVassal);
                }
            }
        }
        return collectedVassals;
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
    #endregion
}   

public enum GovernmentType {
    REPUBLIC,
    MONARCHY,
    AUTOCRACY,
}

public enum Sovereignty
{
    INDEPENDENT = 4,
    REBELLIOUS = 3,
    PUPPET = 2,
    COLONY = 1,
    PROVINCE = 0
}

public enum WarType
{
    CONQUEST,
    CIVIL_WAR,
    REVOLT
}