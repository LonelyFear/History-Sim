using Godot;
using System;

public partial class ObjectInfo : Control
{
    public Panel panel;
    public Label nameLabel;
    public Label typeLabel;
    public Label populationLabel;
    public Label specialLabel;
    public MapManager mapManager;

    public override void _Ready()
    {
        mapManager = GetNode<MapManager>("/root/Game/Map Manager");
        panel = GetNode<Panel>("ObjectInfo");
        nameLabel = GetNode<Label>("ObjectInfo/ScrollContainer/VBoxContainer/Name");
        typeLabel = GetNode<Label>("ObjectInfo/ScrollContainer/VBoxContainer/Type");
        populationLabel = GetNode<Label>("ObjectInfo/ScrollContainer/VBoxContainer/Population");
        specialLabel = GetNode<Label>("ObjectInfo/ScrollContainer/VBoxContainer/Manpower");
    }

    public override void _Process(double delta)
    {
        
        if (!mapManager.initialized || mapManager == null || mapManager.selectedMetaObj == null){
            panel.Visible = false;
        } else {
            PopObject metaObject = mapManager.selectedMetaObj;

            panel.Visible = true;
            typeLabel.Text = metaObject.GetType().ToString();
            populationLabel.Text = "Population: " + Pop.FromNativePopulation(metaObject.population).ToString("#,###0");
            switch (metaObject.GetObjectType()){
                case PopObject.ObjectType.STATE:
                    State state = (State)metaObject;
                    nameLabel.Text = state.displayName;
                    specialLabel.Text = "Manpower: " + Pop.FromNativePopulation(state.manpower).ToString("#,###0") + "\n";
                    foreach (var pair in state.relations)
                    {
                        State subject = pair.Key;
                        Relation relation = pair.Value;
                        specialLabel.Text += "\n" + $"{subject.displayName}: {relation.opinion}";
                    }
                    break;
                case PopObject.ObjectType.REGION:
                    Region region = (Region)metaObject;
                    populationLabel.Text += "\nRequired Farmers: " + Pop.FromNativePopulation(region.maxFarmers - region.professions[Profession.FARMER]);
                    populationLabel.Text += "\nFarmers: " + Pop.FromNativePopulation(region.professions[Profession.FARMER]).ToString("#,###0");
                    populationLabel.Text += "\nMerchants: " + Pop.FromNativePopulation(region.professions[Profession.MERCHANT]).ToString("#,###0");
                    populationLabel.Text += "\nAristocrats: " + Pop.FromNativePopulation(region.professions[Profession.ARISTOCRAT]).ToString("#,###0");
                    populationLabel.Text += "\nSoldiers: " + Pop.FromNativePopulation(region.professions[Profession.SOLDIER]).ToString("#,###0");
                    nameLabel.Text = "Disorganized Tribes";
                    specialLabel.Text = "Ariable Land Ratio: " + (region.arableLand/region.landCount).ToString("0.0%") + "\n";
                    specialLabel.Text += "Average Wealth: " + region.wealth.ToString("#,##0.0");
                    /*
                    specialLabel.Text = "Temperature: " + region.avgTemperature.ToString("0.0C") + "\n";
                    specialLabel.Text += "Rainfall: " + region.avgRainfall.ToString("#,###0 mm") + "\n";
                    specialLabel.Text += "Elevation: " + region.avgElevation.ToString("#,###0 meters");
                    */
                    break;
                case PopObject.ObjectType.CULTURE:
                    Culture culture = (Culture)metaObject;
                    nameLabel.Text = culture.name;
                    specialLabel.Text = "Pops: " + culture.pops.Count.ToString("#,###0");
                    break;
            }
        }
    }

}
