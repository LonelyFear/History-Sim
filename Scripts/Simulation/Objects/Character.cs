using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;

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
    public Gender gender = Gender.MALE;
    public Character parent;
    public SimManager simManager;
    public TraitLevel agression = TraitLevel.MEDIUM;
    public List<Character> children = new List<Character>();
    public Random rng = new Random();
    public int childCooldown = 12;

    public void Die(){
        if (state.leader == this){
            state.lastLeader = this;
        }    
        simManager.DeleteCharacter(this);
    }

    public void HaveChild(){
        if (children.Count <= 20){
            Character child = simManager.CreateCharacter(pop, 0, 0, (Character.Gender)rng.Next(0, 2));
            child.parent = this;
            children.Add(child);
        }
    }

    public bool CanHaveChild(){
        if (state.leader == this || state.leader == parent && age > 240 && childCooldown < 1){
            return true;
        }
        return false;
    }

    public Character GetHeir(){
        bool femaleHeirs = culture.equity > 0;
        foreach (Character child in children){
            if (child.gender == Gender.MALE || femaleHeirs){
                return child;
            }
        }
        return null;
    }

    public float GetDeathChance(){
        if (age < 60){
            return 0.001f;
        } else if (age < 90){
            return 0.01f;
        } else {
            return 0.1f;
        }
    }
    public enum Role {
        LEADER,
        HEIR,
        CIVILIAN,
    }

    public enum Gender{
        MALE = 0,
        FEMALE = 1
    }
}

