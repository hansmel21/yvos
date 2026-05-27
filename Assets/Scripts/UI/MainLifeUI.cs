// ============================================================
//  MainLifeUI.cs
//  The primary gameplay screen.
//  Updates stats panel on YearEndedEvent, controls Age Up button,
//  and delegates to sub-panels for actions.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YVOS.Core;
using YVOS.Character;

namespace YVOS.UI
{
    public class MainLifeUI : MonoBehaviour
    {
        // -----------------------------------------------------------------
        //  Header
        // -----------------------------------------------------------------
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI headerText;      // "YEAR 887 · ELYNDRA · AGE 23 · Adult"

        // -----------------------------------------------------------------
        //  Stat bars (read-only Sliders)
        // -----------------------------------------------------------------
        [Header("Stat Sliders")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider happinessSlider;
        [SerializeField] private Slider smartsSlider;
        [SerializeField] private Slider looksSlider;
        [SerializeField] private Slider strengthSlider;
        [SerializeField] private Slider magicSlider;

        [Header("Stat Colors")]
        [SerializeField] private Color statColorGood    = Color.green;
        [SerializeField] private Color statColorWarning = Color.yellow;
        [SerializeField] private Color statColorDanger  = Color.red;

        // -----------------------------------------------------------------
        //  Economy / Guild info
        // -----------------------------------------------------------------
        [Header("Info Labels")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI reputationText;
        [SerializeField] private TextMeshProUGUI guildText;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI socialClassText;

        // -----------------------------------------------------------------
        //  Age Up button
        // -----------------------------------------------------------------
        [Header("Age Up")]
        [SerializeField] private Button ageUpButton;

        // -----------------------------------------------------------------
        //  Unity lifecycle
        // -----------------------------------------------------------------
        private void OnEnable()
        {
            EventBus.Subscribe<YearEndedEvent>(OnYearEnded);
            EventBus.Subscribe<HistoryEntryAddedEvent>(OnHistoryAdded);
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Subscribe<LifeEventQueuedEvent>(OnEventQueued);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<YearEndedEvent>(OnYearEnded);
            EventBus.Unsubscribe<HistoryEntryAddedEvent>(OnHistoryAdded);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Unsubscribe<LifeEventQueuedEvent>(OnEventQueued);
        }

        private void Start()
        {
            RefreshUI();
        }

        // -----------------------------------------------------------------
        //  Event handlers
        // -----------------------------------------------------------------

        private void OnYearEnded(YearEndedEvent evt)
        {
            RefreshUI();
            // Re-enable Age Up only if no events pending
            SetAgeUpInteractable(!LifeEventEngine.Instance.HasPendingEvents());
        }

        private void OnHistoryAdded(HistoryEntryAddedEvent evt)
        {
            // History scroll panel handles its own update via this event
        }

        private void OnCharacterDied(CharacterDiedEvent evt)
        {
            SetAgeUpInteractable(false);
            // DeathScreenUI will load from this event
        }

        private void OnEventQueued(LifeEventQueuedEvent evt)
        {
            // Block Age Up while events are pending — EventPopupUI controls re-enable
            SetAgeUpInteractable(false);
        }

        // -----------------------------------------------------------------
        //  UI Refresh
        // -----------------------------------------------------------------

        private void RefreshUI()
        {
            var character = GameManager.Instance.Character;
            if (character == null) return;

            var stats = character.stats;

            // Header
            headerText.text = $"YEAR {GameManager.Instance.WorldYear}  ·  {stats.FullName}  ·  AGE {stats.age}  ·  {stats.lifeStage}";

            // Stat sliders
            UpdateSlider(healthSlider,    stats.health);
            UpdateSlider(happinessSlider, stats.happiness);
            UpdateSlider(smartsSlider,    stats.smarts);
            UpdateSlider(looksSlider,     stats.looks);
            UpdateSlider(strengthSlider,  stats.strength);
            UpdateSlider(magicSlider,     stats.magicAffinity);

            // Labels
            goldText.text        = $"{stats.gold}g";
            reputationText.text  = $"Rep: {(stats.reputation >= 0 ? "+" : "")}{stats.reputation}";
            socialClassText.text = stats.socialClass.ToString();

            // Guild info
            if (!string.IsNullOrEmpty(character.activeGuildId))
            {
                var guildMgr = Career.GuildManager.Instance;
                guildText.text = guildMgr.GetGuild(character.activeGuildId)?.displayName ?? character.activeGuildId;
                rankText.text  = guildMgr.GetRankTitle(character, character.activeGuildId);
            }
            else
            {
                guildText.text = "No Guild";
                rankText.text  = string.Empty;
            }
        }

        private void UpdateSlider(Slider slider, int value)
        {
            if (slider == null) return;
            slider.value = value / 100f;

            var fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage == null) return;

            fillImage.color = value >= 60 ? statColorGood
                            : value >= 30 ? statColorWarning
                            : statColorDanger;
        }

        // -----------------------------------------------------------------
        //  Age Up button
        // -----------------------------------------------------------------

        public void OnAgeUpButtonPressed()
        {
            if (!LifeEventEngine.Instance.HasPendingEvents())
                TimeManager.Instance.AdvanceYear();
        }

        public void SetAgeUpInteractable(bool interactable)
        {
            if (ageUpButton != null)
                ageUpButton.interactable = interactable;
        }
    }
}
