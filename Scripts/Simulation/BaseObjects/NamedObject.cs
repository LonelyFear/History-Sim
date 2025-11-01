using System.Runtime.CompilerServices;
using MessagePack;

[MessagePackObject]
public class NamedObject
{
    [Key(400)] public ulong id { get; set; }
    [Key(401)] public string name { get; set; }
    [Key(402)]public string description { get; set; }
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
    public string GenerateUrlText(NamedObject obj, string text, string color = "orange")
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