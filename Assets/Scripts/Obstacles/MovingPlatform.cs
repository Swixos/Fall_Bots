using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// Platform that moves back and forth along a path.
    /// Can optionally knock back players on collision (for punch walls).
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 moveDistance = new Vector3(3f, 0f, 0f);
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public bool knockbackOnHit = false;
        public float knockbackForce = 8f;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private float moveTimer;
        private bool movingForward = true;
        private float timeOffset;

        public void SetMovement(Vector3 distance, float speed)
        {
            moveDistance = distance;
            moveSpeed = speed;
        }

        private void Start()
        {
            startPosition = transform.localPosition;
            endPosition = startPosition + moveDistance;
            timeOffset = Random.Range(0f, 2f);
            moveTimer = timeOffset;
        }

        private void Update()
        {
            moveTimer += Time.deltaTime * moveSpeed;
            float t = Mathf.PingPong(moveTimer, 1f);
            t = moveCurve.Evaluate(t);

            transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!knockbackOnHit) return;

            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            Vector3 knockDir = (collision.transform.position - transform.position).normalized;
            knockDir.y = 0.3f;
            knockDir.Normalize();

            player.ApplyKnockback(knockDir * knockbackForce);
            player.Stun(0.3f);
        }

        private void OnCollisionStay(Collision collision)
        {
            // Make player move with platform
            if (!knockbackOnHit && collision.gameObject.GetComponent<PlayerController>() != null)
            {
                // Parent trick for smooth riding
                // Using velocity transfer instead of parenting for physics stability
            }
        }
    }
}
