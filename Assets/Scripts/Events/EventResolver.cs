// ============================================================
//  EventResolver.cs
//  Stateless. Applies an EventChoiceData to CharacterData,
//  writes to HistoryLog, and publishes LifeEventResolvedEvent.
// ============================================================
using UnityEngine;
using YVOS.Character;
using YVOS.Core;
using YVOS.History;

namespace YVOS.Events
{
    public class EventResolver : MonoBehaviour
    {
        public static EventResolver Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------
        //  Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Resolves the player's choice for the given event.
        /// </summary>
        public void Resolve(LifeEventSO lifeEvent, int choiceIndex)
        {
            var character = GameManager.Instance.Character;
            if (character == null) return;

            var choice = lifeEvent.choices[choiceIndex];

            // 1. Apply stat changes
            if (choice.statChanges != null)
                foreach (var delta in choice.statChanges)
                    character.stats.ApplyDelta(delta);

            // 2. Apply direct gold delta
            if (choice.goldDelta != 0)
            {
                character.stats.gold += choice.goldDelta;
                character.stats.Clamp();
            }

            // 3. Apply tag changes
            if (choice.addTags != null)
                foreach (var tag in choice.addTags)
                    character.AddTag(tag);

            if (choice.removeTags != null)
                foreach (var tag in choice.removeTags)
                    character.RemoveTag(tag);

            // 4. Write to history log
            var outcomeText = TokenReplace(choice.outcomeText, character);
            HistoryLog.Instance.Record(
                HistoryCategory.Event,
                $"{lifeEvent.title}: {outcomeText}"
            );

            // 5. Queue follow-up if specified
            if (!string.IsNullOrEmpty(choice.unlockFollowUpEventId))
                character.AddTag($"followup_{choice.unlockFollowUpEventId}");

            // 6. Handle death outcome
            if (choice.causesDeath)
            {
                character.isAlive = false;
                character.causeOfDeath = lifeEvent.title;
                EventBus.Publish(new CharacterDiedEvent
                {
                    causeOfDeath = character.causeOfDeath,
                    finalAge     = character.stats.age
                });
            }

            // 7. Notify
            EventBus.Publish(new LifeEventResolvedEvent
            {
                eventId     = lifeEvent.eventId,
                choiceIndex = choiceIndex
            });
        }

        // -----------------------------------------------------------------
        //  Token replacement
        // -----------------------------------------------------------------

        public static string TokenReplace(string template, CharacterData character)
        {
            if (string.IsNullOrEmpty(template)) return template;

            var s = template;
            s = s.Replace("{name}",  character.stats.firstName);
            s = s.Replace("{age}",   character.stats.age.ToString());
            s = s.Replace("{year}",  GameManager.Instance.WorldYear.ToString());
            s = s.Replace("{guild}", character.activeGuildId);

            // Replace parent_name with mother's name if available
            foreach (var rel in character.relationships)
            {
                if (rel.type == RelationshipType.Mother)
                {
                    s = s.Replace("{parent_name}", rel.firstName);
                    break;
                }
            }

            return s;
        }
    }
}
