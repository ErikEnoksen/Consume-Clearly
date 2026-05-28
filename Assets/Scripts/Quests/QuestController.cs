using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Save;

namespace Assets.Scripts.Quests
{
	public class QuestController : MonoBehaviour
	{
		public static QuestController Instance { get; private set; }
		public List<QuestProgress> ActiveQuests = new();
		private readonly HashSet<string> completedQuestIDs = new();
		private QuestUI questUI;
		private Item item;

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
			transform.SetParent(null);
			DontDestroyOnLoad(gameObject);
			SceneManager.sceneLoaded += OnSceneLoaded;
			questUI = FindAnyObjectByType<QuestUI>();
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			questUI = FindAnyObjectByType<QuestUI>();
			questUI?.UpdateQuestUI();
		}

		public void AcceptQuest(Quest quest)
		{
			if (quest == null || IsQuestActive(quest.questID) || HasQuestBeenCompleted(quest.questID)) return;

			ActiveQuests.Add(new QuestProgress(quest));

			// Count items already in inventory so progress isn't reset to zero on accept
			InventoryManager inventory = FindAnyObjectByType<InventoryManager>();
			if (inventory != null)
			{
				var checkedIDs = new HashSet<string>();
				foreach (var obj in quest.objectives)
				{
					if (obj.type == objectiveType.CollectItem && checkedIDs.Add(obj.objectiveID))
					{
						int existing = inventory.GetItemCount(obj.objectiveID);
						if (existing > 0)
							UpdateObjectiveProgress(obj.objectiveID, objectiveType.CollectItem, existing);
					}
				}
			}

			questUI?.UpdateQuestUI();
		}

        public void UpdateObjectiveProgress(
			string objectiveID,
			objectiveType type,
			int amount)
			{
				var toComplete = new List<QuestProgress>();

				foreach (var quest in ActiveQuests)
				{
					foreach (var objective in quest.objectives)
					{
						if (objective.objectiveID == objectiveID && objective.type == type)
						{
							objective.currentAmount += amount;

							Debug.Log($"Quest progress updated: {objective.description} ({objective.currentAmount}/{objective.requiredAmount})");
						}
					}

					if (quest.IsCompleted && quest.quest.completionType == QuestCompletionType.AutoComplete)
						toComplete.Add(quest);
				}

				foreach (var quest in toComplete)
					CompleteQuest(quest);

				questUI?.UpdateQuestUI();
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

        private void CompleteQuest(QuestProgress quest)
        {
            if (quest == null)
                return;

            ActiveQuests.Remove(quest);

            completedQuestIDs.Add(quest.questID);

            GrantRewards(quest.quest);

            Debug.Log($"Quest completed: {quest.questID}");

            questUI?.UpdateQuestUI();
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
				switch (objective.type) //more quest types that dont require/use separate script interaction logic can be added here
				{
					case objectiveType.CollectItem:
						if (itemsToRemove.ContainsKey(objective.objectiveID))
						{
							itemsToRemove[objective.objectiveID] += objective.requiredAmount;
						}
						else
						{
							itemsToRemove[objective.objectiveID] = objective.requiredAmount;
						}
						break;

					default:
						Debug.LogWarning($"Unknown objective type: {objective.type}");
						break;
				}
				Debug.Log($"Objective: {objective.description}, ID: {objective.objectiveID}, Required: {objective.requiredAmount}");
			}

			// Remove each item type only once
			foreach (var item in itemsToRemove)
			{
				Debug.Log($"Removing {item.Value} of item ID: {item.Key}");
				inventory?.RemoveItem(item.Key, item.Value);
			}

            CompleteQuest(quest);

            Debug.Log("Quest turned in: " + questID);

            return true;
        }

		public bool TurnInQuest(Quest quest)
		{
			if (quest == null)
				return false;

			InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
			return TurnInQuest(quest.questID, inventoryManager);
		}

		private void GrantRewards(Quest quest)
		{
			if (quest == null || quest.rewards == null)
				return;

			GrantMoneyReward(quest.rewards.money);
			GrantCircularSatisfactionReward(quest.rewards.circularSatisfaction);
            GrantCommunityPointsReward(quest.rewards.communityPoints);
            GrantItemRewards(quest.rewards.items);
        }

