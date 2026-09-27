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
		objStats.Text = loadedObj.GenerateStatsText();
		objHist.Text = loadedObj.GenerateHistoryText();

		PopObject popObject = loadedObj as PopObject;
		chartsContainer.Visible = popObject != null && popObject.population > 0;
		if (chartsContainer.Visible)
		{
			// Culture Chart
			cultureChart.Clear();
			cultureList.Text = "";
			foreach (var pair in popObject.cultureIds.OrderBy(c => c.Value))
			{
				Culture culture = ObjectManager.GetCulture(pair.Key);
				float cultureSize = pair.Value;
				cultureChart.AddElement(culture.name, cultureSize, culture.color);
				cultureList.Text += $"{NamedObject.GenerateUrlText(culture, "■ " + culture.name, culture.color)} - {cultureSize/popObject.population:0%}\n";
			}
			cultureChart.QueueRedraw();
		}
	}
}
