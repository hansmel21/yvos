// ============================================================
//  HistoryLogUI.cs
//  Scrollable list of HistoryEntry items.
//  Subscribes to HistoryEntryAddedEvent and appends a new
//  text row each time. Object-pools row instances.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YVOS.Core;
using YVOS.History;

namespace YVOS.UI
{
    public class HistoryLogUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform        contentContainer;
        [SerializeField] private TextMeshProUGUI  entryLabelPrefab;
        [SerializeField] private ScrollRect       scrollRect;

        [Header("Category Colors")]
        [SerializeField] private Color colorBirth       = new Color(0.7f, 1f, 0.7f);
        [SerializeField] private Color colorDeath       = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color colorFamily      = new Color(1f, 0.85f, 0.5f);
        [SerializeField] private Color colorCombat      = new Color(0.9f, 0.4f, 0.2f);
        [SerializeField] private Color colorCareer      = new Color(0.5f, 0.7f, 1f);
        [SerializeField] private Color colorRomance     = new Color(1f, 0.6f, 0.7f);
        [SerializeField] private Color colorDefault     = new Color(0.9f, 0.9f, 0.9f);

        private void OnEnable()
        {
            EventBus.Subscribe<HistoryEntryAddedEvent>(OnEntryAdded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HistoryEntryAddedEvent>(OnEntryAdded);
        }

        private void OnEntryAdded(HistoryEntryAddedEvent evt)
        {
            AppendEntry(evt.entry);
        }

        public void AppendEntry(HistoryEntry entry)
        {
            var label = Instantiate(entryLabelPrefab, contentContainer);
            label.text  = entry.ToString();
            label.color = CategoryColor(entry.category);

            // Scroll to bottom after layout update
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private Color CategoryColor(Character.HistoryCategory cat) => cat switch
        {
            Character.HistoryCategory.Birth    => colorBirth,
            Character.HistoryCategory.Death    => colorDeath,
            Character.HistoryCategory.Family   => colorFamily,
            Character.HistoryCategory.Combat   => colorCombat,
            Character.HistoryCategory.Career   => colorCareer,
            Character.HistoryCategory.Romance  => colorRomance,
            _                                  => colorDefault
        };
    }
}
