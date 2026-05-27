// ============================================================
//  MiniGameDefinitionSO.cs
//  ScriptableObject definition for a guild mini-game.
//  Reuses the same modal popup framework as life events.
//  Create via: Assets → Create → YVOS → Mini Game Definition
// ============================================================
using UnityEngine;
using YVOS.Character;

namespace YVOS.MiniGames
{
    [CreateAssetMenu(fileName = "NewMiniGame", menuName = "YVOS/Mini Game Definition")]
    public class MiniGameDefinitionSO : ScriptableObject
    {
        // -----------------------------------------------------------------
        //  Identity
        // -----------------------------------------------------------------
        public string miniGameId;
        public string guildId;      // which guild this belongs to
        public string title;

        [TextArea(2, 4)]
        public string introText;

        [Tooltip("Years before this mini-game can be replayed.")]
        public int cooldownYears = 3;

        // -----------------------------------------------------------------
        //  Stages (sequential — final stage determines success/failure)
        // -----------------------------------------------------------------
        public MiniGameStage[] stages;

        // -----------------------------------------------------------------
        //  Rewards / penalties
        // -----------------------------------------------------------------
        public StatDelta[] successReward;
        public int successGoldReward;
        public StatDelta[] failurePenalty;
        public int failureGoldPenalty;
    }

    [System.Serializable]
    public class MiniGameStage
    {
        [TextArea(2, 5)]
        public string promptText;

        public MiniGameChoice[] choices;
    }

    [System.Serializable]
    public class MiniGameChoice
    {
        public string buttonLabel;

        [TextArea(1, 3)]
        public string outcomeText;

        [Tooltip("Which stat is checked to determine success odds.")]
        public string checkStat;        // e.g. "strength", "smarts", "magicAffinity"

        [Tooltip("Stat value needed for a favourable outcome (0 = no check, always succeeds).")]
        public int statThreshold;

        [Tooltip("If stat check passes, proceed to this stage index. -1 = end (success).")]
        public int nextStageOnSuccess = -1;

        [Tooltip("If stat check fails (or no check), proceed to this stage index. -1 = end (failure).")]
        public int nextStageOnFailure = -1;

        [Tooltip("Always proceeds to this stage regardless of stat check.")]
        public bool ignoreStatCheck = false;
    }
}
