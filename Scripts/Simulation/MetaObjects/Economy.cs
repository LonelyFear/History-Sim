using System.Collections.Generic;
using System.Linq;
using Godot;
using MessagePack;

[MessagePackObject]
public class Economy
{
    [Key(0)] public Dictionary<string, float> demand = new();
    [Key(1)] public Dictionary<string, float> supply = new();
    [Key(3)] public Dictionary<string, float> prices = new();
    [IgnoreMember] public Dictionary<string, float> tradeFlow = new();
    public void InitEconomy()
    {
        foreach (var pair in AssetManager.items)
        {
            if (pair.Value.tags.Contains("tradeable"))
            {
                demand[pair.Key] = 0;
                supply[pair.Key] = 0;
                prices[pair.Key] = pair.Value.basePrice;    
                tradeFlow[pair.Key] = 0;           
            }
        }
    }
    public float GetSupply(string itemId)
    {
        return supply[itemId] + tradeFlow[itemId];
    }
    public void CalculatePrices()
    {
        foreach (var pair in prices)
        {
            string itemId = pair.Key;
            Item item = AssetManager.GetItem(itemId);
            float priceMultiplier = demand[itemId] / Mathf.Max(supply[itemId], 1f);
            float targetPrice = item.basePrice * Mathf.Clamp(priceMultiplier, 0f, 4f);
            prices[itemId] = Mathf.Lerp(prices[itemId], targetPrice, 0.1f);
        }
    }
}