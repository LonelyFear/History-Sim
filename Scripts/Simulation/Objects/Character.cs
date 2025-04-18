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
    public Role role;
    public Family family;
    public SimManager simManager;
    public TraitLevel agression = TraitLevel.MEDIUM;

    public void Die(){
              
        if (state.leader == this){
            GD.Print(name + ", leader of " + state.name + ", died of old age");  
            if (GetHeir() != null){
                GD.Print("Heir: " + GetHeir().name);
            }
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
            if (state.leader == this){
                GD.Print(name + ", leader of " + state.name + ", had a child named " + child.name);
            }         
        }
    }

    public Character GetHeir(){
        int eldestAge = -1;
        Character heir = null;
        if (family != null){
            foreach (Character member in family.members){
                if (member.state == state && member != this && member.role != Role.LEADER && member.age > eldestAge){
                    eldestAge = member.age;
                    heir = member;
                }
            }
        }
        return heir;
    }
    public enum Role {
        LEADER,
        ADGITATOR,
        HEIR,
        CIVILIAN,
        FAMILUY_MEMBER
    }
}

