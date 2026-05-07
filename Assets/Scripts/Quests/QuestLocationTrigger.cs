using UnityEngine;
using Assets.Scripts.Quests;

public class QuestLocationTrigger : MonoBehaviour
{
    [SerializeField] private string locationID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log($"Entered location: {locationID}");

        QuestController.Instance?.UpdateObjectiveProgress(
            locationID,
            objectiveType.ReachLocation,
            1
        );
    }
}