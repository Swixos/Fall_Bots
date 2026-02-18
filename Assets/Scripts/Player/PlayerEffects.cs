using UnityEngine;

namespace FallBots.Player
{
    /// <summary>
    /// Handles visual and audio effects for the player (particles, trail, sounds).
    /// </summary>
    public class PlayerEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem landingParticles;
        [SerializeField] private ParticleSystem stunParticles;

        [Header("Trail")]
        [SerializeField] private float trailSpeedThreshold = 5f;

        private bool wasGrounded;

        private void Start()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (playerController == null) return;

            UpdateTrail();
            UpdateDust();
            DetectLanding();

            wasGrounded = playerController.IsGrounded;
        }

        private void UpdateTrail()
        {
            if (trailRenderer == null) return;
            trailRenderer.emitting = playerController.Velocity.magnitude > trailSpeedThreshold;
        }

        private void UpdateDust()
        {
            if (dustParticles == null) return;

            if (playerController.IsGrounded && playerController.IsMoving)
            {
                if (!dustParticles.isPlaying) dustParticles.Play();
            }
            else
            {
                if (dustParticles.isPlaying) dustParticles.Stop();
            }
        }

        private void DetectLanding()
        {
            if (playerController.IsGrounded && !wasGrounded)
            {
                if (landingParticles != null)
                {
                    landingParticles.transform.position = transform.position;
                    landingParticles.Play();
                }
            }
        }

        public void PlayStunEffect()
        {
            if (stunParticles != null)
            {
                stunParticles.Play();
            }
        }
    }
}
