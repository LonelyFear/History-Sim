using System;
using MessagePack;
[MessagePackObject]
public struct Tech
{
    [Key(0)] public int militaryLevel { get; set; } = 0;
    [Key(1)] public int scienceLevel { get; set; } = 0;
    [Key(2)] public int societyLevel { get; set; } = 0;
    [Key(3)] public int industryLevel { get; set; } = 0;

    public Tech() { }
    public Tech(int milLevel, int sciLevel, int socLevel, int indLevel)
    {
        militaryLevel = milLevel;
        scienceLevel = sciLevel;
        societyLevel = socLevel;
        industryLevel = indLevel;
    }
    public static bool SameTech(Tech a, Tech b)
    {
        bool sameMil = a.militaryLevel == b.militaryLevel;
        bool sameSoc = a.societyLevel == b.societyLevel;
        bool sameSci = a.scienceLevel == b.scienceLevel;
        bool sameInd = a.industryLevel == b.industryLevel;
        return sameMil && sameSoc && sameInd && sameSci;
    }

    // public Tech Clone()
    // {
    //     return new Tech()
    //     {
    //         militaryLevel = militaryLevel,
    //         scienceLevel = scienceLevel,
    //         societyLevel = societyLevel,
    //         industryLevel = industryLevel
    //     };
    // }
}
