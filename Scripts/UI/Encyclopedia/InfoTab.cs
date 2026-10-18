using System.Linq;
using Godot;

public partial class InfoTab : BaseEncyclopediaTab
{
	[ExportCategory("Base")]
	[Export] Label objName;
	[Export] Label objType;
	[Export] RichTextLabel objDesc;
	[Export] RichTextLabel objStats;
	[Export] RichTextLabel objHist;
	[ExportCategory("Charts")]
	[Export] HBoxContainer chartsContainer;
	[Export] PieChart cultureChart;
	[Export] RichTextLabel cultureList;
	public NamedObject loadedObj;
	
	public override void _Ready() {
		objDesc.MetaClicked += encyclopediaManager.OpenTab;
		objHist.MetaClicked += encyclopediaManager.OpenTab;
		objStats.MetaClicked += encyclopediaManager.OpenTab;	
		cultureList.MetaClicked += encyclopediaManager.OpenTab;	
	}
	public override void InitTab()
	{
		objName.Text = loadedObj.name;
		objType.Text = loadedObj.GetTypeName();
		objDesc.Text = loadedObj.GenerateDescription();
		objStats.Text = GetStatsText();
		objHist.Text = loadedObj.GenerateHistoryText();

		PopObject popObject = loadedObj as PopObject;

		chartsContainer.Visible = popObject != null && popObject.population > 0 && popObject.GetType() != typeof(Culture);
		if (chartsContainer.Visible)
		{
			// Culture Chart
			cultureChart.Clear();
			cultureList.Text = "";
			float remainingPopulation = popObject.population;
			foreach (var pair in popObject.cultures.OrderByDescending(c => c.Value).Take(5))
			{
				Culture culture = pair.Key;
				float cultureSize = pair.Value;
				float proportion = cultureSize/popObject.population;
				if (proportion < 0.01f) break;

				remainingPopulation -= cultureSize;

				cultureChart.AddElement(culture.name, cultureSize, culture.color);
				cultureList.Text += $"{NamedObject.GenerateUrlText(culture, "■ " + culture.name, culture.color)} - {cultureSize:#,##0} ({proportion:0.#%})\n";
			}
			if (remainingPopulation > 0)
			{
				cultureChart.AddElement("Other", remainingPopulation, new Color("white"));
				cultureList.Text += $"■ Other - {remainingPopulation:#,##0}  ({remainingPopulation/popObject.population:0.#%})\n";
			}
			cultureChart.QueueRedraw();
		}
	}
	
	public string GetStatsText()
	{
		string statsText = "";
		switch (loadedObj)
		{
			case PopObject:
				PopObject popObject = loadedObj as PopObject;
				statsText += $"Population: {popObject.population:#,##0}\n";
				if (popObject is Polity polity)
				{
					float farmerMultiplier = 10f;
					float wealthFactor = (farmerMultiplier * polity.baseWealth) + polity.totalWealth;
					float baseWealthProportion = Mathf.Clamp(farmerMultiplier * polity.baseWealth / wealthFactor, 0, 1);
					statsText += $"Farmers: {polity.population * baseWealthProportion:#,##0} - ({baseWealthProportion:0.#%})\n";
					statsText += $"Merchants: {polity.population * (1f - baseWealthProportion):#,##0} - ({1f - baseWealthProportion:0.#%})\n";				
				}
				break;
		}
		return statsText;
	}
}
