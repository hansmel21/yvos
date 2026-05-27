// ============================================================
//  EnemyDefinitionSO.cs
//  ScriptableObject definition for a combat enemy.
//  Create via: Assets → Create → YVOS → Enemy Definition
// ============================================================
using UnityEngine;
using YVOS.Character;

namespace YVOS.Combat
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "YVOS/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        // -----------------------------------------------------------------
        //  Identity
        // -----------------------------------------------------------------
        public string enemyId;
        public string displayName;

        [TextArea(2, 4)]
        public string flavourDescription;

        public Sprite portrait;

        // -----------------------------------------------------------------
        //  Combat stats
        // -----------------------------------------------------------------
        public int maxHealth = 60;
        public int strength  = 40;
        public int armor     = 10;         // reduces physical damage received
        public int magicResist = 5;        // reduces spell damage received

        // -----------------------------------------------------------------
        //  AI behaviour
        // -----------------------------------------------------------------
        [Tooltip("Which actions this enemy can select.")]
        public CombatAction[] allowedActions = {
            CombatAction.Attack,
            CombatAction.Block
        };

        [Tooltip("Weight per action (same order as allowedActions). Higher = picked more often.")]
        public float[] actionWeights = { 0.6f, 0.4f };

        // -----------------------------------------------------------------
        //  Phase shifts (HP thresholds where AI behaviour changes)
        // -----------------------------------------------------------------
        [Tooltip("HP percentage thresholds that trigger behaviour changes, descending (e.g. 0.6, 0.3).")]
        public float[] phaseThresholds;

        [Tooltip("New action weights for each phase (rows match phaseThresholds).")]
        public ActionWeightRow[] phaseWeights;

        // -----------------------------------------------------------------
        //  Special gimmicks (handled in CombatEngine)
        // -----------------------------------------------------------------
        [Tooltip("Enemy flees when HP falls below this percentage. 0 = never flees.")]
        [Range(0f, 0.5f)]
        public float fleeThreshold = 0f;

        [Tooltip("HP regenerated at the end of each round.")]
        public int regenPerRound = 0;

        [Tooltip("Can deal extra hits per round.")]
        public int hitsPerRound = 1;

        // -----------------------------------------------------------------
        //  Defeat consequence
        // -----------------------------------------------------------------
        public DefeatConsequence defeatConsequence = DefeatConsequence.Injury;

        // -----------------------------------------------------------------
        //  Rewards on victory
        // -----------------------------------------------------------------
        public CombatReward victoryReward;
    }

    [System.Serializable]
    public class ActionWeightRow
    {
        public float[] weights;
    }
}
