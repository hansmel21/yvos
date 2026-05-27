// ============================================================
//  MiniGameEngine.cs
//  Drives the stage-by-stage mini-game flow.
//  Called from ActionMenuUI; presents stages via MiniGamePopupUI.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using YVOS.Character;
using YVOS.Core;
using YVOS.History;

namespace YVOS.MiniGames
{
    public class MiniGameEngine : MonoBehaviour
    {
        public static MiniGameEngine Instance { get; private set; }

        private MiniGameDefinitionSO[] _allMiniGames;

        // Runtime state for the active mini-game
        private MiniGameDefinitionSO _activeMiniGame;
        private int _currentStageIndex;
        private bool _wasSuccessful;

        // Cooldown tracking: key = miniGameId, value = last world year played
        private Dictionary<string, int> _lastPlayedYear = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _allMiniGames = Resources.LoadAll<MiniGameDefinitionSO>("MiniGames");
            Debug.Log($"[MiniGameEngine] Loaded {_allMiniGames.Length} mini-games.");
        }

        // -----------------------------------------------------------------
        //  Public API
        // -----------------------------------------------------------------

        public bool CanPlay(string miniGameId)
        {
            if (!_lastPlayedYear.TryGetValue(miniGameId, out int lastYear)) return true;
            var mg = GetMiniGame(miniGameId);
            if (mg == null) return false;
            return (GameManager.Instance.WorldYear - lastYear) >= mg.cooldownYears;
        }

        public void StartMiniGame(string miniGameId)
        {
            var mg = GetMiniGame(miniGameId);
            if (mg == null)
            {
                Debug.LogError($"[MiniGameEngine] Mini-game not found: {miniGameId}");
                return;
            }

            if (!CanPlay(miniGameId))
            {
                Debug.Log($"[MiniGameEngine] {miniGameId} is on cooldown.");
                return;
            }

            _activeMiniGame    = mg;
            _currentStageIndex = 0;
            _wasSuccessful     = false;

            EventBus.Publish(new MiniGameStartedEvent { miniGameId = miniGameId });

            // Show first stage (UI listens to MiniGameStartedEvent)
        }

        /// <summary>
        /// Called by MiniGamePopupUI when the player picks a choice.
        /// Returns the next stage to show, or null if the game ended.
        /// </summary>
        public MiniGameStage SubmitChoice(int choiceIndex)
        {
            if (_activeMiniGame == null) return null;

            var stage  = _activeMiniGame.stages[_currentStageIndex];
            var choice = stage.choices[choiceIndex];

            // Stat check
            int nextStage;
            if (choice.ignoreStatCheck)
            {
                nextStage = choice.nextStageOnSuccess;
            }
            else
            {
                bool passed = EvaluateStatCheck(choice);
                nextStage   = passed ? choice.nextStageOnSuccess : choice.nextStageOnFailure;
            }

            // -1 = end
            if (nextStage == -1)
            {
                bool success = (choice.nextStageOnSuccess == -1 && !choice.ignoreStatCheck)
                               || choice.ignoreStatCheck;
                EndMiniGame(success);
                return null;
            }

            _currentStageIndex = nextStage;
            return _activeMiniGame.stages[_currentStageIndex];
        }

        public MiniGameStage GetCurrentStage()
            => _activeMiniGame != null ? _activeMiniGame.stages[_currentStageIndex] : null;

        public MiniGameDefinitionSO GetActiveMiniGame() => _activeMiniGame;

        // -----------------------------------------------------------------
        //  Internal
        // -----------------------------------------------------------------

        private bool EvaluateStatCheck(MiniGameChoice choice)
        {
            if (choice.statThreshold == 0) return true;

            var stats = GameManager.Instance.Character.stats;
            int statValue = choice.checkStat?.ToLowerInvariant() switch
            {
                "health"        => stats.health,
                "happiness"     => stats.happiness,
                "smarts"        => stats.smarts,
                "looks"         => stats.looks,
                "strength"      => stats.strength,
                "magicaffinity" => stats.magicAffinity,
                "piety"         => stats.piety,
                _               => 50
            };

            // Partial success: probability scales with stat vs threshold
            float ratio      = (float)statValue / choice.statThreshold;
            float successChance = Mathf.Clamp(ratio, 0.1f, 1f);
            return Random.value < successChance;
        }

        private void EndMiniGame(bool success)
        {
            _lastPlayedYear[_activeMiniGame.miniGameId] = GameManager.Instance.WorldYear;

            var character = GameManager.Instance.Character;
            if (success)
            {
                if (_activeMiniGame.successReward != null)
                    foreach (var delta in _activeMiniGame.successReward)
                        character.stats.ApplyDelta(delta);

                character.stats.gold += _activeMiniGame.successGoldReward;
                HistoryLog.Instance.Record(HistoryCategory.MiniGame,
                    $"Completed '{_activeMiniGame.title}' successfully.",
                    iconId: "minigame_win");
            }
            else
            {
                if (_activeMiniGame.failurePenalty != null)
                    foreach (var delta in _activeMiniGame.failurePenalty)
                        character.stats.ApplyDelta(delta);

                character.stats.gold = Mathf.Max(0, character.stats.gold - _activeMiniGame.failureGoldPenalty);
                HistoryLog.Instance.Record(HistoryCategory.MiniGame,
                    $"Failed '{_activeMiniGame.title}'.",
                    iconId: "minigame_fail");
            }

            character.stats.Clamp();

            EventBus.Publish(new MiniGameCompletedEvent
            {
                miniGameId = _activeMiniGame.miniGameId,
                success    = success
            });

            _activeMiniGame = null;
        }

        private MiniGameDefinitionSO GetMiniGame(string id)
        {
            foreach (var mg in _allMiniGames)
                if (mg.miniGameId == id) return mg;
            return null;
        }
    }
}
