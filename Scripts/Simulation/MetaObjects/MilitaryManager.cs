using Godot;

static class MilitaryManager
{
    public static void Recruitment(this State state)
    {
        if (state.manpower > state.workforce)
        {
            state.manpower = state.workforce;
        }
        if (state.professions.ContainsKey(Profession.FARMER) && state.professions.ContainsKey(Profession.MERCHANT))
        {
            state.manpowerTarget = (long)Mathf.Round((state.professions[Profession.FARMER] + state.professions[Profession.MERCHANT]) * 0.25);
            state.manpower = (long)Mathf.Lerp(state.manpower, state.manpowerTarget, 0.05);
        }
        state.UpdateArmyManpower();
    }

    public static void UpdateArmyManpower(this State state)
    {
        foreach (Army army in state.armies)
        {
            
        }
    }
}