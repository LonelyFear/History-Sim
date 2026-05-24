using Godot;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

[GlobalClass]
public partial class ObjectInfo : Control
{
    [Export] SelectionManager selectionManager;
    [Export] SimManagerHolder simHolder;
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
        simHolder.simStartEvent += GetSimManager;
        mapManager = GetNode<MapManager>("/root/Game/Map Manager"); 
        timeManager = GetNode<TimeManager>("/root/Game/Time Manager");
        specialLabel.MetaClicked += OnMetaClicked;
        encyclopediaButton.Pressed += OnEncyclopediaClicked;
    }
    public void GetSimManager()
    {
        simManager = simHolder.simManager;
        objectManager = simManager.objectManager;
    }

    public override void _Process(double delta)
    {
        if (!selectionManager.IsRegionSelected())
        {
            Visible = false;
            return;
        }
        Visible = true;

        // Otherwise We Are Visible
        Culture selectedCulture = selectionManager.GetSelectedCulture();
        State selectedState = selectionManager.GetSelectedState();
        Alliance selectedAlliance = selectionManager.GetSelectedAlliance(AllianceType.ALLIANCE);
        switch (mapManager.mapMode)
        {
            case MapModes.REALM:
                if (selectedState != null)
                {
                    DisplayPolityText(selectionManager.GetSelectedPolity(), selectedState.diplomacy.GetOverlord()); 
                    selectedObject = selectionManager.GetSelectedPolity();                    
                } else
                {
                    DisplayRegionText(selectionManager.GetSelectedRegion());
                }

                break;
            case MapModes.POLITIY:
                if (selectedState != null)
                {
                    DisplayPolityText(selectedState, selectedState); 
                    selectedObject = selectedState;                    
                } else
                {
                    DisplayRegionText(selectionManager.GetSelectedRegion());
                }
                break;
            case MapModes.ALLIANCE:
                if (selectedState != null)
                {
                    if (selectedAlliance != null)
                    {
                        DisplayAllianceText(selectedAlliance);
                        selectedObject = selectedAlliance;
                    } 
                    else
                    {
                        DisplayPolityText(selectedState, selectedState); 
                        selectedObject = selectedState;                           
                    }
                 
                } else
                {
                    DisplayRegionText(selectionManager.GetSelectedRegion());
                }
            break; 
            case MapModes.CULTURE:
                if (selectedCulture != null)
                {
                    DisplayCultureText(selectedCulture);
                }
                break;   
            default:
                DisplayRegionText(selectionManager.GetSelectedRegion());
                break;
        }
    }
    public void DisplayCultureText(Culture culture){
        nameLabel.Text = culture.name;   
        typeLabel.Text = culture.GetType().ToString();
        selectedObject = culture;

        populationLabel.Text = $"Population: {culture.population:#,###0}";
        specialLabel.Text = "";
    }
    public void DisplayRegionText(Region region)
    {
        nameLabel.Text = region.name;   
        typeLabel.Text = region.GetType().ToString();
        selectedObject = region;

        populationLabel.Text = "Population: " + region.population.ToString("#,###0");

        populationLabel.Text += "\n" + "Wealth: " + region.wealth.ToString("#,###0");
        populationLabel.Text += "\n" + "Trade Weight: " + region.tradeWeight.ToString("#,###0");

        specialLabel.Text = "Ariable Land Ratio: " + (region.arableLand / region.landCount).ToString("0.0%") + "\n";
        specialLabel.Text += "Average Wealth: " + region.wealth.ToString("#,##0.0");
        specialLabel.Text = "Average Temperature: " + region.avgTemperature.ToString("0.0C") + "\n";
        specialLabel.Text += "Average Rainfall: " + region.avgRainfall.ToString("#,###0 mm") + "\n";
        specialLabel.Text += "Average Elevation: " + region.avgElevation.ToString("#,###0 meters") + "\n";
        specialLabel.Text += "Biomes: \n";
        foreach (var pair in region.biomes.OrderByDescending(bp => bp.Value))
        {
            Biome biome = AssetManager.GetBiome(pair.Key);
            int amount = pair.Value;
            specialLabel.Text += $"  {biome.name}: " + amount.ToString("#,###0\n");
        } 
        specialLabel.Text += "\nNatural Resources: \n";
        foreach (var pair in region.naturalResources)
        {
            NaturalResource resource = AssetManager.GetNaturalResource(pair.Key);
            specialLabel.Text += $"  {resource.name}: " + pair.Value.ToString("#,##0\n");
        } 
        specialLabel.Text += "\nEconomy: \n";
        foreach (var pair in region.economy.prices.OrderByDescending(bp => bp.Value))
        {
            Item item = AssetManager.GetItem(pair.Key);
            float price = pair.Value;
            specialLabel.Text += $"  {item.name}: " + price.ToString("$#,###0.00\n");
            //specialLabel.Text += $"     Supply: " + region.economy.supply[item.id].ToString("#,###0\n");
            //specialLabel.Text += $"     Demand: " + region.economy.demand[item.id].ToString("#,###0\n");
        }       
    }
    public void DisplayPolityText(Polity polity, State state)
    {
        if (polity == null || state == null) return;

        selectedObject = polity;

        nameLabel.Text = state.baseName;   
        typeLabel.Text = polity.GetType().ToString();
        //--------------------------------------------------
        populationLabel.Text = "Population: " + polity.population.ToString("#,###0");

        uint yearAge = timeManager.GetYear(polity.GetAge());
        uint monthAge = timeManager.GetMonth(polity.GetAge());

        specialLabel.Text = $"Founded in Month {timeManager.GetMonth(polity.tickCreated)} of Year {timeManager.GetYear(polity.tickCreated)}";
        specialLabel.Text += "\n" + $"Age {yearAge} year(s), {monthAge} month(s)";  

        specialLabel.Text += "\n" + "Total Wealth: " + polity.totalWealth.ToString("#,###0");
        specialLabel.Text += "\n" + "Military Power: " + polity.GetArmyPower().ToString("#,###0") + "\n"; 

        if (state.leader != null) {
            Character leader = state.leader;
            specialLabel.Text += "\n" + $"Leader: {state.leaderTitle} {leader.firstName + " " + leader.lastName}";
            specialLabel.Text += "\n" + $"Leader Age: {timeManager.GetYear(leader.GetAge())} year(s)" + "\n";
        } else {
            specialLabel.Text += "\n" + "Leader: None";
        }
        specialLabel.Text += "\n" + "Stability: " + state.stability.ToString("##0%");
        
        // Wars text
        specialLabel.Text += "\n" + "Wars: ";
        if (!state.diplomacy.wars.IsEmpty) {
            foreach (War war in state.diplomacy.wars.Keys.ToArray()) {
                if (war == null) continue;

                yearAge = timeManager.GetYear(war.GetAge());
                monthAge = timeManager.GetMonth(war.GetAge());
                specialLabel.Text += "\n" + $"{war.name}";
                specialLabel.Text += "\n" + $"Agressor: [color=blue][url=s{war.warLeaderIds[War.WarSide.AGRESSOR]}]{objectManager.GetState(war.warLeaderIds[War.WarSide.AGRESSOR]).name}[/url][/color]";
                specialLabel.Text += "\n" + $"Defender: [color=blue][url=s{war.warLeaderIds[War.WarSide.DEFENDER]}]{objectManager.GetState(war.warLeaderIds[War.WarSide.DEFENDER]).name}[/url][/color]";
                specialLabel.Text += "\n" + $"Age: {yearAge} year(s), {monthAge} month(s)"; ;
            }
        } else {
            specialLabel.Text += "\n" + "At Peace";
        }         
    }
    public void DisplayAllianceText(Alliance alliance)
    {
        if (alliance == null) return;

        selectedObject = alliance;

        nameLabel.Text = alliance.name;   
        typeLabel.Text = alliance.GetType().ToString();
        //--------------------------------------------------
        populationLabel.Text = "Population: " + alliance.population.ToString("#,###0");

        uint yearAge = timeManager.GetYear(alliance.GetAge());
        uint monthAge = timeManager.GetMonth(alliance.GetAge());

        specialLabel.Text = $"Formed in Month {timeManager.GetMonth(alliance.tickCreated)} of Year {timeManager.GetYear(alliance.tickCreated)}";
        specialLabel.Text += "\n" + $"Age {yearAge} year(s), {monthAge} month(s)";  

        specialLabel.Text += "\n" + "Member States: " + alliance.memberStates.Count.ToString("#,###0") + "\n"; 

        specialLabel.Text += "\n" + "Total Wealth: " + alliance.totalWealth.ToString("#,###0");
        specialLabel.Text += "\n" + "Military Power: " + alliance.GetArmyPower().ToString("#,###0") + "\n"; 
        
        // Wars text
        specialLabel.Text += "\n" + "Wars: ";
        if (!alliance.leadState.diplomacy.wars.IsEmpty) {
            foreach (War war in alliance.leadState.diplomacy.wars.Keys.ToArray()) {
                if (war == null) continue;

                yearAge = timeManager.GetYear(war.GetAge());
                monthAge = timeManager.GetMonth(war.GetAge());
                specialLabel.Text += "\n" + $"{war.name}";
                specialLabel.Text += "\n" + $"Agressor: [color=blue][url=s{war.warLeaderIds[War.WarSide.AGRESSOR]}]{objectManager.GetState(war.warLeaderIds[War.WarSide.AGRESSOR]).name}[/url][/color]";
                specialLabel.Text += "\n" + $"Defender: [color=blue][url=s{war.warLeaderIds[War.WarSide.DEFENDER]}]{objectManager.GetState(war.warLeaderIds[War.WarSide.DEFENDER]).name}[/url][/color]";
                specialLabel.Text += "\n" + $"Age: {yearAge} year(s), {monthAge} month(s)"; ;
            }
        } else {
            specialLabel.Text += "\n" + "At Peace";
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
                selectionManager.SelectRegion(simManager.statesIds[id].capital);
                break;
        }
    }
    public void OnEncyclopediaClicked()
    {
        encyclopediaManager.OpenEncyclopedia();
        encyclopediaManager.OpenTab(selectedObject.GetFullId());
    }
}