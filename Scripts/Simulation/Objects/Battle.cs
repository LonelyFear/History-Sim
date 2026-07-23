using System;
using Godot;
using PixelHistory.Objects.States.Base;

public class Battle{
     
    public Region location;
    public State attacker;
    public State defender;
    public long attackerStrength;
    public long defenderStrength;
    public long attackerLosses;
    public long defenderLosses;
    public bool attackSuccessful;
    public static bool CalcBattle(Region site, long attackers, long defenders){
        bool attackSuccessful = false;
        long baseAttackerPower = attackers;
        long baseDefenderPower = defenders;
        double attackPower = Mathf.Round(baseAttackerPower * Mathf.Lerp(0.5, 1.5, NamedObject.rng.NextDouble()));
        double defendPower = Mathf.Round(baseDefenderPower * Mathf.Lerp(0.5, 1.5, NamedObject.rng.NextDouble()));

        defendPower *= Mathf.Lerp(1.2f, 2.2f, 1f - site.navigability);
        if (site.owner?.capital == site) defendPower  *= 2f;

        double totalPower = attackPower + defendPower;

        if (NamedObject.rng.NextDouble() < attackPower/totalPower){
            attackSuccessful = true;
        }
        return attackSuccessful;
    }
}