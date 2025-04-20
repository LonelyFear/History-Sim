using System;

public struct Tech
{
    public ushort militaryLevel;
    public ushort scienceLevel;
    public ushort societyLevel;
    public ushort industryLevel;

    public static bool sameTech(Tech a, Tech b){
        bool sameMil = a.militaryLevel == b.militaryLevel;
        bool sameSoc = a.societyLevel == b.societyLevel;
        bool sameSci = a.scienceLevel == b.scienceLevel;
        bool sameInd = a.industryLevel == b.industryLevel;
        return sameMil && sameSoc && sameInd && sameSci;
    }
}
