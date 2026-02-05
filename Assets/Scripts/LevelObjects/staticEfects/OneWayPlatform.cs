using UnityEngine;

namespace LevelObjects.staticEfects
{
    public class OneWayPlatform : MonoBehaviour
    {
        private PlatformEffector2D platformEffector;
    
        private void Awake()
        {
            platformEffector = GetComponent<PlatformEffector2D>();
        
            if (platformEffector == null)
            {
                Debug.LogError("PlatformEffector2D component is missing!");
            }
        }
    
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                StartCoroutine(DisableCollision());
            }
        }
    
        private System.Collections.IEnumerator DisableCollision()
        {
            // Temporarily disable the platform
            platformEffector.rotationalOffset = 180f;
            yield return new WaitForSeconds(0.7f);
            platformEffector.rotationalOffset = 0f;
        }
    }
}
