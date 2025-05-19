using System.Collections.Generic;
using Godot;

public class Army
{
    public string name;
    public uint age;
    public State state;
    public Region location;
    public Region headquarters;
    public ulong strength;
    public Character leader;
    public Queue<Region> currentPath;
    public static SimManager simManager;

    public void MoveArmy(Region region)
    {
        if (region != location)
        {
            region.AddArmy(this);
        }
    }
    public void MoveHeadquarters(Region region)
    {
        headquarters = region;
    }
    public void TakeLosses(long amount)
    {
        strength -= (ulong)amount;
    }
}