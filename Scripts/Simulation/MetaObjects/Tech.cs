using System;

public class Tech
{
    public int militaryLevel;
    public int scienceLevel;
    public int societyLevel;
    public int industryLevel;

    public static bool sameTech(Tech a, Tech b)
    {
        bool sameMil = a.militaryLevel == b.militaryLevel;
        bool sameSoc = a.societyLevel == b.societyLevel;
        bool sameSci = a.scienceLevel == b.scienceLevel;
        bool sameInd = a.industryLevel == b.industryLevel;
        return sameMil && sameSoc && sameInd && sameSci;
    }

    public Tech Clone()
    {
        return (Tech)MemberwiseClone();
    }
}
