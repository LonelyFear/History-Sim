using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true, AllowPrivate = true)]
public class War
{
    public string warName { get; set; } = "War";
    public List<State> attackers { get; set; } = new List<State>();
    public List<State> defenders { get; set; } = new List<State>();
    public List<State> participants { get; set; } = new List<State>();
    public WarType warType { get; set; } = WarType.CONQUEST;
    [IgnoreMember]
    static Random rng = new Random();
    [IgnoreMember]
    public static SimManager simManager;
    public uint tickStarted;
    public uint age;
    public uint tickEnded;

    public void StartWar (List<State> atk, List<State> def, WarType warType, State agressorLeader, State defenderLeader)
    {
        attackers = atk;
        defenders = def;
        this.warType = warType;

        foreach (State state in attackers)
        {
            state.enemies.AddRange(defenders);
            state.wars.Add(this, true);
            participants.Add(state);
        }
        foreach (State state in defenders)
        {
            state.enemies.AddRange(attackers);
            state.wars.Add(this, false);
            participants.Add(state);
        }
        simManager.wars.Add(this);

        switch (warType)
        {
            case WarType.CONQUEST:
                string[] warNames = { "War", "Conflict" };
                warName = $"{agressorLeader.name}-{defenderLeader.name} {warNames.PickRandom()}";
                if (rng.NextSingle() < 0.25f)
                {
                    warNames = ["Invasion of"];
                    warName = $"{NameGenerator.GetDemonym(agressorLeader.name)} {warNames.PickRandom()} {defenderLeader.name}";
                }
                break;
            case WarType.CIVIL_WAR:
                warName = $"{NameGenerator.GetDemonym(defenderLeader.name)} Civil War";
                break;
            case WarType.REVOLT:
                warNames = ["Revolution", "Uprising", "Rebellion", "Revolt"];
                warName = $"{NameGenerator.GetDemonym(agressorLeader.name)} {warNames.PickRandom()}";
                break;
        }
    }
    public void AddParticipants(List<State> states, bool attacker)
    {
        foreach (State state in states)
        {
            bool isInWar = false;

            if (attacker && !attackers.Contains(state))
            {
                state.enemies.AddRange(attackers);
                isInWar = true;
            }
            else if (!defenders.Contains(state))
            {
                state.enemies.AddRange(attackers);
                isInWar = true;
            }
            if (isInWar)
            {
                state.wars.Add(this, attacker);
                participants.Add(state);
            }
        }
    }
    public void RemoveParticipants(List<State> states)
    {
        foreach (State state in states)
        {
            bool isInWar = false;
            if (participants.Contains(state))
            {
                isInWar = true;
            }
            if (isInWar)
            {
                state.wars.Remove(this);
                participants.Remove(state);
            }
        }
        if (attackers.Count < 1 || defenders.Count < 1)
        {
            EndWar();
        }
    }
    public void EndWar()
    {
        simManager.wars.Remove(this);
        foreach (State state in attackers.Concat(defenders))
        {
            state.wars.Remove(this);
        }
    }
}