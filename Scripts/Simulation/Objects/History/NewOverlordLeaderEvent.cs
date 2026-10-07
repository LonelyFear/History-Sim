using MessagePack;
using PixelHistory.Objects.States.Base;

[MessagePackObject(AllowPrivate = true)]
public class NewOverlordLeaderEvent : HistoricalEvent
{
    public NewOverlordLeaderEvent(){}
    public NewOverlordLeaderEvent(State state, Character character, State vassal)
    {
        text = $"{NamedObject.GenerateUrlText(character, character.name)} became the new leader of the {NamedObject.GenerateUrlText(state, state.name)}, liege of the {NamedObject.GenerateUrlText(vassal, vassal.name)}.";
        vassal.historicalEvents.Add(this);
    }
}