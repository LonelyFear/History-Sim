using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool] [GlobalClass]
public partial class PieChart : Control
{
	[ExportToolButton("Update Chart")] 
	public Callable updateChart => Callable.From(QueueRedraw);

	[ExportToolButton("Clear Chart")] 
	public Callable clearChart => Callable.From(() =>
	{
		Clear();
		QueueRedraw();
	});

	[ExportGroup("New Element")]

	Dictionary<string, PieChartElement> elements = [];
	[Export] string newElementName = "Element 1";
	[Export] float newElementValue = 1;
	[Export] Color newElementColor= new Color("red");

	[ExportToolButton("Add Element")] 
	public Callable addElement => Callable.From(() =>
	{
		AddElement(newElementName, newElementValue, newElementColor);
		newElementName = "Element " + (elements.Count + 1);
		newElementValue = 1;
		newElementColor = new Color("red");
		QueueRedraw();
		GD.Print(elements.Count);
	});
	// Called when the node enters the scene tree for the first time.
	public void AddElement(string name, float value, Color color)
	{
		elements[name] = new PieChartElement()
		{
			value = value,
			color = color
		};
	}

	public void Clear()
	{
		foreach (string key in elements.Keys)
		{
			elements.Remove(key);
		}
	}

	void DrawCircleArcPoly(Vector2 center, float radius, float angleFrom, float angleTo, Color color){
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
		float sum = elements.Sum(element => element.Value.value);
	
		float lastAngle = 0;
		foreach (var pair in elements)
		{
			float proportion = pair.Value.value/sum;

			float newAngle = 359.99f * proportion;
			DrawCircleArcPoly(Size/2, Mathf.Min(Size.X, Size.Y)/2, lastAngle, lastAngle + newAngle, elements[pair.Key].color);
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
