using System;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Tech
{
    public int militaryLevel { get; set; }
    public int scienceLevel { get; set; }
    public int societyLevel { get; set; }
    public int industryLevel { get; set; }

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
        return new Tech()
        {
            militaryLevel = militaryLevel,
            scienceLevel = scienceLevel,
            societyLevel = societyLevel,
            industryLevel = industryLevel
        };
    }
}
