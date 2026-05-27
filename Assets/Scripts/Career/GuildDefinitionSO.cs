// ============================================================
//  GuildDefinitionSO.cs
//  ScriptableObject definition for a guild/career path.
//  Create via: Assets → Create → YVOS → Guild Definition
// ============================================================
using UnityEngine;
using YVOS.Character;

namespace YVOS.Career
{
    [CreateAssetMenu(fileName = "NewGuild", menuName = "YVOS/Guild Definition")]
    public class GuildDefinitionSO : ScriptableObject
    {
        // -----------------------------------------------------------------
        //  Identity
        // -----------------------------------------------------------------
        public string guildId;
        public string displayName;

        [TextArea(2, 4)]
        public string flavourText;

        public Sprite emblem;

        // -----------------------------------------------------------------
        //  Entry requirements
        // -----------------------------------------------------------------
        [Header("Entry Requirements")]
        public int minSmarts        = 0;
        public int minStrength      = 0;
        public int minMagicAffinity = 0;
        public int minAge           = 14;

        [Tooltip("Social classes at or above this cannot join (e.g. Thieves bars nobles).")]
        public SocialClass maxSocialClassToJoin = SocialClass.Royalty;

        public int joinFee = 0;

        // -----------------------------------------------------------------
        //  Ranks (index 0 = lowest)
        // -----------------------------------------------------------------
        public GuildRankData[] ranks;

        // -----------------------------------------------------------------
        //  Income
        // -----------------------------------------------------------------
        [Tooltip("Annual gold income per rank level (same length as ranks[]).")]
        public int[] annualIncomeByRank;

        // -----------------------------------------------------------------
        //  Tags
        // -----------------------------------------------------------------
        [Tooltip("Tags added to the character on joining (e.g. 'guild_mages').")]
        public string[] joinTags;
    }

    [System.Serializable]
    public class GuildRankData
    {
        public string rankTitle;

        [TextArea(1, 3)]
        public string description;

        public int xpRequired;
        public StatModifier[] rankUpBonuses;

        [Tooltip("Action IDs unlocked in the Action Menu at this rank.")]
        public string[] unlockedActionIds;
    }
}
