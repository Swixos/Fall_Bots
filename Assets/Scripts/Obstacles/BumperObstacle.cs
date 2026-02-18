using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// Bouncy bumper that launches players away on contact.
    /// Visual feedback with scale pulse on hit.
    /// </summary>
    public class BumperObstacle : MonoBehaviour
    {
        public float bounceForce = 15f;
        [SerializeField] private float upwardBounceRatio = 0.4f;

        [Header("Visual Feedback")]
        [SerializeField] private float pulseDuration = 0.2f;
        [SerializeField] private float pulseScale = 1.3f;

        private Vector3 originalScale;
        private float pulseTimer;
        private bool isPulsing;

        private void Start()
        {
            originalScale = transform.localScale;
        }

        private void Update()
        {
            if (isPulsing)
            {
                pulseTimer -= Time.deltaTime;
                float t = 1f - (pulseTimer / pulseDuration);
                float scale = Mathf.Lerp(pulseScale, 1f, t);
                transform.localScale = originalScale * scale;

                if (pulseTimer <= 0f)
                {
                    isPulsing = false;
                    transform.localScale = originalScale;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            Vector3 bounceDir = (collision.transform.position - transform.position).normalized;
            bounceDir.y = upwardBounceRatio;
            bounceDir.Normalize();

            player.ApplyKnockback(bounceDir * bounceForce);

            // Visual pulse
            isPulsing = true;
            pulseTimer = pulseDuration;
        }
    }
}
