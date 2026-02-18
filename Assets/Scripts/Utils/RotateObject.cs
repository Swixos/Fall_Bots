using UnityEngine;

namespace FallBots.Utils
{
    /// <summary>
    /// Simple utility to rotate an object continuously. Used for decorative elements.
    /// </summary>
    public class RotateObject : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}
