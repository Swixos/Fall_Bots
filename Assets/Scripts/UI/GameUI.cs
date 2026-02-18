using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FallBots.UI
{
    /// <summary>
    /// Main game UI controller. Handles countdown, timer, finish screen, and notifications.
    /// Creates all UI elements at runtime - no prefabs needed.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        private Canvas canvas;
        private Text countdownText;
        private Text timerText;
        private Text checkpointText;
        private Text seedText;
        private GameObject finishPanel;
        private Text finishTimeText;
        private Text bestTimeText;
        private Text newBestText;
        private Text restartText;
        private Text penaltyText;
        private Text controlsText;

        private Coroutine checkpointCoroutine;
        private Coroutine penaltyCoroutine;

        private void Awake()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            // Canvas
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            // Countdown text (center)
            countdownText = CreateText("CountdownText", Vector2.zero, 120, TextAnchor.MiddleCenter,
                FontStyle.Bold, Color.white);
            countdownText.gameObject.SetActive(false);
            AddOutline(countdownText.gameObject, Color.black, new Vector2(3, -3));

            // Timer text (top center)
            timerText = CreateText("TimerText", new Vector2(0, -60), 48, TextAnchor.UpperCenter,
                FontStyle.Bold, Color.white);
            var timerRect = timerText.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            AddOutline(timerText.gameObject, new Color(0, 0, 0, 0.7f), new Vector2(2, -2));

            // Seed display (top left)
            seedText = CreateText("SeedText", new Vector2(20, -20), 24, TextAnchor.UpperLeft,
                FontStyle.Normal, new Color(1, 1, 1, 0.6f));
            var seedRect = seedText.GetComponent<RectTransform>();
            seedRect.anchorMin = new Vector2(0, 1);
            seedRect.anchorMax = new Vector2(0, 1);
            seedRect.pivot = new Vector2(0, 1);

            // Checkpoint notification (center-top)
            checkpointText = CreateText("CheckpointText", new Vector2(0, -150), 36, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(0.3f, 0.8f, 1f));
            checkpointText.text = "CHECKPOINT!";
            checkpointText.gameObject.SetActive(false);
            AddOutline(checkpointText.gameObject, Color.black, new Vector2(2, -2));

            // Penalty text
            penaltyText = CreateText("PenaltyText", new Vector2(0, -200), 32, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Color(1f, 0.3f, 0.3f));
            penaltyText.text = "+2s PENALTY";
            penaltyText.gameObject.SetActive(false);
            AddOutline(penaltyText.gameObject, Color.black, new Vector2(2, -2));

            // Controls hint (bottom left)
            controlsText = CreateText("ControlsText", new Vector2(20, 20), 20, TextAnchor.LowerLeft,
                FontStyle.Normal, new Color(1, 1, 1, 0.5f));
            var controlsRect = controlsText.GetComponent<RectTransform>();
            controlsRect.anchorMin = Vector2.zero;
            controlsRect.anchorMax = Vector2.zero;
            controlsRect.pivot = Vector2.zero;
            controlsRect.sizeDelta = new Vector2(400, 120);
            controlsText.text = "WASD - Move | SHIFT - Sprint\nSPACE - Jump | CTRL - Dive\nMouse - Camera";

            // Finish panel
            CreateFinishPanel();
        }

        private void CreateFinishPanel()
        {
            finishPanel = new GameObject("FinishPanel");
            finishPanel.transform.SetParent(canvas.transform, false);

            var panelRect = finishPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = finishPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // Title
            Text titleText = CreateTextOnParent(finishPanel.transform, "FinishTitle",
                new Vector2(0, 100), 80, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Color(1f, 0.84f, 0f));
            titleText.text = "FINISH!";
            AddOutline(titleText.gameObject, Color.black, new Vector2(3, -3));

            // Finish time
            finishTimeText = CreateTextOnParent(finishPanel.transform, "FinishTime",
                new Vector2(0, 0), 56, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            AddOutline(finishTimeText.gameObject, Color.black, new Vector2(2, -2));

            // Best time
            bestTimeText = CreateTextOnParent(finishPanel.transform, "BestTime",
                new Vector2(0, -60), 36, TextAnchor.MiddleCenter, FontStyle.Normal,
                new Color(0.8f, 0.8f, 0.8f));

            // New best text
            newBestText = CreateTextOnParent(finishPanel.transform, "NewBest",
                new Vector2(0, -110), 42, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Color(1f, 0.84f, 0f));
            newBestText.text = "NEW BEST TIME!";

            // Restart instruction
            restartText = CreateTextOnParent(finishPanel.transform, "RestartText",
                new Vector2(0, -180), 28, TextAnchor.MiddleCenter, FontStyle.Normal,
                new Color(0.7f, 0.7f, 0.7f));
            restartText.text = "Press R to restart | ESC to quit";

            finishPanel.SetActive(false);
        }

        #region Public Methods

        public void ShowCountdown(bool show)
        {
            countdownText.gameObject.SetActive(show);
        }

        public void UpdateCountdown(string text)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = text;

            if (text == "GO!")
            {
                countdownText.color = new Color(0.2f, 1f, 0.3f);
                StartCoroutine(FadeOutText(countdownText, 0.8f));
            }
            else
            {
                countdownText.color = Color.white;
                // Pulse effect
                StartCoroutine(PulseText(countdownText));
            }
        }

        public void UpdateTimer(float time)
        {
            int minutes = (int)(time / 60f);
            int seconds = (int)(time % 60f);
            int milliseconds = (int)((time * 100f) % 100f);
            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }

        public void ShowFinishScreen(bool show, float time = 0, float bestTime = 0, bool isNewBest = false)
        {
            finishPanel.SetActive(show);

            if (show)
            {
                int minutes = (int)(time / 60f);
                int seconds = (int)(time % 60f);
                int ms = (int)((time * 100f) % 100f);
                finishTimeText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, ms);

                if (bestTime < float.MaxValue)
                {
                    int bMin = (int)(bestTime / 60f);
                    int bSec = (int)(bestTime % 60f);
                    int bMs = (int)((bestTime * 100f) % 100f);
                    bestTimeText.text = string.Format("Best: {0:00}:{1:00}.{2:00}", bMin, bSec, bMs);
                }
                else
                {
                    bestTimeText.text = "";
                }

                newBestText.gameObject.SetActive(isNewBest);
            }
        }

        public void ShowCheckpointMessage()
        {
            if (checkpointCoroutine != null) StopCoroutine(checkpointCoroutine);
            checkpointCoroutine = StartCoroutine(ShowTemporaryText(checkpointText, 1.5f));
        }

        public void ShowRespawnPenalty()
        {
            if (penaltyCoroutine != null) StopCoroutine(penaltyCoroutine);
            penaltyCoroutine = StartCoroutine(ShowTemporaryText(penaltyText, 1.5f));
        }

        public void UpdateSeedDisplay(int seed)
        {
            seedText.text = $"Seed: {seed}";
        }

        #endregion

        #region UI Helpers

        private Text CreateText(string name, Vector2 position, int fontSize, TextAnchor anchor,
            FontStyle style, Color color)
        {
            return CreateTextOnParent(canvas.transform, name, position, fontSize, anchor, style, color);
        }

        private Text CreateTextOnParent(Transform parent, string name, Vector2 position, int fontSize,
            TextAnchor anchor, FontStyle style, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(800, fontSize + 20);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private IEnumerator ShowTemporaryText(Text text, float duration)
        {
            text.gameObject.SetActive(true);
            Color startColor = text.color;
            startColor.a = 1f;
            text.color = startColor;

            yield return new WaitForSeconds(duration * 0.6f);

            float elapsed = 0f;
            float fadeDuration = duration * 0.4f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            text.gameObject.SetActive(false);
        }

        private IEnumerator FadeOutText(Text text, float duration)
        {
            yield return new WaitForSeconds(0.3f);

            Color startColor = text.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / duration);
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                float scale = 1f + elapsed / duration * 0.5f;
                text.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            text.gameObject.SetActive(false);
            text.transform.localScale = Vector3.one;
            text.color = startColor;
        }

        private IEnumerator PulseText(Text text)
        {
            text.transform.localScale = Vector3.one * 1.5f;
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.3f;
                text.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
                yield return null;
            }
            text.transform.localScale = Vector3.one;
        }

        #endregion
    }
}
