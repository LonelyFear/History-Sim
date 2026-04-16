using Godot;

public partial class InfoTab : BaseEncyclopediaTab
{
	[Export] Label objName;
	[Export] Label objType;
	[Export] RichTextLabel objDesc;
	[Export] RichTextLabel objStats;
	[Export] RichTextLabel objHist;
	public ObjectType objectType;
	public NamedObject loadedObj;
	
	public override void _Ready() {
		objDesc.MetaClicked += encyclopediaManager.OpenTab;
		objHist.MetaClicked += encyclopediaManager.OpenTab;
		objStats.MetaClicked += encyclopediaManager.OpenTab;		
	}
	public override void InitTab()
	{
		objName.Text = loadedObj.name;
		objType.Text = objectType.ToString().Capitalize();
		objDesc.Text = loadedObj.GenerateDescription();
		objStats.Text = loadedObj.GenerateStatsText();
		objHist.Text = loadedObj.GenerateHistoryText();
	}
}
