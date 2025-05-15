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
        nameLabel = GetNode<Label>("ObjectInfo/VBoxContainer/Name");
        typeLabel = GetNode<Label>("ObjectInfo/VBoxContainer/Type");
        populationLabel = GetNode<Label>("ObjectInfo/VBoxContainer/Population");
        specialLabel = GetNode<Label>("ObjectInfo/VBoxContainer/Manpower");
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
                    specialLabel.Text = "Manpower: " + Pop.FromNativePopulation(state.manpower).ToString("#,###0");
                    break;
                case PopObject.ObjectType.REGION:
                    Region region = (Region)metaObject;
                    nameLabel.Text = "Region";
                    specialLabel.Text = "Fertility: " + region.avgFertility.ToString("0.00");
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
