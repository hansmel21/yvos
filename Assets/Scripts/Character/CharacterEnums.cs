// ============================================================
//  CharacterEnums.cs
//  Central enum definitions shared across all YVOS systems.
// ============================================================
namespace YVOS.Character
{
    public enum Gender
    {
        Male,
        Female,
        Other
    }

    /// <summary>
    /// Social hierarchy from lowest to highest.
    /// Integer value used for comparison (e.g. minSocialClass gates).
    /// </summary>
    public enum SocialClass
    {
        Serf     = 0,
        Peasant  = 1,
        Artisan  = 2,
        Merchant = 3,
        Knight   = 4,
        Noble    = 5,
        Royalty  = 6
    }

    /// <summary>
    /// Age-based life stage. Thresholds: 0–3, 4–11, 12–17, 18–24, 25–44, 45–59, 60+
    /// Tunable in GameBalanceSO.
    /// </summary>
    public enum LifeStage
    {
        Infant,
        Child,
        Teen,
        YoungAdult,
        Adult,
        MiddleAge,
        Elder
    }

    public enum HistoryCategory
    {
        Birth,
        Death,
        Family,
        Career,
        Event,
        Romance,
        Crime,
        War,
        Magic,
        Economy,
        Achievement,
        Combat,
        MiniGame
    }

    public enum RelationshipType
    {
        Mother,
        Father,
        Sibling,
        Child,
        Friend,
        Enemy,
        Rival,
        RomanticInterest,
        Spouse,
        ExSpouse,
        Liege,
        Vassal,
        GuildMate
    }

    /// <summary>The four actions available on the combat D-pad.</summary>
    public enum CombatAction
    {
        Attack    = 0,
        Dodge     = 1,
        Block     = 2,
        SpellItem = 3
    }

    /// <summary>Outcome of a single combat round matchup.</summary>
    public enum CombatRoundOutcome
    {
        PlayerWins,
        EnemyWins,
        Tie
    }

    /// <summary>Final result of an entire combat encounter.</summary>
    public enum CombatResult
    {
        Victory,
        Defeat,
        Escaped,
        Draw
    }

    /// <summary>What happens when the player loses a combat.</summary>
    public enum DefeatConsequence
    {
        Death,
        Injury,
        Capture,
        Escape
    }
}
