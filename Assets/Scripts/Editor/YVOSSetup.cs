// ============================================================
//  YVOSSetup.cs  —  EDITOR ONLY
//  Auto-runs on script reload AND as manual menu items.
//
//  Auto-behaviour: when Unity reloads scripts (e.g. after a
//  git pull), it checks for missing assets and creates them.
//  Scene is NOT auto-rebuilt (too destructive) — run the
//  menu item once manually after first clone.
//
//  Menu: YVOS → Setup → 1 - Create All Assets
//         YVOS → Setup → 2 - Build GameScene
// ============================================================
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using YVOS.Character;
using YVOS.Events;
using YVOS.Combat;
using YVOS.Career;

namespace YVOS.Editor
{
    [InitializeOnLoad]
    public static class YVOSSetup
    {
        // ================================================================
        //  Auto-run on every script reload
        //  Skips gracefully if assets already exist.
        // ================================================================
        static YVOSSetup()
        {
            // Defer one frame so the AssetDatabase is fully ready
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        static void AutoSetupIfNeeded()
        {
            bool balanceMissing = AssetDatabase.LoadAssetAtPath<Data.GameBalanceSO>("Assets/Data/GameBalance.asset") == null;
            bool eventsMissing  = !AssetDatabase.IsValidFolder("Assets/Data/Events/Resources/Events/Common") ||
                                   AssetDatabase.FindAssets("t:LifeEventSO", new[] { "Assets/Data" }).Length == 0;

            if (balanceMissing || eventsMissing)
            {
                Debug.Log("[YVOS] New or missing assets detected — running auto-setup...");
                CreateAllAssets();
            }
        }

        // ================================================================
        //  MENU: Create All ScriptableObject Assets
        // ================================================================
        [MenuItem("YVOS/Setup/1 - Create All Assets")]
        public static void CreateAllAssets()
        {
            CreateGameBalance();
            CreateLifeEvents();
            CreateGuilds();
            CreateEnemies();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[YVOS Setup] All assets created successfully!");
            EditorUtility.DisplayDialog("YVOS Setup", "All ScriptableObject assets created!\n\nNow run:\nYVOS → Setup → 2 - Build GameScene", "OK");
        }

        // ================================================================
        //  MENU: Build GameScene
        // ================================================================
        [MenuItem("YVOS/Setup/2 - Build GameScene")]
        public static void BuildGameScene()
        {
            // Save current scene first
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Build the hierarchy
            var managers  = BuildManagersHierarchy();
            var canvas    = BuildUICanvas();
            BuildEventSystem();

            // Save scene
            string scenePath = "Assets/Scenes/GameScene.unity";
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log("[YVOS Setup] GameScene built at " + scenePath);
            EditorUtility.DisplayDialog("YVOS Setup",
                "GameScene created at Assets/Scenes/GameScene.unity\n\n" +
                "Next steps:\n" +
                "1. Open GameScene\n" +
                "2. Assign the GameBalance SO to CharacterLifecycle, CombatEngine, EconomyManager\n" +
                "3. Wire Inspector references in the UI scripts\n" +
                "4. Create UI prefabs for buttons/sliders\n" +
                "5. Hit Play!", "OK");
        }

        // ================================================================
        //  GAME BALANCE SO
        // ================================================================
        static void CreateGameBalance()
        {
            EnsureDir("Assets/Data");
            var path = "Assets/Data/GameBalance.asset";
            if (AssetDatabase.LoadAssetAtPath<Data.GameBalanceSO>(path) != null)
            {
                Debug.Log("[YVOS Setup] GameBalance.asset already exists, skipping.");
                return;
            }
            var so = ScriptableObject.CreateInstance<Data.GameBalanceSO>();
            // defaults are set in the class
            AssetDatabase.CreateAsset(so, path);
            Debug.Log("[YVOS Setup] Created GameBalance.asset");
        }

        // ================================================================
        //  LIFE EVENTS
        // ================================================================
        static void CreateLifeEvents()
        {
            EnsureDir("Assets/Data/Events/Resources/Events/Common");
            EnsureDir("Assets/Data/Events/Resources/Events/Childhood");
            EnsureDir("Assets/Data/Events/Resources/Events/Adult");
            EnsureDir("Assets/Data/Events/Resources/Events/Elder");

            // --- INFANT events (age 0-3) ---
            CreateEvent("evt_difficult_birth", "A Difficult Birth",
                "The midwife shakes her head — {name}'s entry into the world was not easy.",
                minAge: 0, maxAge: 0, weight: 1f,
                folder: "Common",
                choices: new[]
                {
                    MakeChoice("Survive through sheer will",
                        "Against the odds, {name} draws breath.",
                        new StatDelta("health", -10)),
                    MakeChoice("The healer is called (costs 30g)",
                        "The healer stabilises mother and child.",
                        new StatDelta("gold", -30), new StatDelta("health", 5))
                });

            CreateEvent("evt_first_steps", "First Steps",
                "At one year old, {name} takes their first wobbling steps across the cottage floor.",
                minAge: 1, maxAge: 2, weight: 2f,
                folder: "Common",
                choices: new[]
                {
                    MakeChoice("Encouraged by family",
                        "The whole family cheers. A joyful moment.",
                        new StatDelta("happiness", 5))
                });

            // --- CHILD events (age 4-11) ---
            CreateEvent("evt_village_school", "The Village School",
                "A travelling scholar offers lessons in the village square. Most children just play.",
                minAge: 6, maxAge: 11, weight: 2f,
                folder: "Childhood",
                choices: new[]
                {
                    MakeChoice("Attend every lesson",
                        "The lessons open {name}'s mind to new ideas.",
                        new StatDelta("smarts", 8), new StatDelta("happiness", -3)),
                    MakeChoice("Skip and play instead",
                        "A carefree childhood afternoon.",
                        new StatDelta("happiness", 8), new StatDelta("smarts", -3))
                });

            CreateEvent("evt_bully", "The Bully",
                "A larger child has been picking on {name} after school.",
                minAge: 7, maxAge: 12, weight: 1.5f,
                folder: "Childhood",
                choices: new[]
                {
                    MakeChoice("Stand up and fight",
                        "{name} throws the first punch. Win or lose, the bullying stops.",
                        new StatDelta("strength", 5), new StatDelta("health", -5)),
                    MakeChoice("Tell a parent",
                        "The bully is reprimanded. {name} feels safe but a little embarrassed.",
                        new StatDelta("happiness", -3)),
                    MakeChoice("Ignore it",
                        "The taunts sting. {name} learns to endure.",
                        new StatDelta("happiness", -8), new StatDelta("smarts", 3))
                });

            CreateEvent("evt_magic_tome", "A Mysterious Tome",
                "While playing near the old ruins, {name} finds a weathered book filled with strange symbols.",
                minAge: 8, maxAge: 12, weight: 0.8f,
                folder: "Childhood",
                choices: new[]
                {
                    MakeChoice("Study it obsessively",
                        "Strange tingling fills {name}'s fingers. Something has awakened.",
                        new StatDelta("magicAffinity", 12), new StatDelta("happiness", 5)),
                    MakeChoice("Bring it to the village elder",
                        "The elder is impressed. {name} gains a reputation for honesty.",
                        new StatDelta("reputation", 8), new StatDelta("piety", 5)),
                    MakeChoice("Burn it — it feels wrong",
                        "The smoke smells of sulfur. Probably the right call.",
                        new StatDelta("piety", 8))
                });

            CreateEvent("evt_harvest_festival", "The Harvest Festival",
                "The village erupts in celebration. Three days of food, music, and dancing.",
                minAge: 5, maxAge: 15, weight: 2f,
                folder: "Childhood",
                choices: new[]
                {
                    MakeChoice("Dance until dawn",
                        "The best night of {name}'s young life.",
                        new StatDelta("happiness", 12), new StatDelta("looks", 3)),
                    MakeChoice("Help organise the feast",
                        "The elders notice {name}'s reliability.",
                        new StatDelta("reputation", 6), new StatDelta("smarts", 3))
                });

            // --- TEEN events (age 12-17) ---
            CreateEvent("evt_first_love", "A Heart Aflutter",
                "At the well one morning, {name}'s eyes meet those of someone rather remarkable.",
                minAge: 13, maxAge: 17, weight: 1.5f,
                folder: "Childhood",
                choices: new[]
                {
                    MakeChoice("Pursue the romance",
                        "A stolen afternoon, a shared secret. {name}'s heart soars.",
                        new StatDelta("happiness", 15), new StatDelta("looks", 5)),
                    MakeChoice("Focus on more important things",
                        "There will be time for love later. Perhaps.",
                        new StatDelta("smarts", 5))
                },
                addTags: new[] { "had_romance" });

            CreateEvent("evt_apprenticeship", "An Offer of Apprenticeship",
                "A local craftsman has noticed {name}'s work ethic and offers a formal apprenticeship.",
                minAge: 14, maxAge: 17, weight: 1.5f,
                folder: "Childhood",
                choices: new[]
                {
                    MakeChoice("Accept the apprenticeship",
                        "{name} begins learning the trade. Hard work, but rewarding.",
                        new StatDelta("strength", 8), new StatDelta("smarts", 5), new StatDelta("gold", 20)),
                    MakeChoice("Decline — bigger ambitions await",
                        "{name} dreams of more than a village workshop.",
                        new StatDelta("happiness", -3), new StatDelta("reputation", -3))
                });

            CreateEvent("evt_tavern_brawl", "Tavern Brawl",
                "A night at the inn turns ugly when a drunk merchant accuses {name} of cheating at cards.",
                minAge: 15, maxAge: 99, weight: 1.2f,
                folder: "Common",
                choices: new[]
                {
                    MakeChoice("Fight back",
                        "Fists fly. {name} gives as good as they get.",
                        new StatDelta("strength", 5), new StatDelta("health", -8), new StatDelta("reputation", -5)),
                    MakeChoice("Talk your way out",
                        "A quick tongue and a calm voice defuse the situation.",
                        new StatDelta("smarts", 5), new StatDelta("reputation", 3)),
                    MakeChoice("Pay the man off",
                        "Cheaper than stitches.",
                        new StatDelta("gold", -15), new StatDelta("happiness", -3))
                });

            // --- YOUNG ADULT events (age 18-24) ---
            CreateEvent("evt_leave_home", "Leaving the Nest",
                "At {age}, {name} stands at the crossroads outside the village, the whole world ahead.",
                minAge: 18, maxAge: 18, weight: 3f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Head to the nearest city",
                        "{name} arrives in the city with nothing but ambition.",
                        new StatDelta("happiness", 10), new StatDelta("smarts", 5)),
                    MakeChoice("Stay close to home",
                        "The familiar is comforting. {name} builds roots nearby.",
                        new StatDelta("happiness", 5), new StatDelta("reputation", 5), new StatDelta("gold", 30))
                });

