using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// Obstacle that spins around an axis. Used for spinning bars, windmills, rollers.
    /// Knocks back player on contact.
    /// </summary>
    public class SpinningObstacle : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 60f;
        public float knockbackForce = 8f;
        public bool isRoller = false;

        [Header("Visual")]
        [SerializeField] private bool pulseColor = false;
        [SerializeField] private float pulseSpeed = 2f;

        private Renderer[] renderers;
        private Color[] originalColors;

        public void SetParameters(Vector3 axis, float speed)
        {
            rotationAxis = axis;
            rotationSpeed = speed;
        }

        private void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }

        private void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

            if (pulseColor)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].material.color = Color.Lerp(originalColors[i], Color.white, t * 0.3f);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            Vector3 knockDir;
            if (isRoller)
            {
                knockDir = (collision.transform.position - transform.position).normalized;
                knockDir.y = 0.3f;
            }
            else
            {
                // Tangential knockback based on spinning direction
                Vector3 contactPoint = collision.contacts[0].point;
                Vector3 toContact = contactPoint - transform.position;
                knockDir = Vector3.Cross(rotationAxis, toContact).normalized;
                knockDir.y = 0.4f;
            }

            player.ApplyKnockback(knockDir * knockbackForce);
            player.Stun(0.3f);
        }
    }
}
