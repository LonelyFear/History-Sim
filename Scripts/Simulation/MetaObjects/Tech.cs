using System;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public struct Tech
{
    public float militaryLevel { get; set; }
    public float scienceLevel { get; set; }
    public float societyLevel { get; set; }
    public float industryLevel { get; set; }

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
