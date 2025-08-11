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
    TimeManager timeManager;

    public override void _Ready()
    {
        mapManager = GetNode<MapManager>("/root/Game/Map Manager");
        timeManager = GetNode<TimeManager>("/root/Game/Time Manager");
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
                    uint yearAge = timeManager.GetYear(state.age);
                    uint monthAge = timeManager.GetMonth(state.age);
                    specialLabel.Text = $"Founded in {timeManager.GetYear(state.tickFounded)} AC";
                    specialLabel.Text +=  "\n" + $"Age {yearAge} year(s), {monthAge} month(s)";
                    specialLabel.Text += "\n" + "Manpower: " + Pop.FromNativePopulation(state.manpower).ToString("#,###0");
                    specialLabel.Text += "\n" + "Military Power: " + Pop.FromNativePopulation(state.GetArmyPower()).ToString("#,###0") + "\n";
                    specialLabel.Text += "\n" + "Relations: ";
                    foreach (var pair in state.relations)
                    {
                        State subject = pair.Key;
                        Relation relation = pair.Value;
                        
                        if (state.vassals.Contains(subject))
                        {
                            specialLabel.Text += "\n" + $"{subject.displayName}: Vassal";
                        }
                        else
                        {
                            specialLabel.Text += "\n" + $"{subject.displayName}: {relation.opinion}";
                        }
                    }
                    break;
                case PopObject.ObjectType.REGION:
                    Region region = (Region)metaObject;
                    populationLabel.Text += "\nRequired Farmers: " + Pop.FromNativePopulation(region.maxFarmers - region.professions[Profession.FARMER]);
                    populationLabel.Text += "\n" + "Trade Weight: " + region.GetTradeWeight().ToString("#,###0.0");
                    populationLabel.Text += "\n" + "    Pop Trade Weight: " + (Pop.FromNativePopulation(region.workforce) * 0.0004f).ToString("#,###0");
                    if (region.tradeLink == null)
                    {
                        populationLabel.Text += "\n" + "    Zone Weight: " + (region.zoneSize * 4f).ToString("#,###0");
                    }
                    else
                    {
                        populationLabel.Text += "\n" + "    Zone Weight: 0";
                    }
                    populationLabel.Text += "\n" + "    Nav Weight: " + (region.navigability * 10f).ToString("#,###0");
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
