// ============================================================
//  SaveManager.cs
//  Handles JSON save/load with 3 slots.
//  Uses Newtonsoft.Json for robust object graph serialization.
//  Auto-saves on YearEndedEvent.
// ============================================================
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using YVOS.Core;

namespace YVOS.Persistence
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const int SLOT_COUNT = 3;
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling      = TypeNameHandling.None,
            NullValueHandling     = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting            = Formatting.Indented
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<YearEndedEvent>(OnYearEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<YearEndedEvent>(OnYearEnded);
        }

        // -----------------------------------------------------------------
        //  Auto-save
        // -----------------------------------------------------------------

        private void OnYearEnded(YearEndedEvent evt)
        {
            // Auto-save to slot 0 (reserved for auto-save)
            Save(0);
        }

        // -----------------------------------------------------------------
        //  Save
        // -----------------------------------------------------------------

        public void Save(int slot)
        {
            if (slot < 0 || slot >= SLOT_COUNT)
            {
                Debug.LogError($"[SaveManager] Invalid slot {slot}.");
                return;
            }

            var data = new SaveData
            {
                saveVersion = "1.0",
                savedAt     = DateTime.UtcNow.ToString("o"),
                character   = GameManager.Instance.Character,
                worldYear   = GameManager.Instance.WorldYear
            };

            try
            {
                string json = JsonConvert.SerializeObject(data, _jsonSettings);
                File.WriteAllText(GetSlotPath(slot), json);
                Debug.Log($"[SaveManager] Saved slot {slot} — {data.character.stats.FullName}, age {data.character.stats.age}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
            }
        }

        // -----------------------------------------------------------------
        //  Load
        // -----------------------------------------------------------------

        public bool Load(int slot)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveManager] No save found in slot {slot}.");
                return false;
            }

            try
            {
                string   json = File.ReadAllText(path);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json, _jsonSettings);
                GameManager.Instance.RestoreFromSave(data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
                return false;
            }
        }

        // -----------------------------------------------------------------
        //  Slot summaries (for the Load Game screen)
        // -----------------------------------------------------------------

        public SaveSlotSummary GetSummary(int slot)
        {
            var summary = new SaveSlotSummary { slot = slot, isEmpty = true };
            string path = GetSlotPath(slot);
            if (!File.Exists(path)) return summary;

            try
            {
                string   json = File.ReadAllText(path);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json, _jsonSettings);
                var stats = data.character.stats;

                summary.isEmpty       = false;
                summary.characterName = stats.FullName;
                summary.age           = stats.age;
                summary.socialClass   = stats.socialClass.ToString();
                summary.savedAt       = data.savedAt;
            }
            catch { /* return empty summary */ }

            return summary;
        }

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

        private string GetSlotPath(int slot)
        {
            string dir = Path.Combine(Application.persistentDataPath, "saves");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"slot_{slot}.json");
        }
    }
}
