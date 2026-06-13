using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
using MessagePack.Formatters;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

namespace PixelHistory.Objects.Wars;
[MessagePackObject]
public partial class War : NamedObject
{
    [Key(7)] public Dictionary<WarSide, List<ulong>> sideIds = [];
    [Key(8)] public HashSet<ulong?> participantIds {get; private set;} = [];
    [Key(10)] public Dictionary<WarSide, ulong> warLeaderIds = [];
    [Key(11)] public WarType warType { get; set; } = WarType.CONQUEST;
    public War() {}
    public void InitWar()
    {
        sideIds[WarSide.AGRESSOR] = [];
        sideIds[WarSide.DEFENDER] = [];
    }
    public void NameWar()
    {
        State agressor = objectManager.GetState(warLeaderIds[WarSide.AGRESSOR]);
        State defender = objectManager.GetState(warLeaderIds[WarSide.DEFENDER]);
        switch (warType)
        {
            case WarType.CONQUEST:
                name = $"{agressor.baseName}-{defender.baseName} War";
                break;
            case WarType.CIVIL_WAR:
                name = $"{NameGenerator.GetDemonym(defender.baseName)} Civil War";
                break;
            case WarType.REVOLT:
                name = $"{NameGenerator.GetDemonym(agressor.baseName)} Rebellion";
                break;
        }
    }
    public static WarSide GetOtherSide(WarSide side)
    {
        return (WarSide)Mathf.PosMod((int)side + 1, 2);
    }

    public void AddParticipant(State state, WarSide side)
    {
        if (participantIds.Contains(state.id)) RemoveParticipant(state);

        WarSide opposingSide = (WarSide)Mathf.PosMod((int)side + 1, 2);

        sideIds[side].Add(state.id);
        StateDiplomacyManager.SetEnemies(state, sideIds[opposingSide], true);

        foreach (ulong enemyId in sideIds[opposingSide])
        {
            State enemy = objectManager.GetState(enemyId);
            StateDiplomacyManager.SetEnemy(enemy, state, true);
        }
 
        state.wars[this] = side;
        participantIds.Add(state.id);
    }

    public void RemoveParticipant(State state)
    {
        // Gets the side this state is on
        WarSide side = state.wars[this];  
        // Checks if we can end the war
        if (!dead && (sideIds[side].Count == 1 || warLeaderIds[side] == state.id))
        {
            EndWar();
            return;
        }
        // Removes us from participants list if the war isnt going to end
        // (Claim Transfer)
        if (!dead) participantIds.Remove(state.id);

        // Removes enemies and sided participation
        if (sideIds[side].Remove(state.id))
        {
            // Gets opposition
            WarSide opposingSide = (WarSide)Mathf.PosMod((int)side + 1, 2);
            StateDiplomacyManager.SetEnemies(state, sideIds[opposingSide], false);

            // Makes it so our (former) opposition wont fight us
            foreach (ulong enemyId in sideIds[opposingSide])
            {
                State enemy = objectManager.GetState(enemyId);
                StateDiplomacyManager.SetEnemy(enemy, state, false);
            }
        }
        
        // Removes from participants list
        state.wars.Remove(this, out _);

        // Claims
        foreach (Region region in state.regions)
        {
            if (participantIds.Contains(region.owner.GetOverlord().id)) {
                region.owner.AddClaim(region);
            }
        }  
    }
    public int GetSideArmyPower(WarSide side)
    {
        int power = 0;
        foreach (ulong parcipant in sideIds[side])
        {
            State state = objectManager.GetState(parcipant);
            if (state.capitualated) continue;
            
            power += state.armyPower;
        }
        return power;
    }
    public void EndWar()
    {
        dead = true;
        foreach (State state in participantIds.ToArray().Select(objectManager.GetState))
        {
            RemoveParticipant(state);
        }
        // Clears all participants (Needed for claim transfer)
        participantIds = [];

        objectManager.ForgetWar(this);
    }
    public enum WarSide
    {
        AGRESSOR,
        DEFENDER
    }
}
public enum WarType
{
    CONQUEST,
    CIVIL_WAR,
    REVOLT
}