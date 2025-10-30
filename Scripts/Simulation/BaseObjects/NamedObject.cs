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