            CreateEvent("evt_bandit_ambush", "Ambushed!",
                "On the road between villages, {name} is set upon by armed bandits.",
                minAge: 16, maxAge: 60, weight: 1f,
                folder: "Adult",
                triggersCombat: true,
                choices: new[]
                {
                    MakeChoice("Stand and fight",
                        "Draw your weapon. This ends now.",
                        new StatDelta("strength", 0))
                });

            CreateEvent("evt_city_festival", "A Grand Festival",
                "The city is alive with colour and noise — a week-long celebration of the realm's founding.",
                minAge: 18, maxAge: 50, weight: 1.8f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Enjoy every moment",
                        "Food, wine, and music. Life is good.",
                        new StatDelta("happiness", 12)),
                    MakeChoice("Network with merchants and nobles",
                        "{name} collects useful contacts.",
                        new StatDelta("reputation", 8), new StatDelta("smarts", 3)),
                    MakeChoice("Enter the fighting contest",
                        "Third place — but the crowd loved it.",
                        new StatDelta("strength", 8), new StatDelta("looks", 5), new StatDelta("gold", 30))
                });

            // --- ADULT events (age 25-44) ---
            CreateEvent("evt_plague", "The Sweating Sickness",
                "A grey plague has crept into the region. Neighbours are falling ill.",
                minAge: 20, maxAge: 70, weight: 0.8f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Isolate and pray",
                        "Three terrible weeks pass. {name} emerges weakened but alive.",
                        new StatDelta("health", -15), new StatDelta("piety", 8)),
                    MakeChoice("Help nurse the sick",
                        "The risk is real, but so is the gratitude.",
                        new StatDelta("health", -10), new StatDelta("reputation", 15), new StatDelta("happiness", 5)),
                    MakeChoice("Flee the region",
                        "{name} runs, and survives. Others did not.",
                        new StatDelta("health", 5), new StatDelta("reputation", -10), new StatDelta("happiness", -8))
                });

            CreateEvent("evt_noble_request", "A Noble's Errand",
                "A minor lord requests {name}'s assistance with a sensitive matter.",
                minAge: 20, maxAge: 60, weight: 1f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Accept the errand",
                        "{name} proves reliable. The lord takes note.",
                        new StatDelta("reputation", 12), new StatDelta("gold", 50)),
                    MakeChoice("Demand a higher fee",
                        "Bold. The lord grumbles, but pays.",
                        new StatDelta("reputation", -5), new StatDelta("gold", 100)),
                    MakeChoice("Refuse on principle",
                        "The lord is not pleased.",
                        new StatDelta("reputation", -8))
                });

            CreateEvent("evt_tax_collector", "The Tax Collector",
                "An officious man with a ledger arrives at {name}'s door.",
                minAge: 18, maxAge: 80, weight: 1.5f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Pay in full",
                        "Lawful and unremarkable.",
                        new StatDelta("gold", -40), new StatDelta("piety", 3)),
                    MakeChoice("Argue the assessment",
                        "After some haggling, a reduction is agreed.",
                        new StatDelta("gold", -20), new StatDelta("smarts", 3)),
                    MakeChoice("Bribe the collector",
                        "A risky gambit that pays off this time.",
                        new StatDelta("gold", -15), new StatDelta("reputation", -5))
                });

            // --- MIDDLE AGE events (age 45-59) ---
            CreateEvent("evt_old_injury", "An Old Wound",
                "An injury from years past has flared up badly. The healer recommends rest.",
                minAge: 40, maxAge: 65, weight: 1.2f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Rest as advised (lose a season's income)",
                        "The rest helps. {name} recovers, if slowly.",
                        new StatDelta("health", 10), new StatDelta("gold", -30)),
                    MakeChoice("Push through it",
                        "The work gets done. The body protests.",
                        new StatDelta("health", -15), new StatDelta("gold", 20))
                });

            CreateEvent("evt_inheritance", "An Unexpected Inheritance",
                "A distant relative {name} barely knew has died, leaving a surprising will.",
                minAge: 25, maxAge: 70, weight: 0.7f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Accept the inheritance",
                        "A modest windfall arrives.",
                        new StatDelta("gold", 200), new StatDelta("landValue", 100)),
                    MakeChoice("Contest the will for more",
                        "After legal fees and months of wrangling, {name} wins a larger share.",
                        new StatDelta("gold", 350), new StatDelta("reputation", -8), new StatDelta("smarts", 5))
                });

            CreateEvent("evt_political_strife", "Political Unrest",
                "Factions are forming in the city. Everyone is being forced to pick a side.",
                minAge: 18, maxAge: 80, weight: 0.9f,
                folder: "Adult",
                choices: new[]
                {
                    MakeChoice("Back the establishment",
                        "A safe choice. Those in power approve.",
                        new StatDelta("reputation", 8), new StatDelta("gold", 30)),
                    MakeChoice("Side with the reformers",
                        "Dangerous, but principled. The people notice.",
                        new StatDelta("reputation", -5), new StatDelta("happiness", 8)),
                    MakeChoice("Stay neutral",
                        "Both sides distrust {name} a little. Neither targets them.",
                        new StatDelta("smarts", 5))
                });

            // --- ELDER events (age 60+) ---
            CreateEvent("evt_grandchild", "A New Generation",
                "Word arrives: {name} has become a grandparent.",
                minAge: 55, maxAge: 99, weight: 1.5f,
                folder: "Elder",
                requiredTags: new[] { "married" },
                choices: new[]
                {
                    MakeChoice("Celebrate with the family",
                        "Pure joy. Life has been worth living.",
                        new StatDelta("happiness", 20), new StatDelta("health", 5))
                });

            CreateEvent("evt_final_pilgrimage", "The Final Pilgrimage",
                "At {age}, {name} feels the pull of the great shrine on the mountain — a journey made once in a lifetime.",
                minAge: 60, maxAge: 99, weight: 0.8f,
                folder: "Elder",
                choices: new[]
                {
                    MakeChoice("Make the journey",
                        "The path is hard, but the view from the top is worth everything.",
                        new StatDelta("piety", 20), new StatDelta("happiness", 15), new StatDelta("health", -5)),
                    MakeChoice("Too old for that climb",
                        "{name} lights a candle at the village shrine instead.",
                        new StatDelta("piety", 8), new StatDelta("happiness", 5))
                });

            Debug.Log("[YVOS Setup] Created 20 life events.");
        }

        // ================================================================
        //  GUILDS
        // ================================================================
        static void CreateGuilds()
        {
            EnsureDir("Assets/Data/Careers/Resources/Guilds");

            CreateGuild("guild_blacksmiths", "Blacksmiths Guild",
                "Master the forge. Craft weapons, armour, and tools for the realm.",
                minStrength: 30, joinFee: 20,
                ranks: new[] { "Apprentice", "Journeyman", "Master Smith", "Grand Master" },
                income: new[] { 25, 50, 90, 150 },
                joinTags: new[] { "guild_blacksmiths" });

            CreateGuild("guild_mages", "Mages Guild",
                "Unlock the secrets of the arcane. Knowledge is power.",
                minMagic: 25, joinFee: 50,
                ranks: new[] { "Initiate", "Adept", "Archmage", "Arcanum Elder" },
                income: new[] { 20, 45, 80, 140 },
                joinTags: new[] { "guild_mages" });

            CreateGuild("guild_thieves", "Thieves Guild",
                "Work in the shadows. The law is a suggestion.",
                minSmarts: 30, joinFee: 0,
                ranks: new[] { "Cutpurse", "Rogue", "Master Thief", "Guildmaster" },
                income: new[] { 30, 60, 100, 180 },
                joinTags: new[] { "guild_thieves" });

            CreateGuild("guild_knights", "Knights Order",
                "Serve the realm with sword and honour.",
                minStrength: 40, joinFee: 100,
                ranks: new[] { "Squire", "Knight", "Knight-Commander", "Grand Marshal" },
                income: new[] { 40, 80, 130, 220 },
                joinTags: new[] { "guild_knights" });

            CreateGuild("guild_merchants", "Merchants League",
                "Trade routes, profit margins, and the art of the deal.",
                minSmarts: 30, joinFee: 75,
                ranks: new[] { "Peddler", "Merchant", "Senior Merchant", "Trade Baron" },
                income: new[] { 35, 70, 120, 200 },
                joinTags: new[] { "guild_merchants" });

            CreateGuild("guild_healers", "Healers Covenant",
                "Mend the broken. Fight plague. Serve the living.",
                minSmarts: 25, joinFee: 30,
                ranks: new[] { "Acolyte", "Healer", "Senior Healer", "High Healer" },
                income: new[] { 20, 40, 70, 110 },
                joinTags: new[] { "guild_healers" });

            Debug.Log("[YVOS Setup] Created 6 guilds.");
        }

        // ================================================================
        //  ENEMIES
        // ================================================================
        static void CreateEnemies()
        {
            EnsureDir("Assets/Data/Enemies/Resources/Enemies");

            CreateEnemy("enemy_bandit", "River Bandit",
                "A desperate man with a rusty blade and nothing to lose.",
                maxHP: 50, strength: 35, armor: 5,
                allowedActions: new[] { CombatAction.Attack, CombatAction.Dodge, CombatAction.Block },
                weights: new[] { 0.5f, 0.3f, 0.2f },
                fleeThreshold: 0.2f,
                goldReward: 25,
                consequence: DefeatConsequence.Injury);

            CreateEnemy("enemy_wolf_pack", "Wolf Pack",
                "Three gaunt wolves, circling with hungry eyes.",
                maxHP: 60, strength: 40, armor: 0,
                allowedActions: new[] { CombatAction.Attack, CombatAction.Dodge },
                weights: new[] { 0.7f, 0.3f },
                hitsPerRound: 2,
                goldReward: 10,
                consequence: DefeatConsequence.Injury);

            CreateEnemy("enemy_corrupt_guard", "Corrupt Guard",
                "He wears the king's colours but serves only himself.",
                maxHP: 70, strength: 50, armor: 15,
                allowedActions: new[] { CombatAction.Attack, CombatAction.Block, CombatAction.Dodge, CombatAction.SpellItem },
                weights: new[] { 0.4f, 0.3f, 0.2f, 0.1f },
                goldReward: 40,
                consequence: DefeatConsequence.Capture);

            CreateEnemy("enemy_goblin_shaman", "Goblin Shaman",
                "Small, green, and surprisingly lethal with a bone staff.",
                maxHP: 45, strength: 30, armor: 0, magicResist: 0,
                allowedActions: new[] { CombatAction.Attack, CombatAction.SpellItem, CombatAction.Dodge },
                weights: new[] { 0.2f, 0.6f, 0.2f },
                goldReward: 15,
                consequence: DefeatConsequence.Injury);

            CreateEnemy("enemy_undead_knight", "Undead Knight",
                "Once a defender of the realm. Now something far worse.",
                maxHP: 90, strength: 65, armor: 20,
                allowedActions: new[] { CombatAction.Attack, CombatAction.Block },
                weights: new[] { 0.6f, 0.4f },
                regenPerRound: 5,
                goldReward: 60,
                consequence: DefeatConsequence.Death);

            CreateEnemy("enemy_dragon", "The Ancient Dragon",
                "It has been awake for three centuries. You are barely an amusement.",
                maxHP: 200, strength: 90, armor: 30, magicResist: 20,
                allowedActions: new[] { CombatAction.Attack, CombatAction.Block, CombatAction.Dodge, CombatAction.SpellItem },
                weights: new[] { 0.5f, 0.2f, 0.1f, 0.2f },
                phaseThresholds: new[] { 0.6f, 0.3f },
                phase1Weights: new[] { 0.2f, 0.1f, 0.1f, 0.6f },  // goes spell-heavy below 60%
                phase2Weights: new[] { 0.25f, 0.25f, 0.25f, 0.25f }, // chaos below 30%
                goldReward: 500,
                consequence: DefeatConsequence.Death);

            Debug.Log("[YVOS Setup] Created 6 enemies.");
        }

        // ================================================================
        //  BUILD GAME SCENE HIERARCHY
        // ================================================================
        static GameObject BuildManagersHierarchy()
        {
            var managers = new GameObject("--- MANAGERS ---");

            // Core singletons
            AddComponent<Core.GameManager>(managers, "GameManager");
            AddComponent<Core.TimeManager>(managers, "TimeManager");
            AddComponent<History.HistoryLog>(managers, "HistoryLog");
            AddComponent<Events.LifeEventEngine>(managers, "LifeEventEngine");
            AddComponent<Events.EventResolver>(managers, "EventResolver");
            AddComponent<Character.CharacterLifecycle>(managers, "CharacterLifecycle");
            AddComponent<Career.GuildManager>(managers, "GuildManager");
            AddComponent<Economy.EconomyManager>(managers, "EconomyManager");
            AddComponent<Relationships.RelationshipManager>(managers, "RelationshipManager");
            AddComponent<Persistence.SaveManager>(managers, "SaveManager");
            AddComponent<Combat.CombatEngine>(managers, "CombatEngine");
            AddComponent<MiniGames.MiniGameEngine>(managers, "MiniGameEngine");

            return managers;
        }

        static GameObject BuildUICanvas()
        {
            // Main Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // ---- Main Life Panel ----
            var lifePanelGO = CreatePanel(canvasGO, "MainLifePanel");
            lifePanelGO.AddComponent<UI.MainLifeUI>();

            // Header text
            var headerGO = CreateTextObject(lifePanelGO, "HeaderText", "YEAR 850 · CHARACTER · AGE 0");

            // Stat bars placeholder group
            var statsGO = new GameObject("StatsPanel");
            statsGO.transform.SetParent(lifePanelGO.transform, false);

            // History log panel
            var historyGO = CreatePanel(lifePanelGO, "HistoryLogPanel");
            var historyUI = historyGO.AddComponent<UI.HistoryLogUI>();
            var scrollRect = historyGO.AddComponent<ScrollRect>();

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(historyGO.transform, false);
            contentGO.AddComponent<VerticalLayoutGroup>();
            contentGO.AddComponent<ContentSizeFitter>();

            // Age Up button
            var ageUpGO  = CreateButton(lifePanelGO, "AgeUpButton", "AGE UP");

            // ---- Event Popup Panel ----
            var eventPopupGO = CreatePanel(canvasGO, "EventPopupPanel");
            eventPopupGO.SetActive(false);
            var eventPopupUI = eventPopupGO.AddComponent<UI.EventPopupUI>();
            CreateTextObject(eventPopupGO, "TitleText",       "Event Title");
            CreateTextObject(eventPopupGO, "DescriptionText", "Event description goes here...");
            CreateTextObject(eventPopupGO, "OutcomeText",     "Outcome text.");
            var choiceContainerGO = new GameObject("ChoiceButtonContainer");
            choiceContainerGO.transform.SetParent(eventPopupGO.transform, false);
            choiceContainerGO.AddComponent<VerticalLayoutGroup>();

            // ---- Combat Panel ----
            var combatGO = CreatePanel(canvasGO, "CombatPanel");
            combatGO.SetActive(false);
            var combatUI = combatGO.AddComponent<UI.CombatUI>();
            CreateTextObject(combatGO, "EnemyNameText",   "Enemy Name");
            CreateTextObject(combatGO, "RoundText",       "Round 1");
            CreateTextObject(combatGO, "FlavourText",     "");
            CreateTextObject(combatGO, "LastRoundLog",    "");
            CreateTextObject(combatGO, "ResultBanner",    "VICTORY!");
            CreateButton(combatGO, "AttackButton",    "⬆ ATTACK");
            CreateButton(combatGO, "DodgeButton",     "⬅ DODGE");
            CreateButton(combatGO, "BlockButton",     "➡ BLOCK");
            CreateButton(combatGO, "SpellButton",     "⬇ SPELL");

            // ---- Mini-Game Popup ----
            var miniGameGO = CreatePanel(canvasGO, "MiniGamePopupPanel");
            miniGameGO.SetActive(false);
            miniGameGO.AddComponent<UI.MiniGamePopupUI>();
            CreateTextObject(miniGameGO, "TitleText",       "Mini-Game Title");
            CreateTextObject(miniGameGO, "StagePromptText", "Stage prompt...");
            CreateTextObject(miniGameGO, "OutcomeText",     "Outcome.");

            // ---- Death Screen ----
            var deathGO = CreatePanel(canvasGO, "DeathPanel");
            deathGO.SetActive(false);
            deathGO.AddComponent<UI.DeathScreenUI>();
            CreateTextObject(deathGO, "NameText",        "Character Name");
            CreateTextObject(deathGO, "YearsText",       "850 - 900 · Age 50");
            CreateTextObject(deathGO, "CauseText",       "Died of: Old Age");
            CreateTextObject(deathGO, "SocialClassText", "Final Class: Peasant");
            CreateTextObject(deathGO, "GuildText",       "Guild: None");
            CreateTextObject(deathGO, "WealthText",      "Wealth: 0g");
            CreateTextObject(deathGO, "ChildrenText",    "Children: 0");
            CreateTextObject(deathGO, "MarriagesText",   "Marriages: 0");
            CreateTextObject(deathGO, "EpitaphText",     "\"A life lived, in full.\"");
            CreateButton(deathGO, "PlayAsChildButton", "Play as Child");
            CreateButton(deathGO, "NewLifeButton",     "New Life");

            return canvasGO;
        }

        static void BuildEventSystem()
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // ================================================================
        //  ASSET CREATION HELPERS
        // ================================================================

        static void CreateEvent(string id, string title, string description,
            int minAge, int maxAge, float weight, string folder,
            EventChoiceData[] choices,
            bool triggersCombat = false,
            string[] requiredTags = null,
            string[] addTags = null)
        {
            string path = $"Assets/Data/Events/Resources/Events/{folder}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<LifeEventSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<LifeEventSO>();
            so.eventId             = id;
            so.title               = title;
            so.descriptionTemplate = description;
            so.minAge              = minAge;
            so.maxAge              = maxAge;
            so.baseWeight          = weight;
            so.choices             = choices;
            so.triggersCombat      = triggersCombat;
            so.requiredTags        = requiredTags ?? new string[0];

            AssetDatabase.CreateAsset(so, path);
        }

        static EventChoiceData MakeChoice(string label, string outcome, params StatDelta[] deltas)
        {
            return new EventChoiceData
            {
                buttonLabel  = label,
                outcomeText  = outcome,
                statChanges  = deltas
            };
        }

        static StatDelta SD(string stat, int delta) => new StatDelta { statName = stat, delta = delta };

        static void CreateGuild(string id, string name, string flavour,
            int minSmarts = 0, int minStrength = 0, int minMagic = 0,
            int joinFee = 0,
            string[] ranks = null,
            int[] income = null,
            string[] joinTags = null)
        {
            string path = $"Assets/Data/Careers/Resources/Guilds/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<GuildDefinitionSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<GuildDefinitionSO>();
            so.guildId           = id;
            so.displayName       = name;
            so.flavourText       = flavour;
            so.minSmarts         = minSmarts;
            so.minStrength       = minStrength;
            so.minMagicAffinity  = minMagic;
            so.joinFee           = joinFee;
            so.joinTags          = joinTags ?? new string[0];
            so.annualIncomeByRank = income  ?? new[] { 20, 40, 70, 120 };

            if (ranks != null)
            {
                so.ranks = new GuildRankData[ranks.Length];
                for (int i = 0; i < ranks.Length; i++)
                {
                    so.ranks[i] = new GuildRankData
                    {
                        rankTitle   = ranks[i],
                        xpRequired  = i * 100,
                        rankUpBonuses = new StatModifier[0],
                        unlockedActionIds = new string[0]
                    };
                }
            }

            AssetDatabase.CreateAsset(so, path);
        }

        static void CreateEnemy(string id, string name, string flavour,
            int maxHP, int strength, int armor, int magicResist = 0,
            CombatAction[] allowedActions = null, float[] weights = null,
            float fleeThreshold = 0f, int regenPerRound = 0, int hitsPerRound = 1,
            int goldReward = 0,
            DefeatConsequence consequence = DefeatConsequence.Injury,
            float[] phaseThresholds = null,
            float[] phase1Weights = null,
            float[] phase2Weights = null)
        {
            string path = $"Assets/Data/Enemies/Resources/Enemies/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(path) != null) return;

            var so = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            so.enemyId           = id;
            so.displayName       = name;
            so.flavourDescription = flavour;
            so.maxHealth         = maxHP;
            so.strength          = strength;
            so.armor             = armor;
            so.magicResist       = magicResist;
            so.allowedActions    = allowedActions ?? new[] { CombatAction.Attack, CombatAction.Block };
            so.actionWeights     = weights        ?? new[] { 0.6f, 0.4f };
            so.fleeThreshold     = fleeThreshold;
            so.regenPerRound     = regenPerRound;
            so.hitsPerRound      = hitsPerRound;
            so.defeatConsequence = consequence;
            so.victoryReward     = new CombatReward { goldReward = goldReward };

            if (phaseThresholds != null)
            {
                so.phaseThresholds = phaseThresholds;
                int phaseCount = phaseThresholds.Length;
                so.phaseWeights = new ActionWeightRow[phaseCount];
                if (phase1Weights != null) so.phaseWeights[0] = new ActionWeightRow { weights = phase1Weights };
                if (phase2Weights != null && phaseCount > 1) so.phaseWeights[1] = new ActionWeightRow { weights = phase2Weights };
            }

            AssetDatabase.CreateAsset(so, path);
        }

        // ================================================================
        //  SCENE HELPERS
        // ================================================================

        static GameObject CreatePanel(GameObject parent, string name)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            go.AddComponent<CanvasRenderer>();
            return go;
        }

        static GameObject CreateTextObject(GameObject parent, string name, string defaultText)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text     = defaultText;
            tmp.fontSize = 18;
            return go;
        }

        static GameObject CreateButton(GameObject parent, string name, string label)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<Image>();
            go.AddComponent<Button>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            textGO.AddComponent<RectTransform>();
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 16;
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        static void AddComponent<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<T>();
        }

        static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts  = path.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
