// ============================================================
//  RelationshipData.cs
//  Represents one person the character knows.
//  Pure data — no MonoBehaviour.
// ============================================================
using System;
using YVOS.Character;

namespace YVOS.Relationships
{
    [System.Serializable]
    public class RelationshipData
    {
        // -----------------------------------------------------------------
        //  Identity
        // -----------------------------------------------------------------
        public string personId;          // GUID, unique per person
        public string firstName;
        public string lastName;
        public string raceId;
        public Gender gender;

        // -----------------------------------------------------------------
        //  Relationship meta
        // -----------------------------------------------------------------
        public RelationshipType type;
        public int affection;            // 0–100
        public int respect;              // 0–100

        // -----------------------------------------------------------------
        //  State
        // -----------------------------------------------------------------
        public bool isAlive = true;
        public int birthYear;
        public int deathYear = -1;       // -1 = still alive

        // -----------------------------------------------------------------
        //  Romance / family flags
        // -----------------------------------------------------------------
        public bool isSpouse;
        public bool isRomantic;

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------
        public string FullName => $"{firstName} {lastName}";
        public int CurrentAge(int worldYear) => worldYear - birthYear;

        public RelationshipData()
        {
            personId = Guid.NewGuid().ToString();
        }
    }
}
