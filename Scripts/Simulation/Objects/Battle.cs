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
    public static bool CalcBattle(Region loc, long attackers, long defenders){
        bool attackSuccessful = false;
        long baseAttackerPower = Pop.FromNativePopulation(attackers);
        long baseDefenderPower = Pop.FromNativePopulation(defenders);
        double attackPower = Mathf.Round(baseAttackerPower * Mathf.Lerp(0.5, 1.5, rng.NextDouble()));
        double defendPower = Mathf.Round(baseDefenderPower * Mathf.Lerp(0.5, 1.5, rng.NextDouble()));

        defendPower *= Mathf.Lerp(1.2f, 2.2f, 1f - loc.navigability);

        double totalPower = attackPower + defendPower;

        if (rng.NextDouble() < attackPower/totalPower){
            attackSuccessful = true;
        }
        /*
        float attackerLossRatio = (float)(defendPower / totalPower);
        float defenderLossRatio = (float)(attackPower / totalPower);

        if (result.attackSuccessful){
            defenderLossRatio += Mathf.Lerp(0f, 0.05f, rng.NextSingle());
        } else {
            attackerLossRatio += Mathf.Lerp(0f, 0.05f, rng.NextSingle());
        }

        attackerLossRatio = Mathf.Clamp(attackerLossRatio, 0f, 1f);
        defenderLossRatio = Mathf.Clamp(defenderLossRatio, 0f, 1f);
        
        result.defenderLosses = (long)(defenders * defenderLossRatio);
        result.attackerLosses = (long)(attackers * attackerLossRatio);
        */
        return attackSuccessful;
    }
}