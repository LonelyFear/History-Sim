using System;
using System.Data.Common;
using System.Runtime.Serialization;
using MessagePack;
[MessagePackObject]
public struct Tech
{
    [Key(0)] public int militaryLevel { get; set; } = 0;
    [Key(1)] public int scienceLevel { get; set; } = 0;
    [Key(2)] public int societyLevel { get; set; } = 0;
    [Key(3)] public int industryLevel { get; set; } = 0;
    [IgnoreMember] public float fMilitaryLevel { get; set; } = 0;
    [IgnoreMember] public float fScienceLevel { get; set; } = 0;
    [IgnoreMember] public float fSocietyLevel { get; set; } = 0;
    [IgnoreMember] public float fIndustryLevel { get; set; } = 0;
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
    public int GetAdvancement()
    {
        return industryLevel + societyLevel + scienceLevel + militaryLevel;
    }

    public Tech AddTech(Tech tech)
    {
        return new Tech()
        {
            industryLevel = industryLevel + tech.industryLevel,
            scienceLevel = scienceLevel + tech.scienceLevel,
            societyLevel = societyLevel + tech.societyLevel,
            militaryLevel = militaryLevel + tech.militaryLevel,
        };
    }
    public string GetString()
    {
        return $"Soc: {societyLevel} | Mil: {militaryLevel} | Ind: {industryLevel}";
    }
}
