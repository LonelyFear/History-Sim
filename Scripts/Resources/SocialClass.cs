using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class SocialClass : SimResource
{
    [Export] public string name = "New Social Class";
    [Export(PropertyHint.MultilineText)] public string description = "A new social class.";
    [ExportCategory("Stats")]
    [Export] public float basePoliticalPower = 1;
    [ExportCategory("Needs")]
    [Export] public float foodDemand = 1;
    [Export] public float goodsDemand = 1;
    [Export] public float luxuryDemand = 1;
}
