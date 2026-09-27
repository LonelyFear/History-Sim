using Godot;
using PixelHistory.Objects.States.Base;
using System;
using System.Linq;

public partial class EconomyBox : InfoPanelBox
{
	[Export] Label wealthLabel;
	[Export] RichTextLabel tradeZoneLabel;
	
	public override void _Process(double delta)
	{
		Visible = (infoHolder.selectedObject is Polity) || (infoHolder.selectedObject is Region) || (infoHolder.selectedObject is TradeZone);	
		if (!Visible) return;

		wealthLabel.Text = "Wealth: ";
		switch (infoHolder.selectedObject) {
			case Region r:
				wealthLabel.Text += r.wealth.ToString("#,##0.0");
				break;
			case Polity p:
				wealthLabel.Text += p.totalWealth.ToString("#,##0.0");
				break;
			case TradeZone tz:
				wealthLabel.Text += tz.regions.Sum(r => r.wealth).ToString("#,##0.0");
				break;
		}

		tradeZoneLabel.Visible = !(infoHolder.selectedObject is TradeZone);
		Region zoneRegion = infoHolder.selectedObject as Region;
        if (zoneRegion == null)
        {
            if (infoHolder.selectedObject is State s)
            {
                zoneRegion = s.capital;
            }
            else if (infoHolder.selectedObject is Alliance a)
            {
                zoneRegion = a?.
				leadState?.
				capital;
            }
        }
		tradeZoneLabel.Text = $"Trade Zone: {(zoneRegion?.tradeZone == null ? "None" : NamedObject.GenerateUrlText(zoneRegion.tradeZone, zoneRegion.tradeZone.name))}";
    }
}
