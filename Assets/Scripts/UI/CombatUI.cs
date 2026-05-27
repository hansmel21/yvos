// ============================================================
//  CombatUI.cs
//  The D-Pad combat screen.
//  Four directional buttons — greyed out when on cooldown.
//  Shows HP bars, stamina bar, round log, and reveals the
//  CPU's choice after the player picks.
// ============================================================
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YVOS.Character;
using YVOS.Combat;
using YVOS.Core;

namespace YVOS.UI
{
    public class CombatUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject combatPanel;

        [Header("Enemy Info")]
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private Slider          enemyHPSlider;
        [SerializeField] private TextMeshProUGUI enemyHPText;

        [Header("Player Info")]
        [SerializeField] private Slider          playerHPSlider;
        [SerializeField] private TextMeshProUGUI playerHPText;
        [SerializeField] private Slider          playerStaminaSlider;

        [Header("Round Info")]
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI lastRoundLog;    // 2-line log
        [SerializeField] private TextMeshProUGUI flavourText;

        [Header("D-Pad Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button dodgeButton;
        [SerializeField] private Button blockButton;
        [SerializeField] private Button spellButton;

        [Header("Cooldown Indicators")]
        [SerializeField] private GameObject attackCooldownDot;
        [SerializeField] private GameObject dodgeCooldownDot;
        [SerializeField] private GameObject blockCooldownDot;
        [SerializeField] private GameObject spellCooldownDot;

        [Header("Result")]
        [SerializeField] private TextMeshProUGUI resultBanner;   // "VICTORY!" / "DEFEAT"
        [SerializeField] private float           revealDelay = 0.8f;

        // -----------------------------------------------------------------
        //  Unity lifecycle
        // -----------------------------------------------------------------
        private void OnEnable()
        {
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }

        // -----------------------------------------------------------------
        //  Event handlers
        // -----------------------------------------------------------------

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            combatPanel.SetActive(true);
            resultBanner.gameObject.SetActive(false);
            lastRoundLog.text = "";
            flavourText.text  = "";

            enemyNameText.text = evt.enemy.displayName;
            RefreshUI();
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            resultBanner.gameObject.SetActive(true);
            resultBanner.text = evt.result == CombatResult.Victory ? "⚔ VICTORY!" : "💀 DEFEAT";

            // Disable all buttons
            SetButtonsInteractable(false);

            // Auto-close after a delay
            StartCoroutine(CloseCombatAfterDelay(3f));
        }

        // -----------------------------------------------------------------
        //  D-Pad button callbacks (wired in Inspector)
        // -----------------------------------------------------------------

        public void OnAttackPressed()    => SubmitAndRefresh(CombatAction.Attack);
        public void OnDodgePressed()     => SubmitAndRefresh(CombatAction.Dodge);
        public void OnBlockPressed()     => SubmitAndRefresh(CombatAction.Block);
        public void OnSpellItemPressed() => SubmitAndRefresh(CombatAction.SpellItem);

        private void SubmitAndRefresh(CombatAction action)
        {
            SetButtonsInteractable(false);
            StartCoroutine(RevealAndRefresh(action));
        }

        private IEnumerator RevealAndRefresh(CombatAction playerAction)
        {
            var result = CombatEngine.Instance.SubmitPlayerAction(playerAction);

            if (result == null)
            {
                SetButtonsInteractable(true);
                yield break;
            }

            // Show player's action immediately, then reveal CPU's after a beat
            flavourText.text = $"You chose {ActionLabel(playerAction)}...";
            yield return new WaitForSeconds(revealDelay);

            flavourText.text  = result.flavourText;
            lastRoundLog.text = $"You: {ActionLabel(result.playerAction)} | Enemy: {ActionLabel(result.enemyAction)}\n{result.flavourText}";

            RefreshUI();

            if (!CombatEngine.Instance.State.isOver)
                SetButtonsInteractable(true);
        }

        // -----------------------------------------------------------------
        //  UI refresh
        // -----------------------------------------------------------------

        private void RefreshUI()
        {
            var state = CombatEngine.Instance.State;
            if (state == null) return;

            // HP bars
            playerHPSlider.value  = (float)state.playerHP  / state.playerMaxHP;
            enemyHPSlider.value   = (float)state.enemyHP   / state.enemyMaxHP;
            playerHPText.text     = $"HP {state.playerHP}/{state.playerMaxHP}";
            enemyHPText.text      = $"HP {state.enemyHP}/{state.enemyMaxHP}";

            // Stamina
            playerStaminaSlider.value = (float)state.playerStamina / state.playerMaxStamina;

            // Round
            roundText.text = $"Round {state.currentRound}";

            // Cooldown dots
            UpdateCooldownDot(attackCooldownDot, state.playerCooldowns[(int)CombatAction.Attack]);
            UpdateCooldownDot(dodgeCooldownDot,  state.playerCooldowns[(int)CombatAction.Dodge]);
            UpdateCooldownDot(blockCooldownDot,  state.playerCooldowns[(int)CombatAction.Block]);
            UpdateCooldownDot(spellCooldownDot,  state.playerCooldowns[(int)CombatAction.SpellItem]);

            // Button availability
            var available = CombatEngine.Instance.GetAvailablePlayerActions();
            attackButton.interactable    = available.Contains(CombatAction.Attack);
            dodgeButton.interactable     = available.Contains(CombatAction.Dodge);
            blockButton.interactable     = available.Contains(CombatAction.Block);
            spellButton.interactable     = available.Contains(CombatAction.SpellItem);
        }

        private void UpdateCooldownDot(GameObject dot, int cooldown)
        {
            if (dot != null) dot.SetActive(cooldown > 0);
        }

        private void SetButtonsInteractable(bool value)
        {
            attackButton.interactable = value;
            dodgeButton.interactable  = value;
            blockButton.interactable  = value;
            spellButton.interactable  = value;
        }

        private string ActionLabel(CombatAction action) => action switch
        {
            CombatAction.Attack    => "⬆ ATTACK",
            CombatAction.Dodge     => "⬅ DODGE",
            CombatAction.Block     => "➡ BLOCK",
            CombatAction.SpellItem => "⬇ SPELL",
            _ => action.ToString()
        };

        private IEnumerator CloseCombatAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            combatPanel.SetActive(false);
        }
    }
}
