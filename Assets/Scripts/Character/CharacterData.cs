// ============================================================
//  CharacterData.cs
//  Runtime container for the active character.
//  Owned exclusively by GameManager.
//  Fully JSON-serializable (no UnityEngine types).
// ============================================================
using System.Collections.Generic;
using YVOS.History;
using YVOS.Relationships;

namespace YVOS.Character
{
    [System.Serializable]
    public class CharacterData
    {
        // -----------------------------------------------------------------
        //  Core identity & stats
        // -----------------------------------------------------------------
        public CharacterStats stats = new CharacterStats();

        // -----------------------------------------------------------------
        //  Alive state
        // -----------------------------------------------------------------
        public bool isAlive = true;
        public string causeOfDeath = string.Empty;

        // -----------------------------------------------------------------
        //  Career
        // -----------------------------------------------------------------
        /// <summary>Guild IDs the character is a member of.</summary>
        public List<string> guildMemberships = new List<string>();

        /// <summary>The guild the character is actively working this year.</summary>
        public string activeGuildId = string.Empty;

        /// <summary>XP earned per guild (key = guildId).</summary>
        public Dictionary<string, int> guildXP = new Dictionary<string, int>();

        /// <summary>Current rank index per guild (key = guildId).</summary>
        public Dictionary<string, int> guildRank = new Dictionary<string, int>();

        // -----------------------------------------------------------------
        //  Relationships
        // -----------------------------------------------------------------
        public List<RelationshipData> relationships = new List<RelationshipData>();

        // -----------------------------------------------------------------
        //  Inventory
        // -----------------------------------------------------------------
        /// <summary>Item IDs in the character's inventory.</summary>
        public List<string> inventoryItemIds = new List<string>();

        // -----------------------------------------------------------------
        //  Tags (used by event filtering)
        // -----------------------------------------------------------------
        /// <summary>
        /// Freeform string tags that track persistent character state.
        /// Examples: "guild_mages", "married", "wanted_thieves", "plague_survivor"
        /// </summary>
        public HashSet<string> tags = new HashSet<string>();

        // -----------------------------------------------------------------
        //  History log
        // -----------------------------------------------------------------
        public List<HistoryEntry> lifeLog = new List<HistoryEntry>();

        // -----------------------------------------------------------------
        //  Tag helpers
        // -----------------------------------------------------------------
        public void AddTag(string tag) => tags.Add(tag.ToLowerInvariant());
        public void RemoveTag(string tag) => tags.Remove(tag.ToLowerInvariant());
        public bool HasTag(string tag) => tags.Contains(tag.ToLowerInvariant());
    }
}
