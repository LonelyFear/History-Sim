using MessagePack;
using PixelHistory.Objects.States.Base;
using PixelHistory.Objects.Wars;

[MessagePackObject(AllowPrivate = true)]
public class EndWarEvent : HistoricalEvent
{
    public EndWarEvent(){}
    public EndWarEvent(War war, bool whitePeace = false)
    {
        if (war.victor != War.WarSide.NEUTRAL)
        {
            switch (war.warType)
            {
                case WarType. CIVIL_WAR:
                    if (war.victor == War.WarSide.AGRESSOR)
                    {
                        text = $"The {NamedObject.GenerateUrlText(war, war.name)} ended in a rebel victory. The {NamedObject.GenerateUrlText(war.defenderLeader, war.defenderLeader.name)} has collapsed!"; 
                    } 
                    else
                    {
                        text = $"The {NamedObject.GenerateUrlText(war, war.name)} ended in a government victory."; 
                    }   
                    break;
                default:
                    if (war.victor == War.WarSide.AGRESSOR && war.attackerLeader != null)
                    {
                        text = $"The {NamedObject.GenerateUrlText(war, war.name)} ended in an attacker victory. The {NamedObject.GenerateUrlText(war.attackerLeader, war.attackerLeader.name)} annexed its conquered lands."; 
                    } else if (war.victor == War.WarSide.DEFENDER && war.defenderLeader != null)
                    {
                        text = $"The {NamedObject.GenerateUrlText(war, war.name)} ended in a defender victory. The {NamedObject.GenerateUrlText(war.defenderLeader, war.defenderLeader.name)} annexed its conquered lands."; 
                    } else
                    {
                        text = $"The {NamedObject.GenerateUrlText(war, war.name)} ended";
                    }  
                    break;
            }
        } 
        else
        {
            if (whitePeace)
            {
                text = $"The {NamedObject.GenerateUrlText(war, war.name)} ended in a status quo ante bellum."; 
            } else
            {
                text = $"The {NamedObject.GenerateUrlText(war, war.name)} ended in a draw.";   
            }       
        }

        foreach (State state in war.participants)
        {
            state.historicalEvents.Add(this);
        }
    }
        
}