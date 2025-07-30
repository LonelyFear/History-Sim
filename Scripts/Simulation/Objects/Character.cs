using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using Godot;

public class Character
{
    public string name = "John";
    public float wealth;
    public Culture culture;
    public State state;
    public uint age;
    public uint birthTick;
    public uint existTime;
    public Pop pop;
    public Gender gender = Gender.MALE;
    public static SimManager simManager;

    public Random rng = new Random();
    public int childCooldown = 12;

    // Family 
    public Character parent;
    public Character spouse;
    public List<Character> children = new List<Character>();

    // Traits
    public TraitLevel agression = 0;

    public const uint maturityAge = 18 * TimeManager.ticksPerYear;

    public void Die()
    {
        simManager.DeleteCharacter(this);
    }
    public void HaveChild()
    {
        if (children.Count <= 20)
        {
            Character child = simManager.CreateCharacter(pop, 0, 0, (Gender)rng.Next(0, 2));
            child.parent = this;
            children.Add(child);
        }
    }

    public void ChildUpdate()
    {
        if (parent != null)
        {
        }
    }
    public Character GetHeir()
    {
        foreach (Character child in children)
        {
            if (child.gender == Gender.MALE)
            {
                return child;
            }
        }
        if (parent != null)
        {
            foreach (Character sibling in parent.children)
            {
                if (sibling.gender == Gender.MALE)
                {
                    return sibling;
                }
            }
        }
        return null;
    }

    public bool CanHaveChild()
    {
        return (state.leader == this || state.leader == parent) && age > maturityAge && childCooldown < 1;
    }
    public double GetDeathChance()
    {
        if (age < 60 * TimeManager.ticksPerYear)
        {
            return 0.0001;
        }
        else if (age < 90 * TimeManager.ticksPerYear)
        {
            return 0.01;
        }
        else
        {
            return 0.1;
        }
    }

    public enum Gender
    {
        MALE = 0,
        FEMALE = 1
    }
}

public enum TraitLevel
{
    VERY_LOW,
    LOW,
    MEDIUM,
    HIGH,
    VERY_HIGH,
}

