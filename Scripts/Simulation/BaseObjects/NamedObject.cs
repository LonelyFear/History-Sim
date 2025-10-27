public abstract class NamedObject
{
    public string name { get; set; }
    public string description { get; set; }

}
public enum ObjectType
{
    STATE,
    REGION,
    CULTURE,
    CHARACTER,
    LANDFORM,
    UNKNOWN
}