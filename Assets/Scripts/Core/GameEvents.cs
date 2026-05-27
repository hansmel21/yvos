// ============================================================
//  GameEvents.cs
//  All IEvent implementations. Lightweight structs/classes
//  published on EventBus — no MonoBehaviour dependencies.
// ============================================================
using YVOS.Character;

namespace YVOS.Core
{
    // ---------- Time ----------
    public struct YearStartedEvent : IEvent
    {
        public int worldYear;
    }

    public struct YearEndedEvent : IEvent
    {
        public int worldYear;
    }

    // ---------- Character ----------
    public struct CharacterDiedEvent : IEvent
    {
        public string causeOfDeath;
        public int finalAge;
    }

    public struct LifeStageChangedEvent : IEvent
    {
        public LifeStage previousStage;
        public LifeStage newStage;
    }

    // ---------- History ----------
    public struct HistoryEntryAddedEvent : IEvent
    {
        public History.HistoryEntry entry;
    }

    // ---------- Life Events ----------
    public struct LifeEventQueuedEvent : IEvent
    {
        public Events.LifeEventSO lifeEvent;
    }

    public struct LifeEventResolvedEvent : IEvent
    {
        public string eventId;
        public int choiceIndex;
    }

    // ---------- Combat ----------
    public struct CombatStartedEvent : IEvent
    {
        public Combat.EnemyDefinitionSO enemy;
    }

    public struct CombatEndedEvent : IEvent
    {
        public CombatResult result;
        public string enemyName;
    }

    // ---------- MiniGame ----------
    public struct MiniGameStartedEvent : IEvent
    {
        public string miniGameId;
    }

    public struct MiniGameCompletedEvent : IEvent
    {
        public string miniGameId;
        public bool success;
    }

    // ---------- Economy ----------
    public struct GoldChangedEvent : IEvent
    {
        public int previousGold;
        public int newGold;
        public int delta;
    }

    // ---------- Career ----------
    public struct GuildJoinedEvent : IEvent
    {
        public string guildId;
    }

    public struct GuildRankUpEvent : IEvent
    {
        public string guildId;
        public int newRank;
        public string newRankTitle;
    }
}
