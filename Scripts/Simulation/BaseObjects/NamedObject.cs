using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MessagePack;
[MessagePackObject]
public class NamedObject
{
    [Key(401)] public uint tickCreated { get; set; }
    [Key(402)] public uint tickDestroyed { get; set; } = 0;
    [Key(410)] public bool dead = false;
    [IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static ObjectManager objectManager;
    [Key(403)] public ulong id { get; set; }
    [Key(404)] public string name { get; set; }
    [Key(405)] public string description { get; set; }
    [Key(406)] public List<ulong> eventIds = new List<ulong>();
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
			default:
				return ObjectType.UNKNOWN;
        }
    }
    public string GetFullId()
    {
        string typeId = "sta";
        switch (this)
        {
            case State:
                typeId = "sta";
                break;
            case War:
                typeId = "war";
                break;
            case Character:
                typeId = "cha";
                break;
            case Culture:
                typeId = "cul";
                break;
            case Region:
                typeId = "reg";
                break;
            default:
                break;
        }
        return typeId + id;        
    }
    public static string GenerateUrlText(NamedObject obj, string text, string color = "orange")
    {
        string typeId = "sta";
        switch (obj)
        {
            case State:
                typeId = "sta";
                break;
            case War:
                typeId = "war";
                break;
            case Character:
                typeId = "cha";
                break;
            case Culture:
                typeId = "cul";
                break;
            case Region:
                typeId = "reg";
                break;
            default:
                break;
        }
        if (obj != null && GetNamedObject(typeId + obj.id) != null)
        {
            return $"[color={color}][url={typeId}{obj.id}]{text}[/url][/color]";
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
    LANDFORM,
    UNKNOWN
}