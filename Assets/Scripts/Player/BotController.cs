using UnityEngine;

namespace FallBots.Player
{
    /// <summary>
    /// Simple AI bot that navigates the course by moving forward and
    /// avoiding obstacles with basic raycasting.
    /// Creates visual variety with random colors.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class BotController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float jumpForce = 9f;

        [Header("AI")]
        [SerializeField] private float obstacleDetectDistance = 4f;
        [SerializeField] private float sideRayAngle = 30f;
        [SerializeField] private float jumpCheckHeight = 1.5f;
        [SerializeField] private float randomStrafeInterval = 2f;
        [SerializeField] private float strafeAmount = 3f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody rb;
        private bool isGrounded;
        private float strafeTimer;
        private float currentStrafe;
        private Vector3 targetDirection;
        private bool isStunned;
        private float stunTimer;

        private static readonly Color[] botColors = new Color[]
        {
            new Color(1f, 0.4f, 0.4f),   // Red
            new Color(0.4f, 1f, 0.5f),   // Green
            new Color(1f, 0.9f, 0.3f),   // Yellow
            new Color(1f, 0.5f, 0.8f),   // Pink
            new Color(0.5f, 0.8f, 1f),   // Light Blue
            new Color(1f, 0.6f, 0.3f),   // Orange
            new Color(0.7f, 0.5f, 1f),   // Purple
            new Color(0.4f, 1f, 0.9f),   // Cyan
        };

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.mass = 1.5f;
            rb.drag = 0.5f;

            targetDirection = Vector3.forward;
        }

        public void SetupVisuals()
        {
            Color botColor = botColors[Random.Range(0, botColors.Length)];

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "BotBody";
            body.transform.SetParent(transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Destroy(body.GetComponent<Collider>());

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = botColor;
            mat.SetFloat("_Smoothness", 0.7f);
            body.GetComponent<Renderer>().material = mat;

            // Eyes
            CreateBotEye(body.transform, new Vector3(-0.15f, 0.5f, 0.4f));
            CreateBotEye(body.transform, new Vector3(0.15f, 0.5f, 0.4f));
        }

        private void CreateBotEye(Transform parent, Vector3 pos)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.transform.SetParent(parent);
            eye.transform.localPosition = pos;
            eye.transform.localScale = new Vector3(0.15f, 0.2f, 0.1f);
            Destroy(eye.GetComponent<Collider>());

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            eye.GetComponent<Renderer>().material = new Material(shader) { color = Color.white };
        }

        private void Update()
        {
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0) isStunned = false;
                return;
            }

            UpdateAI();
        }

        private void FixedUpdate()
        {
            CheckGround();
            if (!isStunned)
            {
                ApplyMovement();
            }
        }

        private void UpdateAI()
        {
            // Random strafe to simulate imperfect navigation
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                currentStrafe = Random.Range(-strafeAmount, strafeAmount);
                strafeTimer = randomStrafeInterval + Random.Range(-0.5f, 0.5f);
            }

            // Obstacle avoidance
            Vector3 forward = transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -sideRayAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, sideRayAngle, 0) * forward;

            bool obstacleAhead = Physics.Raycast(transform.position + Vector3.up, forward,
                obstacleDetectDistance);
            bool obstacleLeft = Physics.Raycast(transform.position + Vector3.up, leftDir,
                obstacleDetectDistance * 0.7f);
            bool obstacleRight = Physics.Raycast(transform.position + Vector3.up, rightDir,
                obstacleDetectDistance * 0.7f);

            // Check if should jump
            bool shouldJump = Physics.Raycast(transform.position + Vector3.up * 0.3f, forward,
                jumpCheckHeight);

            targetDirection = Vector3.forward; // Always move forward towards finish

            if (obstacleAhead)
            {
                if (!obstacleLeft) targetDirection += Vector3.left * 2f;
                else if (!obstacleRight) targetDirection += Vector3.right * 2f;
                else if (shouldJump && isGrounded) Jump();
            }

            targetDirection += Vector3.right * currentStrafe * 0.3f;
            targetDirection.Normalize();

            if (shouldJump && isGrounded)
            {
                Jump();
            }
        }

        private void CheckGround()
        {
            isGrounded = Physics.SphereCast(transform.position + Vector3.up * 0.5f,
                groundCheckRadius, Vector3.down, out _, 0.35f, groundLayers);
        }

        private void ApplyMovement()
        {
            float speed = moveSpeed + Random.Range(-0.5f, 0.5f);
            Vector3 targetVel = targetDirection * speed;

            rb.velocity = new Vector3(
                Mathf.Lerp(rb.velocity.x, targetVel.x, 8f * Time.fixedDeltaTime),
                rb.velocity.y,
                Mathf.Lerp(rb.velocity.z, targetVel.z, 8f * Time.fixedDeltaTime)
            );

            if (targetDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                    rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void Jump()
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        public void Stun(float duration)
        {
            isStunned = true;
            stunTimer = duration;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // React to obstacles
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                Stun(0.5f);
                Vector3 knockDir = (transform.position - collision.transform.position).normalized;
                knockDir.y = 0.3f;
                rb.AddForce(knockDir * 5f, ForceMode.Impulse);
            }
        }
    }
}
