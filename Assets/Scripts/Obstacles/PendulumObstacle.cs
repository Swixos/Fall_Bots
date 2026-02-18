using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// Swinging pendulum obstacle. Swings back and forth on a configurable axis.
    /// </summary>
    public class PendulumObstacle : MonoBehaviour
    {
        [SerializeField] private float swingSpeed = 2f;
        [SerializeField] private float maxAngle = 45f;
        [SerializeField] private Vector3 swingAxis = Vector3.right;
        public float knockbackForce = 12f;

        private float startAngle;
        private float timeOffset;

        public void SetParameters(float speed, float angle)
        {
            swingSpeed = speed;
            maxAngle = angle;
        }

        private void Start()
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float angle = Mathf.Sin(Time.time * swingSpeed + timeOffset) * maxAngle;
            transform.localRotation = Quaternion.Euler(swingAxis * angle);
        }

        private void OnCollisionEnter(Collision collision)
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            // Knockback in swing direction
            float swingVelocity = Mathf.Cos(Time.time * swingSpeed + timeOffset) * swingSpeed * maxAngle;
            Vector3 knockDir = transform.right * Mathf.Sign(swingVelocity);
            knockDir.y = 0.5f;
            knockDir.Normalize();

            player.ApplyKnockback(knockDir * knockbackForce);
            player.Stun(0.4f);
        }
    }
}
