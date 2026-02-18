using UnityEngine;
using FallBots.Player;
using FallBots.Managers;

namespace FallBots.Utils
{
    /// <summary>
    /// Kills/respawns the player when they enter this trigger zone.
    /// Placed below the course as a safety net.
    /// </summary>
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // GameManager handles respawn through its height check
                // This is a backup trigger
            }
        }
    }
}
