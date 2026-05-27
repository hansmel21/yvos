// ============================================================
//  CharacterStats.cs
//  Pure serializable data — no MonoBehaviour, no Unity deps
//  beyond Mathf. This is the atom of the game; every system
//  reads or writes from here.
// ============================================================
using UnityEngine;

namespace YVOS.Character
{
    [System.Serializable]
    public class CharacterStats
    {
        // -----------------------------------------------------------------
        //  Identity
        // -----------------------------------------------------------------
        public string firstName;
        public string lastName;
        public string raceId;           // matches RaceDefinitionSO.raceId
        public Gender gender;
        public SocialClass socialClass;

        // -----------------------------------------------------------------
        //  Core stats (0–100)
        // -----------------------------------------------------------------
        public int health;
        public int happiness;
        public int smarts;
        public int looks;
        public int strength;
        public int magicAffinity;
        public int piety;

        // -----------------------------------------------------------------
        //  Economy
        // -----------------------------------------------------------------
        public int gold;
        public int landValue;           // aggregate of owned land deeds

        // -----------------------------------------------------------------
        //  Social
        // -----------------------------------------------------------------
        public int reputation;          // -100 to +100

        // -----------------------------------------------------------------
        //  Age / Time
        // -----------------------------------------------------------------
        public int age;
        public int birthYear;
        public LifeStage lifeStage;

        // -----------------------------------------------------------------
        //  Derived
        // -----------------------------------------------------------------
        public string FullName => $"{firstName} {lastName}";

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Clamp all stats to valid ranges. Call after every modification.
        /// </summary>
        public void Clamp()
        {
            health        = Mathf.Clamp(health,        0, 100);
            happiness     = Mathf.Clamp(happiness,     0, 100);
            smarts        = Mathf.Clamp(smarts,        0, 100);
            looks         = Mathf.Clamp(looks,         0, 100);
            strength      = Mathf.Clamp(strength,      0, 100);
            magicAffinity = Mathf.Clamp(magicAffinity, 0, 100);
            piety         = Mathf.Clamp(piety,         0, 100);
            reputation    = Mathf.Clamp(reputation,  -100, 100);
            gold          = Mathf.Max(gold,     0);
            landValue     = Mathf.Max(landValue, 0);
        }

        /// <summary>
        /// Apply a StatDelta to this stats object.
        /// </summary>
        public void ApplyDelta(StatDelta delta)
        {
            switch (delta.statName.ToLowerInvariant())
            {
                case "health":        health        += delta.isPercentage ? Mathf.RoundToInt(health        * delta.delta / 100f) : delta.delta; break;
                case "happiness":     happiness     += delta.isPercentage ? Mathf.RoundToInt(happiness     * delta.delta / 100f) : delta.delta; break;
                case "smarts":        smarts        += delta.isPercentage ? Mathf.RoundToInt(smarts        * delta.delta / 100f) : delta.delta; break;
                case "looks":         looks         += delta.isPercentage ? Mathf.RoundToInt(looks         * delta.delta / 100f) : delta.delta; break;
                case "strength":      strength      += delta.isPercentage ? Mathf.RoundToInt(strength      * delta.delta / 100f) : delta.delta; break;
                case "magicaffinity": magicAffinity += delta.isPercentage ? Mathf.RoundToInt(magicAffinity * delta.delta / 100f) : delta.delta; break;
                case "piety":         piety         += delta.isPercentage ? Mathf.RoundToInt(piety         * delta.delta / 100f) : delta.delta; break;
                case "gold":          gold          += delta.delta; break;
                case "landvalue":     landValue     += delta.delta; break;
                case "reputation":    reputation    += delta.delta; break;
            }
            Clamp();
        }
    }

    // -----------------------------------------------------------------
    //  Supporting types referenced by CharacterStats and event data
    // -----------------------------------------------------------------

    [System.Serializable]
    public class StatDelta
    {
        public string statName;      // e.g. "health", "gold", "reputation"
        public int delta;
        public bool isPercentage;    // if true, delta is % of current value
    }

    [System.Serializable]
    public class StatModifier
    {
        public string statName;
        public int value;
        public bool isPercentage;
    }
}
