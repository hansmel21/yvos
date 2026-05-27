// ============================================================
//  HistoryEntry.cs
//  One item in the character's life timeline log.
// ============================================================
using YVOS.Character;

namespace YVOS.History
{
    [System.Serializable]
    public class HistoryEntry
    {
        public int worldYear;
        public int age;
        public string text;
        public HistoryCategory category;
        public string iconId;           // optional: maps to icon sprite in UI

        public HistoryEntry() { }

        public HistoryEntry(int worldYear, int age, string text,
                            HistoryCategory category, string iconId = "")
        {
            this.worldYear = worldYear;
            this.age       = age;
            this.text      = text;
            this.category  = category;
            this.iconId    = iconId;
        }

        public override string ToString() => $"Age {age}: {text}";
    }
}
