using System;
using Godot;

public class Battle{
    public Region location;
    public State attacker;
    public State defender;
    public long attackerStrength;
    public long defenderStrength;
    public long attackerLosses;
    public long defenderLosses;
    public bool attackSuccessful;
    static Random rng = new Random();
    public static Battle CalcBattle(Region loc, State atk, State def, long attackers, long defenders, long attackingMilitia = 0, long defendingMilitia = 0){
        Battle result = new Battle(){
            location = loc,
            attacker = atk,
            defender = def,
            attackerStrength = attackers,
            defenderStrength = defenders
        };
        long baseAttackerPower = Pop.FromNativePopulation(attackers) + Pop.FromNativePopulation((long)(attackingMilitia * 0.8f));
        long baseDefenderPower = Pop.FromNativePopulation(defenders) + Pop.FromNativePopulation((long)(defendingMilitia * 0.8f));
        double attackPower = Mathf.Round(baseAttackerPower * Mathf.Lerp(0.9, 1.1, rng.NextDouble()));
        double defendPower = Mathf.Round(baseDefenderPower * Mathf.Lerp(0.9, 1.1, rng.NextDouble()));
        
        defendPower = defendPower * 1.2;

        double totalPower = attackPower + defendPower;

        if (rng.NextDouble() < attackPower/totalPower){
            result.attackSuccessful = true;
        }
        float attackerLossRatio = (float)(defendPower / totalPower);
        float defenderLossRatio = (float)(attackPower / totalPower);

        if (result.attackSuccessful){
            defenderLossRatio += Mathf.Lerp(0.1f, 0.2f, rng.NextSingle());
        } else {
            attackerLossRatio += Mathf.Lerp(0.1f, 0.2f, rng.NextSingle());
        }

        attackerLossRatio = Mathf.Clamp(attackerLossRatio, 0f, 1f);
        defenderLossRatio = Mathf.Clamp(defenderLossRatio, 0f, 1f);
        
        result.defenderLosses = (long)(defenders * defenderLossRatio);
        result.attackerLosses = (long)(attackers * attackerLossRatio);

        return result;
    }
}