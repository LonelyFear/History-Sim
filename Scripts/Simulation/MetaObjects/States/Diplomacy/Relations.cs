using MessagePack;

[MessagePackObject]
public class Relations
{
    [Key(0)] public int opinion;
    [Key(1)] public bool rival;
    [Key(2)] public bool enemy;
    [Key(3)] public bool isLiege;
}   