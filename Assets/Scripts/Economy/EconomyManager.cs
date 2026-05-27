// ============================================================
//  EconomyManager.cs
//  Handles yearly income, living costs, item passives, and
//  social class promotion checks.
// ============================================================
using UnityEngine;
using YVOS.Character;
using YVOS.Core;
using YVOS.History;

namespace YVOS.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private Data.GameBalanceSO balance;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------
        //  Yearly processing
        // -----------------------------------------------------------------

        public void ProcessYearlyIncome()
        {
            var character = GameManager.Instance.Character;
            if (character == null || !character.isAlive) return;

            int income     = CalculateIncome(character);
            int livingCost = CalculateLivingCost(character);
            int net        = income - livingCost;

            int prevGold = character.stats.gold;
            character.stats.gold += net;
            character.stats.Clamp();

            if (net != 0)
                EventBus.Publish(new GoldChangedEvent
                {
                    previousGold = prevGold,
                    newGold      = character.stats.gold,
                    delta        = net
                });

            // Social class promotion check
            CheckSocialPromotion(character);
        }

        // -----------------------------------------------------------------
        //  Income & costs
        // -----------------------------------------------------------------

        private int CalculateIncome(CharacterData character)
        {
            int classIdx    = (int)character.stats.socialClass;
            int baseIncome  = GetBalance().baseIncomeByClass[Mathf.Clamp(classIdx, 0, 6)];
            return baseIncome;
        }

        private int CalculateLivingCost(CharacterData character)
        {
            int classIdx = (int)character.stats.socialClass;
            return GetBalance().livingCostByClass[Mathf.Clamp(classIdx, 0, 6)];
        }

        // -----------------------------------------------------------------
        //  Purchase / Sell
        // -----------------------------------------------------------------

        public bool TryPurchase(string itemId)
        {
            var item      = ItemRegistry.Get(itemId);
            var character = GameManager.Instance.Character;
            if (item == null || character == null) return false;

            if (character.stats.gold < item.baseGoldValue)
            {
                Debug.Log($"[EconomyManager] Not enough gold to buy {itemId}.");
                return false;
            }

            character.stats.gold -= item.baseGoldValue;
            character.inventoryItemIds.Add(itemId);
            character.stats.Clamp();

            HistoryLog.Instance.Record(HistoryCategory.Economy,
                $"Purchased {item.displayName} for {item.baseGoldValue}g.");
            return true;
        }

        public bool TrySell(string itemId)
        {
            var item      = ItemRegistry.Get(itemId);
            var character = GameManager.Instance.Character;
            if (item == null || character == null) return false;
            if (!character.inventoryItemIds.Remove(itemId)) return false;

            int salePrice = Mathf.RoundToInt(item.baseGoldValue * 0.7f);
            character.stats.gold += salePrice;
            character.stats.Clamp();

            HistoryLog.Instance.Record(HistoryCategory.Economy,
                $"Sold {item.displayName} for {salePrice}g.");
            return true;
        }

        // -----------------------------------------------------------------
        //  Social class promotion
        // -----------------------------------------------------------------

        private void CheckSocialPromotion(CharacterData character)
        {
            var stats = character.stats;
            var sc    = stats.socialClass;

            // Serf → Peasant: just time + age
            if (sc == SocialClass.Serf && stats.age >= 16)
            {
                PromoteTo(character, SocialClass.Peasant, "Came of age and left serfdom.");
                return;
            }

            // Peasant → Merchant: wealth + rep
            if (sc == SocialClass.Peasant && stats.gold > 500 && stats.reputation > 20)
            {
                PromoteTo(character, SocialClass.Merchant, "Accumulated wealth and reputation — rose to Merchant status.");
                return;
            }

            // Merchant/Artisan → Knight: guild + wealth
            if ((sc == SocialClass.Merchant || sc == SocialClass.Artisan) &&
                character.HasTag("guild_knights") &&
                stats.gold > 2000)
            {
                PromoteTo(character, SocialClass.Knight, "Knighted in recognition of service and wealth.");
            }
        }

        private void PromoteTo(CharacterData character, SocialClass newClass, string reason)
        {
            character.stats.socialClass = newClass;
            HistoryLog.Instance.Record(HistoryCategory.Achievement,
                $"Rose to {newClass}: {reason}", iconId: "promotion");
        }

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

        private Data.GameBalanceSO GetBalance()
        {
            if (balance != null) return balance;
            // Fallback defaults
            return ScriptableObject.CreateInstance<Data.GameBalanceSO>();
        }
    }
}
