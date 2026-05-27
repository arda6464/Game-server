using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

[Serializable]
public class UpdateNoteMessageData
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public int IconId { get; set; }
    public int TypeId { get; set; }
}

[Serializable]
public class GameUpdateNoteData
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Version { get; set; } = "";
    public string Date { get; set; } = "";
    public int BannerId { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public List<UpdateNoteMessageData> Messages { get; set; } = new List<UpdateNoteMessageData>();
}

public static class UpdateNotesManager
{
    private static readonly List<GameUpdateNoteData> _notes = new List<GameUpdateNoteData>();
    private static readonly string SavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database", "update_notes.json");

    public static void Init()
    {
        Load();
    }

    public static List<GameUpdateNoteData> GetAll() =>
        _notes.OrderBy(n => n.SortOrder).ThenByDescending(n => n.Id).ToList();

    public static List<GameUpdateNoteData> GetPublished() =>
        _notes.Where(n => n.IsPublished).OrderBy(n => n.SortOrder).ThenByDescending(n => n.Id).ToList();

    public static GameUpdateNoteData? GetById(int id) => _notes.FirstOrDefault(n => n.Id == id);

    public static GameUpdateNoteData Save(GameUpdateNoteData note)
    {
        if (note == null) throw new ArgumentNullException(nameof(note));

        var existing = _notes.FirstOrDefault(n => n.Id == note.Id);
        if (existing != null)
        {
            existing.Title = note.Title ?? "";
            existing.Version = note.Version ?? "";
            existing.Date = note.Date ?? "";
            existing.BannerId = note.BannerId;
            existing.IsPublished = note.IsPublished;
            existing.SortOrder = note.SortOrder;
            existing.Messages = note.Messages ?? new List<UpdateNoteMessageData>();
            SaveToDisk();
            return existing;
        }

        if (note.Id <= 0)
            note.Id = _notes.Count > 0 ? _notes.Max(n => n.Id) + 1 : 1;

        if (note.SortOrder == 0)
            note.SortOrder = _notes.Count > 0 ? _notes.Max(n => n.SortOrder) + 1 : 1;

        note.Messages ??= new List<UpdateNoteMessageData>();
        _notes.Add(note);
        SaveToDisk();
        return note;
    }

    public static bool Delete(int id)
    {
        int removed = _notes.RemoveAll(n => n.Id == id);
        if (removed > 0)
        {
            SaveToDisk();
            return true;
        }
        return false;
    }

    public static bool SetPublishState(int id, bool published)
    {
        var note = _notes.FirstOrDefault(n => n.Id == id);
        if (note == null) return false;
        note.IsPublished = published;
        SaveToDisk();
        return true;
    }

    public static void Reorder(List<int> orderedIds)
    {
        if (orderedIds == null || orderedIds.Count == 0) return;
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var note = _notes.FirstOrDefault(n => n.Id == orderedIds[i]);
            if (note != null)
                note.SortOrder = i;
        }
        SaveToDisk();
    }

    private static void Load()
    {
        _notes.Clear();
        try
        {
            string dir = Path.GetDirectoryName(SavePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(SavePath))
                return;

            string json = File.ReadAllText(SavePath);
            var loaded = JsonConvert.DeserializeObject<List<GameUpdateNoteData>>(json);
            if (loaded != null)
                _notes.AddRange(loaded);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[UpdateNotesManager] Yükleme hatası: {ex.Message}");
        }
    }

    private static void SaveToDisk()
    {
        try
        {
            string dir = Path.GetDirectoryName(SavePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonConvert.SerializeObject(_notes, Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[UpdateNotesManager] Kaydetme hatası: {ex.Message}");
        }
    }
}
