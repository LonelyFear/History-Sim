using Godot;
using Godot.Collections;
using System;

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
