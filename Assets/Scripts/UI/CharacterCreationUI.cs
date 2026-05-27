// ============================================================
//  CharacterCreationUI.cs
//  Lets the player set name, race, gender, and starting
//  background before beginning a new life.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YVOS.Character;
using YVOS.Core;
using YVOS.Relationships;

namespace YVOS.UI
{
    public class CharacterCreationUI : MonoBehaviour
    {
        [Header("Name")]
        [SerializeField] private TMP_InputField firstNameInput;
        [SerializeField] private TMP_InputField lastNameInput;

        [Header("Gender Buttons")]
        [SerializeField] private Button maleButton;
        [SerializeField] private Button femaleButton;
        [SerializeField] private Button otherButton;

        [Header("Race")]
        [SerializeField] private Transform raceButtonContainer;
        [SerializeField] private Button    raceButtonPrefab;
        [SerializeField] private TextMeshProUGUI raceDescriptionText;

        [Header("Background")]
        [SerializeField] private Transform backgroundButtonContainer;
        [SerializeField] private Button    backgroundButtonPrefab;
        [SerializeField] private TextMeshProUGUI backgroundDescriptionText;

        [Header("Begin")]
        [SerializeField] private Button beginLifeButton;

        // -----------------------------------------------------------------
        //  Starting data
        // -----------------------------------------------------------------
        private static readonly RaceOption[] Races =
        {
            new RaceOption("human",    "Human",    "Adaptable and ambitious. No special bonuses but gain +5 to all stats from backgrounds.",
                health:60, happiness:55, smarts:50, looks:50, strength:50, magic:20),
            new RaceOption("elf",      "Elf",      "Graceful and long-lived. +15 Looks, +15 Magic, -5 Strength. Slower aging after 60.",
                health:55, happiness:60, smarts:60, looks:65, strength:35, magic:45),
            new RaceOption("dwarf",    "Dwarf",    "Stalwart and industrious. +20 Strength, +10 Health, -10 Magic. Lives to ~200.",
                health:70, happiness:50, smarts:45, looks:40, strength:65, magic:10),
            new RaceOption("orc",      "Orc",      "Fierce and proud. +25 Strength, +10 Health, -15 Looks. Ages faster.",
                health:70, happiness:45, smarts:40, looks:35, strength:70, magic:15),
            new RaceOption("halfling", "Halfling", "Small and clever. +15 Smarts, +15 Looks, -10 Strength. High luck in events.",
                health:50, happiness:65, smarts:65, looks:60, strength:30, magic:25),
            new RaceOption("tiefling", "Tiefling", "Infernal heritage. +25 Magic, +10 Looks, -5 Happiness. Unique magic events.",
                health:55, happiness:45, smarts:55, looks:60, strength:40, magic:70),
        };

        private static readonly BackgroundOption[] Backgrounds =
        {
            new BackgroundOption("serf",     "Serf Child",       "Born into bondage. Start with 10g, Serf class. A hard life ahead.",
                socialClass:SocialClass.Serf, gold:10),
            new BackgroundOption("peasant",  "Village Artisan",  "Born to a craftsman family. Start with 50g, Peasant class.",
                socialClass:SocialClass.Peasant, gold:50),
            new BackgroundOption("merchant", "Merchant Family",  "Born into trade. Start with 200g, Merchant class, +10 Smarts.",
                socialClass:SocialClass.Merchant, gold:200, smartsBonus:10),
            new BackgroundOption("noble",    "Minor Noble",      "Born with privilege. Start with 500g, Noble class, +10 Rep, -10 Strength.",
                socialClass:SocialClass.Noble, gold:500, repBonus:10, strengthBonus:-10),
        };

        // -----------------------------------------------------------------
        //  State
        // -----------------------------------------------------------------
        private Gender          _selectedGender     = Gender.Male;
        private string          _selectedRaceId     = "human";
        private string          _selectedBackground = "peasant";

        // -----------------------------------------------------------------
        //  Unity lifecycle
        // -----------------------------------------------------------------
        private void Start()
        {
            BuildRaceButtons();
            BuildBackgroundButtons();
            SetGender(Gender.Male);
        }

        // -----------------------------------------------------------------
        //  Build dynamic buttons
        // -----------------------------------------------------------------

