using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerResources;

namespace Assets.Scripts.Quests
{
    public class QuestController : MonoBehaviour
    {
        public static QuestController Instance { get; private set; }
        public List<QuestProgress> ActiveQuests = new();
        private readonly HashSet<string> completedQuestIDs = new();
        private QuestUI questUI;
        
        public bool IsQuestActive(string questID) => ActiveQuests.Exists(q => q.questID == questID);
        public bool HasQuestBeenCompleted(string questID) => completedQuestIDs.Contains(questID);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            questUI = FindObjectOfType<QuestUI>();
        }

        public void AcceptQuest(Quest quest)
        {
            if (quest == null || IsQuestActive(quest.questID) || HasQuestBeenCompleted(quest.questID)) return;

            ActiveQuests.Add(new QuestProgress(quest));

            questUI?.UpdateQuestUI();
        }

        public void UpdateObjectiveProgress(string objectiveID, int amount)
        {
            foreach (var quest in ActiveQuests)
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveID == objectiveID && !objective.IsCompleted)
                    {
                        objective.currentAmount += amount;

                        if (objective.currentAmount > objective.requiredAmount)
                            objective.currentAmount = objective.requiredAmount;

                        Debug.Log($"Quest progress updated: {objective.description} ({objective.currentAmount}/{objective.requiredAmount})");

                        questUI?.UpdateQuestUI();
                        return;  // ADD THIS - Only update the first incomplete objective, then stop
                    }
                }
            }
        }

        public bool IsQuestCompleted(string questID)
        {
            if (HasQuestBeenCompleted(questID))
                return true;

            var quest = ActiveQuests.Find(q => q.questID == questID);

            if (quest == null)
                return false;

            foreach (var objective in quest.objectives)
            {
                if (objective.currentAmount < objective.requiredAmount)
                    return false;
            }

            return true;
        }

        public bool TurnInQuest(string questID, InventoryManager inventory)
        {
            var quest = ActiveQuests.Find(q => q.questID == questID);

            if (quest == null)
                return false;

            if (!IsQuestCompleted(questID))
                return false;

            // Group objectives by itemID
            var itemsToRemove = new Dictionary<string, int>();
            foreach (var objective in quest.objectives)
            {
                Debug.Log($"Objective: {objective.description}, ID: {objective.objectiveID}, Required: {objective.requiredAmount}");

                if (objective.type != objectiveType.CollectItem)
                {
                    continue;
                }
                
                if (itemsToRemove.ContainsKey(objective.objectiveID))
                {
                    itemsToRemove[objective.objectiveID] += objective.requiredAmount;
                }
                else
                {
                    itemsToRemove[objective.objectiveID] = objective.requiredAmount;
                }
            }

            // Remove each item type only once
            foreach (var item in itemsToRemove)
            {
                Debug.Log($"Removing {item.Value} of item ID: {item.Key}");
                inventory?.RemoveItem(item.Key, item.Value);
            }

            ActiveQuests.Remove(quest);
            completedQuestIDs.Add(questID);

            Debug.Log("Quest turned in: " + questID);

            questUI?.UpdateQuestUI();

            GrantRewards(quest.quest);

            return true;
        }

        public bool TurnInQuest(Quest quest)
        {
            if (quest == null)
                return false;

            InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
            return TurnInQuest(quest.questID, inventoryManager);
        }

        private void GrantRewards(Quest quest)
        {
            if (quest == null || quest.rewards == null)
                return;

            GrantMoneyReward(quest.rewards.money);
            GrantCommunitySpiritReward(quest.rewards.communitySpirit);
            GrantItemRewards(quest.rewards.items);
        }

        private void GrantMoneyReward(int amount)
        {
            if (amount == 0)
                return;

            MoneyManager moneyManager = FindObjectOfType<MoneyManager>();
            if (moneyManager != null)
            {
                moneyManager.ChangeMoneyAmount(amount);
                return;
            }

            ResourceManager resourceManager = FindObjectOfType<ResourceManager>();
            if (resourceManager != null)
            {
                resourceManager.UpdateMoney(amount);
                return;
            }

            Debug.LogWarning($"QuestController: Could not grant money reward of {amount}. No money manager found.");
        }

        private void GrantCommunitySpiritReward(float amount)
        {
            if (Mathf.Approximately(amount, 0f))
                return;

            ResourceManager resourceManager = FindObjectOfType<ResourceManager>();
            if (resourceManager != null)
            {
                resourceManager.UpdateCommunitySpirit(amount);
                return;
            }

            Debug.LogWarning($"QuestController: Could not grant community spirit reward of {amount}. No ResourceManager found.");
        }

        private void GrantItemRewards(List<QuestRewardItem> rewardItems)
        {
            if (rewardItems == null || rewardItems.Count == 0)
                return;

            InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogWarning("QuestController: Could not grant item rewards. No InventoryManager found.");
                return;
            }

            foreach (QuestRewardItem rewardItem in rewardItems)
            {
                if (rewardItem == null || rewardItem.itemPrefab == null || rewardItem.quantity <= 0)
                    continue;

                string itemTag = string.IsNullOrEmpty(rewardItem.inventoryTag) || rewardItem.inventoryTag == "Untagged"
                    ? rewardItem.itemPrefab.tag
                    : rewardItem.inventoryTag;

                int leftover = inventoryManager.AddItem(
                    rewardItem.itemPrefab.Id,
                    rewardItem.itemPrefab.ItemName,
                    rewardItem.quantity,
                    rewardItem.itemPrefab.Sprite,
                    rewardItem.itemPrefab.ItemDescription,
                    rewardItem.itemPrefab.MaxStack,
                    string.IsNullOrEmpty(itemTag) ? "Untagged" : itemTag
                );

                if (leftover > 0)
                {
                    Debug.LogWarning($"QuestController: Could not add full reward stack for {rewardItem.itemPrefab.ItemName}. Leftover: {leftover}");
                }
            }
        }
    }
}
