using Assets.Scripts.Quests;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour, IUILockable
{
    public Transform questListContent;
    public GameObject questEntryPrefab;
    public GameObject objectiveTextPrefab;
    public GameObject questPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIManager.Instance?.RegisterUI(this);
        UpdateQuestUI();
    }

    private void OnDestroy()
    {
        UIManager.Instance?.UnregisterUI(this);
    }

    public void SetLocked(bool locked)
    {
        var target = questPanel != null ? questPanel : gameObject;
        target.SetActive(!locked);
    }

    public void UpdateQuestUI()
    {
        //destroy existing quest entries
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        //build quest entries
        foreach (var quest in QuestController.Instance.ActiveQuests)
        {
            GameObject entry = Instantiate(questEntryPrefab, questListContent);
            TMP_Text questNameText = entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();
            Transform objectiveList = entry.transform.Find("ObjectiveList");

            questNameText.text = quest.quest.name;

            foreach (var objective in quest.objectives)
            {
                GameObject objTextGO = Instantiate(objectiveTextPrefab, objectiveList);
                TMP_Text objText = objTextGO.GetComponent<TMP_Text>();
                objText.text = $"{objective.description} ({objective.currentAmount}/{objective.requiredAmount})"; //Collect 5 TNT (0/5)
            }
        }
    }
}
