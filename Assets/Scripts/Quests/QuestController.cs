using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Quests
{
    public class QuestController : MonoBehaviour
    {
        public static QuestController Instance { get; private set; }
        public List<QuestProgress> ActiveQuests = new();
        private QuestUI questUI;
        
        public bool IsQuestActive(string questID) => ActiveQuests.Exists(q => q.questID == questID);

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
            if (IsQuestActive(quest.questID)) return;

            ActiveQuests.Add(new QuestProgress(quest));

            questUI.UpdateQuestUI();
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

                        questUI.UpdateQuestUI();
                        return;  // ADD THIS - Only update the first incomplete objective, then stop
                    }
                }
            }
        }

        public bool IsQuestCompleted(string questID)
        {
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
                inventory.RemoveItem(item.Key, item.Value);
            }

            ActiveQuests.Remove(quest);

            Debug.Log("Quest turned in: " + questID);

            questUI.UpdateQuestUI();

            return true;
        }
    }
}