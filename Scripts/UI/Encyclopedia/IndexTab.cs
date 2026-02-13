using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class IndexTab : BaseEncyclopediaTab
{
	[Export] LineEdit search;
	[Export] Label indexNameLabel;
	[Export] VBoxContainer resultsContainer;
	[Export] PackedScene resultScene;
	[Export] Label resultsLabel;
	public ObjectType type;
	Dictionary<string, Button> resultDictionary = new Dictionary<string, Button>();
	public static SimManager sim;
	public static EncyclopediaManager encyclopediaManager;
	int currentIdentifier = 0;
	// Called when the node enters the scene tree for the first time.
	public override void InitTab()
	{
		search.TextChanged += OnSearchEditSubmitted;
		switch (type)
		{
			case ObjectType.CHARACTER:
				Name = "Character Index";
				indexNameLabel.Text = "Character Index";
				foreach (Character character in sim.characters)
				{
					CreateResultButton(character);
				}
				break;
			case ObjectType.STATE:
				Name = "State Index";
				indexNameLabel.Text = "State Index";
				foreach (State state in sim.statesIds.Values)
				{
					CreateResultButton(state);
				}
				break;
			case ObjectType.REGION:
				Name = "Region Index";
				indexNameLabel.Text = "Region Index";
				foreach (Region region in sim.regionIds.Values)
				{
					if (!region.habitable) continue;
					CreateResultButton(region);
				}
				break;
		}
		
		resultsLabel.Text = $"Results ({resultDictionary.Count:#,##0}):";
	}
	public void OnResultClicked(string fullId)
    {
        encyclopediaManager.OpenTab(fullId);
    }
	public void CreateResultButton(NamedObject obj)
    {
		// Identifier Updating
		currentIdentifier++;
		if (currentIdentifier > 999999)
        {
            currentIdentifier = 0;
        }
		string identifier = currentIdentifier.ToString("000000");

        // Button initialization
        Button newResult = (Button)resultScene.Instantiate();
        newResult.Pressed += () => OnResultClicked(newResult.Name);
		resultsContainer.AddChild(newResult);
		newResult.Name = obj.GetFullId();
		newResult.Text = obj.name;
		resultDictionary.Add(identifier + obj.name, newResult);        
    }
	public void OnSearchEditSubmitted(string text)
    {
		int resultCount = 0;
        foreach (var pair in resultDictionary)
        {
			string name = pair.Key[6..];
            Button resultButton = pair.Value;
			resultButton.Visible = true;
			resultCount++;
			if (text == "") continue;

			if (!name.Contains(text))
            {
                resultButton.Visible = false;
				resultCount--;
            }
        }
		resultsLabel.Text = $"Results ({resultCount:#,##0}):";
    }
}
