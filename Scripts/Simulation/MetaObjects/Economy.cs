using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Economy
{
    public Dictionary<BaseResource, double> resources = new Dictionary<BaseResource, double>();

    public double ChangeResourceAmount(BaseResource resource, double amount)
    {
        if (amount > 0)
        {
            if (!resources.ContainsKey(resource))
            {
                resources.Add(resource, amount);
            }
            else
            {
                resources[resource] += amount;
            }
        }
        else if (resources.ContainsKey(resource))
        {
            resources[resource] += amount;
            if (resources[resource] <= 0)
            {
                double extra = resources[resource];
                resources.Remove(resource);
                return Mathf.Abs(extra);
            }
        }
        return 0;
    }
    public void TransferResources(Economy newEconomy, BaseResource resource, double amount)
    {
        double clampedAmount = Mathf.Clamp(amount, 0, resources[resource]);
        if (resources.ContainsKey(resource))
        {
            ChangeResourceAmount(resource, -clampedAmount);
        }
        newEconomy.ChangeResourceAmount(resource, clampedAmount);
    }
    public void SetResourceAmount(BaseResource resource, double amount)
    {
        if (amount > 0)
        {
            if (!resources.ContainsKey(resource))
            {
                resources.Add(resource, amount);
            }
            else
            {
                resources[resource] = amount;
            }
        }
        else if (resources.ContainsKey(resource))
        {
            resources.Remove(resource);
        }
    }
    public double GetResourceAmount(BaseResource resource)
    {
        if (resources.ContainsKey(resource))
        {
            return resources[resource];
        }
        return 0;
    }
    public double GetTotalFoodAmount()
    {
        double amount = 0;
        foreach (BaseResource resource in resources.Keys)
        {
            if (resource.GetType() == typeof(FoodResouce))
            {
                amount += resources[resource];
            }
        }
        return amount;
    }
    public double GetTotalNutrition()
    {
        double amount = 0;
        foreach (BaseResource resource in resources.Keys)
        {
            if (resource.GetType() == typeof(FoodResouce))
            {
                FoodResouce food = (FoodResouce)resource;
                amount += resources[food] * food.nutrition;
            }
        }
        return amount;
    }
}
