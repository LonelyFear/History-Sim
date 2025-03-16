using Godot;

public partial class Tech : GodotObject
{
    public int militaryLevel;
    public int societyLevel;
    public int industryLevel;

    public static bool sameTech(Tech a, Tech b){
        bool sameMil = a.militaryLevel == b.militaryLevel;
        bool sameSoc = a.societyLevel == b.societyLevel;
        bool sameInd = a.industryLevel == b.industryLevel;
        return sameMil && sameSoc && sameInd;
    }
}
