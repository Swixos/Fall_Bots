using UnityEngine;

namespace FallBots.Utils
{
    /// <summary>
    /// Makes an object bob up and down. Used for decorative/collectible elements.
    /// </summary>
    public class BobObject : MonoBehaviour
    {
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.5f;

        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }
}
