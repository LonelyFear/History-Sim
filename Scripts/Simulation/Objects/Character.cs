using Godot;

public partial class Character : GodotObject
{
    public string firstName = "John";
    public string lastName = "Doe";
    public float wealth;
    public Culture culture;
    public Pop pop;
    public Role role;
    public TraitLevel agression = TraitLevel.MEDIUM;

    public enum Role {
        LEADER,
        ADGITATOR
    }
}