		private void GrantMoneyReward(int amount)
		{
			if (amount == 0)
				return;

			MoneyManager moneyManager = FindAnyObjectByType<MoneyManager>();
			if (moneyManager != null)
			{
				moneyManager.ChangeMoneyAmount(amount);
				return;
			}

			Debug.LogWarning($"QuestController: Could not grant money reward of {amount}. No money manager found.");
		}

		private void GrantCircularSatisfactionReward(int amount)
		{
			if (Mathf.Approximately(amount, 0f))
				return;

			CircularSatisfactionMeter satisfactionMeter = FindAnyObjectByType<CircularSatisfactionMeter>();
			if (satisfactionMeter != null)
			{
				satisfactionMeter.IncreaseSatisfactionValue(amount);
				return;
			}

			Debug.LogWarning($"QuestController: Could not grant community spirit reward of {amount}. No CircularSatisfactionMeter found.");
		}

		public List<QuestSaveState> GetSaveData()
		{
			var result = new List<QuestSaveState>();
			foreach (var qp in ActiveQuests)
			{
				var state = new QuestSaveState { questID = qp.questID };
				foreach (var obj in qp.objectives)
					state.objectives.Add(new QuestObjectiveSaveState { objectiveID = obj.objectiveID, currentAmount = obj.currentAmount });
				result.Add(state);
			}
			return result;
		}

		public List<string> GetCompletedQuestIDs() => new List<string>(completedQuestIDs);

		public void LoadSaveData(List<QuestSaveState> activeData, List<string> completedIDs)
		{
			ActiveQuests.Clear();
			completedQuestIDs.Clear();

			if (completedIDs != null)
				foreach (var id in completedIDs)
					completedQuestIDs.Add(id);

			if (activeData == null) return;

			Quest[] allQuests = Resources.FindObjectsOfTypeAll<Quest>();
			foreach (var qd in activeData)
			{
				Quest quest = System.Array.Find(allQuests, q => q.questID == qd.questID);
				if (quest == null)
				{
					Debug.LogWarning($"QuestController: Could not find Quest asset with ID '{qd.questID}' during load.");
					continue;
				}

				var progress = new QuestProgress(quest);
				foreach (var objData in qd.objectives)
				{
					var obj = progress.objectives.Find(o => o.objectiveID == objData.objectiveID);
					if (obj != null) obj.currentAmount = objData.currentAmount;
				}
				ActiveQuests.Add(progress);
			}

			questUI?.UpdateQuestUI();
		}

		private void GrantItemRewards(List<QuestRewardItem> rewardItems)
		{
			if (rewardItems == null || rewardItems.Count == 0)
				return;

			InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
			if (inventoryManager == null)
			{
				Debug.LogWarning("QuestController: Could not grant item rewards. No InventoryManager found.");
				return;
			}

			foreach (QuestRewardItem rewardItem in rewardItems)
			{
				if (rewardItem == null || rewardItem.itemPrefab == null || rewardItem.quantity <= 0)
					continue;

				// Instantiate the Item prefab instead of using new
				Item newItem = Instantiate(rewardItem.itemPrefab);
				
				string itemTag = string.IsNullOrEmpty(rewardItem.inventoryTag) || rewardItem.inventoryTag == "Untagged"
					? rewardItem.itemPrefab.tag
					: rewardItem.inventoryTag;

				newItem.Initialize(rewardItem.itemPrefab.ItemName, 
					rewardItem.itemPrefab.Quantity,
					rewardItem.itemPrefab.Sprite,
					rewardItem.itemPrefab.ItemDescription,
					rewardItem.itemPrefab.MaxStack,
                    itemTag,
                    rewardItem.itemPrefab.EKeySprite
                );

				int leftover = inventoryManager.AddItem(newItem, newItem.Quantity);

				if (leftover > 0)
				{
					Debug.LogWarning($"QuestController: Could not add full reward stack for {rewardItem.itemPrefab.ItemName}. Leftover: {leftover}");
				}
			}
		}

		private void GrantCommunityPointsReward(int amount)
		{
			if (amount <= 0)
				return;
			CommunityMeter communityMeter = FindAnyObjectByType<CommunityMeter>();
			if (communityMeter != null)
			{
				communityMeter.IncreaseCommunityLevel(amount);
                return;
			}
			Debug.LogWarning($"QuestController: Could not grant community points reward of {amount}. No CommunityMeter found.");
        }
    }
}
