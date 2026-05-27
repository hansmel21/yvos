// ============================================================
//  LifeEventSO.cs
//  ScriptableObject definition for a single life event.
//  Create instances via: Assets → Create → YVOS → Life Event
//  All .asset instances live under Resources/Events/ and are
//  loaded at runtime by LifeEventEngine.
// ============================================================
using UnityEngine;
using YVOS.Character;

namespace YVOS.Events
{
    [CreateAssetMenu(fileName = "NewLifeEvent", menuName = "YVOS/Life Event")]
    public class LifeEventSO : ScriptableObject
    {
        // -----------------------------------------------------------------
        //  Identity
        // -----------------------------------------------------------------
        [Tooltip("Unique string ID. Never change after authoring.")]
        public string eventId;
        public string title;

        [TextArea(3, 8)]
        [Tooltip("Supports tokens: {name}, {age}, {guild}, {year}, {parent_name}")]
        public string descriptionTemplate;

        // -----------------------------------------------------------------
        //  Filtering — all conditions must pass for this event to fire
        // -----------------------------------------------------------------
        [Header("Filtering")]
        [Tooltip("Character must have ALL of these tags.")]
        public string[] requiredTags;

        [Tooltip("Character must have NONE of these tags.")]
        public string[] blockedTags;

        public int minAge = 0;
        [Tooltip("0 = no upper limit.")]
        public int maxAge = 0;
        public SocialClass minSocialClass = SocialClass.Serf;

        [Tooltip("Higher = more likely to be picked when eligible.")]
        [Range(0.1f, 10f)]
        public float baseWeight = 1f;

        // -----------------------------------------------------------------
        //  Choices (1–4)
        // -----------------------------------------------------------------
        public EventChoiceData[] choices;

        // -----------------------------------------------------------------
        //  Chaining
        // -----------------------------------------------------------------
        [Tooltip("If set, this event fires automatically next year.")]
        public string followUpEventId;

        [Tooltip("If true, triggers a combat encounter instead of standard resolution.")]
        public bool triggersCombat;

        [Tooltip("Enemy to fight if triggersCombat is true.")]
        public Combat.EnemyDefinitionSO combatEnemy;
    }

    // -----------------------------------------------------------------
    //  Choice data
    // -----------------------------------------------------------------
    [System.Serializable]
    public class EventChoiceData
    {
        public string buttonLabel;

        [TextArea(2, 5)]
        public string outcomeText;

        public StatDelta[] statChanges;

        [Tooltip("Tags to add to the character after this choice.")]
        public string[] addTags;

        [Tooltip("Tags to remove from the character after this choice.")]
        public string[] removeTags;

        public int goldDelta;
        public bool causesDeath;

        [Tooltip("If set, queues this event for next year.")]
        public string unlockFollowUpEventId;
    }
}
