using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
	public string questID;
	public string questName;
	public string description;

	public List<QuestObjective> objectives;
	
	public QuestReward rewards;
	
	public QuestCompletionType completionType;

	//called when scriptable obj is edited
	public void OnValidate()
	{
		if (string.IsNullOrEmpty(questID))
		{
			questID = questName + Guid.NewGuid().ToString();
		}
	}

}

[Serializable]
public class QuestReward
{
	public int money;
	public int circularSatisfaction;
    public int communityPoints;
    public List<QuestRewardItem> items;
}

[Serializable]
public class QuestRewardItem
{
	public Item itemPrefab;
	public int quantity = 1;
	public string inventoryTag = "Untagged";
}



[Serializable]
public class QuestObjective
{
	public string objectiveID; //match with item ID you need to collect or task you need to complete
	public string description;
	public objectiveType type;
	public int requiredAmount;
	public int currentAmount;

	public bool IsCompleted => currentAmount >= requiredAmount;

}
public enum QuestCompletionType
{
	ManualTurnIn,
	AutoComplete
}

public enum objectiveType
{
	CollectItem, ReachLocation, RepairObject
}

[Serializable]
public class QuestProgress
{
	public Quest quest;
	public List<QuestObjective> objectives;

	public QuestProgress(Quest quest)
	{
		this.quest = quest;
		objectives = new List<QuestObjective>();

		//deep copy to avoid modifying original
		foreach (var obj in quest.objectives)
		{
			objectives.Add(new QuestObjective
			{
				objectiveID = obj.objectiveID,
				description = obj.description,
				type = obj.type,
				requiredAmount = obj.requiredAmount,
				currentAmount = 0
			});
		}
	}

	public bool IsCompleted => objectives.TrueForAll(o => o.IsCompleted);

	public string questID => quest.questID;
}
