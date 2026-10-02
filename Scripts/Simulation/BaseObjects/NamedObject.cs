using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.Wars;

public abstract class NamedObject
{
    [Key(0)] public uint tickCreated { get; set; }
    [Key(1)] public uint tickDestroyed { get; set; } = 0;
    [Key(2)] public bool dead = false;
    [IgnoreMember] public static SimManager simManager;
    [Key(3)] public ulong id { get; set; }
    [Key(4)] public string name { get; set; }
    [Key(5)] public string description { get; set; }
    [Key(6)] public List<HistoricalEvent> historicalEvents = [];
    [IgnoreMember] public static Random rng = null;
    public virtual void PrepareForSave()
    {

    }
    public virtual void LoadFromSave()
    {

    }

    public uint TicksBetween(uint start, uint end)
    {
        return end - start;
    }
    public uint GetAge()
    {
        if (dead)
        {
            return TicksBetween(tickCreated, tickDestroyed);
        }
        return TicksBetween(tickCreated, simManager.timeManager.ticks);
    }
    public virtual string GenerateDescription()
    {
        string desc = $"{name} is a named object. This is placeholder text";
        return desc;
    }
    public virtual string GenerateStatsText()
    {
        string text = $"Name: {name}";
        text += $"\nID: {id}";
        return text;
    }
    public virtual void Die()
    {
        dead = true;
        tickDestroyed = simManager.timeManager.ticks;        
    }
    public string GenerateHistoryText()
    {
        string text = "This object doesnt have any recorded history yet.";
        if (historicalEvents.Count < 1)
        {
            return text;
        }
        text = "";
        foreach (HistoricalEvent historicalEvent in historicalEvents)
        {
            text += $"{TimeManager.GetStringDate(historicalEvent.tickOccured)}: {historicalEvent.text}\n";
        }
        return text;
    }
    public static string GetTypeFromId(string fullId)
    {
        return Regex.Match(fullId, "^[a-zA-Z_]+").ToString();
    }
    public static ulong GetNumFromId(string fullId)
    {
        return ulong.Parse(Regex.Match(fullId, "[0-9]+$").ToString());
    }
    public static T GetNamedObject<T>(string fullId) where T : NamedObject
    {
        string typeString = GetTypeFromId(fullId);
        ulong id = GetNumFromId(fullId);
        NamedObject obj = typeString switch
        {
            "state" => ObjectManager.GetState(id),
            "region" => ObjectManager.GetRegion(id),
            "culture" => ObjectManager.GetCulture(id),
            "character" => ObjectManager.GetCharacter(id),
            "war" => ObjectManager.GetWar(id),
            "alliance" => ObjectManager.GetAlliance(id),
            "market" => ObjectManager.GetTradeZone(id),
            //"ocean" => ObjectManager.GetOcean(id),
            _ => null,
        };
        if (obj is T typeObj)
        {
            return typeObj;
        }
        return null;
    }
    public string GetFullId()
    {
        string typeId = this switch
        {
            State => "state",
            Region => "region",
            Culture => "culture",
            Character => "character",
            War => "war",
            Alliance => "alliance",
            TradeZone => "market",
            //Ocean => "ocean",
            _ => "unknown"
        };
        return typeId + id;        
    }
    public static NamedObject GetNamedObject(string fullId)
    {
        return GetNamedObject<NamedObject>(fullId);
    }
    public string GetTypeName()
    {
        string tName = this switch
        {
            State => "State",
            Region => "Region",
            Culture => "Culture",
            Character => "Character",
            War => "War",
            Alliance => "Alliance",
            TradeZone => "Market",
            _ => "Unknown"
        };
        return tName;        
    }
    public static string GenerateUrlText(NamedObject obj, string text, string color = "orange")
    {
        if (obj != null && GetNamedObject(obj.GetFullId()) != null)
        {
            return $"[color={color}][url={obj.GetFullId()}]{text}[/url][/color]";
        } else
        {
            return $"{text}";
        }
    }
    public static string GenerateUrlText(NamedObject obj, string text, Color color)
    {
        if (obj != null && GetNamedObject(obj.GetFullId()) != null)
        {
            return $"[color=#{color.ToHtml(false)}][url={obj.GetFullId()}]{text}[/url][/color]";
        } else
        {
            return $"{text}";
        }
    }
    public NamedObject Clone()
    {
        return (NamedObject)MemberwiseClone();
    }
}