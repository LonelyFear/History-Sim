using Godot;
using Godot.Collections;


public partial class Tile : GodotObject
{
    public Dictionary biome;
    public bool hasRoad;
    public SettlementTypes settlementType = SettlementTypes.NONE;
   
}
public enum SettlementTypes{
	NONE,
	FARMS,
	TOWN,
	CITY
}
