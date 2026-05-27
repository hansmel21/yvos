// ============================================================
//  EventPopupUI.cs
//  Modal overlay for life events. Consumes the LifeEventEngine
//  queue, presents the event + choices, then chains to the next
//  event or closes. Blocks the Age Up button while open.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YVOS.Core;
using YVOS.Events;

namespace YVOS.UI
{
    public class EventPopupUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI outcomeText;
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Timing")]
        [SerializeField] private float outcomeFadeTime = 1.5f;

        // -----------------------------------------------------------------
        //  State
        // -----------------------------------------------------------------
        private bool _isShowingOutcome = false;

        private void OnEnable()
        {
            EventBus.Subscribe<LifeEventQueuedEvent>(OnEventQueued);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LifeEventQueuedEvent>(OnEventQueued);
        }

        private void OnEventQueued(LifeEventQueuedEvent evt)
        {
            // If already showing, the current event will chain to the next on close
            if (!popupPanel.activeSelf)
                ShowNextEvent();
        }

        // -----------------------------------------------------------------
        //  Show / Hide
        // -----------------------------------------------------------------

        public void ShowNextEvent()
        {
            var nextEvent = LifeEventEngine.Instance.DequeueNextEvent();
            if (nextEvent == null)
            {
                popupPanel.SetActive(false);
                // Notify MainLifeUI that the queue is empty (Age Up can be re-enabled)
                EventBus.Publish(new Core.YearEndedEvent
                    { worldYear = GameManager.Instance.WorldYear });
                return;
            }

            ShowEvent(nextEvent);
        }

        private void ShowEvent(LifeEventSO lifeEvent)
        {
            popupPanel.SetActive(true);
            outcomeText.gameObject.SetActive(false);

            // Token-replace description
            var character = GameManager.Instance.Character;
            string desc = EventResolver.TokenReplace(lifeEvent.descriptionTemplate, character);

            titleText.text       = lifeEvent.title;
            descriptionText.text = desc;

            // Rebuild choice buttons
            foreach (Transform child in choiceButtonContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < lifeEvent.choices.Length; i++)
            {
                int capturedIndex = i;
                var btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = lifeEvent.choices[i].buttonLabel;
                btn.onClick.AddListener(() => OnChoiceSelected(lifeEvent, capturedIndex));
            }
        }

        private void OnChoiceSelected(LifeEventSO lifeEvent, int choiceIndex)
        {
            if (_isShowingOutcome) return;

            // Disable buttons while showing outcome
            foreach (Transform child in choiceButtonContainer)
                child.GetComponent<Button>().interactable = false;

            // If this event triggers combat, hand off to CombatEngine
            if (lifeEvent.triggersCombat && lifeEvent.combatEnemy != null)
            {
                popupPanel.SetActive(false);
                Combat.CombatEngine.Instance.StartCombat(lifeEvent.combatEnemy);
                return;
            }

            // Resolve choice
            EventResolver.Instance.Resolve(lifeEvent, choiceIndex);

            // Show outcome text briefly
            string outcome = EventResolver.TokenReplace(
                lifeEvent.choices[choiceIndex].outcomeText,
                GameManager.Instance.Character);

            StartCoroutine(ShowOutcomeThenNext(outcome));
        }

        private IEnumerator ShowOutcomeThenNext(string outcome)
        {
            _isShowingOutcome          = true;
            outcomeText.text           = outcome;
            outcomeText.gameObject.SetActive(true);

            yield return new WaitForSeconds(outcomeFadeTime);

            _isShowingOutcome = false;
            ShowNextEvent();
        }
    }
}
