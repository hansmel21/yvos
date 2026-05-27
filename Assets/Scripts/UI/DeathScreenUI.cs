// ============================================================
//  DeathScreenUI.cs
//  Shown when the character dies. Displays life summary and
//  offers "Play as Child" (dynasty) or "New Life".
// ============================================================
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using YVOS.Character;
using YVOS.Core;
using YVOS.Relationships;

namespace YVOS.UI
{
    public class DeathScreenUI : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI yearsText;
        [SerializeField] private TextMeshProUGUI causeText;

        [Header("Summary Stats")]
        [SerializeField] private TextMeshProUGUI socialClassText;
        [SerializeField] private TextMeshProUGUI guildText;
        [SerializeField] private TextMeshProUGUI wealthText;
        [SerializeField] private TextMeshProUGUI childrenText;
        [SerializeField] private TextMeshProUGUI marriagesText;

        [Header("Epitaph")]
        [SerializeField] private TextMeshProUGUI epitaphText;

        [Header("Buttons")]
        [SerializeField] private GameObject playAsChildButton;   // hidden if no living children

        [Header("Panel")]
        [SerializeField] private GameObject deathPanel;

        // -----------------------------------------------------------------

        private void OnEnable()
        {
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        private void OnCharacterDied(CharacterDiedEvent evt)
        {
            Populate();
            deathPanel.SetActive(true);
        }

        // -----------------------------------------------------------------
        //  Populate
        // -----------------------------------------------------------------

        private void Populate()
        {
            var character = GameManager.Instance.Character;
            var stats     = character.stats;

            nameText.text   = $"✦  {stats.FullName}  ✦";
            yearsText.text  = $"{stats.birthYear} – {GameManager.Instance.WorldYear}  ·  Age {stats.age}";
            causeText.text  = $"Died of: {character.causeOfDeath}";

            socialClassText.text = $"Final Social Class: {stats.socialClass}";
            guildText.text       = BuildGuildSummary(character);
            wealthText.text      = $"Wealth Left: {stats.gold}g";

            int childCount = character.relationships.Count(r => r.type == RelationshipType.Child);
            int spouseCount = character.relationships.Count(r =>
                r.type == RelationshipType.Spouse || r.type == RelationshipType.ExSpouse);

            childrenText.text  = $"Children: {childCount}";
            marriagesText.text = $"Marriages: {spouseCount}";

            epitaphText.text = GenerateEpitaph(character);

            // Show dynasty button only if a living child exists
            bool hasLivingChild = character.relationships.Any(r =>
                r.type == RelationshipType.Child && r.isAlive);
            playAsChildButton.SetActive(hasLivingChild);
        }

        private string BuildGuildSummary(CharacterData character)
        {
            if (character.guildMemberships.Count == 0) return "Guild: None";
            var gm   = Career.GuildManager.Instance;
            var last = character.guildMemberships[character.guildMemberships.Count - 1];
            var guild = gm.GetGuild(last);
            string title = gm.GetRankTitle(character, last);
            return $"Guild: {title}, {guild?.displayName ?? last}";
        }

        private string GenerateEpitaph(CharacterData character)
        {
            var stats = character.stats;
            if (stats.reputation > 60)  return "\"A life remembered fondly by all.\"";
            if (stats.reputation < -30) return "\"Few mourned the passing.\"";
            if (stats.magicAffinity > 70) return "\"The arcane arts claimed another devotee.\"";
            if (stats.strength > 70)    return "\"A warrior born, a warrior died.\"";
            if (stats.gold > 2000)      return "\"Wealth was their monument.\"";
            return "\"A life lived, in full.\"";
        }

        // -----------------------------------------------------------------
        //  Button callbacks
        // -----------------------------------------------------------------

        public void OnPlayAsChildPressed()
        {
            var character = GameManager.Instance.Character;
            var child     = character.relationships.FirstOrDefault(r =>
                r.type == RelationshipType.Child && r.isAlive);

            if (child == null) return;

            // Create a new CharacterData seeded from the child relationship
            var newChar = new CharacterData();
            newChar.stats.firstName  = child.firstName;
            newChar.stats.lastName   = child.lastName;
            newChar.stats.raceId     = child.raceId;
            newChar.stats.gender     = child.gender;
            newChar.stats.age        = GameManager.Instance.WorldYear - child.birthYear;
            newChar.stats.lifeStage  = CharacterLifecycle.CalculateLifeStage(newChar.stats.age);
            newChar.stats.socialClass = character.stats.socialClass; // inherit social class

            // Inherit some wealth
            newChar.stats.gold       = character.stats.gold / 2;
            newChar.stats.reputation = character.stats.reputation / 4;

            // Seed base stats
            newChar.stats.health = 80; newChar.stats.happiness = 60;
            newChar.stats.smarts = Random.Range(30, 70);
            newChar.stats.looks  = Random.Range(30, 70);
            newChar.stats.strength = Random.Range(20, 60);
            newChar.stats.magicAffinity = character.stats.magicAffinity / 4;
            newChar.stats.Clamp();

            GameManager.Instance.StartNewGame(newChar, GameManager.Instance.WorldYear);
            SceneManager.LoadScene("GameScene");
        }

        public void OnNewLifePressed()
        {
            SceneManager.LoadScene("CharacterCreation");
        }

        public void OnViewLifeLogPressed()
        {
            // Show full history log panel (History Log UI handles this)
        }
    }
}
