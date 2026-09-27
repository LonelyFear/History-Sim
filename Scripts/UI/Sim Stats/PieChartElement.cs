using Godot;

[GlobalClass]
public partial class PieChartElement : Resource
{
	[Export] public float value;
	[Export] public Color color = new("Red");
}
