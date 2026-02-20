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
        Tile tile = simManager.tiles[lx, ly];
        return GetRegion(tile.regionId);
    }
    public Region GetRegion(Vector2I pos)
    {
        return GetRegion(pos.X, pos.Y);
    }
    public Market GetMarket(ulong? id)
    {
        try
        {
            return simManager.marketIds[(ulong)id];
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
            id = GetId(),
            linkUpdateCountdown = simManager.rng.Next(0, 13),
            pos = new Vector2I(x,y)
        };
        region.AddTile(simManager.tiles[x,y]);
        region.terrainType = simManager.tiles[x,y].terrainType;

        simManager.regionIds.Add(region.id, region);
        return region;
    }
    public Pop CreatePop(long workforce, long dependents, Region region, Tech tech, Culture culture, SocialClass profession = SocialClass.FARMER)
    {
        simManager.currentBatch++;
        if (simManager.currentBatch > 12)
        {
            simManager.currentBatch = 1;
        }
        Pop pop = new Pop()
        {
            id = GetId(),
            batchId = simManager.currentBatch,
            profession = profession,
            tech = tech,
            workforce = workforce,
            dependents = dependents,
            population = workforce + dependents,
            shipborne = region.isWater
        };
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
        if (pop.region != null && pop.region.owner != null && pop.region.owner.rulingPop == pop)
        {
            lock (pop.region.owner)
            {
                pop.region.owner.rulingPop = null;
            }
        }

        pop.region.RemovePop(pop, pop.region);
        pop.culture.RemovePop(pop, pop.culture);
        simManager.popsIds.Remove(pop.id);
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
            id = GetId(),
            name = NameGenerator.GenerateCultureName(),
            color = new Color(r, g, b),
            tickCreated = timeManager.ticks,
        };

        simManager.cultureIds.Add(culture.id, culture);
        return culture;
    }
    public void DeleteCulture(Culture culture)
    {
        foreach (Pop pop in culture.pops)
        {
            culture.RemovePop(pop, culture);
        }
        simManager.cultureIds.Remove(culture.id);
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
                id = GetId(),
                baseName = NameGenerator.GenerateNationName(),
                color = new Color(r, g, b),
                capital = region,
                tickCreated = timeManager.ticks,
            };
            state.diplomacy = new StateDiplomacyManager(state);
            state.vassalManager = new StateVassalManager(state);
            state.AddRegion(region);
            simManager.statesIds.Add(state.id, state);
        }
    }
    public void DeleteState(State deletedState)
    {
        if (simManager.mapManager.selectedMetaObj == deletedState)
        {
            simManager.mapManager.SelectMetaObject(null);
        }
        if (deletedState.vassalManager.liegeId != null)
        {
            State liege = GetState(deletedState.vassalManager.liegeId);
            liege.vassalManager.RemoveVassal(deletedState.id);
        }
        foreach (ulong warId in deletedState.diplomacy.warIds.Keys)
        {
            War war = GetWar(warId);
            war.RemoveParticipant(deletedState.id);
        }
        foreach (ulong vassalId in deletedState.vassalManager.vassalIds.ToArray())
        {
            deletedState.vassalManager.RemoveVassal(vassalId);
        }
        foreach (Region region in deletedState.regions.ToArray())
        {
            deletedState.RemoveRegion(region);
        }
        foreach (ulong characterId in deletedState.characterIds.ToArray())
        {
            GetCharacter(characterId).LeaveState();
        }
        foreach (ulong relationId in simManager.statesIds.Keys)
        {
            State relation = GetState(relationId);
            relation.diplomacy.RemoveRelations(deletedState.id);
            relation.borderingStateIds.Remove(deletedState.id);
        }
        foreach (ulong allianceId in deletedState.allianceIds.ToArray())
        {
            Alliance alliance = GetAlliance(allianceId);
            alliance.RemoveMember(deletedState.id);
        }

        simManager.objectDeleted.Invoke(deletedState.id);
        //simManager.deletedStateIds.Add(deletedState.id);
        simManager.statesIds.Remove(deletedState.id);
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
            if (simManager.characterIds.ContainsKey((ulong)id))
            {
                return simManager.characterIds[(ulong)id];
            }
            return null;
        }
    }
    public Character GetCharacter(ulong id)
    {
        if (simManager.characterIds.ContainsKey(id))
        {
            return simManager.characterIds[id];
        }
        return null;
    }
    public Character CreateCharacter(string firstName, string lastName, uint age, State state, CharacterRole role)
    {
        Character character = new Character()
        {
            // Gives character id
            id = GetId(),

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
        simManager.characterIds.Add(character.id, character);
        return character;
    }
    public void DeleteCharacter(Character character)
    {

        character.LeaveState();
        // Removes reference to character from it children
        foreach (ulong charId in character.childIds)
        {
            Character child = simManager.characterIds[charId];
            child.parentIds.Remove(charId);
        }
        // And removes reference as child from parent
        foreach (ulong? parentId in character.parentIds)
        {
            Character parent = GetCharacter(parentId);
            parent.childIds.Remove(character.id);
        }
        simManager.objectDeleted.Invoke(character.id);
        simManager.characterIds.Remove(character.id);
    }
    public Alliance CreateAlliance(State founder, AllianceType type)
    {
        Alliance alliance = new Alliance()
        {
            id = GetId(),
            tickCreated = timeManager.ticks,
            type = type
        };
        alliance.AddMember(founder.id);
        alliance.SetLeader(founder.id);

        simManager.allianceIds.Add(alliance.id, alliance);
        return alliance;
    }
    public void DeleteAlliance(Alliance alliance)
    {
        foreach (ulong memberId in alliance.memberStateIds.ToArray())
        {
            alliance.RemoveMember(memberId);
        }
        simManager.objectDeleted.Invoke(alliance.id);
        simManager.allianceIds.Remove(alliance.id);
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
    public Market CreateTradeZone(Region region)
    {
        Market zone = new Market()
        {
            id = GetId(),
            color = new Color(simManager.rng.NextSingle(), simManager.rng.NextSingle(), simManager.rng.NextSingle()),
            centerId = region.id,
            name = region.name + " Market"
        };

        zone.AddRegion(region);

        zone.regionIds.Add(region.id);
        simManager.marketIds.Add(zone.id, zone);
        return zone;
    }
    public void DeleteTradeZone(Market tradeZone )
    {
        if (tradeZone == null) return;
        foreach (ulong regionId in tradeZone.regionIds.ToArray())
        {
            Region region = GetRegion(regionId);
            tradeZone.RemoveRegion(region);
        }
        simManager.objectDeleted.Invoke(tradeZone.id);
        simManager.marketIds.Remove(tradeZone.id);     
    }
    public War StartWar(List<State> atk, List<State> def, WarType warType, ulong agressorLeader, ulong defenderLeader)
    {
        if (agressorLeader == defenderLeader || GetState(agressorLeader) == null || GetState(defenderLeader) == null)
        {
            return null;
        }
        War war = new War()
        {
            id = GetId(),
            warType = warType,
            primaryAgressorId = agressorLeader,
            primaryDefenderId = defenderLeader,
            tickCreated = timeManager.ticks,
        };
        CreateHistoricalEvent([GetState(agressorLeader), GetState(defenderLeader)], EventType.WAR_DECLARATION);
        war.InitWarLead(true);
        war.InitWarLead(false);
        war.NameWar();

        simManager.warIds.Add(war.id, war);
        return war;
    }
    public void EndWar(War war)
    {
        try
        {
            CreateHistoricalEvent([GetState(war.primaryAgressorId), GetState(war.primaryDefenderId)], EventType.WAR_END);
            war.dead = true;
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
            objIds = relevantObjects.Select(obj => obj == null ? "nullObject" : obj.GetFullId()).ToList(),
            tickOccured = timeManager.ticks,
            id = GetId(),
            type = eventType
        };
        historicalEvent.InitEvent();
        foreach (NamedObject obj in relevantObjects)
        {
            if (obj == null) continue;
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
    ulong GetId()
    {
        lock (this)
        {
            currentId++;
            if (currentId == ulong.MaxValue)
            {
                currentId = 1;
            }
            return currentId;            
        }
    }
}