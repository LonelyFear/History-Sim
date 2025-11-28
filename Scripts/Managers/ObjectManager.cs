using System;
using System.Collections.Generic;
using System.Data.Common;
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
    [Key(0)] public ulong currentId = 20;
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
    public Culture CreateCulture()
    {
        float r = simManager.rng.NextSingle();
        float g = simManager.rng.NextSingle();
        float b = simManager.rng.NextSingle();
        Culture culture = new Culture()
        {
            id = getID(),
            name = NameGenerator.GenerateCultureName(),
            color = new Color(r, g, b),
            tickCreated = timeManager.ticks,
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
                baseName = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b),
                capital = region,
                tickCreated = timeManager.ticks,
            };
            state.diplomacy = new StateDiplomacyManager(state);
            state.vassalManager = new StateVassalManager(state);
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
        if (state.vassalManager.liegeId != null)
        {
            State liege = GetState(state.vassalManager.liegeId);
            liege.vassalManager.RemoveVassal(state.id);
        }
        foreach (ulong warId in state.diplomacy.warIds.Keys)
        {
            War war = GetWar(warId);
            war.RemoveParticipant(state.id);
        }
        foreach (ulong vassalId in state.vassalManager.vassalIds.ToArray())
        {
            State vassal = GetState(vassalId);
            state.vassalManager.RemoveVassal(vassalId);
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
        if (id == null)
        {
            return null;
        }
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
            tickCreated = timeManager.ticks - age,
        };
        // Adds character to state and gives it role
        character.name = $"{character.firstName} {character.lastName}";
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
    public Alliance CreateAlliance(State founder, AllianceType type)
    {
        Alliance alliance = new Alliance()
        {
            id = getID(),
            tickCreated = timeManager.ticks,
            type = type
        };
        alliance.AddMember(founder.id);
        alliance.SetLeader(founder.id);

        simManager.allianceIds.Add(alliance.id, alliance);
        simManager.alliances.Add(alliance);
        return alliance;
    }
    public void DeleteAlliance(Alliance alliance)
    {
        foreach (ulong memberId in alliance.memberStateIds)
        {
            alliance.RemoveMember(memberId);
        }
        simManager.allianceIds.Remove(alliance.id);
        simManager.alliances.Remove(alliance);
    }
    public Alliance GetAlliance(ulong? id)
    {
        try
        {
            return simManager.allianceIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }    
    }
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
            tickCreated = timeManager.ticks,
        };

        war.InitWarLead(true);
        war.InitWarLead(false);
        war.NameWar();

        simManager.wars.Add(war);
        simManager.warIds.Add(war.id, war);
        return war;
    }
    public void EndWar(War war)
    {
        try
        {
            simManager.wars.Remove(war);
            simManager.warIds.Remove(war.id);
            foreach (ulong stateId in war.participantIds.ToArray())
            {
                war.RemoveParticipant(stateId);           
            }            
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    public void CreateHistoricalEvent(NamedObject[] relevantObjects, EventType eventType)
    {
        HistoricalEvent historicalEvent = new HistoricalEvent()
        {
            objIds = relevantObjects.Select(obj => obj.GetFullId()).ToList(),
            tickOccured = timeManager.ticks,
            id = getID(),
            
        };
        historicalEvent.eventText = historicalEvent.GetEventText();
        foreach (NamedObject obj in relevantObjects)
        {
            obj.eventIds.Add(historicalEvent.id);
        }
        simManager.historicalEventIds.Add(historicalEvent.id, historicalEvent);
    }
    public void DeleteHistoricalEvent(HistoricalEvent historicalEvent)
    {
        foreach (string fullId in historicalEvent.objIds)
        {
            NamedObject obj = NamedObject.GetNamedObject(fullId);
            obj.eventIds.Remove(historicalEvent.id);
        }  
        simManager.historicalEventIds.Remove(historicalEvent.id);      
    }
    public HistoricalEvent GetHistoricalEvent(ulong? id)
    {
        if (id == null)
        {
            return null;
        }
        try {
            return simManager.historicalEventIds[(ulong)id];
        } catch {
            //GD.PushWarning(e);
            return null;
        }        
    }
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