// ============================================================
//  CombatData.cs
//  Runtime state for an active combat encounter.
//  Allocated by CombatEngine.StartCombat(), discarded on end.
// ============================================================
using YVOS.Character;

namespace YVOS.Combat
{
    [System.Serializable]
    public class CombatState
    {
        // -----------------------------------------------------------------
        //  Player
        // -----------------------------------------------------------------
        public int playerHP;
        public int playerMaxHP;
        public int playerStamina;
        public int playerMaxStamina;

        /// <summary>Cooldown remaining per action (index = CombatAction enum value).</summary>
        public int[] playerCooldowns = new int[4];

        // -----------------------------------------------------------------
        //  Enemy
        // -----------------------------------------------------------------
        public int enemyHP;
        public int enemyMaxHP;
        public int[] enemyCooldowns = new int[4];

        // -----------------------------------------------------------------
        //  Round tracking
        // -----------------------------------------------------------------
        public int currentRound = 1;
        public bool isOver = false;
        public CombatResult result;
    }

    [System.Serializable]
    public class CombatRoundResult
    {
        public CombatAction playerAction;
        public CombatAction enemyAction;
        public CombatRoundOutcome outcome;
        public int playerDamageReceived;
        public int enemyDamageReceived;
        public string flavourText;
    }

    [System.Serializable]
    public class CombatReward
    {
        public StatDelta[] statChanges;
        public string[] itemRewardIds;
        public int goldReward;
    }
}
