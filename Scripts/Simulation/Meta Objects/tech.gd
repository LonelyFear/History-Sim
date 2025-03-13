extends Node
class_name Tech

var militaryLevel : int
var societyLevel : int
var industryLevel : int

static func sameTech(techA : Tech, techB : Tech):
	var sameMil : bool = techA.militaryLevel == techB.militaryLevel
	var sameSoc : bool = techA.societyLevel == techB.societyLevel
	var sameInd : bool = techA.industryLevel == techB.industryLevel
	return sameMil && sameSoc && sameInd
