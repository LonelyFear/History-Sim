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
            nameLabel.Text = metaObject.name;
            typeLabel.Text = metaObject.GetType().ToString();
            populationLabel.Text = "Population: " + Pop.FromNativePopulation(metaObject.population).ToString("#,###0");
        }
    }

}
