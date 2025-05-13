using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public class State : PopObject
{
    public string displayName = "Nation";
    public Color color;
    public Color displayColor;

    public GovernmentTypes government = GovernmentTypes.MONARCHY;
    public List<Region> regions = new List<Region>();
    public Region capital;
    public uint age;
    public long manpowerTarget;
    public long manpower;
    List<State> vassals = new List<State>();
    State liege;
    Dictionary<State, Relation> relations;
    public List<State> borderingStates = new List<State>();
    Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    public Economy economy = new Economy();
    public SimManager simManager;
    public Character leader;
    public Character lastLeader = null;
    public Pop rulingPop;
    public List<Character> characters = new List<Character>();
    int monthsSinceElection = 0;
    Random rng = new Random();
    public Tech tech;

    public List<Conflict> conflicts = new List<Conflict>();
    public List<War> wars = new List<War>();

    public void UpdateCapital(){
        if (capital == null){
            capital = regions[0];
        }
    }
    public void RulersCheck(){
        tech = rulingPop.tech;
        monthsSinceElection++;
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
        monthsSinceElection = 0;
        Character newLeader = simManager.CreateCharacter(rulingPop, 20, 50);
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
    public void CountStatePopulation(){
        long countedP = 0;
        long countedW = 0;

        List<Pop> countedPops = new List<Pop>();
        Dictionary<Profession, long> countedProfessions = new Dictionary<Profession, long>();
        foreach (Profession profession in Enum.GetValues(typeof(Profession))){
            countedProfessions.Add(profession, 0);
        }
        Dictionary<Culture, long> cCultures = new Dictionary<Culture, long>();

        foreach (Region region in regions.ToArray()){
            countedP += region.population;
            countedW += region.workforce;
            countedPops.AddRange(region.pops);
            
            foreach (Profession profession in region.professions.Keys){
                countedProfessions[profession] += region.professions[profession];            
            }
            foreach (Culture culture in region.cultures.Keys){
                if (cCultures.ContainsKey(culture)){
                    cCultures[culture] += region.cultures[culture];
                } else {
                    cCultures.Add(culture, region.cultures[culture]);
                }            
            }
        }
        professions = countedProfessions;
        cultures = cCultures;
        population = countedP;
        workforce = countedW;
        pops = countedPops;
    }

    public void Recruitment(){
        if (manpower > workforce){
            manpower = workforce;
        }
        if (professions.ContainsKey(Profession.FARMER) && professions.ContainsKey(Profession.MERCHANT)){
            manpowerTarget = (long)Mathf.Round((professions[Profession.FARMER] + professions[Profession.MERCHANT]) * 0.5);
            manpower = (long)Mathf.Lerp(manpower, manpowerTarget, 0.05);            
        }
        //manpower = 400 * regions.Count;
    }

    public void TakeLosses(long amount){
        while (amount > 0){
            Pop pop = pops[rng.Next(0, pops.Count - 1)];
            if (pop.profession != Profession.ARISTOCRAT){
                if (pop.workforce >= amount){
                    amount = 0;
                    pop.ChangeWorkforce(-amount);
                } else {
                    amount -= pop.workforce;
                    pop.ChangeWorkforce(-pop.workforce);
                }
            }
        }
        manpower -= amount;
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
    public void AddRegion(Region region){
        if (!regions.Contains(region)){
            if (region.owner != null){
                region.owner.RemoveRegion(region);
            }
            region.owner = this;
            regions.Add(region);
            pops.AddRange(region.pops);
        }
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

    public double GetArmyPower(){
        return Pop.FromNativePopulation(manpower) * ((tech.militaryLevel + 1) / 20d)/regions.Count;
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
