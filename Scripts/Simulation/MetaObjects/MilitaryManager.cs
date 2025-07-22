using Godot;

static class MilitaryManager
{
    public static void Recruitment(this State state)
    {
        state.manpower = state.professions[Profession.SOLDIER];
    }
}