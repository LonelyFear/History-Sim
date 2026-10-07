using MessagePack;
using PixelHistory.Objects.States.Base;

[MessagePackObject(AllowPrivate = true)]
public class CharacterDeathEvent : HistoricalEvent
{
    public CharacterDeathEvent(){}
    public CharacterDeathEvent(Character character)
    {
        State state = character.state;
        text = $"{NamedObject.GenerateUrlText(character, character.name)} died at age {TimeManager.GetYear(character.GetAge())}.";
        if (state?.leader == character)
        {
            text += $" He was the leader of the {NamedObject.GenerateUrlText(state, state.name)}";
            state.historicalEvents.Add(this);
        }
        character.historicalEvents.Add(this);
    }
}