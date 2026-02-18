using UnityEngine;
using System.Collections;

namespace FallBots.Obstacles
{
    /// <summary>
    /// Tile that falls after being stepped on. Can respawn after a delay.
    /// Visual warning: shakes before falling.
    /// </summary>
    public class FallingTile : MonoBehaviour
    {
        [SerializeField] private float warningDuration = 1f;
        [SerializeField] private float respawnDelay = 4f;
        [SerializeField] private bool doesRespawn = true;

        [Header("Warning")]
        [SerializeField] private float shakeIntensity = 0.05f;
        [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f);

        private Vector3 originalPosition;
        private Color originalColor;
        private Renderer meshRenderer;
        private Collider tileCollider;
        private Rigidbody rb;
        private bool isTriggered;
        private bool isFalling;

        public void SetParameters(float warning, float respawn, bool canRespawn)
        {
            warningDuration = warning;
            respawnDelay = respawn;
            doesRespawn = canRespawn;
        }

        private void Awake()
        {
            originalPosition = transform.position;
            meshRenderer = GetComponent<Renderer>();
            tileCollider = GetComponent<Collider>();

            if (meshRenderer != null)
                originalColor = meshRenderer.material.color;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isTriggered || isFalling) return;
            if (collision.gameObject.GetComponent<FallBots.Player.PlayerController>() == null) return;

            isTriggered = true;
            StartCoroutine(FallSequence());
        }

        private IEnumerator FallSequence()
        {
            // Warning phase - shake and change color
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / warningDuration;

                // Shake
                Vector3 shake = Random.insideUnitSphere * shakeIntensity;
                shake.y = 0;
                transform.position = originalPosition + shake;

                // Color lerp
                if (meshRenderer != null)
                {
                    meshRenderer.material.color = Color.Lerp(originalColor, warningColor, t);
                }

                yield return null;
            }

            // Fall
            isFalling = true;
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 5f;

            yield return new WaitForSeconds(2f);

            if (doesRespawn)
            {
                // Hide
                meshRenderer.enabled = false;
                tileCollider.enabled = false;
                Destroy(rb);

                yield return new WaitForSeconds(respawnDelay);

                // Respawn
                transform.position = originalPosition;
                meshRenderer.enabled = true;
                tileCollider.enabled = true;
                if (meshRenderer != null)
                    meshRenderer.material.color = originalColor;

                isTriggered = false;
                isFalling = false;
            }
            else
            {
                yield return new WaitForSeconds(5f);
                Destroy(gameObject);
            }
        }
    }
}
