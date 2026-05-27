// ============================================================
//  CharacterLifecycle.cs
//  Governs age-up, life stage transitions, race passives,
//  and death evaluation. Called once per year by TimeManager.
// ============================================================
using UnityEngine;
using YVOS.Core;
using YVOS.History;

namespace YVOS.Character
{
    public class CharacterLifecycle : MonoBehaviour
    {
        public static CharacterLifecycle Instance { get; private set; }

        [SerializeField] private Data.GameBalanceSO balance;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------
        //  Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Called by TimeManager. Ages the character, recalculates life stage,
        /// applies passives, then checks for death.
        /// </summary>
        public void AdvanceYear()
        {
            var character = GameManager.Instance.Character;
            if (character == null || !character.isAlive) return;

            character.stats.age++;

            // Recalculate life stage
            var previousStage = character.stats.lifeStage;
            character.stats.lifeStage = CalculateLifeStage(character.stats.age);

            if (character.stats.lifeStage != previousStage)
            {
                EventBus.Publish(new LifeStageChangedEvent
                {
                    previousStage = previousStage,
                    newStage      = character.stats.lifeStage
                });
                HistoryLog.Instance.Record(
                    HistoryCategory.Event,
                    $"Entered the {character.stats.lifeStage} stage of life."
                );
            }

            // Apply passive stat changes (race passives, aging effects)
            ApplyAnnualPassives(character);

            // Evaluate death
            EvaluateDeath(character);
        }

        // -----------------------------------------------------------------
        //  Life stage
        // -----------------------------------------------------------------

        public static LifeStage CalculateLifeStage(int age)
        {
            if (age <= 3)  return LifeStage.Infant;
            if (age <= 11) return LifeStage.Child;
            if (age <= 17) return LifeStage.Teen;
            if (age <= 24) return LifeStage.YoungAdult;
            if (age <= 44) return LifeStage.Adult;
            if (age <= 59) return LifeStage.MiddleAge;
            return LifeStage.Elder;
        }

        // -----------------------------------------------------------------
        //  Annual passives
        // -----------------------------------------------------------------

        private void ApplyAnnualPassives(CharacterData character)
        {
            // Elder health decay (natural aging)
            if (character.stats.lifeStage == LifeStage.Elder)
            {
                int decay = balance != null ? balance.elderHealthDecayPerYear : 2;
                character.stats.health -= decay;
            }

            // TODO: Load race definition and apply annualPassives[]
            // RaceDefinitionSO race = RaceRegistry.Get(character.stats.raceId);
            // if (race != null) foreach (var mod in race.annualPassives) character.stats.ApplyModifier(mod);

            character.stats.Clamp();
        }

        // -----------------------------------------------------------------
        //  Death evaluation
        // -----------------------------------------------------------------

        private void EvaluateDeath(CharacterData character)
        {
            // Certain death: health at zero
            if (character.stats.health <= 0)
            {
                Die(character, "Illness");
                return;
            }

            // Probabilistic natural death for elders
            if (character.stats.age > 60)
            {
                float baseProbability  = (character.stats.age - 60) * 0.015f;
                float healthModifier   = character.stats.health < 20 ? 0.25f : 0f;
                float deathProbability = Mathf.Clamp01(baseProbability + healthModifier);

                if (Random.value < deathProbability)
                {
                    Die(character, "Old Age");
                }
            }
        }

        private void Die(CharacterData character, string cause)
        {
            character.isAlive       = false;
            character.causeOfDeath  = cause;

            HistoryLog.Instance.Record(
                HistoryCategory.Death,
                $"Died of {cause} at age {character.stats.age}.",
                iconId: "death"
            );

            EventBus.Publish(new CharacterDiedEvent
            {
                causeOfDeath = cause,
                finalAge     = character.stats.age
            });
        }
    }
}
