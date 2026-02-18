using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// A floor pad that launches the player upward when stepped on.
    /// </summary>
    public class LauncherObstacle : MonoBehaviour
    {
        public float launchForce = 18f;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField] private Color activeColor = new Color(1f, 1f, 0.2f);
        [SerializeField] private Color cooldownColor = new Color(0.5f, 0.5f, 0.1f);

        private float lastLaunchTime;
        private Renderer rend;

        private void Start()
        {
            rend = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (rend == null) return;

            if (Time.time - lastLaunchTime < cooldown)
                rend.material.color = cooldownColor;
            else
                rend.material.color = activeColor;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time - lastLaunchTime < cooldown) return;

            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            // Check player is landing from above
            if (collision.relativeVelocity.y < -0.5f)
            {
                player.ApplyKnockback(Vector3.up * launchForce);
                lastLaunchTime = Time.time;
            }
        }
    }
}
