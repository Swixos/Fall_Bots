using UnityEngine;

namespace FallBots.Player
{
    /// <summary>
    /// Handles procedural animation for the player bean character.
    /// Squash/stretch, wobble, and tilt based on movement state.
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform bodyTransform;

        [Header("Squash & Stretch")]
        [SerializeField] private float squashAmount = 0.15f;
        [SerializeField] private float stretchAmount = 0.1f;
        [SerializeField] private float squashSpeed = 10f;

        [Header("Wobble")]
        [SerializeField] private float wobbleAmount = 5f;
        [SerializeField] private float wobbleSpeed = 8f;

        [Header("Tilt")]
        [SerializeField] private float moveTiltAmount = 10f;
        [SerializeField] private float tiltSpeed = 5f;

        [Header("Landing")]
        [SerializeField] private float landSquashIntensity = 0.3f;
        [SerializeField] private float landSquashDuration = 0.2f;

        private Vector3 baseScale;
        private Vector3 targetScale;
        private float wobbleTimer;
        private float landingTimer;
        private bool wasGrounded;

        private void Start()
        {
            if (bodyTransform == null) bodyTransform = transform;
            if (playerController == null) playerController = GetComponentInParent<PlayerController>();
            baseScale = bodyTransform.localScale;
            targetScale = baseScale;
        }

        private void Update()
        {
            if (playerController == null) return;

            HandleSquashStretch();
            HandleWobble();
            HandleLanding();
            ApplyScale();

            wasGrounded = playerController.IsGrounded;
        }

        private void HandleSquashStretch()
        {
            float verticalVelocity = playerController.Velocity.y;

            if (!playerController.IsGrounded)
            {
                if (verticalVelocity > 1f)
                {
                    // Stretching while going up
                    float t = Mathf.Clamp01(verticalVelocity / 10f);
                    targetScale = new Vector3(
                        baseScale.x * (1f - stretchAmount * t),
                        baseScale.y * (1f + stretchAmount * t),
                        baseScale.z * (1f - stretchAmount * t)
                    );
                }
                else if (verticalVelocity < -1f)
                {
                    // Squashing while falling
                    float t = Mathf.Clamp01(-verticalVelocity / 15f);
                    targetScale = new Vector3(
                        baseScale.x * (1f + squashAmount * t * 0.5f),
                        baseScale.y * (1f - squashAmount * t),
                        baseScale.z * (1f + squashAmount * t * 0.5f)
                    );
                }
            }
            else if (landingTimer <= 0)
            {
                targetScale = baseScale;
            }
        }

        private void HandleWobble()
        {
            if (playerController.IsMoving && playerController.IsGrounded)
            {
                wobbleTimer += Time.deltaTime * wobbleSpeed;
                float wobble = Mathf.Sin(wobbleTimer) * wobbleAmount;
                float speed = playerController.Velocity.magnitude;
                float wobbleFactor = Mathf.Clamp01(speed / 8f);

                if (bodyTransform != null)
                {
                    Vector3 euler = bodyTransform.localEulerAngles;
                    euler.z = wobble * wobbleFactor;
                    bodyTransform.localEulerAngles = euler;
                }
            }
            else
            {
                wobbleTimer = 0f;
            }
        }

        private void HandleLanding()
        {
            // Detect landing
            if (playerController.IsGrounded && !wasGrounded)
            {
                float fallSpeed = -playerController.Velocity.y;
                if (fallSpeed > 2f)
                {
                    float intensity = Mathf.Clamp01(fallSpeed / 20f) * landSquashIntensity;
                    targetScale = new Vector3(
                        baseScale.x * (1f + intensity),
                        baseScale.y * (1f - intensity),
                        baseScale.z * (1f + intensity)
                    );
                    landingTimer = landSquashDuration;
                }
            }

            if (landingTimer > 0)
            {
                landingTimer -= Time.deltaTime;
                if (landingTimer <= 0)
                {
                    targetScale = baseScale;
                }
            }
        }

        private void ApplyScale()
        {
            bodyTransform.localScale = Vector3.Lerp(bodyTransform.localScale, targetScale,
                squashSpeed * Time.deltaTime);
        }
    }
}
