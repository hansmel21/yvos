// ============================================================
//  LifeEventEngine.cs
//  Loads all LifeEventSO assets, filters by character state,
//  and returns a weighted-random selection of events each year.
//
//  Flow: TimeManager → PickEventsForYear() → enqueues events
//        → UI pumps queue via EventPopupUI
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YVOS.Character;
using YVOS.Core;

namespace YVOS.Events
{
    public class LifeEventEngine : MonoBehaviour
    {
        public static LifeEventEngine Instance { get; private set; }

        // -----------------------------------------------------------------
        //  Config
        // -----------------------------------------------------------------
        [Tooltip("Min events fired per year.")]
        [SerializeField] private int minEventsPerYear = 1;

        [Tooltip("Max events fired per year.")]
        [SerializeField] private int maxEventsPerYear = 3;

        // -----------------------------------------------------------------
        //  Internal state
        // -----------------------------------------------------------------
        private LifeEventSO[] _allEvents;
        private Queue<LifeEventSO> _eventQueue = new Queue<LifeEventSO>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            LoadAllEvents();
        }

        // -----------------------------------------------------------------
        //  Loading
        // -----------------------------------------------------------------

        private void LoadAllEvents()
        {
            _allEvents = Resources.LoadAll<LifeEventSO>("Events");
            Debug.Log($"[LifeEventEngine] Loaded {_allEvents.Length} life events.");
        }

        // -----------------------------------------------------------------
        //  Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Called by TimeManager each year. Filters eligible events,
        /// picks 1–3 at random, and enqueues them.
        /// </summary>
        public void PickEventsForYear()
        {
            var character = GameManager.Instance.Character;
            if (character == null || !character.isAlive) return;

            var eligible = FilterEvents(character);
            var picks    = WeightedSample(eligible, Random.Range(minEventsPerYear, maxEventsPerYear + 1));

            foreach (var evt in picks)
            {
                _eventQueue.Enqueue(evt);
                EventBus.Publish(new LifeEventQueuedEvent { lifeEvent = evt });
            }
        }

        /// <summary>Returns true if the queue has pending events.</summary>
        public bool HasPendingEvents() => _eventQueue.Count > 0;

        /// <summary>Dequeues and returns the next event, or null if empty.</summary>
        public LifeEventSO DequeueNextEvent()
            => _eventQueue.Count > 0 ? _eventQueue.Dequeue() : null;

        // -----------------------------------------------------------------
        //  Filtering
        // -----------------------------------------------------------------

        private List<LifeEventSO> FilterEvents(CharacterData character)
        {
            var result = new List<LifeEventSO>();
            int age   = character.stats.age;
            var sc    = character.stats.socialClass;

            foreach (var evt in _allEvents)
            {
                // Age gates
                if (evt.minAge > 0 && age < evt.minAge) continue;
                if (evt.maxAge > 0 && age > evt.maxAge) continue;

                // Social class gate
                if (sc < evt.minSocialClass) continue;

                // Required tags: must have ALL
                if (evt.requiredTags != null && evt.requiredTags.Length > 0)
                    if (!evt.requiredTags.All(t => character.HasTag(t))) continue;

                // Blocked tags: must have NONE
                if (evt.blockedTags != null && evt.blockedTags.Length > 0)
                    if (evt.blockedTags.Any(t => character.HasTag(t))) continue;

                result.Add(evt);
            }

            return result;
        }

        // -----------------------------------------------------------------
        //  Weighted sampling (without replacement)
        // -----------------------------------------------------------------

        private List<LifeEventSO> WeightedSample(List<LifeEventSO> pool, int count)
        {
            var result    = new List<LifeEventSO>();
            var remaining = new List<LifeEventSO>(pool);
            count = Mathf.Min(count, remaining.Count);

            for (int i = 0; i < count; i++)
            {
                float totalWeight = remaining.Sum(e => e.baseWeight);
                float roll        = Random.Range(0f, totalWeight);
                float cumulative  = 0f;

                for (int j = 0; j < remaining.Count; j++)
                {
                    cumulative += remaining[j].baseWeight;
                    if (roll <= cumulative)
                    {
                        result.Add(remaining[j]);
                        remaining.RemoveAt(j);
                        break;
                    }
                }
            }

            return result;
        }
    }
}
