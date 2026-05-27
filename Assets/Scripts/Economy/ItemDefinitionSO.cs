// ============================================================
//  ItemDefinitionSO.cs
//  ScriptableObject definition for an inventory item.
//  Create via: Assets → Create → YVOS → Item Definition
// ============================================================
using UnityEngine;
using YVOS.Character;

namespace YVOS.Economy
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "YVOS/Item Definition")]
    public class ItemDefinitionSO : ScriptableObject
    {
        public string itemId;
        public string displayName;

        [TextArea(1, 3)]
        public string description;

        public ItemCategory category;
        public int baseGoldValue;
        public Sprite icon;

        [Header("Passive Effects (while owned)")]
        public StatModifier[] passiveModifiers;

        [Header("Consumable")]
        public bool isConsumable;
        public StatDelta[] consumeEffects;

        [Header("Requirements to Equip/Use")]
        public int minStrength      = 0;
        public int minMagicAffinity = 0;
    }

    public enum ItemCategory
    {
        Weapon,
        Armor,
        Potion,
        LandDeed,
        Relic,
        Tool,
        Tome
    }

    /// <summary>
    /// Lightweight registry loaded from Resources. Allows EconomyManager
    /// to look up items by ID without a direct reference.
    /// </summary>
    public static class ItemRegistry
    {
        private static ItemDefinitionSO[] _items;

        private static void EnsureLoaded()
        {
            if (_items == null)
                _items = Resources.LoadAll<ItemDefinitionSO>("Items");
        }

        public static ItemDefinitionSO Get(string itemId)
        {
            EnsureLoaded();
            foreach (var item in _items)
                if (item.itemId == itemId) return item;
            return null;
        }
    }
}
