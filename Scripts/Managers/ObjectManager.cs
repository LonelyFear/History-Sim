using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject]
public class ObjectManager
{
    [IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static TimeManager timeManager;
    [Key(0)] public ulong currentId = 0;
    #region Object Getting
    public Region GetRegion(ulong? id)
    {
        try
        {
            return simManager.regionIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }
    }
    public Region GetRegion(int x, int y)
    {
        int lx = Mathf.PosMod(x, SimManager.worldSize.X);
        int ly = Mathf.PosMod(y, SimManager.worldSize.Y);

        int index = (lx * SimManager.worldSize.Y) + ly;
        return simManager.regions[index];
    }
    public Region GetRegion(Vector2I pos)
    {
        int lx = Mathf.PosMod(pos.X, SimManager.worldSize.X);
        int ly = Mathf.PosMod(pos.Y, SimManager.worldSize.Y);

        int index = (lx * SimManager.worldSize.Y) + ly;
        return simManager.regions[index];
    }
    public TradeZone GetTradeZone(ulong? id)
    {
        try
        {
            return simManager.tradeZonesIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }
    }
    public War GetWar(ulong? id)
    {
        try
        {
            return simManager.warIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }
    }
    public Region CreateRegion(int x, int y)
    {
        Region region = new Region()
        {
            id = getID(),
            pos = new Vector2I(x, y),
            tiles = new Tile[simManager.tilesPerRegion, simManager.tilesPerRegion],
            biomes = new Biome[simManager.tilesPerRegion, simManager.tilesPerRegion]
        };
        simManager.regions.Add(region);
        simManager.regionIds.Add(region.id, region);
        return region;
    }
    #region Pops Creation
    public Pop CreatePop(long workforce, long dependents, Region region, Tech tech, Culture culture, SocialClass profession = SocialClass.FARMER)
    {
        simManager.currentBatch++;
        if (simManager.currentBatch > 12)
        {
            simManager.currentBatch = 2;
        }
        Pop pop = new Pop()
        {
            id = getID(),
            batchId = simManager.currentBatch,
            profession = profession,
            Tech = tech,
            workforce = workforce,
            dependents = dependents,
            population = workforce + dependents,
        };
        //pop.ChangePopulation(workforce, dependents);
        lock (simManager.pops)
        {
            simManager.pops.Add(pop);
        }
        lock (simManager.popsIds)
        {
            simManager.popsIds.Add(pop.id, pop);
        }
        lock (culture)
        {
            culture.AddPop(pop, culture);
        }
        lock (region)
        {
            region.AddPop(pop, region);
        }

        return pop;
    }

    public void DestroyPop(Pop pop)
    {
        try
        {
            if (pop.region != null && pop.region.owner != null && pop.region.owner.rulingPop == pop)
            {
                lock (pop.region.owner)
                {
                    pop.region.owner.rulingPop = null;
                }
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }

        pop.ClaimLand(-pop.ownedLand);
        lock (pop.region)
        {
            pop.region.RemovePop(pop, pop.region);
        }
        lock (pop.culture)
        {
            pop.culture.RemovePop(pop, pop.culture);
        }
        lock (simManager.pops)
        {
            simManager.pops.Remove(pop);
        }
        lock (simManager.popsIds)
        {
            simManager.popsIds.Remove(pop.id);
        }
    }
    public Pop GetPop(ulong? id)
    {
        try
        {
            return simManager.popsIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }
    }
    #endregion
    #region Cultures Creation
    public Culture CreateCulture()
    {
        float r = simManager.rng.NextSingle();
        float g = simManager.rng.NextSingle();
        float b = simManager.rng.NextSingle();
        Culture culture = new Culture()
        {
            id = getID(),
            name = "Culture",
            color = new Color(r, g, b),
            tickFounded = timeManager.ticks
        };

        simManager.cultures.Add(culture);
        simManager.cultureIds.Add(culture.id, culture);
        return culture;
    }
    public Culture GetCulture(ulong? id)
    {
        try {
            return simManager.cultureIds[(ulong)id];
        } catch {
            //GD.PushWarning(e);
            return null;
        }        
    }
    #endregion
    #region States Creation
    public void CreateState(Region region)
    {
        if (region.owner == null)
        {
            float r = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
            float g = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
            float b = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
            State state = new State()
            {
                id = getID(),
                name = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b),
                capital = region,
                tickFounded = timeManager.ticks,
            };
            state.diplomacy = new DiplomacyManager(state);
            state.AddRegion(region);
            simManager.states.Add(state);
            simManager.statesIds.Add(state.id, state);
        }
    }
    public void DeleteState(State state)
    {
        if (simManager.mapManager.selectedMetaObj == state)
        {
            simManager.mapManager.selectedMetaObj = null;
            simManager.mapManager.UpdateRegionColors(simManager.regions);
        }
        if (state.liege != null)
        {
            state.liege.RemoveVassal(state);
        }
        foreach (ulong warId in state.diplomacy.warIds.Keys)
        {
            War war = GetWar(warId);
            war.RemoveParticipant(state.id);
        }
        foreach (State vassal in state.vassals.ToArray())
        {
            state.RemoveVassal(vassal);
        }
        foreach (Region region in state.regions.ToArray())
        {
            state.RemoveRegion(region);
        }
        foreach (ulong characterId in state.characterIds.ToArray())
        {
            GetCharacter(characterId).LeaveState();
        }
        foreach (ulong relationId in simManager.statesIds.Keys)
        {
            State relation = GetState(relationId);
            relation.diplomacy.RemoveRelations(state.id);
        }

        simManager.objectDeleted.Invoke(state.id);
        simManager.deletedStateIds.Add(state.id);
        simManager.states.Remove(state);
        simManager.statesIds.Remove(state.id);
    }
    public State GetState(ulong? id)
    {
        try {
            return simManager.statesIds[(ulong)id];
        } catch {
            if (simManager.deletedStateIds.Contains((ulong)id))
            {
                GD.PushError("Deleted state still referenced!");
            }
            //GD.PushWarning(e);
            return null;
        }
    }
    #endregion
    #region Characters Creation
    public Character GetCharacter(ulong? id)
    {
        if (id == null)
        {
            return null;
        }
        else
        {
            if (simManager.charactersIds.ContainsKey((ulong)id))
            {
                return simManager.charactersIds[(ulong)id];
            }
            return null;
        }
    }
    public Character GetCharacter(ulong id)
    {
        if (simManager.charactersIds.ContainsKey(id))
        {
            return simManager.charactersIds[id];
        }
        return null;
    }
    public Character CreateCharacter(string firstName, string lastName, uint age, State state, CharacterRole role)
    {
        Character character = new Character()
        {
            // Gives character id
            id = getID(),

            // Names character
            firstName = firstName,
            lastName = lastName,

            age = age,
            birthTick = timeManager.ticks - age,
        };
        // Adds character to state and gives it role
        character.JoinState(state.id);
        character.SetRole(role);
        // Documents character
        simManager.characters.Add(character);
        simManager.charactersIds.Add(character.id, character);
        return character;
    }
    public void DeleteCharacter(Character character)
    {

        character.LeaveState();
        // Removes reference to character from it children
        foreach (ulong charId in character.childIds)
        {
            Character child = simManager.charactersIds[charId];
            child.parentIds.Remove(charId);
        }
        // And removes reference as child from parent
        foreach (ulong? parentId in character.parentIds)
        {
            Character parent = GetCharacter(parentId);
            parent.childIds.Remove(character.id);
        }
        simManager.objectDeleted.Invoke(character.id);
        simManager.characters.Remove(character);
        simManager.charactersIds.Remove(character.id);
    }
    #endregion
    #region Alliances
    public Alliance CreateAlliance(State founder, AllianceType type)
    {
        Alliance alliance = new Alliance();
        return alliance;
    }
    #endregion
    #region Trade Zones
    public TradeZone CreateTradeZone(Region region)
    {
        TradeZone zone = new TradeZone()
        {
            id = getID(),
            color = new Color(simManager.rng.NextSingle(), simManager.rng.NextSingle(), simManager.rng.NextSingle()),
            CoT = region,
            regions = [region],
        };

        simManager.tradeZones.Add(zone);
        simManager.tradeZonesIds.Add(zone.id, zone);
        return zone;
    }
    public void DeleteTradeZone(TradeZone tradeZone )
    {
        if (tradeZone == null) return;
        foreach (Region region in tradeZone.regions.ToArray())
        {
            tradeZone.RemoveRegion(region);
        }
        simManager.tradeZones.Remove(tradeZone);
        simManager.tradeZonesIds.Remove(tradeZone.id);     
    }
    #endregion
    #region Wars
    public War StartWar(List<State> atk, List<State> def, WarType warType, ulong agressorLeader, ulong defenderLeader)
    {
        if (agressorLeader == defenderLeader || GetState(agressorLeader) == null || GetState(defenderLeader) == null)
        {
            return null;
        }
        War war = new War()
        {
            id = getID(),
            warType = warType,
            primaryAgressorId = agressorLeader,
            primaryDefenderId = defenderLeader,
            attackerIds = atk.Select(attacker => attacker.id).ToList(),
            defenderIds = def.Select(defender => defender.id).ToList(),
        };

        war.InitWarLead(true);
        war.InitWarLead(false);
        war.InitEnemies(true);
        war.InitEnemies(false);
        war.NameWar();

        simManager.wars.Add(war);
        simManager.warIds.Add(war.id, war);
        return war;
    }
    public void EndWar(War war)
    {
        simManager.wars.Remove(war);
        simManager.warIds.Remove(war.id);
        foreach (ulong stateId in war.participantIds)
        {
            State state = GetState(stateId);
            state.diplomacy.warIds.Remove(war.id);
        }
    }
    #endregion
    #endregion
    ulong getID()
    {
        currentId++;
        if (currentId == ulong.MaxValue)
        {
            currentId = 1;
        }
        return currentId;
    }
}