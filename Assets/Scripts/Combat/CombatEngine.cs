// ============================================================
//  CombatEngine.cs
//  Runs the simultaneous D-Pad combat loop.
//
//  Each round:
//    1. Player picks action (via CombatUI)
//    2. PickEnemyAction() runs (simultaneous hidden pick)
//    3. ResolveRound() → applies damage, returns result
//    4. TickCooldowns() → enforces no-repeat rule
//    5. CheckCombatEnd() → win/loss/escape check
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using YVOS.Character;
using YVOS.Core;
using YVOS.History;

namespace YVOS.Combat
{
    public class CombatEngine : MonoBehaviour
    {
        public static CombatEngine Instance { get; private set; }

        [SerializeField] private Data.GameBalanceSO balance;

        // -----------------------------------------------------------------
        //  State
        // -----------------------------------------------------------------
        public CombatState State   { get; private set; }
        public EnemyDefinitionSO CurrentEnemy { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------
        //  Start / End
        // -----------------------------------------------------------------

        public void StartCombat(EnemyDefinitionSO enemy)
        {
            CurrentEnemy = enemy;
            int maxHP      = balance != null ? balance.combatMaxPlayerHP      : 100;
            int maxStamina = balance != null ? balance.combatMaxPlayerStamina : 60;

            // Seed player HP from character health stat (0–100 health → 10–100 combat HP)
            var charStats = GameManager.Instance.Character.stats;
            int playerHP  = Mathf.Max(10, charStats.health);

            State = new CombatState
            {
                playerHP        = playerHP,
                playerMaxHP     = maxHP,
                playerStamina   = maxStamina,
                playerMaxStamina = maxStamina,
                playerCooldowns = new int[4],
                enemyHP         = enemy.maxHealth,
                enemyMaxHP      = enemy.maxHealth,
                enemyCooldowns  = new int[4],
                currentRound    = 1,
                isOver          = false
            };

            EventBus.Publish(new CombatStartedEvent { enemy = enemy });
        }

        // -----------------------------------------------------------------
        //  Player action submission (called by CombatUI button)
        // -----------------------------------------------------------------

        public CombatRoundResult SubmitPlayerAction(CombatAction playerAction)
        {
            if (State == null || State.isOver)
            {
                Debug.LogWarning("[CombatEngine] SubmitPlayerAction called but combat is over.");
                return null;
            }

            // Validate cooldown
            if (State.playerCooldowns[(int)playerAction] > 0)
            {
                Debug.LogWarning($"[CombatEngine] {playerAction} is on cooldown!");
                return null;
            }

            // Stamina check for SpellItem
            if (playerAction == CombatAction.SpellItem)
            {
                int cost = balance != null ? balance.spellStaminaCost : 10;
                if (State.playerStamina < cost)
                {
                    Debug.Log("[CombatEngine] Not enough stamina for Spell/Item.");
                    return null;
                }
                State.playerStamina -= cost;
            }

            // Enemy picks simultaneously
            CombatAction enemyAction = PickEnemyAction();

            // Resolve the round
            var result = ResolveRound(playerAction, enemyAction);
            result.playerAction = playerAction;
            result.enemyAction  = enemyAction;

            // Apply damage
            State.playerHP  -= result.playerDamageReceived;
            State.enemyHP   -= result.enemyDamageReceived;

            // Enemy regen
            if (CurrentEnemy.regenPerRound > 0)
                State.enemyHP = Mathf.Min(State.enemyHP + CurrentEnemy.regenPerRound, State.enemyMaxHP);

            // Tick cooldowns and set used action cooldowns
            TickCooldowns(playerAction, enemyAction);

            State.currentRound++;

            // Check end conditions
            CheckCombatEnd();

            return result;
        }

        // -----------------------------------------------------------------
        //  Enemy AI
        // -----------------------------------------------------------------

        private CombatAction PickEnemyAction()
        {
            // Determine current phase
            float hpPct = (float)State.enemyHP / State.enemyMaxHP;
            float[] weights = GetPhaseWeights(hpPct);

            // Filter by cooldown
            var available = new List<int>();
            float totalWeight = 0f;

            for (int i = 0; i < CurrentEnemy.allowedActions.Length; i++)
            {
                var action = CurrentEnemy.allowedActions[i];
                if (State.enemyCooldowns[(int)action] == 0)
                {
                    available.Add(i);
                    totalWeight += (i < weights.Length ? weights[i] : 1f);
                }
            }

            if (available.Count == 0)
            {
                // All on cooldown — fallback to Attack
                return CombatAction.Attack;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (int idx in available)
            {
                cumulative += (idx < weights.Length ? weights[idx] : 1f);
                if (roll <= cumulative)
                    return CurrentEnemy.allowedActions[idx];
            }

            return CurrentEnemy.allowedActions[available[available.Count - 1]];
        }

        private float[] GetPhaseWeights(float hpPct)
        {
            if (CurrentEnemy.phaseThresholds == null || CurrentEnemy.phaseThresholds.Length == 0)
                return CurrentEnemy.actionWeights;

            for (int i = 0; i < CurrentEnemy.phaseThresholds.Length; i++)
            {
                if (hpPct <= CurrentEnemy.phaseThresholds[i] && CurrentEnemy.phaseWeights != null && i < CurrentEnemy.phaseWeights.Length)
                    return CurrentEnemy.phaseWeights[i].weights;
            }

            return CurrentEnemy.actionWeights;
        }

        // -----------------------------------------------------------------
        //  Round resolution (4×4 matchup table)
        //
        //  Logic:
        //    Dodge  beats Attack  (evasion avoids force)
        //    Dodge  beats Spell   (nimble enough to evade)
        //    Block  beats Attack  (absorbs the blow)
        //    Spell  beats Block   (magic pierces armor)
        //    Attack beats Block   → No: Block wins vs Attack
        //    Attack beats Dodge   → No: Dodge wins vs Attack
        //    Ties:  same action, both take partial damage or nothing
        // -----------------------------------------------------------------

        private CombatRoundResult ResolveRound(CombatAction player, CombatAction enemy)
        {
            var result = new CombatRoundResult();
            result.outcome = DetermineOutcome(player, enemy);

            int playerStr  = GameManager.Instance.Character.stats.strength;
            int enemyStr   = CurrentEnemy.strength;
            int enemyArmor = CurrentEnemy.armor;

            switch (result.outcome)
            {
                case CombatRoundOutcome.PlayerWins:
                    result.enemyDamageReceived  = CalcDamage(playerStr, player, enemyArmor);
                    result.playerDamageReceived = 0;
                    result.flavourText          = GetFlavourText(player, enemy, true);
                    break;

                case CombatRoundOutcome.EnemyWins:
                    result.playerDamageReceived = CalcDamage(enemyStr, enemy, 0);
                    result.enemyDamageReceived  = 0;
                    result.flavourText          = GetFlavourText(player, enemy, false);
                    break;

                case CombatRoundOutcome.Tie:
                    bool bothAttack = player == CombatAction.Attack && enemy == CombatAction.Attack;
                    bool bothSpell  = player == CombatAction.SpellItem && enemy == CombatAction.SpellItem;
                    if (bothAttack || bothSpell)
                    {
                        result.playerDamageReceived = CalcDamage(enemyStr, enemy, 0) / 2;
                        result.enemyDamageReceived  = CalcDamage(playerStr, player, enemyArmor) / 2;
                    }
                    // Dodge-Dodge or Block-Block: no damage
                    result.flavourText = GetFlavourText(player, enemy, false, tie: true);
                    break;
            }

            return result;
        }

        private CombatRoundOutcome DetermineOutcome(CombatAction player, CombatAction enemy)
        {
            if (player == enemy) return CombatRoundOutcome.Tie;

            // Player wins scenarios:
            bool playerWins =
                (player == CombatAction.Dodge  && enemy == CombatAction.Attack)   ||
                (player == CombatAction.Dodge  && enemy == CombatAction.SpellItem)||
                (player == CombatAction.Block  && enemy == CombatAction.Attack)   ||
                (player == CombatAction.SpellItem && enemy == CombatAction.Block) ||
                (player == CombatAction.Attack && enemy == CombatAction.SpellItem);

            if (playerWins) return CombatRoundOutcome.PlayerWins;
            return CombatRoundOutcome.EnemyWins;
        }

        private int CalcDamage(int attackerStrength, CombatAction action, int defenderArmor)
        {
            int baseDamage = Mathf.RoundToInt(attackerStrength / 10f) + Random.Range(1, 7);
            int armorReduction = action == CombatAction.SpellItem ? 0 : defenderArmor / 10;
            return Mathf.Max(1, baseDamage - armorReduction);
        }

        private string GetFlavourText(CombatAction player, CombatAction enemy, bool playerWon, bool tie = false)
        {
            if (tie) return $"You both {ActionName(player)} — a stalemate!";
            if (playerWon) return $"Your {ActionName(player)} beats their {ActionName(enemy)}!";
            return $"Their {ActionName(enemy)} overcomes your {ActionName(player)}.";
        }

        private string ActionName(CombatAction a) => a switch
        {
            CombatAction.Attack    => "attack",
            CombatAction.Dodge     => "dodge",
            CombatAction.Block     => "block",
            CombatAction.SpellItem => "spell",
            _ => a.ToString()
        };

        // -----------------------------------------------------------------
        //  Cooldown management (no-repeat rule)
        // -----------------------------------------------------------------

        private void TickCooldowns(CombatAction playerAction, CombatAction enemyAction)
        {
            int cd = balance != null ? balance.combatActionCooldown : 1;

            // Decrement existing cooldowns
            for (int i = 0; i < 4; i++)
            {
                if (State.playerCooldowns[i] > 0) State.playerCooldowns[i]--;
                if (State.enemyCooldowns[i]  > 0) State.enemyCooldowns[i]--;
            }

            // Set cooldown for used actions
            State.playerCooldowns[(int)playerAction] = cd;
            State.enemyCooldowns[(int)enemyAction]   = cd;
        }

        /// <summary>Returns which actions the player can currently pick.</summary>
        public List<CombatAction> GetAvailablePlayerActions()
        {
            var available = new List<CombatAction>();
            for (int i = 0; i < 4; i++)
            {
                if (State.playerCooldowns[i] == 0)
                {
                    var action = (CombatAction)i;
                    // Check stamina for SpellItem
                    if (action == CombatAction.SpellItem)
                    {
                        int cost = balance != null ? balance.spellStaminaCost : 10;
                        if (State.playerStamina >= cost)
                            available.Add(action);
                    }
                    else
                    {
                        available.Add(action);
                    }
                }
            }
            return available;
        }

        // -----------------------------------------------------------------
        //  End condition
        // -----------------------------------------------------------------

        private void CheckCombatEnd()
        {
            State.playerHP = Mathf.Max(0, State.playerHP);
            State.enemyHP  = Mathf.Max(0, State.enemyHP);

            // Enemy flee check
            float enemyHpPct = (float)State.enemyHP / State.enemyMaxHP;
            if (CurrentEnemy.fleeThreshold > 0 && enemyHpPct <= CurrentEnemy.fleeThreshold)
            {
                EndCombat(CombatResult.Victory, "Enemy fled");
                return;
            }

            if (State.enemyHP <= 0)
            {
                EndCombat(CombatResult.Victory, $"Defeated {CurrentEnemy.displayName}");
                return;
            }

            if (State.playerHP <= 0)
            {
                EndCombat(CombatResult.Defeat, CurrentEnemy.displayName);
            }
        }

        private void EndCombat(CombatResult result, string context)
        {
            State.isOver  = true;
            State.result  = result;

            var character = GameManager.Instance.Character;

            if (result == CombatResult.Victory)
            {
                // Apply rewards
                if (CurrentEnemy.victoryReward != null)
                {
                    if (CurrentEnemy.victoryReward.statChanges != null)
                        foreach (var delta in CurrentEnemy.victoryReward.statChanges)
                            character.stats.ApplyDelta(delta);

                    character.stats.gold += CurrentEnemy.victoryReward.goldReward;
                    character.stats.Clamp();
                }

                HistoryLog.Instance.Record(
                    HistoryCategory.Combat,
                    $"Defeated {CurrentEnemy.displayName} in combat.",
                    iconId: "combat_win"
                );
            }
            else if (result == CombatResult.Defeat)
            {
                switch (CurrentEnemy.defeatConsequence)
                {
                    case DefeatConsequence.Death:
                        character.isAlive      = false;
                        character.causeOfDeath = CurrentEnemy.displayName;
                        EventBus.Publish(new CharacterDiedEvent
                        {
                            causeOfDeath = character.causeOfDeath,
                            finalAge     = character.stats.age
                        });
                        break;

                    case DefeatConsequence.Injury:
                        character.stats.health -= 20;
                        character.stats.Clamp();
                        HistoryLog.Instance.Record(HistoryCategory.Combat,
                            $"Was defeated by {CurrentEnemy.displayName} and suffered injuries.");
                        break;

                    case DefeatConsequence.Capture:
                        character.AddTag("captured");
                        HistoryLog.Instance.Record(HistoryCategory.Combat,
                            $"Was captured by {CurrentEnemy.displayName}.");
                        break;
                }
            }

            EventBus.Publish(new CombatEndedEvent
            {
                result     = result,
                enemyName  = CurrentEnemy.displayName
            });
        }
    }
}
