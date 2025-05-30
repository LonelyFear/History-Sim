using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Economy
{
    public Dictionary<BaseResource, double> resources = new Dictionary<BaseResource, double>();
    
    public double ChangeResourceAmount(BaseResource resource, double amount){
        if (amount > 0){
            if (!resources.ContainsKey(resource)){
                resources.Add(resource, amount);
            } else {
                resources[resource] += amount;
            }            
        } else if (resources.ContainsKey(resource)){
            resources[resource] += amount;
            if (resources[resource] <= 0){
                double extra = resources[resource];
                resources.Remove(resource);                
                return Mathf.Abs(extra);
            }
        }
        return 0;
    }
    public void SetResourceAmount(BaseResource resource, double amount){
        if (amount > 0){
            if (!resources.ContainsKey(resource)){
                resources.Add(resource, amount);
            } else {
                resources[resource] = amount;
            }                
        } else if (resources.ContainsKey(resource)){
            resources.Remove(resource);
        }
    }
    public double AmountOfResource(BaseResource resource){
        return resources[resource];
    }
}
