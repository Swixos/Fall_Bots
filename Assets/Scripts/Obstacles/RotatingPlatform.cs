using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// A platform that rotates slowly, requiring the player to balance.
    /// Can be used as a tilting/spinning disc obstacle.
    /// </summary>
    public class RotatingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 20f;
        [SerializeField] private bool tiltMode = false;
        [SerializeField] private float tiltAngle = 15f;
        [SerializeField] private float tiltSpeed = 1f;

        private void Update()
        {
            if (tiltMode)
            {
                float tilt = Mathf.Sin(Time.time * tiltSpeed) * tiltAngle;
                transform.localRotation = Quaternion.Euler(tilt, transform.localEulerAngles.y, 0f);
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }
            else
            {
                transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
