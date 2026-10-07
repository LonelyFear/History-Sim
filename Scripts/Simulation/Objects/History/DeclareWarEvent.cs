using System.Collections.Generic;
using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.Wars;

[MessagePackObject(AllowPrivate = true)]
public class DeclareWarEvent : HistoricalEvent
{
    public DeclareWarEvent(){}
    public DeclareWarEvent(State agressor, State target, War war)
    {
        switch (war.warType)
        {
            case WarType.CIVIL_WAR:
                int rebellingVassalCount = war.attackers.Count - 1;
                string additionalText = "";
                if (rebellingVassalCount > 1)
                {
                    additionalText = $" and {rebellingVassalCount} other vassal{(rebellingVassalCount != 1 ? "s" : "")}";
                }

                text = $"Due to low stability, the {NamedObject.GenerateUrlText(agressor, agressor.name)}{additionalText} led a civil war against the {NamedObject.GenerateUrlText(target, target.name)}.";
                break;
            default:
                text = $"The {NamedObject.GenerateUrlText(agressor, agressor.name)} declared war on the {NamedObject.GenerateUrlText(target, target.name)}.";
                break;
        }
        foreach (State state in war.participants)
        {
            state.historicalEvents.Add(this);
        }
    }
        
}