using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class State : GodotObject
{
    public string name = "Nation";
    public string displayName = "Nation";
    public Color color;
    public Color displayColor;

    public GovernmentTypes government = GovernmentTypes.MONARCHY;
    public Array<Region> regions = new Array<Region>();
    public Region capital;
    public long population;
    public long workforce;
    public Dictionary<Profession, long> professions = new Dictionary<Profession, long>();
    public long manpowerTarget;
    public long manpower;
    Array<State> vassals;
    State liege;
    Dictionary<State, Relation> relations;
    public Array<State> wars = new Array<State>();
    public Array<State> borderingStates = new Array<State>();
    Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    public Economy economy = new Economy();
    public SimManager simManager;
    public Character leader;
    public Character lastLeader = null;
    public Pop rulingPop;
    public Family rulingFamily;
    public Array<Character> characters = new Array<Character>();
    long age = 0;
    int monthsSinceElection = 0;
    Random rng = new Random();
    public void LeaderCheck(){
        monthsSinceElection++;
        age++;
        switch (government){
            case GovernmentTypes.MONARCHY:
                if (lastLeader != leader && leader == null){
                    SetLeader(lastLeader.GetHeir());
                    lastLeader = null;
                }
                break;
            case GovernmentTypes.REPUBLIC:
                if (monthsSinceElection > 48 || leader == null){
                    RunElection();
                }
                break;
        }
    }

    public void RunElection(){
        Character newLeader = simManager.CreateCharacter(rulingPop, null, 20, 50);
        if (characters.Count > 0){
            newLeader = characters[rng.Next(0, characters.Count - 1)];
        }
        SetLeader(newLeader);
    }
    public void UpdateDisplayName(){
        string govtName;
        switch (government){
            case GovernmentTypes.REPUBLIC:
                govtName = "Republic";
                break;
            case GovernmentTypes.MONARCHY:
                govtName = "Kingdom";
                break;
            case GovernmentTypes.AUTOCRACY:
                govtName = "Dictatorship";
                break;
            default:
                govtName = "State";
                break;
        }
        displayName = govtName + " of " + name;
    }
    public void CountPopulation(){
        long countedP = 0;
        long countedW = 0;

        Dictionary<Profession, long> countedProfessions = new Dictionary<Profession, long>();
        foreach (Profession profession in Enum.GetValues(typeof(Profession))){
            countedProfessions.Add(profession, 0);
        }
        foreach (Region region in regions.ToArray()){
            countedP += region.population;
            countedW += region.workforce;
            
            foreach (Profession profession in region.professions.Keys){
                countedProfessions[profession] += region.professions[profession];            
            }

        }
        professions = countedProfessions;
        population = countedP;
        workforce = countedW;
    }
    public void AddRegion(Region region){
        if (!regions.Contains(region)){
            if (region.owner != null){
                region.owner.RemoveRegion(region);
            }
            region.owner = this;
            regions.Add(region);
        }
    }
    public void Recruitment(){
        manpowerTarget = (long)Mathf.Round((professions[Profession.FARMER] + professions[Profession.MERCHANT]) * 0.7);
        manpower = (long)Mathf.Lerp(manpower, manpowerTarget, 0.05);
    }

    public void SetLeader(Character newLeader){
        // string newName = "nobody";
        // if (newLeader != null){
        //     newName = newLeader.name;
        // }
        // if (leader != null){
        //     GD.Print("Leader of " + name + " changed from " + leader.name + " to " + newName);
        // } else {
        //     GD.Print("Leader of " + name + " changed nobody to " + newName);
        // }
        if (leader != null){
            leader.role = Character.Role.CIVILIAN;
        }
        
        if (newLeader != null){
            newLeader.role = Character.Role.LEADER;
            if (!characters.Contains(newLeader)){
                AddCharacter(newLeader);
            }            
        }
        leader = newLeader;
    }

    public void RemoveRegion(Region region){
        if (regions.Contains(region)){
            region.owner = null;
            regions.Remove(region);
        }
    }

    public void SetStateSovereignty(State state, Sovereignty sovereignty){
        if (state != this){
            if (sovereignty == Sovereignty.INDEPENDENT){
                if (vassals.Contains(state)){
                    state.sovereignty = Sovereignty.INDEPENDENT;
                    vassals.Remove(state);
                }                 
            } else {
                if (vassals.Contains(state)){
                    state.sovereignty = sovereignty;
                } else {
                    AddVassal(state, sovereignty);
                }                
            }
        }
    }

    public void AddVassal(State state, Sovereignty sovereignty = Sovereignty.PUPPET){
        if (sovereignty != Sovereignty.INDEPENDENT){
            if (state.liege != null){
                state.liege.RemoveVassal(state);
            }
            state.liege = this;
            state.sovereignty = sovereignty;
            vassals.Add(state);                
        }
    }

    public void RemoveVassal(State state){
        if (vassals.Contains(state)){
            state.liege = null;
            state.sovereignty = Sovereignty.INDEPENDENT;
            vassals.Remove(state);
        }
    }

    public void AddCharacter(Character character){
        if (!characters.Contains(character)){
            if (character.state != null){
                character.state.RemoveCharacter(character);
            } 
            character.state = this;
            characters.Add(character);
        }
    }
    public void RemoveCharacter(Character character){
        if (characters.Contains(character)){
            if (leader == character){
                SetLeader(null);
            }
            characters.Remove(character);
            character.state = null;
        }
    }
}

public enum GovernmentTypes {
    REPUBLIC,
    MONARCHY,
    AUTOCRACY,
}

public enum Sovereignty {
    INDEPENDENT,
    DOMINION,
    PUPPET,
    COLONY, 
    FEDERAL_STATE,
    PROVINCE
}
