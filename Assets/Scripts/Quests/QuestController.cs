using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Quests
{
    public class QuestController : MonoBehaviour
    {
        public static QuestController Instance { get; private set; }
        public List<QuestProgress> ActivateQuests = new();
        private QuestUI questUI;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            questUI = FindObjectOfType<QuestUI>();
        }

        public void AcceptQuest(Quest quest)
        {
            if (IsQuestActive(quest.questID)) return;

            ActivateQuests.Add(new QuestProgress(quest));

            questUI.UpdateQuestUI();
        }

        public bool IsQuestActive(string questID) => ActivateQuests.Exists(q => q.QuestID == questID);
    }
}