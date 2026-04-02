using System;
[Serializable]
public class SaveData()
{
    public string saveName { get; set; } = "New Save";
    public string saveID { get; set; } = "Save1";
    public string saveVersion { get; set; } = validSaveVersion;
    public const string validSaveVersion = "alpha-1";
}