using Godot;
using Godot.Collections;
using System;

public partial class Family : GodotObject
{
    public string name = "Regal";
    public Character head;
    public Array<Character> members = new Array<Character>();
    public Culture culture;
    public void AddCharacter(Character character){
        if (!members.Contains(character)){
            if (character.family != null){
                character.family.RemoveCharacter(character);
            } 
            character.family = this;
            members.Add(character);
        }
    }
    public void RemoveCharacter(Character character){
        if (members.Contains(character)){
            members.Remove(character);
            character.family = null;
        }
    }
}
