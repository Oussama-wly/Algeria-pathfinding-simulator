using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PathRecord
{
    public string algo;
    public string start;
    public string goal;
    public float  dist;
    public int    visited;
    public string travel;
    public string date;
}

[System.Serializable]
class RecordList { public List<PathRecord> records = new List<PathRecord>(); }

public class HistoryManager
{
    static string FilePath => Application.persistentDataPath + "/path_history.json";

    public static void Save(PathRecord r)
    {
        var list = Load();
        list.Insert(0, r);
        if (list.Count > 50) list.RemoveAt(list.Count - 1); // keep last 50
        File.WriteAllText(FilePath, JsonUtility.ToJson(new RecordList { records = list }));
    }

    public static List<PathRecord> Load()
    {
        if (!File.Exists(FilePath)) return new List<PathRecord>();
        try
        {
            var rl = JsonUtility.FromJson<RecordList>(File.ReadAllText(FilePath));
            return rl?.records ?? new List<PathRecord>();
        }
        catch { return new List<PathRecord>(); }
    }

    public static void Clear()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}
