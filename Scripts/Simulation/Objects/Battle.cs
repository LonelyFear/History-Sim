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
    public Conflict.Side victor = Conflict.Side.DEFENDER;
    static Random rng = new Random();
    public static Battle CalcBattle(Region loc, State atk, State def, long attackers, long defenders){
        Battle result = new Battle(){
            location = loc,
            attacker = atk,
            defender = def,
            attackerStrength = attackers,
            defenderStrength = defenders
        };

        long attackPower = (long)(Pop.FromNativePopulation(attackers) * Mathf.Lerp(0.9f, 1.1f, rng.NextSingle()));
        long defendPower = (long)(Pop.FromNativePopulation(defenders) * Mathf.Lerp(0.9f, 1.1f, rng.NextSingle()));
        
        // Disorganized people lack defenders advantage
        if (def != null){
            defendPower = (long)(defendPower * 1.2);
        }

        long totalPower = attackPower + defendPower;
        if (rng.NextSingle() >= (float)attackPower/totalPower){
            result.victor = Conflict.Side.AGRESSOR;
        }
        float attackerLossRatio = (float)defendPower / totalPower;
        float defenderLossRatio = (float)attackPower / defendPower;

        if (result.victor == Conflict.Side.AGRESSOR){
            defenderLossRatio += Mathf.Lerp(0.01f, 0.1f, rng.NextSingle());
        } else {
            attackerLossRatio += Mathf.Lerp(0.01f, 0.1f, rng.NextSingle());
        }
        attackerLossRatio = Mathf.Clamp(attackerLossRatio, 0f, 1f);
        defenderLossRatio = Mathf.Clamp(defenderLossRatio, 0f, 1f);
        
        result.defenderLosses = (long)(defenders * defenderLossRatio);
        result.attackerLosses = (long)(attackers * attackerLossRatio);

        return result;
    }
}