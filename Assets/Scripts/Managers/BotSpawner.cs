using UnityEngine;
using FallBots.Player;

namespace FallBots.Managers
{
    /// <summary>
    /// Spawns AI bot opponents at the start of each race.
    /// </summary>
    public class BotSpawner : MonoBehaviour
    {
        [SerializeField] private int botCount = 5;
        [SerializeField] private float spawnRadius = 4f;

        private GameObject[] bots;

        public void SpawnBots(Vector3 startPosition)
        {
            DespawnBots();

            bots = new GameObject[botCount];
            for (int i = 0; i < botCount; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-spawnRadius, spawnRadius),
                    0f,
                    Random.Range(-1f, 1f)
                );

                GameObject bot = new GameObject($"Bot_{i}");
                bot.layer = 0; // Default layer
                bot.transform.position = startPosition + offset + Vector3.up * 2f;

                var capsule = bot.AddComponent<CapsuleCollider>();
                capsule.height = 1.8f;
                capsule.radius = 0.4f;
                capsule.center = new Vector3(0f, 0.9f, 0f);

                var botCtrl = bot.AddComponent<BotController>();
                botCtrl.SetupVisuals();

                bots[i] = bot;
            }
        }

        public void DespawnBots()
        {
            if (bots == null) return;
            foreach (var bot in bots)
            {
                if (bot != null) Destroy(bot);
            }
            bots = null;
        }
    }
}
