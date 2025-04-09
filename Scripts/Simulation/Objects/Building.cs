using Godot;
using Godot.Collections;
using System;
public partial class Building : GodotObject
{
    BuildingData data;
    public int level;
    public long maxWorkforce;    
    public long workforce;
    public Array<Pop> pops = new Array<Pop>();
}
