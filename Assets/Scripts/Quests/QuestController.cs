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
        
        public bool IsQuestActive(string questID) => ActiveQuests.Exists(q => q.QuestID == questID);

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
                    }
                }
            }
        }

        
    }
}