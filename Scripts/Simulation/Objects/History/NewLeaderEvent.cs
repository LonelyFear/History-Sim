using MessagePack;
using PixelHistory.Objects.States.Base;

[MessagePackObject(AllowPrivate = true)]
public class NewLeaderEvent : HistoricalEvent
{
    public NewLeaderEvent(){}
    public NewLeaderEvent(State state, Character character)
    {
        text = $"{NamedObject.GenerateUrlText(character, character.name)} became the new leader of the {NamedObject.GenerateUrlText(state, state.name)}.";
        character.historicalEvents.Add(this);
        state.historicalEvents.Add(this);
    }
}