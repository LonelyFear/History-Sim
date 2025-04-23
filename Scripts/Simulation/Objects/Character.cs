using System;
using System.Collections.Generic;

public class Character
{
    public string name = "John";
    public float wealth;
    public Culture culture;
    public State state;
    public uint age;
    public int existTime;
    public Pop pop;
    public Role role = Role.CIVILIAN;
    public Family family;
    public Character parent;
    public SimManager simManager;
    public TraitLevel agression = TraitLevel.MEDIUM;
    public List<Character> children = new List<Character>();

    public void Die(){
        if (state.leader == this){
            state.lastLeader = this;
        }    
        simManager.DeleteCharacter(this);
    }

    public void HaveChild(){
        if (children.Count <= 12){
            Character child = simManager.CreateCharacter(pop, family, 0, 0);
            child.parent = this;
            children.Add(child);
        }
    }

    public Character GetHeir(){
        if (children.Count > 0){
            return children[0];
        }
        return null;
    }

    public void FoundFamily(){
        if (family != null){
            family.RemoveCharacter(this);
        }
        family = new Family();
        if (state.leader == this){
            state.rulingFamily = family;
        }     
    }
    public enum Role {
        LEADER,
        HEIR,
        CIVILIAN,
    }
}

