using System;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool] [GlobalClass]
public partial class PieChart : Control
{
	[Export] Dictionary<string, float> elements = new Dictionary<string, float>();
	[Export] Dictionary<string, Color> colors;
	Random rng = new Random();
	// Called when the node enters the scene tree for the first time.
	public void AddElement(string name, float value, Color color)
	{
		elements.Add(name, value);
		colors.Add(name, color);
		QueueRedraw();
	}

	void DrawCircleArcPoly( Vector2 center, float radius, float angleFrom, float angleTo, Color color){
		int points = 32;
        Vector2[] pointVectors = [center];

		for (int i = 0; i <= points; i++)
		{
			float anglePoint = Mathf.DegToRad(angleFrom + i * (angleTo - angleFrom) / points);
			pointVectors = [..pointVectors.Append(center + new Vector2(Mathf.Cos(anglePoint), Mathf.Sin(anglePoint)) * radius)];
		}
		
		DrawColoredPolygon(pointVectors, color);
	}
	void DrawLabels()
	{
		foreach (Node node in GetChildren())
		{
			node.QueueFree();
		}
	}

	public override void _Draw()
    {
		float sum = 0;
		foreach (var pair in elements)
		{
			sum += pair.Value;
		}

		float lastAngle = 0;
		foreach (var pair in elements)
		{
			float proportion = pair.Value/sum;

			float newAngle = 360f * proportion;
			DrawCircleArcPoly(Size/2, Mathf.Min(Size.X, Size.Y)/2, lastAngle, lastAngle + newAngle, colors[pair.Key]);
			lastAngle += newAngle;
		}
		
    }
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			QueueRedraw();
		}
	}
}
