using UnityEngine;
using UnityEngine.InputSystem;

namespace FallBots.Player
{
    /// <summary>
    /// Third-person camera that follows the player with smooth damping.
    /// Supports collision avoidance and dynamic FOV changes.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Follow")]
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float heightDamping = 3f;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minVerticalAngle = -20f;
        [SerializeField] private float maxVerticalAngle = 60f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private LayerMask collisionLayers = ~0;

        [Header("Dynamic FOV")]
        [SerializeField] private float baseFOV = 60f;
        [SerializeField] private float sprintFOV = 70f;
        [SerializeField] private float fovLerpSpeed = 5f;

        private Camera cam;
        private float currentYaw;
        private float currentPitch = 15f;
        private float targetFOV;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = gameObject.AddComponent<Camera>();
            targetFOV = baseFOV;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            UpdatePosition();
            UpdateRotation();
            UpdateFOV();
        }

        private void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float mouseX = mouse.delta.x.ReadValue() * mouseSensitivity * 0.1f;
            float mouseY = mouse.delta.y.ReadValue() * mouseSensitivity * 0.1f;

            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }

        private void UpdatePosition()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredPosition = target.position + rotation * offset;

            // Smoothly adjust height
            float currentHeight = transform.position.y;
            float targetHeight = desiredPosition.y;
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, heightDamping * Time.deltaTime);
            desiredPosition.y = currentHeight;

            // Collision detection
            Vector3 dirToCamera = desiredPosition - (target.position + lookOffset);
            float maxDistance = dirToCamera.magnitude;

            if (Physics.SphereCast(target.position + lookOffset, collisionRadius,
                dirToCamera.normalized, out RaycastHit hit, maxDistance, collisionLayers))
            {
                desiredPosition = hit.point + hit.normal * collisionRadius;
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition,
                followSpeed * Time.deltaTime);
        }

        private void UpdateRotation()
        {
            Vector3 lookTarget = target.position + lookOffset;
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        private void UpdateFOV()
        {
            // Speed-based FOV
            PlayerController pc = target.GetComponent<PlayerController>();
            if (pc != null)
            {
                float speed = pc.Velocity.magnitude;
                targetFOV = speed > 9f ? sprintFOV : baseFOV;
            }

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
        }
    }
}
