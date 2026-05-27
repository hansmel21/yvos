// ============================================================
//  MiniGamePopupUI.cs
//  Reuses the same modal popup framework as EventPopupUI but
//  drives the MiniGameEngine stage-by-stage flow.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YVOS.Core;
using YVOS.MiniGames;

namespace YVOS.UI
{
    public class MiniGamePopupUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject        popupPanel;
        [SerializeField] private TextMeshProUGUI   titleText;
        [SerializeField] private TextMeshProUGUI   stagePromptText;
        [SerializeField] private TextMeshProUGUI   outcomeText;
        [SerializeField] private Transform         choiceButtonContainer;
        [SerializeField] private Button            choiceButtonPrefab;

        private void OnEnable()
        {
            EventBus.Subscribe<MiniGameStartedEvent>(OnMiniGameStarted);
            EventBus.Subscribe<MiniGameCompletedEvent>(OnMiniGameCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MiniGameStartedEvent>(OnMiniGameStarted);
            EventBus.Unsubscribe<MiniGameCompletedEvent>(OnMiniGameCompleted);
        }

        // -----------------------------------------------------------------

        private void OnMiniGameStarted(MiniGameStartedEvent evt)
        {
            var mg = MiniGameEngine.Instance.GetActiveMiniGame();
            if (mg == null) return;

            popupPanel.SetActive(true);
            titleText.text  = mg.title;
            outcomeText.gameObject.SetActive(false);

            ShowStage(MiniGameEngine.Instance.GetCurrentStage());
        }

        private void OnMiniGameCompleted(MiniGameCompletedEvent evt)
        {
            outcomeText.gameObject.SetActive(true);
            outcomeText.text = evt.success
                ? "✦ Success! The challenge is complete."
                : "✖ You failed the challenge.";

            // Close after a short delay
            Invoke(nameof(ClosePanel), 2f);
        }

        private void ShowStage(MiniGameStage stage)
        {
            if (stage == null) { ClosePanel(); return; }

            stagePromptText.text = stage.promptText;
            outcomeText.gameObject.SetActive(false);

            // Rebuild buttons
            foreach (Transform child in choiceButtonContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < stage.choices.Length; i++)
            {
                int capturedIdx = i;
                var btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = stage.choices[i].buttonLabel;
                btn.onClick.AddListener(() => OnChoiceSelected(capturedIdx));
            }
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            // Disable buttons
            foreach (Transform child in choiceButtonContainer)
                child.GetComponent<Button>().interactable = false;

            // Show outcome text for this choice before advancing
            var currentStage = MiniGameEngine.Instance.GetCurrentStage();
            if (currentStage != null && choiceIndex < currentStage.choices.Length)
            {
                outcomeText.text = currentStage.choices[choiceIndex].outcomeText;
                outcomeText.gameObject.SetActive(true);
            }

            var nextStage = MiniGameEngine.Instance.SubmitChoice(choiceIndex);
            if (nextStage != null)
            {
                Invoke(nameof(RefreshToCurrentStage), 1.2f);
            }
            // If nextStage is null, MiniGameEngine already fired MiniGameCompletedEvent
        }

        private void RefreshToCurrentStage()
        {
            ShowStage(MiniGameEngine.Instance.GetCurrentStage());
        }

        private void ClosePanel()
        {
            popupPanel.SetActive(false);
        }
    }
}
