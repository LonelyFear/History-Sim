using Godot;

static class MilitaryManager
{
    public static void Recruitment(this State state)
    {
        state.manpower = 0;
        foreach (Region region in state.regions)
        {
            state.manpower += (long)(region.workforce * state.mobilizationRate * region.control);
        }
        
    }
}