// ============================================================
//  TimeManager.cs
//  The game loop heartbeat. "Age Up" button calls AdvanceYear().
//  Orchestrates: lifecycle → events → relationships → economy.
// ============================================================
using UnityEngine;
using YVOS.Character;
using YVOS.Events;
using YVOS.Economy;
using YVOS.Relationships;

namespace YVOS.Core
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------
        //  Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Called by the "Age Up" button in MainLifeUI.
        /// Blocked while event popup queue is non-empty (UI enforces this).
        /// </summary>
        public void AdvanceYear()
        {
            var character = GameManager.Instance.Character;
            if (character == null || !character.isAlive)
            {
                Debug.LogWarning("[TimeManager] AdvanceYear called but character is dead or null.");
                return;
            }

            // Advance world year through GameManager
            GameManager.Instance.AdvanceYear();

            EventBus.Publish(new YearStartedEvent
            {
                worldYear = GameManager.Instance.WorldYear
            });

            // 1. Age the character, recalculate life stage, check death
            CharacterLifecycle.Instance.AdvanceYear();

            // If character died during lifecycle check, stop here
            if (!character.isAlive) return;

            // 2. Pick life events for this year and enqueue them
            LifeEventEngine.Instance.PickEventsForYear();

            // 3. Process yearly relationship decay
            RelationshipManager.Instance.ProcessYearlyDecay();

            // 4. Process yearly income and living costs
            EconomyManager.Instance.ProcessYearlyIncome();

            EventBus.Publish(new YearEndedEvent
            {
                worldYear = GameManager.Instance.WorldYear
            });
        }
    }
}
