// ============================================================
//  GameManager.cs
//  Singleton. Owns the live CharacterData reference and the
//  current world year. All other systems reach character data
//  through GameManager.Instance.Character.
// ============================================================
using UnityEngine;
using YVOS.Character;
using YVOS.Persistence;

namespace YVOS.Core
{
    public class GameManager : MonoBehaviour
    {
        // -----------------------------------------------------------------
        //  Singleton
        // -----------------------------------------------------------------
        public static GameManager Instance { get; private set; }

        // -----------------------------------------------------------------
        //  State
        // -----------------------------------------------------------------
        public CharacterData Character { get; private set; }
        public int WorldYear { get; private set; } = 850;

        // -----------------------------------------------------------------
        //  Unity lifecycle
        // -----------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            EventBus.ClearAll();
        }

        // -----------------------------------------------------------------
        //  Character management
        // -----------------------------------------------------------------

        /// <summary>
        /// Creates a brand-new character and starts a new game.
        /// Called from CharacterCreationUI after the player fills in the form.
        /// </summary>
        public void StartNewGame(CharacterData newCharacter, int startYear = 850)
        {
            Character  = newCharacter;
            WorldYear  = startYear;
            EventBus.ClearAll();
            Debug.Log($"[GameManager] New game started: {Character.stats.FullName}");
        }

        /// <summary>Advance the world year. Delegates to TimeManager.</summary>
        public void AdvanceYear()
        {
            WorldYear++;
        }

        /// <summary>
        /// Restore game state from a loaded save.
        /// </summary>
        public void RestoreFromSave(SaveData save)
        {
            Character = save.character;
            WorldYear = save.worldYear;
            EventBus.ClearAll();
            Debug.Log($"[GameManager] Loaded save for {Character.stats.FullName}, age {Character.stats.age}");
        }
    }
}
