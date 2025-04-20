using System;
using Godot;

public partial class Character : GodotObject
{
    public string name = "John";
    public float wealth;
    public Culture culture;
    public State state;
    public int age;
    public Pop pop;
    public Role role = Role.CIVILIAN;
    public Family family;
    public Character parent;
    public SimManager simManager;
    public TraitLevel agression = TraitLevel.MEDIUM;

    public void Die(){
        if (state.leader == this){
            state.lastLeader = this;
        }    
        simManager.DeleteCharacter(this);
    }

    public void HaveChild(){
        if (family == null){
            family = new Family();
            if (state.leader == this){
                state.rulingFamily = family;
            }            
        }
        if (family.members.Count <= 12){
            Character child = simManager.CreateCharacter(pop, family, 0, 0);
            child.parent = this;
            if (state.leader == this){
            }         
        }
    }

    public Character GetHeir(){
        int eldestAge = -1;
        Character heir = null;
        if (family != null){
            foreach (Character member in family.members){
                if ((member.parent == this || member.state == state) && member.state.leader != member && member.age > eldestAge){
                    eldestAge = member.age;
                    heir = member;
                }
            }
        }
        return heir;
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

