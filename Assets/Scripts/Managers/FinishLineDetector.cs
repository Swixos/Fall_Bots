using UnityEngine;
using FallBots.Player;

namespace FallBots.Managers
{
    /// <summary>
    /// Detects when the player crosses the finish line and notifies the GameManager.
    /// </summary>
    public class FinishLineDetector : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.HasFinished)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPlayerFinished();
                }
            }
        }
    }
}
