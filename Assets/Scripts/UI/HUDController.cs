using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.UI
{
    /// <summary>
    /// HUD控制器。显示金币、生命值、波次信息、开始波次按钮、暂停按钮。
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private Text _goldText;
        private Text _livesText;
        private Text _waveText;
        private Text _enemyCountText;
        private Button _pauseButton;
        private Button _speedButton;
        private Text _countdownText;
        private Text _speedButtonText;
        private Text _pauseButtonText;
        private Text _fpsText;
        private Image _waveProgressFill;

        private int _currentSpeedIndex = 0;
        private readonly float[] _speedOptions = { 1f, 2f, 3f };

        private float _fpsTimer;
        private int _fpsFrameCount;

        public void Initialize()
        {
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 顶部信息栏背景（半透明木牌风格）
            UIManager.CreatePanel(transform, "TopBar", new Color(0.12f, 0.08f, 0.04f, 0.85f),
                new Vector2(0, 85), new Vector2(0, 0),
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1));
            // 底部金色装饰线
            UIManager.CreatePanel(transform, "TopBarLine", new Color(0.85f, 0.65f, 0.2f, 0.7f),
                new Vector2(0, 3), new Vector2(0, -42),
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1));

            // ===== 左侧：金币标签 =====
            _goldText = CreateResourceLabel("GoldLabel", new Vector2(40, -50), "金币", "200",
                new Color(1f, 0.82f, 0.15f, 0.9f), new Color(0.4f, 0.25f, 0f, 1f), new Color(0.4f, 0.25f, 0f, 1f));

            // ===== 生命标签 =====
            _livesText = CreateResourceLabel("LivesLabel", new Vector2(230, -48), "生命", "20",
                new Color(0.95f, 0.3f, 0.3f, 0.9f), Color.white, Color.white);
            // ===== 中间：波次徽章 =====
            var waveBadgeGo = new GameObject("WaveBadge", typeof(RectTransform));
            waveBadgeGo.transform.SetParent(transform, false);
            var waveBadgeImg = waveBadgeGo.AddComponent<Image>();
            waveBadgeImg.color = new Color(0.2f, 0.15f, 0.1f, 0.9f);
            waveBadgeImg.sprite = GenerateRoundedSprite(64, 20, new Color(0.2f, 0.15f, 0.1f, 1f));
            var waveBadgeRect = waveBadgeGo.GetComponent<RectTransform>();
            waveBadgeRect.anchorMin = new Vector2(0.5f, 1);
            waveBadgeRect.anchorMax = new Vector2(0.5f, 1);
            waveBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            waveBadgeRect.sizeDelta = new Vector2(240, 76);
            waveBadgeRect.anchoredPosition = new Vector2(0, -50);

            _waveText = UIManager.CreateText(waveBadgeGo.transform, "WaveText", "波次 0 / 10", 32,
                TextAnchor.MiddleCenter, new Vector2(220, 36), new Vector2(0, 14),
                new Color(1f, 0.9f, 0.5f));
            _enemyCountText = UIManager.CreateText(waveBadgeGo.transform, "EnemyCount", "", 20,
                TextAnchor.MiddleCenter, new Vector2(220, 22), new Vector2(0, -18),
                new Color(0.85f, 0.8f, 0.7f));

            // 波次进度条
            var progressBgGo = new GameObject("ProgressBg", typeof(RectTransform));
            progressBgGo.transform.SetParent(waveBadgeGo.transform, false);
            var progressBgImg = progressBgGo.AddComponent<Image>();
            progressBgImg.color = new Color(0.1f, 0.08f, 0.05f, 0.8f);
            var progressBgRect = progressBgGo.GetComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.5f, 0);
            progressBgRect.anchorMax = new Vector2(0.5f, 0);
            progressBgRect.pivot = new Vector2(0.5f, 0);
            progressBgRect.sizeDelta = new Vector2(140, 6);
            progressBgRect.anchoredPosition = new Vector2(0, 4);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(progressBgGo.transform, false);
            _waveProgressFill = fillGo.AddComponent<Image>();
            _waveProgressFill.color = new Color(0.9f, 0.6f, 0.2f, 1f);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.sizeDelta = new Vector2(0, 0);
            fillRect.anchoredPosition = Vector2.zero;

            // ===== 右侧：控制按钮 =====
            // 波次倒计时文本（替代开始波次按钮）
            var countdownBg = new GameObject("CountdownBg", typeof(RectTransform));
            countdownBg.transform.SetParent(transform, false);
            var countdownBgImg = countdownBg.AddComponent<Image>();
            countdownBgImg.color = new Color(0.15f, 0.12f, 0.08f, 0.9f);
            var countdownBgRect = countdownBg.GetComponent<RectTransform>();
            countdownBgRect.anchorMin = new Vector2(0.5f, 1);
            countdownBgRect.anchorMax = new Vector2(0.5f, 1);
            countdownBgRect.pivot = new Vector2(0.5f, 1);
            countdownBgRect.sizeDelta = new Vector2(200, 40);
            countdownBgRect.anchoredPosition = new Vector2(0, -95);
            _countdownText = UIManager.CreateText(countdownBg.transform, "Countdown", "下一波: 5.0s", 22,
                TextAnchor.MiddleCenter, new Vector2(190, 36), Vector2.zero,
                new Color(1f, 0.9f, 0.4f));
            // 速度按钮
            _speedButton = CreateRoundButton("SpeedButton", "1x", 80, 66,
                new Vector2(-250, -50), new Color(0.35f, 0.55f, 0.8f, 1f), new Color(0.2f, 0.35f, 0.6f, 1f));
            _speedButton.onClick.AddListener(OnSpeedClicked);
            _speedButtonText = _speedButton.GetComponentInChildren<Text>();

            // 菜单按钮
            var menuButton = CreateRoundButton("MenuButton", "菜单", 90, 66,
                new Vector2(-150, -50), new Color(0.4f, 0.4f, 0.6f, 1f), new Color(0.25f, 0.25f, 0.4f, 1f));
            menuButton.onClick.AddListener(OnMenuClicked);

            // FPS显示（右上角）
            _fpsText = UIManager.CreateText(transform, "FPSText", "FPS: 60", 20,
                TextAnchor.MiddleRight, new Vector2(120, 30), new Vector2(-30, -30),
                new Color(0.7f, 1f, 0.7f, 0.8f));
            var fpsRect = _fpsText.GetComponent<RectTransform>();
            fpsRect.anchorMin = new Vector2(1, 1);
            fpsRect.anchorMax = new Vector2(1, 1);
            fpsRect.pivot = new Vector2(1, 1);

            // 订阅事件
            EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            EventBus.Subscribe<LivesChangedEvent>(OnLivesChanged);
            EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<EnemiesRemainingChangedEvent>(OnEnemiesRemainingChanged);
        }

        /// <summary>
        /// 创建圆形徽章（金币/生命风格）。
        /// </summary>
        private void CreateBadge(string name, Vector2 pos, float size, Color innerColor, Color outerColor,
            string text, Color textColor, out Text textComp)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            // 外圈
            var outerGo = new GameObject("Outer", typeof(RectTransform));
            outerGo.transform.SetParent(go.transform, false);
            var outerImg = outerGo.AddComponent<Image>();
            outerImg.color = outerColor;
            outerImg.sprite = GenerateCircleSprite(64, Color.white);
            var outerRect = outerGo.GetComponent<RectTransform>();
            outerRect.anchorMin = new Vector2(0, 0.5f);
            outerRect.anchorMax = new Vector2(0, 0.5f);
            outerRect.pivot = new Vector2(0, 0.5f);
            outerRect.sizeDelta = new Vector2(size, size);
            outerRect.anchoredPosition = Vector2.zero;

            // 内圈
            var innerGo = new GameObject("Inner", typeof(RectTransform));
            innerGo.transform.SetParent(go.transform, false);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = innerColor;
            innerImg.sprite = GenerateCircleSprite(64, Color.white);
            var innerRect = innerGo.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0, 0.5f);
            innerRect.anchorMax = new Vector2(0, 0.5f);
            innerRect.pivot = new Vector2(0, 0.5f);
            innerRect.sizeDelta = new Vector2(size - 10, size - 10);
            innerRect.anchoredPosition = new Vector2(5, 0);

            // 文字（在徽章右侧）
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            textComp = textGo.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.alignment = TextAnchor.MiddleLeft;
            textComp.color = textColor;
            textComp.fontSize = 32;
            textComp.fontStyle = FontStyle.Bold;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.5f);
            textRect.anchorMax = new Vector2(0, 0.5f);
            textRect.pivot = new Vector2(0, 0.5f);
            textRect.sizeDelta = new Vector2(100, 50);
            textRect.anchoredPosition = new Vector2(size + 10, 0);

            // 徽章整体位置
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(size + 90, size);
            rect.anchoredPosition = pos;
        }


        /// <summary>
        /// 创建资源标签（金币/生命文字样式）。
        /// </summary>
        private Text CreateResourceLabel(string name, Vector2 pos, string label, string value,
            Color bgColor, Color labelColor, Color valueColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            // 圆角背景
            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = GenerateRoundedSprite(64, 20, bgColor);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.sizeDelta = new Vector2(150, 50);
            bgRect.anchoredPosition = Vector2.zero;

            // 标签文字
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelText = labelGo.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = labelColor;
            labelText.fontSize = 20;
            labelText.fontStyle = FontStyle.Bold;
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.sizeDelta = new Vector2(50, 50);
            labelRect.anchoredPosition = new Vector2(8, 0);

            // 数值文字（动态更新）
            var valueGo = new GameObject("Value", typeof(RectTransform));
            valueGo.transform.SetParent(go.transform, false);
            var valueText = valueGo.AddComponent<Text>();
            valueText.text = value;
            valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.color = valueColor;
            valueText.fontSize = 26;
            valueText.fontStyle = FontStyle.Bold;
            var valueRect = valueGo.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0.5f);
            valueRect.anchorMax = new Vector2(0, 0.5f);
            valueRect.pivot = new Vector2(0, 0.5f);
            valueRect.sizeDelta = new Vector2(80, 50);
            valueRect.anchoredPosition = new Vector2(58, 0);

            // 整体位置
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(150, 50);
            rect.anchoredPosition = pos;

            return valueText;
        }
        /// <summary>
        /// 创建圆角按钮（保卫萝卜风格）。
        /// </summary>
        private Button CreateRoundButton(string name, string text, float width, float height,
            Vector2 anchoredPos, Color topColor, Color bottomColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = anchoredPos;

            // 按钮主体（圆角）
            var image = go.AddComponent<Image>();
            image.color = topColor;
            image.sprite = GenerateRoundedSprite(64, 16, topColor);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
            button.targetGraphic = image;

            // 文字
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textComp = textGo.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            textComp.fontSize = 28;
            textComp.fontStyle = FontStyle.Bold;

            var textRect = textComp.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>
        /// 生成圆形Sprite（带抗锯齿）。
        /// </summary>
        private static Sprite GenerateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - dist) * 2.5f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// 生成圆角矩形Sprite。
        /// </summary>
        private static Sprite GenerateRoundedSprite(int size, int radius, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 简单圆角算法：四角用圆形裁剪
                    bool inRect = x >= radius && x < size - radius || y >= radius && y < size - radius;
                    bool inCorner = false;

                    if (!inRect)
                    {
                        Vector2 cornerCenter;
                        if (x < radius && y < radius)
                            cornerCenter = new Vector2(radius, radius);
                        else if (x >= size - radius && y < radius)
                            cornerCenter = new Vector2(size - radius - 1, radius);
                        else if (x < radius && y >= size - radius)
                            cornerCenter = new Vector2(radius, size - radius - 1);
                        else
                            cornerCenter = new Vector2(size - radius - 1, size - radius - 1);

                        float dist = Vector2.Distance(new Vector2(x, y), cornerCenter);
                        inCorner = dist <= radius;
                    }

                    pixels[y * size + x] = (inRect || inCorner) ? color : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            EventBus.Unsubscribe<LivesChangedEvent>(OnLivesChanged);
            EventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<EnemiesRemainingChangedEvent>(OnEnemiesRemainingChanged);
        }

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            _goldText.text = evt.CurrentGold.ToString();
        }

        private void OnLivesChanged(LivesChangedEvent evt)
        {
            _livesText.text = evt.CurrentLives.ToString();
            // 生命低时变红闪烁效果
            if (evt.CurrentLives <= 5)
            {
                _livesText.color = new Color(1f, 0.3f, 0.3f);
            }
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            _waveText.text = $"波次 {evt.WaveNumber} / {GameManager.Instance.TotalWaves}";
            _enemyCountText.text = $"剩余敌人: {evt.TotalEnemies}";
            _countdownText.text = "战斗中...";
            UpdateWaveProgress(evt.WaveNumber);
        }

        public void RefreshWaveDisplay(int currentWave, int totalWaves)
        {
            _waveText.text = $"波次 {currentWave} / {totalWaves}";
            _enemyCountText.text = "准备战斗";
            _countdownText.text = "准备中...";
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            _enemyCountText.text = "波次完成！";
            if (evt.WaveNumber < GameManager.Instance.TotalWaves)
            {
                // 倒计时自动更新
            }
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case GameState.Preparation: _countdownText.text = "准备中..."; break;
                case GameState.BetweenWaves: break;
                case GameState.InWave: _countdownText.text = "战斗中..."; break;
                case GameState.Paused:
                    if (_pauseButtonText != null) _pauseButtonText.text = "继续";
                    break;
                default:
                    if (_pauseButtonText != null) _pauseButtonText.text = "暂停";
                    break;
            }
        }

        /// <summary>
        /// 更新波次进度条。
        /// </summary>
        private void UpdateWaveProgress(int currentWave)
        {
            if (_waveProgressFill == null) return;
            float progress = (float)currentWave / GameManager.Instance.TotalWaves;
            var rect = _waveProgressFill.GetComponent<RectTransform>();
            rect.anchorMax = new Vector2(progress, 1);
        }

        private void Update()
        {
            // 更新波次倒计时
            if (_countdownText != null && GameManager.Instance != null)
            {
                var ws = GameManager.Instance.WaveSystem;
                if (ws != null && ws.WaveCountdown > 0)
                {
                    _countdownText.text = $"下一波: {ws.WaveCountdown:F1}s";
                }
            }

            // 更新FPS（每0.5秒刷新一次）
            _fpsFrameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                float fps = _fpsFrameCount / _fpsTimer;
                if (_fpsText != null) _fpsText.text = $"FPS: {fps:F0}";
                _fpsFrameCount = 0;
                _fpsTimer = 0f;
            }
        }
        private void OnPauseClicked()
        {
            GameManager.Instance.TogglePause();
        }

        /// <summary>重置倍速为1x（重新开始关卡时调用）。</summary>
        public void ResetSpeed()
        {
            _currentSpeedIndex = 0;
            Time.timeScale = 1f;
            if (_speedButtonText != null) _speedButtonText.text = "1x";
        }

        private void OnSpeedClicked()
        {
            _currentSpeedIndex = (_currentSpeedIndex + 1) % _speedOptions.Length;
            float speed = _speedOptions[_currentSpeedIndex];
            Time.timeScale = speed;
            _speedButtonText.text = $"{speed}x";
        }

        private void OnMenuClicked()
        {
            // 显示暂停菜单
            UIManager.Instance.PauseMenu.Show();
        }

        /// <summary>
        /// 更新剩余敌人数显示。
        /// </summary>
        public void UpdateEnemyCount(int count)
        {
            if (count > 0)
            {
                _enemyCountText.text = $"剩余敌人: {count}";
            }
        }

        private void OnEnemiesRemainingChanged(EnemiesRemainingChangedEvent evt)
        {
            _enemyCountText.text = evt.Remaining > 0 ? $"剩余敌人: {evt.Remaining}" : "波次完成！";
        }
    }
}












