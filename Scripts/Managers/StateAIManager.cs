using PixelHistory.Objects.States.Base;

public static class StateAIManager
{
    public static void ChangeRelations(State state)
    {
        foreach (var pair in state.relations)
        {
            State target = pair.Key;
            DiplomaticRelations relations = pair.Value;
        }
    }
}