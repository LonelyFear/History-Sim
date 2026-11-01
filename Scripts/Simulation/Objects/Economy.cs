using System;
using System.Collections.Generic;
using MessagePack;
using static Defines;

[MessagePackObject]
public class Economy
{
    [Key(0)] public Dictionary<Goods, float> production = [];
    [Key(1)] public Dictionary<Goods, float> trade = []; 
    [Key(2)] public Dictionary<Goods, float> supplies = [];
    [Key(3)] public Dictionary<Goods, float> demands = [];

    public Economy()
    {
        foreach (Goods good in Enum.GetValues<Goods>())
        {
            production[good] = 0;
            demands[good] = 0;
            supplies[good] = 0;
            trade[good] = 0;
        }        
    }
}