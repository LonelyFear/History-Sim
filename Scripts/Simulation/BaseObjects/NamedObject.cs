using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    [IgnoreMember] public static ObjectManager objectManager;
    [Key(3)] public ulong id { get; set; }
    [Key(4)] public string name { get; set; }
    [Key(5)] public string description { get; set; }
    [Key(6)] public List<ulong> eventIds = [];
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
        if (eventIds.Count < 1)
        {
            return text;
        }
        text = "";
        foreach (ulong eventId in eventIds)
        {
            HistoricalEvent historicalEvent = objectManager.GetHistoricalEvent(eventId);
            text += $"{historicalEvent.GetEventText()}\n";
        }
        return text;
    }
    public static T GetNamedObject<T>(string fullId) where T : NamedObject
    {
        ulong id = ulong.Parse(fullId[3..]);
        NamedObject obj;
        switch (GetTypeFromString(fullId[..3]))
        {
			case ObjectType.STATE:
				obj = objectManager.GetState(id);
				break;
			case ObjectType.REGION:
				obj = objectManager.GetRegion(id);
				break;
			case ObjectType.CULTURE:
				obj = objectManager.GetCulture(id);
				break;
			case ObjectType.CHARACTER:
				obj = objectManager.GetCharacter(id);
				break;
			case ObjectType.WAR:
				obj = objectManager.GetWar(id);
				break;
			case ObjectType.ALLIANCE:
				obj = objectManager.GetAlliance(id);
				break;
            default:
                obj = null;
                break;
        }
        return (T)obj;
    }
    public static NamedObject GetNamedObject(string fullId)
    {
        return GetNamedObject<NamedObject>(fullId);
    }
	public static ObjectType GetTypeFromString(string s)
    {
        switch (s)
        {
			case "sta":
				return ObjectType.STATE;
			case "reg":
				return ObjectType.REGION;
			case "cul":
				return ObjectType.CULTURE;
			case "cha":
				return ObjectType.CHARACTER;
			case "war":
				return ObjectType.WAR;
			case "all":
				return ObjectType.ALLIANCE;
			default:
				return ObjectType.UNKNOWN;
        }
    }
    public string GetFullId()
    {
        string typeId = this switch
        {
            State => "sta",
            Region => "reg",
            Culture => "cul",
            Character => "cha",
            War => "war",
            Alliance => "all",
            _ => "idk"
        };
        return typeId + id;        
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
    public NamedObject Clone()
    {
        return (NamedObject)MemberwiseClone();
    }
}
public enum ObjectType
{
    STATE,
    REGION,
    CULTURE,
    CHARACTER,
    WAR,
    ALLIANCE,
    OCEAN,
    UNKNOWN
}