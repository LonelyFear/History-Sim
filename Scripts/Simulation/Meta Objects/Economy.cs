using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class Economy : GodotObject
{
    public Dictionary<SimResource, double> resources;
    
    public void AddResources(SimResource resource, double amount){
        if (!resources.ContainsKey(resource)){
            resources.Add(resource, amount);
        } else {
            resources[resource] += amount;
        }
    }

    public bool EconomyHasResourceType(ResourceType type){
        foreach (var pair in resources){
            SimResource resource = pair.Key;
            double amount = pair.Value;
            if (amount > 0 && resource.types.Contains(type)){
                return true;
            }
        }
        return false;  
    }

    public bool EconomyHasResource(string id){
        foreach (var pair in resources){
            SimResource resource = pair.Key;
            double amount = pair.Value;
            if (amount > 0 && resource.id == id.ToLower()){
                return true;
            }
        }
        return false;  
    }

    public void RemoveResources(SimResource resource, double amount){
        if (resources.ContainsKey(resource)){
            resources[resource] -= amount;
            if (resources[resource] <= 0){
                resources.Remove(resource);
            }
        } else {
            GD.PrintErr("Tried to remove resource that doesn't exist from economy");
        }
    }
}