        private void BuildRaceButtons()
        {
            foreach (Transform child in raceButtonContainer) Destroy(child.gameObject);
            foreach (var race in Races)
            {
                var r   = race;
                var btn = Instantiate(raceButtonPrefab, raceButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = r.displayName;
                btn.onClick.AddListener(() =>
                {
                    _selectedRaceId         = r.id;
                    raceDescriptionText.text = r.description;
                });
            }
        }

        private void BuildBackgroundButtons()
        {
            foreach (Transform child in backgroundButtonContainer) Destroy(child.gameObject);
            foreach (var bg in Backgrounds)
            {
                var b   = bg;
                var btn = Instantiate(backgroundButtonPrefab, backgroundButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = b.displayName;
                btn.onClick.AddListener(() =>
                {
                    _selectedBackground            = b.id;
                    backgroundDescriptionText.text = b.description;
                });
            }
        }

        // -----------------------------------------------------------------
        //  Gender buttons
        // -----------------------------------------------------------------

        public void OnMalePressed()   => SetGender(Gender.Male);
        public void OnFemalePressed() => SetGender(Gender.Female);
        public void OnOtherPressed()  => SetGender(Gender.Other);

        private void SetGender(Gender g) => _selectedGender = g;

        // -----------------------------------------------------------------
        //  Begin Life
        // -----------------------------------------------------------------

        public void OnBeginLifePressed()
        {
            string firstName = firstNameInput.text.Trim();
            string lastName  = lastNameInput.text.Trim();

            if (string.IsNullOrEmpty(firstName)) firstName = "Aldric";
            if (string.IsNullOrEmpty(lastName))  lastName  = "Ashford";

            // Find selected race and background
            RaceOption      race = System.Array.Find(Races, r => r.id == _selectedRaceId)
                                   ?? Races[0];
            BackgroundOption bg  = System.Array.Find(Backgrounds, b => b.id == _selectedBackground)
                                   ?? Backgrounds[1];

            // Build CharacterData
            var character = new CharacterData();
            var stats     = character.stats;

            stats.firstName     = firstName;
            stats.lastName      = lastName;
            stats.raceId        = race.id;
            stats.gender        = _selectedGender;
            stats.socialClass   = bg.socialClass;
            stats.age           = 0;
            stats.birthYear     = 850;  // world year at start
            stats.lifeStage     = LifeStage.Infant;

            // Race base stats
            stats.health        = race.baseHealth;
            stats.happiness     = race.baseHappiness;
            stats.smarts        = race.baseSmarts;
            stats.looks         = race.baseLooks;
            stats.strength      = race.baseStrength;
            stats.magicAffinity = race.baseMagic;
            stats.piety         = 30;

            // Background bonuses
            stats.gold          = bg.startingGold;
            stats.smarts       += bg.smartsBonus;
            stats.strength     += bg.strengthBonus;
            stats.reputation   += bg.repBonus;

            stats.Clamp();

            // Add race tag
            character.AddTag($"race_{race.id}");

            // Start game
            GameManager.Instance.StartNewGame(character, startYear: 850);

            // Generate family
            RelationshipManager.Instance.GenerateFamily(character);

            SceneManager.LoadScene("GameScene");
        }
    }

    // -----------------------------------------------------------------
    //  Helper data classes
    // -----------------------------------------------------------------

    public class RaceOption
    {
        public string id, displayName, description;
        public int baseHealth, baseHappiness, baseSmarts, baseLooks, baseStrength, baseMagic;

        public RaceOption(string id, string displayName, string description,
            int health, int happiness, int smarts, int looks, int strength, int magic)
        {
            this.id = id; this.displayName = displayName; this.description = description;
            baseHealth = health; baseHappiness = happiness; baseSmarts = smarts;
            baseLooks = looks; baseStrength = strength; baseMagic = magic;
        }
    }

    public class BackgroundOption
    {
        public string id, displayName, description;
        public SocialClass socialClass;
        public int startingGold, smartsBonus, strengthBonus, repBonus;

        public BackgroundOption(string id, string displayName, string description,
            SocialClass socialClass, int gold, int smartsBonus = 0, int strengthBonus = 0, int repBonus = 0)
        {
            this.id = id; this.displayName = displayName; this.description = description;
            this.socialClass = socialClass; startingGold = gold;
            this.smartsBonus = smartsBonus; this.strengthBonus = strengthBonus; this.repBonus = repBonus;
        }
    }
}
