// ============================================================
//  GuildManager.cs
//  Manages guild membership, work actions, and promotions.
// ============================================================
using UnityEngine;
using YVOS.Character;
using YVOS.Core;
using YVOS.History;

namespace YVOS.Career
{
    public class GuildManager : MonoBehaviour
    {
        public static GuildManager Instance { get; private set; }

        private GuildDefinitionSO[] _allGuilds;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _allGuilds = Resources.LoadAll<GuildDefinitionSO>("Guilds");
            Debug.Log($"[GuildManager] Loaded {_allGuilds.Length} guilds.");
        }

        // -----------------------------------------------------------------
        //  Join
        // -----------------------------------------------------------------

        public bool TryJoin(string guildId)
        {
            var guild     = GetGuild(guildId);
            var character = GameManager.Instance.Character;
            if (guild == null || character == null) return false;

            // Already a member?
            if (character.guildMemberships.Contains(guildId))
            {
                Debug.Log($"[GuildManager] Already a member of {guildId}.");
                return false;
            }

            var stats = character.stats;

            // Check requirements
            if (stats.smarts        < guild.minSmarts        ||
                stats.strength      < guild.minStrength      ||
                stats.magicAffinity < guild.minMagicAffinity ||
                stats.age           < guild.minAge           ||
                stats.socialClass   > guild.maxSocialClassToJoin ||
                stats.gold          < guild.joinFee)
            {
                Debug.Log($"[GuildManager] Requirements not met for {guildId}.");
                return false;
            }

            // Pay fee
            stats.gold -= guild.joinFee;
            stats.Clamp();

            // Register membership
            character.guildMemberships.Add(guildId);
            character.guildXP[guildId]   = 0;
            character.guildRank[guildId] = 0;

            // Apply join tags
            if (guild.joinTags != null)
                foreach (var tag in guild.joinTags)
                    character.AddTag(tag);

            HistoryLog.Instance.Record(
                HistoryCategory.Career,
                $"Joined the {guild.displayName}.",
                iconId: "guild_join"
            );

            EventBus.Publish(new GuildJoinedEvent { guildId = guildId });
            return true;
        }

        // -----------------------------------------------------------------
        //  Yearly work
        // -----------------------------------------------------------------

        public void DoYearlyWork(string guildId)
        {
            var guild     = GetGuild(guildId);
            var character = GameManager.Instance.Character;
            if (guild == null || character == null) return;
            if (!character.guildMemberships.Contains(guildId)) return;

            int rank   = GetRank(character, guildId);
            int income = (guild.annualIncomeByRank != null && rank < guild.annualIncomeByRank.Length)
                         ? guild.annualIncomeByRank[rank] : 0;

            character.stats.gold += income;
            character.stats.Clamp();

            // Grant XP
            int xpGain = 10 + Random.Range(0, 6);
            AddXP(character, guildId, xpGain);

            HistoryLog.Instance.Record(
                HistoryCategory.Career,
                $"Worked at the {guild.displayName} (earned {income}g)."
            );

            CheckPromotion(guildId);
        }

        // -----------------------------------------------------------------
        //  XP and promotion
        // -----------------------------------------------------------------

        public void AddXP(CharacterData character, string guildId, int amount)
        {
            if (!character.guildXP.ContainsKey(guildId))
                character.guildXP[guildId] = 0;
            character.guildXP[guildId] += amount;
        }

        public void CheckPromotion(string guildId)
        {
            var guild     = GetGuild(guildId);
            var character = GameManager.Instance.Character;
            if (guild == null || character == null || guild.ranks == null) return;

            int currentRank = GetRank(character, guildId);
            int nextRank    = currentRank + 1;

            if (nextRank >= guild.ranks.Length) return; // already max rank

            int xpRequired = guild.ranks[nextRank].xpRequired;
            int currentXP  = character.guildXP.TryGetValue(guildId, out int xp) ? xp : 0;

            if (currentXP >= xpRequired)
            {
                character.guildRank[guildId] = nextRank;
                var rankData = guild.ranks[nextRank];

                // Apply rank-up bonuses
                if (rankData.rankUpBonuses != null)
                    foreach (var mod in rankData.rankUpBonuses)
                    {
                        // Convert StatModifier to StatDelta for apply
                        character.stats.ApplyDelta(new StatDelta
                        {
                            statName    = mod.statName,
                            delta       = mod.value,
                            isPercentage = mod.isPercentage
                        });
                    }

                HistoryLog.Instance.Record(
                    HistoryCategory.Career,
                    $"Promoted to {rankData.rankTitle} in the {guild.displayName}!",
                    iconId: "rank_up"
                );

                EventBus.Publish(new GuildRankUpEvent
                {
                    guildId      = guildId,
                    newRank      = nextRank,
                    newRankTitle = rankData.rankTitle
                });
            }
        }

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

        public GuildDefinitionSO GetGuild(string guildId)
        {
            foreach (var g in _allGuilds)
                if (g.guildId == guildId) return g;
            return null;
        }

        public int GetRank(CharacterData character, string guildId)
            => character.guildRank.TryGetValue(guildId, out int rank) ? rank : 0;

        public string GetRankTitle(CharacterData character, string guildId)
        {
            var guild = GetGuild(guildId);
            if (guild == null || guild.ranks == null) return "Member";
            int rank = GetRank(character, guildId);
            return rank < guild.ranks.Length ? guild.ranks[rank].rankTitle : "Member";
        }
    }
}
