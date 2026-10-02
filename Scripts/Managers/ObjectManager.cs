using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PixelHistory.Objects.States.AI;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;
using PixelHistory.Objects.Wars;
public static class ObjectManager
{
    public static SelectionManager selectionManager;
    public static SimManager simManager;
    public static TimeManager timeManager;
    public static readonly object locker = new();
    
    public static Region GetRegion(ulong? id)
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
    public static Region GetRegion(int x, int y)
    {
        int lx = Mathf.PosMod(x, SimManager.worldSize.X);
        int ly = Mathf.PosMod(y, SimManager.worldSize.Y);
        Tile tile = simManager.tiles[lx, ly];
        return GetRegion(tile.regionId);
    }
    public static Region GetRegion(Vector2I pos)
    {
        return GetRegion(pos.X, pos.Y);
    }
    public static TradeZone GetTradeZone(ulong? id)
    {
        try
        {
            return simManager.tradeZoneIds[(ulong)id];
        }
        catch
        {
            //GD.PushWarning(e);
            return null;
        }
    }
    public static War GetWar(ulong? id)
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
    public static Region CreateRegion(int x, int y)
    {
        Region region = new Region()
        {
            id = GetId(),
            linkUpdateCountdown = simManager.rng.Next(0, 13),
            centerPos = new Vector2I(x,y),
            gridPos = new Vector2I(x/RegionGenerator.tilesPerRegion,y/RegionGenerator.tilesPerRegion)
        };
        region.AddTile(simManager.tiles[x,y]);
        region.terrainType = simManager.tiles[x,y].terrainType;

        simManager.regionIds.Add(region.id, region);
        return region;
    }
    public static Pop CreatePop(int workforce, int dependents, Region region, Tech tech, Culture culture, string socialClass)
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
            socialClass = AssetManager.GetSocialClass(socialClass),
            tech = tech,
            workforce = workforce,
            dependents = dependents,
            population = workforce + dependents,
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

