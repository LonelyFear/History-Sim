using Godot;
using System;
using System.Collections.Generic;

public class Family
{
    public string name = "Regal";
    public Character head;
    public List<Character> members = new List<Character>();
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
