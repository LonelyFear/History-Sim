using System;
using Godot;
[Serializable]
public class SaveData()
{
    public string saveName { get; set; } = "New Save";
    public string saveId { get; set; } = "unknown-auto";
    public double saveTimestamp { get; set; } = Time.GetUnixTimeFromSystem();
    public string saveVersion { get; set; } = validSaveVersion;
    public const string validSaveVersion = "alpha-2";
}