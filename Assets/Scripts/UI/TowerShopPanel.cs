using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;
using TowerDefense.Systems;
using TowerDefense.Towers;

namespace TowerDefense.UI
{
    /// <summary>
    /// 塔商店面板（保卫萝卜风格：底部横排大圆图标按钮，选中高亮发光）。
    /// </summary>
    public class TowerShopPanel : MonoBehaviour
    {
        private List<TowerData> _availableTowers = new List<TowerData>();
        private List<GameObject> _towerButtons = new List<GameObject>();
        private RectTransform _buttonContainer;
        private RectTransform _panelRect;
        private TowerData _selectedTower;
        private GameObject _selectedHighlight;
        private Text _descText;

        public void Initialize()
        {
            _panelRect = GetComponent<RectTransform>();
            // 右侧竖条
            _panelRect.anchorMin = new Vector2(1f, 0.5f);
            _panelRect.anchorMax = new Vector2(1f, 0.5f);
            _panelRect.pivot = new Vector2(1f, 0.5f);
            _panelRect.sizeDelta = new Vector2(175, 780);
            _panelRect.anchoredPosition = new Vector2(-10, 0);

            // 面板背景（圆角感的深色条）
            var bg = UIManager.CreatePanel(transform, "Background", new Color(0.08f, 0.06f, 0.04f, 0.92f),
                Vector2.zero, Vector2.zero,
                anchorMin: Vector2.zero, anchorMax: Vector2.one);

            // 顶部高光边
            UIManager.CreatePanel(transform, "TopHighlight", new Color(0.6f, 0.45f, 0.25f, 0.6f),
                new Vector2(0, 3), new Vector2(0, 0),
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1));

            // 塔描述（商店上方悬浮显示）
            _descText = UIManager.CreateText(transform.parent, "TowerDesc", "", 22,
                TextAnchor.UpperCenter, new Vector2(600, 80), new Vector2(0, -180),
                new Color(1f, 0.95f, 0.8f));

            // 按钮容器
            var containerGo = new GameObject("ButtonContainer", typeof(RectTransform));
            containerGo.transform.SetParent(transform, false);
            _buttonContainer = containerGo.GetComponent<RectTransform>();
            _buttonContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _buttonContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _buttonContainer.pivot = new Vector2(0.5f, 0.5f);
            _buttonContainer.sizeDelta = Vector2.zero;
            _buttonContainer.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 设置可建造的塔列表。
        /// </summary>
        public void SetAvailableTowers(List<TowerData> towers)
        {
            _availableTowers = towers;
            RebuildButtons();
        }

