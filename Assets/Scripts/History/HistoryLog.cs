// ============================================================
//  HistoryLog.cs
//  Thin service. Every system calls Record() to write to the
//  character's life timeline. Publishes events so UI can react.
// ============================================================
using UnityEngine;
using YVOS.Character;
using YVOS.Core;

namespace YVOS.History
{
    public class HistoryLog : MonoBehaviour
    {
        public static HistoryLog Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------
        //  Public API
        // -----------------------------------------------------------------

        public void Record(string text, HistoryCategory category, string iconId = "")
        {
            var character = GameManager.Instance.Character;
            if (character == null) return;

            var entry = new HistoryEntry(
                worldYear: GameManager.Instance.WorldYear,
                age:       character.stats.age,
                text:      text,
                category:  category,
                iconId:    iconId
            );

            character.lifeLog.Add(entry);
            EventBus.Publish(new HistoryEntryAddedEvent { entry = entry });
        }

        /// <summary>
        /// Convenience overload for commonly formatted entries.
        /// </summary>
        public void Record(HistoryCategory category, string text)
            => Record(text, category);
    }
}
