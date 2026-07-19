using System.Collections.Generic;
using System.Text.Json;
using Godot;

public static class SaveManager
{
    public static void CreateSave(string saveName, SimManager sim, WorldGenerator world, string saveId = "")
    {
        DirAccess.MakeDirAbsolute($"user://saves/{saveName}");

        if (saveId.Length < 1) saveId = sim.worldName.ToLower();

        SaveData data = new()
        {
            saveName = saveName,
            saveId = saveId,
        };
        // Creates a new save folder
        string saveDir = $"user://saves/{saveName}";

		// Saves save data
        FileAccess saveDataFile = FileAccess.Open(saveDir.PathJoin("save_data.json"), FileAccess.ModeFlags.Write);
        saveDataFile.StoreString(JsonSerializer.Serialize(data));
		saveDataFile.Dispose(); 

		// Saves world and simulation
		world.SaveTerrainToFile(saveDir);
        sim.SaveSimToFile(saveDir);

        GD.Print($"Created save at user://saves/{saveName}");
    }
    public static void CreateAutoSave(SimManager sim, WorldGenerator world)
    {
        string saveId = $"{sim.worldName.ToLower()}-auto";

        string[] pastAutosaves = GetSavesWithId(saveId);
        string saveName = $"{sim.worldName.ToLower()}-autosave-";
        if (pastAutosaves.Length < 5)
        {
            // Gives us autosaves per world going up to 5
            saveName += pastAutosaves.Length;
        } else
        {
            // Overwrites the oldest autosave
            double oldestTimestamp = double.MaxValue;
            saveName = "you wont ever see this (probably)";
            foreach(string autoSavePath in pastAutosaves)
            {
                SaveData autoSaveData = GetSaveData(autoSavePath);
                double timeStamp = autoSaveData.saveTimestamp;
                if (timeStamp < oldestTimestamp)
                {
                    oldestTimestamp = timeStamp;
                    saveName = autoSaveData.saveName;
                }
            }
        }
        CreateSave(saveName, sim, world, saveId);
    }
    public static string FindSaveWithName(string name)
    {
        foreach (string saveDir in DirAccess.GetDirectoriesAt("user://saves"))
        {
            string savePath = "user://saves/" + saveDir;

            SaveData saveData = GetSaveData(savePath);
            if (saveData.saveName == name)
            {
                return savePath;
            }
        }
        return "";
    }
    public static string[] GetSavesWithId(string id)
    {
        List<string> foundSaves = [];
        foreach (string saveDir in DirAccess.GetDirectoriesAt("user://saves"))
        {
            string savePath = "user://saves/" + saveDir;

            SaveData saveData = GetSaveData(savePath);
            if (saveData.saveId == id)
            {
                foundSaves.Add(savePath);
            }
        }
        return [..foundSaves];        
    }
    public static void DeleteDirectory(string path)
	{
		if (!DirAccess.DirExistsAbsolute(path)) return;

		DirAccess dir = DirAccess.Open(path);
		dir.ListDirBegin();
		string fileName = dir.GetNext();

		while (fileName != "")
		{
			if (dir.CurrentIsDir())
			{
				DeleteDirectory(path.PathJoin(fileName));
			} else
			{
				dir.Remove(fileName);
			}
			fileName = dir.GetNext();
		}
		DirAccess.RemoveAbsolute(path);
	}

    public static bool IsSaveValid(string path)
    {
        if (DirAccess.Open(path) != null)
        {
            bool saveDataExists = FileAccess.FileExists(path.PathJoin("save_data.json"));
            bool terrainDataExists = FileAccess.FileExists(path.PathJoin("terrain_data.pxsave"));
            bool simDataExists = FileAccess.FileExists(path.PathJoin("sim_data.pxsave"));
            //bool dataWritingFinished = FileAccess.Open(path + "/save_data.json", FileAccess.ModeFlags.Read).GetAsText(true).Length > 0;
            return saveDataExists && terrainDataExists && simDataExists;// && dataWritingFinished;
        }
        return false;
    }
    public static SaveData GetSaveData(string savePath)
    {
        DirAccess saveDirectory = DirAccess.Open(savePath);
        if (saveDirectory.FileExists("save_data.json"))
        {
            FileAccess saveDataFile = FileAccess.Open(savePath.PathJoin("save_data.json"), FileAccess.ModeFlags.Read);
            string saveText = saveDataFile.GetAsText(true); 

            saveDataFile.Dispose();  
            saveDirectory.Dispose();

            return JsonSerializer.Deserialize<SaveData>(saveText);     
        }
        return null;
    }
}