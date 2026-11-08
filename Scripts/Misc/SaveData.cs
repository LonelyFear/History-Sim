using System;
using Godot;
[Serializable]
public class SaveData()
{
    public string saveName { get; set; } = "New Save";
    public string saveId { get; set; } = "unknown-auto";
    public double saveTimestamp { get; set; } = Time.GetUnixTimeFromSystem();
    public string saveFormat { get; set; } = SaveFormat;
    public const string SaveFormat = "save-a3";

    public bool IsAutosave()
    {
        return saveId.EndsWith("-auto");
    }
}