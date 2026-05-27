// ============================================================
//  RelationshipManager.cs
//  Generates families on character creation, processes yearly
//  relationship decay, and manages romance/marriage/children.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using YVOS.Character;
using YVOS.Core;
using YVOS.History;

namespace YVOS.Relationships
{
    public class RelationshipManager : MonoBehaviour
    {
        public static RelationshipManager Instance { get; private set; }

        [SerializeField] private Data.GameBalanceSO balance;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------
        //  Family generation (called on new game)
        // -----------------------------------------------------------------

        public void GenerateFamily(CharacterData character)
        {
            int worldYear = GameManager.Instance.WorldYear;

            // Mother
            var mother = CreatePerson("Mother", RelationshipType.Mother,
                birthYear: worldYear - character.stats.age - UnityEngine.Random.Range(20, 36),
                gender: Gender.Female,
                raceId: character.stats.raceId);
            character.relationships.Add(mother);

            // Father
            var father = CreatePerson("Father", RelationshipType.Father,
                birthYear: worldYear - character.stats.age - UnityEngine.Random.Range(20, 36),
                gender: Gender.Male,
                raceId: character.stats.raceId);
            character.relationships.Add(father);

            // Siblings (0–3)
            int siblingCount = UnityEngine.Random.Range(0, 4);
            for (int i = 0; i < siblingCount; i++)
            {
                int ageDiff  = UnityEngine.Random.Range(-10, 11);
                var sibling  = CreatePerson($"Sibling {i + 1}", RelationshipType.Sibling,
                    birthYear: worldYear - character.stats.age - ageDiff,
                    gender: (Gender)UnityEngine.Random.Range(0, 2),
                    raceId: character.stats.raceId);
                character.relationships.Add(sibling);
            }

            HistoryLog.Instance.Record(HistoryCategory.Birth,
                $"Born to {mother.firstName} and {father.firstName} {character.stats.lastName}.");
        }

        // -----------------------------------------------------------------
        //  Yearly decay
        // -----------------------------------------------------------------

        public void ProcessYearlyDecay()
        {
            var character = GameManager.Instance.Character;
            if (character == null) return;

            int decay = balance != null ? balance.relationshipDecayPerYear : 2;
            int worldYear = GameManager.Instance.WorldYear;

            foreach (var rel in character.relationships)
            {
                if (!rel.isAlive) continue;

                // Natural aging — check if NPC dies of old age
                int npcAge = rel.CurrentAge(worldYear);
                if (npcAge > 75 && UnityEngine.Random.value < 0.05f)
                {
                    rel.isAlive   = false;
                    rel.deathYear = worldYear;

                    HistoryLog.Instance.Record(HistoryCategory.Family,
                        $"{rel.FullName} ({rel.type}) passed away at age {npcAge}.",
                        iconId: "death");

                    if (rel.affection > 50)
                        character.stats.happiness -= 10;

                    character.stats.Clamp();
                    continue;
                }

                // Passive affection decay if no interaction tag
                if (!character.HasTag($"interacted_{rel.personId}"))
                    rel.affection = Mathf.Max(0, rel.affection - decay);

                // Remove interaction tag ready for next year
                character.RemoveTag($"interacted_{rel.personId}");
            }
        }

        // -----------------------------------------------------------------
        //  Romance / Marriage / Children
        // -----------------------------------------------------------------

        public bool TryStartRomance(string personId)
        {
            var rel = FindRelationship(personId);
            if (rel == null || rel.affection < 40) return false;
            rel.isRomantic = true;
            rel.type       = RelationshipType.RomanticInterest;
            HistoryLog.Instance.Record(HistoryCategory.Romance,
                $"Began a romantic relationship with {rel.firstName}.");
            return true;
        }

        public bool TryMarry(string personId)
        {
            var rel = FindRelationship(personId);
            if (rel == null || !rel.isRomantic || rel.affection < 60) return false;

            rel.isSpouse = true;
            rel.type     = RelationshipType.Spouse;
            GameManager.Instance.Character.AddTag("married");

            HistoryLog.Instance.Record(HistoryCategory.Romance,
                $"Married {rel.FullName}.", iconId: "marriage");
            return true;
        }

        public void HaveChild()
        {
            var character = GameManager.Instance.Character;
            if (character == null) return;

            var child = CreatePerson("Child", RelationshipType.Child,
                birthYear: GameManager.Instance.WorldYear,
                gender: (Gender)UnityEngine.Random.Range(0, 2),
                raceId: character.stats.raceId);

            // Seed child's name from character's last name
            child.lastName = character.stats.lastName;
            character.relationships.Add(child);

            HistoryLog.Instance.Record(HistoryCategory.Family,
                $"Welcomed a child, {child.FullName}, into the world.",
                iconId: "family");
        }

        // -----------------------------------------------------------------
        //  Interaction (resets decay for this year)
        // -----------------------------------------------------------------

        public void Interact(string personId)
        {
            var rel = FindRelationship(personId);
            if (rel == null || !rel.isAlive) return;

            rel.affection = Mathf.Min(100, rel.affection + 5);
            GameManager.Instance.Character.AddTag($"interacted_{personId}");
        }

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

        private RelationshipData CreatePerson(string firstName, RelationshipType type,
            int birthYear, Gender gender, string raceId)
        {
            // Random name from a simple pool — replace with name table later
            string[] maleNames   = { "Aldric", "Rowan", "Edric", "Gareth", "Theron", "Cedric", "Oswin" };
            string[] femaleNames = { "Mira", "Elyndra", "Sable", "Isolde", "Lyra", "Theia", "Brynn" };
            string[] surnames    = { "Ashford", "Millward", "Blackwood", "Stonehaven", "Vane", "Holt" };

            string name = gender == Gender.Female
                ? femaleNames[UnityEngine.Random.Range(0, femaleNames.Length)]
                : maleNames[UnityEngine.Random.Range(0, maleNames.Length)];

            return new RelationshipData
            {
                firstName  = name,
                lastName   = surnames[UnityEngine.Random.Range(0, surnames.Length)],
                type       = type,
                gender     = gender,
                raceId     = raceId,
                birthYear  = birthYear,
                affection  = UnityEngine.Random.Range(50, 80),
                respect    = UnityEngine.Random.Range(40, 70),
                isAlive    = true
            };
        }

        private RelationshipData FindRelationship(string personId)
        {
            var character = GameManager.Instance.Character;
            if (character == null) return null;
            foreach (var rel in character.relationships)
                if (rel.personId == personId) return rel;
            return null;
        }
    }
}
