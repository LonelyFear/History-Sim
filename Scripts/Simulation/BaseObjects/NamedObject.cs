using System.Runtime.CompilerServices;
using MessagePack;
public abstract class NamedObject
{
    [Key(401)] public uint tickCreated { get; set; }
    [Key(402)] public uint tickDestroyed { get; set; }
    [Key(410)] public bool dead = false;
    [IgnoreMember] public static SimManager simManager;
    [IgnoreMember] public static ObjectManager objectManager;
    [Key(403)] public ulong id { get; set; }
    [Key(404)] public string name { get; set; }
    [Key(405)] public string description { get; set; }
    public uint TicksSince(uint tick)
    {
        return simManager.timeManager.ticks - tick;
    }
    public uint GetAge()
    {
        return TicksSince(tickCreated);
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
        return $"[color={color}][url={typeId}{obj.id}]{text}[/url][/color]";
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