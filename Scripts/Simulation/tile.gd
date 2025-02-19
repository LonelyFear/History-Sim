extends Node
class_name Tile

var biome : Dictionary
var road : bool
var settlementType : SettlementTypes = SettlementTypes.NONE

enum SettlementTypes{
	NONE,
	TOWN,
	CITY,
	LARGE_CITY
}