        /// <summary>
        /// 重建塔按钮列表（底部水平排列）。
        /// </summary>
        private void RebuildButtons()
        {
            foreach (var btn in _towerButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _towerButtons.Clear();

            float buttonSize = 125f;
            float spacing = 22f;
            float totalHeight = _availableTowers.Count * buttonSize + (_availableTowers.Count - 1) * spacing;
            float startY = totalHeight / 2f - buttonSize / 2f;

            for (int i = 0; i < _availableTowers.Count; i++)
            {
                var towerData = _availableTowers[i];
                float yPos = startY - i * (buttonSize + spacing);

                var button = CreateTowerButton(towerData, yPos, buttonSize);
                _towerButtons.Add(button);
            }

        }
        /// <summary>
        /// 创建单个圆形塔按钮。
        /// </summary>
        private GameObject CreateTowerButton(TowerData data, float yPos, float size)
        {
            var go = new GameObject($"TowerButton_{data.Type}", typeof(RectTransform));
            go.transform.SetParent(_buttonContainer, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size + 70); // 圆形+名称+花费
            rect.anchoredPosition = new Vector2(0, yPos);

            // 选中高亮环（默认隐藏）
            var highlightGo = new GameObject("Highlight", typeof(RectTransform));
            highlightGo.transform.SetParent(go.transform, false);
            var highlightImg = highlightGo.AddComponent<Image>();
            highlightImg.color = new Color(1f, 0.9f, 0.3f, 0f);
            highlightImg.sprite = GenerateCircleSprite(64, new Color(1f, 0.9f, 0.3f, 1f));
            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            highlightRect.pivot = new Vector2(0.5f, 0.5f);
            highlightRect.sizeDelta = new Vector2(size + 16, size + 16);
            highlightRect.anchoredPosition = new Vector2(0, 32);
            highlightGo.SetActive(false);

            // 外圈底座（深色圆环）
            var baseGo = new GameObject("Base", typeof(RectTransform));
            baseGo.transform.SetParent(go.transform, false);
            var baseImg = baseGo.AddComponent<Image>();
            baseImg.color = new Color(0.2f, 0.15f, 0.1f, 1f);
            baseImg.sprite = GenerateCircleSprite(64, Color.white);
            var baseRect = baseGo.GetComponent<RectTransform>();
            baseRect.anchorMin = new Vector2(0.5f, 0.5f);
            baseRect.anchorMax = new Vector2(0.5f, 0.5f);
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = new Vector2(size, size);
            baseRect.anchoredPosition = new Vector2(0, 32);

            // 内圈（塔模型精灵，与实际塔一致）
            var innerGo = new GameObject("Inner", typeof(RectTransform));
            innerGo.transform.SetParent(go.transform, false);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = Color.white;
            innerImg.sprite = TowerVisualFactory.GetTowerSprite(data.Type);
            innerImg.preserveAspect = true;
            var innerRect = innerGo.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.pivot = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(size - 8, size - 8);
            innerRect.anchoredPosition = new Vector2(0, 32);

            // 塔名称（圆形内底部）
            // 塔名称（圆形下方，深色背景）
            var nameBgGo = new GameObject("NameBg", typeof(RectTransform));
            nameBgGo.transform.SetParent(go.transform, false);
            var nameBgImg = nameBgGo.AddComponent<Image>();
            nameBgImg.color = new Color(0.1f, 0.08f, 0.05f, 0.9f);
            var nameBgRect = nameBgGo.GetComponent<RectTransform>();
            nameBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameBgRect.pivot = new Vector2(0.5f, 0.5f);
            nameBgRect.sizeDelta = new Vector2(size - 5, 30);
            nameBgRect.anchoredPosition = new Vector2(0, -22);
            UIManager.CreateText(nameBgGo.transform, "Name", data.DisplayName, 26,
                TextAnchor.MiddleCenter, new Vector2(size - 10, 28), Vector2.zero,
                Color.white);
            // 花费标签（圆形下方）
            var costBgGo = new GameObject("CostBg", typeof(RectTransform));
            costBgGo.transform.SetParent(go.transform, false);
            var costBgImg = costBgGo.AddComponent<Image>();
            costBgImg.color = new Color(0.15f, 0.12f, 0.08f, 0.95f);
            var costBgRect = costBgGo.GetComponent<RectTransform>();
            costBgRect.anchorMin = new Vector2(0.5f, 0);
            costBgRect.anchorMax = new Vector2(0.5f, 0);
            costBgRect.pivot = new Vector2(0.5f, 0);
            costBgRect.sizeDelta = new Vector2(85, 30);
            costBgRect.anchoredPosition = new Vector2(0, -52);

            var costText = UIManager.CreateText(costBgGo.transform, "Cost", $"{data.BuildCost}", 28,
                TextAnchor.MiddleCenter, new Vector2(75, 26), new Vector2(5, 0),
                new Color(1f, 0.85f, 0.3f));

            // 按钮组件
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            button.colors = colors;
            // 按钮的targetGraphic设为内圈，这样点击效果作用在圆上
            button.targetGraphic = innerImg;

            // 点击事件
            TowerData capturedData = data;
            GameObject capturedBtn = go;
            button.onClick.AddListener(() => OnTowerButtonClicked(capturedData, capturedBtn));

            // 金币变化更新
            var updater = go.AddComponent<TowerButtonGoldUpdater>();
            updater.Setup(button, data.BuildCost, innerImg, baseImg);

            return go;
        }

        private void OnTowerButtonClicked(TowerData data, GameObject buttonGo)
        {
            if (GameManager.Instance.CurrentGold < data.BuildCost)
            {
                return;
            }

            // 切换选中状态
            if (_selectedTower == data)
            {
                // 再次点击取消
                TowerPlacer.Instance.CancelPlacing();
                _selectedTower = null;
                SetHighlight(buttonGo, false);
                HideDescription();
            }
            else
            {
                // 取消之前的选中
                ClearAllHighlights();
                _selectedTower = data;
                SetHighlight(buttonGo, true);
                ShowDescription(data);
                TowerPlacer.Instance.StartPlacing(data);
            }
        }

        private void SetHighlight(GameObject buttonGo, bool show)
        {
            var highlight = buttonGo.transform.Find("Highlight");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(show);
                if (show)
                {
                    var img = highlight.GetComponent<Image>();
                    img.color = new Color(1f, 0.9f, 0.3f, 0.7f);
                }
            }
            // 选中时按钮放大
            var rect = buttonGo.GetComponent<RectTransform>();
            rect.localScale = show ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
        }

        private void ClearAllHighlights()
        {
            foreach (var btn in _towerButtons)
            {
                if (btn != null) SetHighlight(btn, false);
            }
        }

        /// <summary>
        /// 当放置完成或取消时，清除选中状态（由外部调用）。
        /// </summary>
        public void ClearSelection()
        {
            _selectedTower = null;
            ClearAllHighlights();
            HideDescription();
        }

        private void ShowDescription(TowerData data)
        {
            if (_descText != null && !string.IsNullOrEmpty(data.Description))
            {
                _descText.text = $"{data.DisplayName}：{data.Description}";
            }
        }

        private void HideDescription()
        {
            if (_descText != null)
            {
                _descText.text = "";
            }
        }

        /// <summary>
        /// 生成圆形Sprite。
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
                    // 抗锯齿边缘
                    float alpha = Mathf.Clamp01((radius - dist) * 2f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }

    /// <summary>
    /// 塔按钮金币状态更新辅助组件。
    /// </summary>
    public class TowerButtonGoldUpdater : MonoBehaviour
    {
        private Button _button;
        private Image _innerImg;
        private Image _baseImg;
        private int _cost;
        private Color _normalInnerColor;
        private Color _normalBaseColor;

        public void Setup(Button button, int cost, Image innerImg, Image baseImg)
        {
            _button = button;
            _cost = cost;
            _innerImg = innerImg;
            _baseImg = baseImg;
            _normalInnerColor = innerImg.color;
            _normalBaseColor = baseImg.color;
            EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            UpdateState();
        }

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            UpdateState();
        }

        private void UpdateState()
        {
            if (_button == null) return;
            bool canAfford = GameManager.Instance.CurrentGold >= _cost;
            _button.interactable = canAfford;
            if (_innerImg != null)
            {
                _innerImg.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            if (_baseImg != null)
            {
                _baseImg.color = canAfford ? _normalBaseColor : new Color(0.15f, 0.15f, 0.15f, 0.8f);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        }
    }
}










