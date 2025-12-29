using Godot;
using System;
using System.Linq;

public partial class ObjectInfo : Control
{
    [Export] Panel panel;
    [Export] Label nameLabel;
    [Export] Label typeLabel;
    [Export] Label populationLabel;
    [Export] RichTextLabel specialLabel;
    [Export] Button encyclopediaButton;
    [Export] EncyclopediaManager encyclopediaManager;
    MapManager mapManager;
    SimManager simManager;
    ObjectManager objectManager;
    TimeManager timeManager;
    NamedObject selectedObject;

    public override void _Ready()
    {
        GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += GetSimManager;
        mapManager = GetNode<MapManager>("/root/Game/Map Manager"); 
        timeManager = GetNode<TimeManager>("/root/Game/Time Manager");
        specialLabel.MetaClicked += OnMetaClicked;
        encyclopediaButton.Pressed += OnEncyclopediaClicked;
    }
    public void GetSimManager()
    {
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        objectManager = simManager.objectManager;
    }

    public override void _Process(double delta)
    {
        try
        {
            if (!mapManager.initialized || mapManager == null || mapManager.selectedMetaObj == null) {
                panel.Visible = false;
            } else {
                selectedObject = mapManager.selectedMetaObj;

                panel.Visible = true;
                typeLabel.Text = selectedObject.GetType().ToString();
                nameLabel.Text = selectedObject.name;
                switch (selectedObject.GetObjectType()) {
                    case ObjectType.STATE:
                        populationLabel.Text = "Population: " + Pop.FromNativePopulation(((PopObject)selectedObject).population).ToString("#,###0");
                        State state = (State)selectedObject;
                        switch (state.vassalManager.sovereignty)
                        {
                            case Sovereignty.COLONY:
                                typeLabel.Text = "Colonial State";
                                break;
                            case Sovereignty.PROVINCE:
                                typeLabel.Text = "Provincial State";
                                break;
                            case Sovereignty.PUPPET:
                                typeLabel.Text = "Puppet State";
                                break;
                            case Sovereignty.INDEPENDENT:
                                typeLabel.Text = "Sovereign State";
                                break;
                        }

                        uint yearAge = timeManager.GetYear(state.GetAge());
                        uint monthAge = timeManager.GetMonth(state.GetAge());
                        specialLabel.Text = $"Founded in Month {timeManager.GetMonth(state.tickCreated)} of Year {timeManager.GetYear(state.tickCreated)}";
                        specialLabel.Text += "\n" + $"Age {yearAge} year(s), {monthAge} month(s)";
                        specialLabel.Text += "\n" + "Wealth: " + state.totalWealth.ToString("#,###0");
                        specialLabel.Text += "\n" + "Military Power: " + Pop.FromNativePopulation(state.GetArmyPower(true)).ToString("#,###0") + "\n";
                        if (state.leaderId != null)
                        {
                            Character leader = simManager.charactersIds[(ulong)state.leaderId];
                            specialLabel.Text += "\n" + $"Leader: {state.leaderTitle} {leader.firstName + " " + leader.lastName}";
                            specialLabel.Text += "\n" + $"Leader Age: {timeManager.GetYear(leader.GetAge())} year(s)" + "\n";
                        } else
                        {
                            specialLabel.Text += "\n" + "Leader: None";
                        }

                        specialLabel.Text += "\n" + "Stability: " + state.stability.ToString("##0%");
                        if (state.vassalManager.liegeId != null)
                        {
                            specialLabel.Text += "\n" + "Loyalty: " + state.loyalty.ToString("##0%");
                        }

                        // Wars text
                        specialLabel.Text += "\n" + "Wars: ";
                        if (state.diplomacy.warIds.Count > 0)
                        {
                            foreach (War war in state.diplomacy.warIds.Keys.Select(id => objectManager.GetWar(id)).ToArray())
                            {
                                yearAge = timeManager.GetYear(war.GetAge());
                                monthAge = timeManager.GetMonth(war.GetAge());
                                specialLabel.Text += "\n" + $"{war.name}";
                                specialLabel.Text += "\n" + $"Agressor: [color=blue][url=s{war.primaryAgressorId}]{objectManager.GetState(war.primaryAgressorId).name}[/url][/color]";
                                specialLabel.Text += "\n" + $"Defender: [color=blue][url=s{war.primaryDefenderId}]{objectManager.GetState(war.primaryDefenderId).name}[/url][/color]";
                                specialLabel.Text += "\n" + $"Age: {yearAge} year(s), {monthAge} month(s)"; ;
                            }
                        }
                        else
                        {
                            specialLabel.Text += "\n" + "At Peace";
                        }
                        break;
                    case ObjectType.REGION:
                        populationLabel.Text = "Population: " + Pop.FromNativePopulation(((PopObject)selectedObject).population).ToString("#,###0");
                        Region region = (Region)selectedObject;
                        populationLabel.Text += "\n" + "Wealth: " + region.wealth.ToString("#,###0");
                        populationLabel.Text += "\n" + "Trade Weight: " + region.GetTradeWeight().ToString("#,###0");

                        specialLabel.Text = "Ariable Land Ratio: " + (region.arableLand / region.landCount).ToString("0.0%") + "\n";
                        specialLabel.Text += "Average Wealth: " + region.wealth.ToString("#,##0.0");
                        specialLabel.Text = "Average Temperature: " + region.avgMonthlyTemps[timeManager.GetMonth() - 1].ToString("0.0C") + "\n";
                        specialLabel.Text += "Average Rainfall: " + region.avgMonthlyRainfall[timeManager.GetMonth() - 1].ToString("#,###0 mm") + "\n";
                        specialLabel.Text += "Average Elevation: " + region.avgElevation.ToString("#,###0 meters");
                        break;
                    case ObjectType.CULTURE:
                        populationLabel.Text = "Population: " + Pop.FromNativePopulation(((PopObject)selectedObject).population).ToString("#,###0");
                        Culture culture = (Culture)selectedObject;
                        specialLabel.Text = "Pops: " + culture.pops.Count.ToString("#,###0");
                        break;
                }
            }            
        } catch (Exception e)
        {
            GD.PushError(e);
        }
    }

    public void OnMetaClicked(Variant meta)
    {
        string metaData = meta.ToString();
        switch (metaData[0])
        {
            case 's':
                ulong id = ulong.Parse(metaData.Substring(1));
                // That means that it is a state
                mapManager.SelectMetaObject(simManager.statesIds[id]);
                break;
        }
    }
    
    public void OnEncyclopediaClicked()
    {
        encyclopediaManager.OpenEncyclopedia();
        encyclopediaManager.OpenTab(selectedObject.GetFullId());
    }
}