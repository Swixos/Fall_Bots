using UnityEngine;
using UnityEngine.InputSystem;

namespace FallBots.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float sprintSpeed = 12f;
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 8f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float jumpCooldown = 0.15f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Dive")]
        [SerializeField] private float diveForce = 8f;
        [SerializeField] private float diveDownForce = 4f;
        [SerializeField] private float diveRecoveryTime = 0.6f;

        [Header("Physics")]
        [SerializeField] private float gravityMultiplier = 2.5f;
        [SerializeField] private float fallMultiplier = 3f;
        [SerializeField] private float maxFallSpeed = 30f;
        [SerializeField] private PhysicsMaterial slipperyMaterial;
        [SerializeField] private PhysicsMaterial normalMaterial;

        private Rigidbody rb;
        private CapsuleCollider capsule;
        private Camera mainCamera;

        private Vector3 moveInput;
        private Vector3 currentVelocity;
        private bool isGrounded;
        private bool wasGrounded;
        private float lastGroundedTime;
        private float lastJumpPressTime;
        private float lastJumpTime;
        private bool isDiving;
        private float diveTimer;
        private bool isStunned;
        private float stunTimer;
        private bool isSprinting;
        private bool isInSlime;
        private float slimeSpeedMultiplier = 0.4f;

        // Public properties
        public bool IsGrounded => isGrounded;
        public bool IsDiving => isDiving;
        public bool IsStunned => isStunned;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public Vector3 Velocity => rb.velocity;
        public bool HasFinished { get; set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            mainCamera = Camera.main;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.mass = 1.5f;
            rb.drag = 0.5f;
            rb.angularDrag = 5f;
        }

        private void Update()
        {
            if (HasFinished || isStunned)
            {
                moveInput = Vector3.zero;
                UpdateTimers();
                return;
            }

            GatherInput();
            UpdateTimers();
        }

        private void FixedUpdate()
        {
            CheckGround();
            ApplyMovement();
            ApplyGravity();
            ClampFallSpeed();
        }

        private void GatherInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = 0f;
            float v = 0f;

            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;

            // Camera-relative movement
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 camForward = mainCamera.transform.forward;
                Vector3 camRight = mainCamera.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                moveInput = (camForward * v + camRight * h).normalized;
            }
            else
            {
                moveInput = new Vector3(h, 0f, v).normalized;
            }

            isSprinting = kb.leftShiftKey.isPressed;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                lastJumpPressTime = Time.time;
                TryJump();
            }

            if (kb.leftCtrlKey.wasPressedThisFrame && !isDiving && !isGrounded)
            {
                StartDive();
            }
        }

        private void UpdateTimers()
        {
            if (isDiving)
            {
                diveTimer -= Time.deltaTime;
                if (diveTimer <= 0f && isGrounded)
                {
                    isDiving = false;
                }
            }

            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                {
                    isStunned = false;
                }
            }
        }

        private void CheckGround()
        {
            wasGrounded = isGrounded;
            Vector3 origin = transform.position + Vector3.up * (capsule.radius + 0.05f);
            isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
                out _, groundCheckDistance + capsule.radius, groundLayers);

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }
        }

        private void ApplyMovement()
        {
            if (isDiving || isStunned) return;

            float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
            if (isInSlime) targetSpeed *= slimeSpeedMultiplier;

            Vector3 targetVelocity = moveInput * targetSpeed;
            float smoothRate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;

            currentVelocity = Vector3.Lerp(
                new Vector3(rb.velocity.x, 0, rb.velocity.z),
                targetVelocity,
                smoothRate * Time.fixedDeltaTime
            );

            rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);

            // Rotation towards movement direction
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveInput, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void ApplyGravity()
        {
            if (isGrounded) return;

            float multiplier = rb.velocity.y < 0 ? fallMultiplier : gravityMultiplier;
            rb.AddForce(Vector3.down * multiplier, ForceMode.Acceleration);
        }

        private void ClampFallSpeed()
        {
            if (rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
            }
        }

        private void TryJump()
        {
            bool canCoyote = Time.time - lastGroundedTime <= coyoteTime;
            bool canBuffer = Time.time - lastJumpPressTime <= jumpBufferTime;
            bool cooldownPassed = Time.time - lastJumpTime >= jumpCooldown;

            if ((isGrounded || canCoyote) && canBuffer && cooldownPassed && !isDiving)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                lastJumpTime = Time.time;
                isGrounded = false;
            }
        }

        private void StartDive()
        {
            isDiving = true;
            diveTimer = diveRecoveryTime;

            Vector3 diveDirection = moveInput.sqrMagnitude > 0.01f
                ? moveInput.normalized
                : transform.forward;

            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(diveDirection * diveForce + Vector3.down * diveDownForce, ForceMode.Impulse);
        }

        public void ApplyKnockback(Vector3 force)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        public void Stun(float duration)
        {
            isStunned = true;
            stunTimer = duration;
            rb.velocity = new Vector3(rb.velocity.x * 0.3f, rb.velocity.y, rb.velocity.z * 0.3f);
        }

        public void SetSlimeZone(bool inSlime)
        {
            isInSlime = inSlime;
            if (capsule != null)
            {
                capsule.material = inSlime ? slipperyMaterial : normalMaterial;
            }
        }

        public void Respawn(Vector3 position)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = position + Vector3.up * 2f;
            isDiving = false;
            isStunned = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Landing from dive causes brief stun
            if (isDiving && isGrounded)
            {
                isDiving = false;
                Stun(0.2f);
            }
        }
    }
}
