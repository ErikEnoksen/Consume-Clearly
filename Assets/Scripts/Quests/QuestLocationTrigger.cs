using UnityEngine;
using Assets.Scripts.Quests;

// Used for ReachLocation quest type, to use in unity put this on a GameObject with a 2d collider
public class QuestLocationTrigger : MonoBehaviour
{
    [SerializeField] private string locationID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log($"Entered location: {locationID}");

        // Call UpdateObjectiveProgress to autocomplete or update a quest objective
        QuestController.Instance?.UpdateObjectiveProgress(
            locationID,
            objectiveType.ReachLocation,
            1
        );
    }
}