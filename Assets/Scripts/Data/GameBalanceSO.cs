// ============================================================
//  GameBalanceSO.cs
//  Designer-tunable parameters. All magic numbers live here.
//  Create via: Assets → Create → YVOS → Game Balance
// ============================================================
using UnityEngine;

namespace YVOS.Data
{
    [CreateAssetMenu(fileName = "GameBalance", menuName = "YVOS/Game Balance")]
    public class GameBalanceSO : ScriptableObject
    {
        [Header("Aging")]
        [Tooltip("HP lost per year once Elder stage is reached.")]
        public int elderHealthDecayPerYear = 2;

        [Tooltip("Base death probability per year over age 60. Multiplied by (age - 60).")]
        public float elderDeathProbabilityBase = 0.015f;

        [Tooltip("Added to death probability if health < 20.")]
        public float lowHealthDeathModifier = 0.25f;

        [Header("Combat")]
        [Tooltip("Rounds an action is unavailable after being used (no-repeat rule).")]
        public int combatActionCooldown = 1;

        [Tooltip("Stamina cost per Spell/Item use.")]
        public int spellStaminaCost = 10;

        [Tooltip("Maximum player HP in combat.")]
        public int combatMaxPlayerHP = 100;

        [Tooltip("Maximum player stamina in combat.")]
        public int combatMaxPlayerStamina = 60;

        [Header("Economy")]
        [Tooltip("Base annual income per social class (index = SocialClass enum value).")]
        public int[] baseIncomeByClass = { 5, 15, 30, 60, 100, 200, 500 };

        [Tooltip("Annual living cost per social class.")]
        public int[] livingCostByClass = { 3, 10, 20, 40, 70, 150, 400 };

        [Header("Relationships")]
        [Tooltip("Affection lost per year for relationships the player doesn't interact with.")]
        public int relationshipDecayPerYear = 2;

        [Header("Events")]
        [Tooltip("Minimum events fired per year.")]
        public int minEventsPerYear = 1;

        [Tooltip("Maximum events fired per year.")]
        public int maxEventsPerYear = 3;
    }
}
