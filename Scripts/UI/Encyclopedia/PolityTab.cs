using Godot;
using System;
using System.Linq;

public partial class PolityTab : BaseEncyclopediaTab
{
	[Export] Label objName;
	[Export] Label objType;
	[Export] RichTextLabel objDesc;
	[Export] RichTextLabel objStats;
	[Export] RichTextLabel objHist;
	[Export] PieChart culturesChart;
	[Export] Button mapButton;
	public Polity polity;
	
    public override void InitTab()
    {
		objName.Text = polity.name;
		objType.Text = polity.GetObjectType().ToString();
		objDesc.Text = polity.GenerateDescription();
		objStats.Text = GetStats();
		objHist.Text = polity.GenerateHistoryText();
		mapButton.Pressed += () => encyclopediaManager.OpenMap(polity);
		InitCulturePieChart();
    }
	public string GetStats()
	{
        string text = $"Name: {polity.name}";
        text += $"\nPopulation: {polity.population:#,###0}\n";
        
        if (polity.population > 0)
        {
            text += $"Cultures Breakdown:\n";

            foreach (var cultureSizePair in polity.cultureIds.OrderByDescending(pair => pair.Value))
            {
                Culture culture = ObjectManager.GetCulture(cultureSizePair.Key);
                long localPopulation = cultureSizePair.Value;
                
                // Skips if the culture is too small
                if (localPopulation < 1) continue;

                text += NamedObject.GenerateUrlText(culture, culture.name) + ":\n";
                text += $"  Population: {localPopulation:#,###0} ";

                float culturePercentage = localPopulation/(float)polity.population;
                text += $"({culturePercentage:P0})\n";
            }    
            text += $"\nWorkforce: {polity.workforce:#,###0}\n";    
        }
        return text;		
	}
	public override void _Ready() {
		objDesc.MetaClicked += encyclopediaManager.OpenTab;
		objHist.MetaClicked += encyclopediaManager.OpenTab;
		objStats.MetaClicked += encyclopediaManager.OpenTab;		
	}
	public void InitCulturePieChart()
	{
		culturesChart.Clear();
		
		foreach (var pair in polity.cultureIds)
		{
			Culture culture = ObjectManager.GetCulture(pair.Key);
			culturesChart.AddElement(culture.name, pair.Value, culture.color);
		}
		//culturesChart.AddElement("Placeholder Culture So Pie Chart Works-ism", 1f, Color.FromString("Red", new()));
		culturesChart.QueueRedraw();
	}
}
