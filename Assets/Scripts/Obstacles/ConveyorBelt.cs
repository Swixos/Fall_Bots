using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// Surface that pushes the player in a direction while they stand on it.
    /// </summary>
    public class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] private Vector3 pushDirection = Vector3.forward;
        [SerializeField] private float pushForce = 5f;

        [Header("Visual")]
        [SerializeField] private float scrollSpeed = 2f;
        private Renderer rend;
        private float scrollOffset;

        private void Start()
        {
            rend = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (rend != null)
            {
                scrollOffset += Time.deltaTime * scrollSpeed;
                rend.material.mainTextureOffset = new Vector2(0, scrollOffset);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                Vector3 worldPush = transform.TransformDirection(pushDirection.normalized) * pushForce;
                player.ApplyKnockback(worldPush * Time.fixedDeltaTime);
            }
        }
    }
}