    public static void DestroyPop(Pop pop)
    {
        if (pop.region != null && pop.region.owner != null && pop.region.owner.rulingPop == pop)
        {
            lock (pop.region.owner)
            {
                pop.region.owner.rulingPop = null;
            }
            

        }
        pop.region?.RemovePop(pop, pop.region);
        pop.culture?.RemovePop(pop, pop.culture);
        simManager.popsIds.Remove(pop.id);
    }
    public static Pop GetPop(ulong? id)
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
    public static Culture CreateCulture()
    {
        float r = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
        float g = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
        float b = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
        Culture culture = new Culture()
        {
            id = GetId(),
            name = NameGenerator.GenerateCultureName(simManager.rng),
            color = new Color(r, g, b),
            tickCreated = timeManager.ticks,
        };

        simManager.cultureIds.Add(culture.id, culture);
        return culture;
    }
    public static void DeleteCulture(Culture culture)
    {
        foreach (Pop pop in culture.pops)
        {
            culture.RemovePop(pop, culture);
        }
        simManager.cultureIds.Remove(culture.id);
    }
    public static Culture GetCulture(ulong? id)
    {
        try {
            return simManager.cultureIds[(ulong)id];
        } catch {
            //GD.PushWarning(e);
            return null;
        }        
    }
    public static void CreateState(Region region)
    {
        try
        {
            if (region.owner == null)
            {
                float r = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
                float g = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
                float b = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
                string name = NameGenerator.GenerateNationName(simManager.rng);

                if (name.Equals("koria", StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }

                State state = new()
                {
                    id = GetId(),
                    baseName = name,
                    color = new Color(r, g, b),
                    capital = region,
                    tickCreated = timeManager.ticks,
                };
                state.name = NameGenerator.GetStateName(state);
                state.AddRegion(region, true);
                simManager.statesIds.Add(state.id, state);      
                    
                state.AIManager = new StateAIManager(state);
            }            
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    public static void DeleteState(State deletedState)
    {
        if (selectionManager.GetSelectedState() == deletedState)
        {
            selectionManager.SelectRegion(null);
        }
        deletedState.GetLiege()?.RemoveVassal(deletedState);
        deletedState.LeaveAllWars();
        deletedState.RemoveAllVassals();
        foreach (State state in simManager.statesIds.Values)
        {
            state.relations.Remove(deletedState);
            state.borderingStates.Remove(deletedState);
        }    
        foreach (ulong characterId in deletedState.characterIds.ToArray())
        {
            GetCharacter(characterId).LeaveState();
        }
        foreach (Alliance alliance in deletedState.alliances.ToArray())
        {
            alliance.RemoveMember(deletedState);
        }      
        foreach (Region region in deletedState.regions.ToArray())
        {
            if (region.claimant != region.owner)
            {
                region.claimant.AddRegion(region, true);
            }
            else deletedState.RemoveRegion(region);
        }        
        foreach (Region claim in deletedState.claims.ToArray())
        {
            deletedState.RemoveClaim(claim);
        }      
        simManager.objectDeleted.Invoke(deletedState.id);
        simManager.statesIds.Remove(deletedState.id);
    }
    public static State GetState(ulong? id)
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
    public static Character GetCharacter(ulong? id)
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
    public static Character GetCharacter(ulong id)
    {
        if (simManager.characterIds.ContainsKey(id))
        {
            return simManager.characterIds[id];
        }
        return null;
    }
    public static Character CreateCharacter(string firstName, string lastName, uint age, State state, CharacterRole role)
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
        foreach (string trait in character.personality.Keys)
        {
            character.personality[trait] = Utility.RandomRange(0, 1, simManager.rng);
        }
        // Adds character to state and gives it role
        character.name = $"{character.firstName} {character.lastName}";
        character.JoinState(state);
        character.SetRole(role);
        // Documents character
        if (simManager.characterIds.TryAdd(character.id, character))
        {
            return character;
        }
        return null;
    }
    public static void DeleteCharacter(Character character)
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
        simManager.characterIds.Remove(character.id, out _);
    }
    public static Alliance CreateAlliance(State founder, AllianceType type = AllianceType.ALLIANCE, bool exclusive = true)
    {
        Alliance alliance = new Alliance()
        {
            id = GetId(),
            tickCreated = timeManager.ticks,
            type = type,
            exclusive = exclusive
        };

        alliance.SetLeader(founder);
        alliance.AddMember(founder);

        simManager.allianceIds.Add(alliance.id, alliance);
        return alliance;
    }
    public static void DeleteAlliance(Alliance alliance)
    {
        foreach (State member in alliance.memberStates)
        {
            alliance.RemoveMember(member);
        }
        simManager.objectDeleted.Invoke(alliance.id);
        simManager.allianceIds.Remove(alliance.id);
    }
    public static Alliance GetAlliance(ulong? id)
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
    public static void EstablishRelations(State initiator, State target)
    {
        try
        {
            if (initiator.relations.ContainsKey(target)) return;
            
            DiplomaticRelations relations = new()
            {
                id = GetId(),
                initiator = initiator,
                recipient = target
            };
            initiator.relations[target] = relations;
            target.relations[initiator] = relations;

            simManager.relationIds.Add(relations.id, relations);            
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }
    public static void BreakRelations(DiplomaticRelations relations)
    {
        State initiator = relations.initiator;
        State target = relations.recipient;

        initiator.relations.Remove(target);
        target.relations.Remove(initiator);
        
        simManager.relationIds.Remove(relations.id);
    }
    public static TradeZone CreateTradeZone(Region region)
    {
        TradeZone zone = new TradeZone()
        {
            id = GetId(),
            color = new Color(simManager.rng.NextSingle(), simManager.rng.NextSingle(), simManager.rng.NextSingle()),
            centerId = region.id,
            name = region.name + " Market"
        };
        zone.AddRegion(region);
        simManager.tradeZoneIds.Add(zone.id, zone);
        return zone;
    }
    public static void DeleteTradeZone(TradeZone tradeZone)
    {
        if (tradeZone == null) return;
        foreach (Region region in tradeZone.regions.ToArray())
        {
            tradeZone.RemoveRegion(region);
        }
        simManager.objectDeleted.Invoke(tradeZone.id);
        simManager.tradeZoneIds.Remove(tradeZone.id);     
    }
    public static War StartWar(WarType warType, State agressorLeader, State defenderLeader)
    {
        if (agressorLeader == defenderLeader || agressorLeader == null || defenderLeader == null)
        {
            return null;
        }
        War war = new()
        {
            id = GetId(),
            warType = warType,
            tickCreated = timeManager.ticks,
        };
        war.warLeaderIds[War.WarSide.AGRESSOR] = agressorLeader.id;
        war.warLeaderIds[War.WarSide.DEFENDER] = defenderLeader.id;
        war.InitWar();
        
        war.AddParticipant(agressorLeader, War.WarSide.AGRESSOR);
        war.AddParticipant(defenderLeader, War.WarSide.DEFENDER);
        war.NameWar();

        simManager.warIds.TryAdd(war.id, war);
        return war;
    }
    public static void ForgetWar(War war)
    {
        if (!war.dead) war.EndWar();
        simManager.warIds.Remove(war.id, out _);          
    }
    public static Ocean CreateOcean(Region[] waterRegions)
    {
        float r = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
        float g = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
        float b = Mathf.Lerp(0.2f, 1f, simManager.rng.NextSingle());
        Ocean ocean = new Ocean()
        {
            id = GetId(),
            color = new Color(r, g, b),
            waterRegions = [..waterRegions]
        };
        foreach (Region region in waterRegions)
        {
            region.ocean = ocean;
        }
        simManager.oceanIds.Add(ocean.id, ocean);
        return ocean;
    }
    public static HistoricalEvent GetHistoricalEvent(ulong? id)
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
    static ulong GetId()
    {
        lock (locker)
        {
            simManager.currentId++;
            if (simManager.currentId == ulong.MaxValue)
            {
                simManager.currentId = 1;
            }
            return simManager.currentId;              
        }
    }
}