using UnityEngine;
using FallBots.Player;

namespace FallBots.Obstacles
{
    /// <summary>
    /// Zone that slows down and makes the player slippery.
    /// Visual: greenish goo material.
    /// </summary>
    public class SlimeZone : MonoBehaviour
    {
        [SerializeField] private Color slimeColor = new Color(0.3f, 0.9f, 0.2f, 0.8f);

        private void Start()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = slimeColor;
            }

            // Ensure trigger collider
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetSlimeZone(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetSlimeZone(false);
            }
        }
    }
}
