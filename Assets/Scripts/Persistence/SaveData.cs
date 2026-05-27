// ============================================================
//  SaveData.cs
//  The JSON serialization envelope for a save slot.
// ============================================================
using YVOS.Character;

namespace YVOS.Persistence
{
    [System.Serializable]
    public class SaveData
    {
        public string saveVersion = "1.0";
        public string savedAt;           // ISO 8601 timestamp
        public CharacterData character;
        public int worldYear;
        public System.Collections.Generic.List<string> globalFlags
            = new System.Collections.Generic.List<string>();
    }

    /// <summary>Lightweight summary for the save slot selection screen.</summary>
    [System.Serializable]
    public class SaveSlotSummary
    {
        public int slot;
        public bool isEmpty = true;
        public string characterName;
        public int age;
        public string socialClass;
        public string savedAt;
    }
}
