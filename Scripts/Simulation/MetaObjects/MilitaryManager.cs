using Godot;

static class MilitaryManager
{
    public static void Recruitment(this State state)
    {
        long mp = 0;
        foreach (Region region in state.regions)
        {
            mp += (long)(region.workforce * state.mobilizationRate);
        }
        state.manpower = mp;
        //GD.Print(state.GetArmyPower());
    }
}