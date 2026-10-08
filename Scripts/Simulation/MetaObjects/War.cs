using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Godot;
using MessagePack;
using MessagePack.Formatters;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.States.Diplomacy;

namespace PixelHistory.Objects.Wars;
[MessagePackObject(AllowPrivate = true)]
public partial class War : NamedObject
{
    //[Key(7)] public Dictionary<WarSide, List<ulong>> sideIds = [];
    [IgnoreMember] public State attackerLeader;
    [IgnoreMember] public State defenderLeader;
    [IgnoreMember] public List<State> attackers = [];
    [IgnoreMember] public List<State> defenders = [];
    [IgnoreMember] public List<State> participants = [];
    [Key(7)] ulong attackerLeaderId;
    [Key(8)] ulong defenderLeaderId;
    [Key(9)] List<ulong> attackerIds;
    [Key(10)] List<ulong> defenderIds;
    [Key(11)] List<ulong> participantIds;
    [Key(12)] public WarType warType { get; set; } = WarType.CONQUEST;
    [Key(13)] public WarSide ?victor = null;

    public War() {}
    public override void PrepareForSave()
    {
        attackerLeaderId = attackerLeader.id;
        defenderLeaderId = defenderLeader.id;
        attackerIds = [..attackers.Select(s => s.id)];
        defenderIds = [..defenders.Select(s => s.id)];
        participantIds = [..participants.Select(s => s.id)];
        base.PrepareForSave();
    }
    public override void LoadFromSave()
    {
        attackerLeader = ObjectManager.GetState(attackerLeaderId);
        defenderLeader = ObjectManager.GetState(defenderLeaderId);
        attackers = [..attackerIds.Select(i => ObjectManager.GetState(i))];
        defenders = [..defenderIds.Select(i => ObjectManager.GetState(i))];
        participants = [..participantIds.Select(i => ObjectManager.GetState(i))];
        base.LoadFromSave();
    }
    public void NameWar()
    {
        switch (warType)
        {
            case WarType.CONQUEST:
                name = $"{attackerLeader.baseName}-{defenderLeader.baseName} War";
                break;
            case WarType.CIVIL_WAR:
                name = $"{NameGenerator.GetDemonym(defenderLeader.baseName)} Civil War";
                break;
            case WarType.REVOLT:
                name = $"{NameGenerator.GetDemonym(attackerLeader.baseName)} Rebellion";
                break;
        }
    }
    public static WarSide GetOtherSide(WarSide side)
    {
        return (WarSide)Mathf.PosMod((int)side + 1, 2);
    }
    public void AddParticipant(State state, WarSide side)
    {
        if (participants.Contains(state)) RemoveParticipant(state);

        state.wars[this] = side;
        state.lastLiegeId = state.liegeId;

        List<State> alliedSide = GetAllies(state);
        List<State> enemySide = GetEnemies(state);

        state.SetEnemies(enemySide, true);
        //if (warType == WarType.CIVIL_WAR) GD.Print("War Leads Fighting: " + defenderLeader.IsEnemyWithState(attackerLeader));

        alliedSide.Add(state);
        participants.Add(state);
    }

    public void RemoveParticipant(State state)
    {
        // Gets the side this state is on
        List<State> alliedSide = GetAllies(state);
        List<State> enemySide = GetEnemies(state);

        state.SetEnemies(enemySide, false);
        
        if (IsStateWarLead(state))
        {
            SetVictor(GetOtherSide(state.wars[this]));
        }

        // Removes from participants list
        state.wars.Remove(this, out WarSide side);
        alliedSide.Remove(state);

        // Claims
        // Gives owner a claim to the conquered land
        if (!state.capitualated)
        {
            foreach (Region region in state.regions)
            {
                if (!state.IsEnemyWithState(region.owner))
                {
                    region.owner.AddClaim(region);
                }
            }              
        }

        participants.Remove(state);
    }
    public int GetSideCombatPower(WarSide side)
    {
        int power = 0;
        List<State> alliedSide = side == WarSide.AGRESSOR ? attackers : defenders;

        foreach (State state in alliedSide)
        {
            if (state.capitualated) continue;
            
            power += state.GetCombatPower();
        }
        return power;
    }
    public List<State> GetAllies(State state)
    {
        if (state.wars.TryGetValue(this, out WarSide stateSide))
        {
            return stateSide == WarSide.AGRESSOR ? attackers : defenders;
        }
        return [];
    }
    public List<State> GetEnemies(State state)
    {
        if (state.wars.TryGetValue(this, out WarSide stateSide))
        {
            return stateSide == WarSide.DEFENDER ? attackers : defenders;
        }
        return [];
    }
    public void EndWar()
    {
        dead = true;
        foreach (State state in participants.ToArray())
        {
            if (state.sovereignty == Sovereignty.REBELLIOUS)
            {
                if (state.GetLiege() != null) state.sovereignty = Sovereignty.PUPPET;
                else state.sovereignty = Sovereignty.INDEPENDENT;
            }
            RemoveParticipant(state);
        }
        // Clears all participants (Needed for claim transfer)
        participants = [];
        attackerLeader = null;
        defenderLeader = null;
        ObjectManager.ForgetWar(this);
    }
    public bool IsStateWarLead(State state)
    {
        return attackerLeader == state || defenderLeader == state;
    }
    public void SetVictor(WarSide winner)
    {
        if (victor == null){
            victor = winner;
        }
    }
    public enum WarSide
    {
        AGRESSOR = 0,
        DEFENDER = 1,
        NEUTRAL = 2
    }
}
public enum WarType
{
    CONQUEST,
    CIVIL_WAR,
    REVOLT
}