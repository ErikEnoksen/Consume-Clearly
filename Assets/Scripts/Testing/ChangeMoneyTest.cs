using UnityEngine;

public class ChangeMoneyTest : MonoBehaviour
{
    public int changeAmount = 5;

    private MoneyManager moneyManager;

    private void Awake()
    {
        moneyManager = GameObject.Find("MoneyManager").GetComponent<MoneyManager>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {

            if (Input.GetKey(KeyCode.F))
            {
                Debug.Log(moneyManager.ChangeMoneyAmount(changeAmount));
            }
        }
        
    }

}
