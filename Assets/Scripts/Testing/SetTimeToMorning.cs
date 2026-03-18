using UnityEngine;

public class SetTimeToMorning : MonoBehaviour
{

    private SkyCycle skyCycle;

    private void Start()
    {
        skyCycle = GameObject.Find("SkyManager").GetComponent<SkyCycle>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            Debug.Log("Player present");

            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("F pressed");

                skyCycle.SetMorning();
            }
        }
    }

}
