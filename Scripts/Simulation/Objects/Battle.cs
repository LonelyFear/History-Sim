using System;
using Godot;

public class Battle{
    public static ObjectManager objectManager;
    public Region location;
    public State attacker;
    public State defender;
    public long attackerStrength;
    public long defenderStrength;
    public long attackerLosses;
    public long defenderLosses;
    public bool attackSuccessful;
    static readonly Random rng = new();
    public static bool CalcBattle(Region site, long attackers, long defenders){
        bool attackSuccessful = false;
        long baseAttackerPower = attackers;
        long baseDefenderPower = defenders;
        double attackPower = Mathf.Round(baseAttackerPower * Mathf.Lerp(0.5, 1.5, rng.NextDouble()));
        double defendPower = Mathf.Round(baseDefenderPower * Mathf.Lerp(0.5, 1.5, rng.NextDouble()));

        defendPower *= Mathf.Lerp(1.2f, 4.2f, 1f - site.navigability);
        if (site.owner?.capital == site) defendPower  *= 3f;

        double totalPower = attackPower + defendPower;

        if (rng.NextDouble() < attackPower/totalPower){
            attackSuccessful = true;
        }
        return attackSuccessful;
    }
}