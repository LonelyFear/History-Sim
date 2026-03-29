using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
using MessagePack.Formatters;
[MessagePackObject]
public partial class War : NamedObject
{
    [Key(1)] public Dictionary<WarSide, List<ulong>> sideIds = new Dictionary<WarSide, List<ulong>>();
    [Key(2)] public HashSet<ulong> participantIds {get; private set;} = [];
    [Key(22)] public List<ulong> removedIds = [];
    [Key(3)] public Dictionary<WarSide, ulong> warLeaderIds = new Dictionary<WarSide, ulong>();
    [Key(5)] public WarType warType { get; set; } = WarType.CONQUEST;
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
                name = $"{NameGenerator.GetDemonym(defender.name)} Civil War";
                break;
            case WarType.REVOLT:
                name = $"{NameGenerator.GetDemonym(agressor.name)} Rebellion";
                break;
        }
    }
    public static WarSide GetOtherSide(WarSide side)
    {
        return (WarSide)Mathf.PosMod((int)side + 1, 2);
    }

    public void AddParticipant(State state, WarSide side)
    {
        if (participantIds.Contains(state.id)) return;

        WarSide opposingSide = (WarSide)Mathf.PosMod((int)side + 1, 2);

        sideIds[side].Add(state.id);
        state.diplomacy.SetEnemies(sideIds[opposingSide], true);

        foreach (ulong enemyId in sideIds[opposingSide])
        {
            State enemy = objectManager.GetState(enemyId);
            enemy.diplomacy.SetEnemy(state.id, true);
        }
 
        state.diplomacy.warIds[id] = side;
        participantIds.Add(state.id);
    }
    public void RemoveParticipant(State state)
    {
        // Gets the side this state is on
        WarSide side = WarSide.DEFENDER;
        try
        {
            side = state.diplomacy.warIds[id];
        } catch (Exception e)
        {
            GD.PushError(state.name);
            GD.PushError(participantIds.Contains(state.id));
            GD.PushError(e);
        }
        
        if (!participantIds.Remove(state.id)) return;

        // Removes enemies and sided participation
        if (sideIds[side].Remove(state.id))
        {
            // Gets opposition
            WarSide opposingSide = (WarSide)Mathf.PosMod((int)side + 1, 2);
            state.diplomacy.SetEnemies(sideIds[opposingSide], false);

            // Makes it so our (former) opposition wont fight us
            foreach (ulong enemyId in sideIds[opposingSide])
            {
                State enemy = objectManager.GetState(enemyId);
                enemy.diplomacy.SetEnemy(state.id, false);
            }
        }
        // Removes from participants list
        state.diplomacy.warIds.Remove(id);

        // Checks if we can end the war
        bool warEndConditions = sideIds[WarSide.AGRESSOR].Count < 1 || sideIds[WarSide.DEFENDER].Count < 1 || warLeaderIds[side] == state.id;
        if (warEndConditions)
        {
            objectManager.EndWar(this);
        }
    }

    public enum WarSide
    {
        AGRESSOR,
        DEFENDER
    }
}