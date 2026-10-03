using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 经济系统。管理金币的获取、花费、波次奖励、利息等。
    /// 核心存储在GameManager中，本系统提供扩展逻辑。
    /// </summary>
    public class EconomySystem : MonoBehaviour
    {
        [Header("经济设置")]
        [Tooltip("每波完成基础奖励")]
        [SerializeField] private int _waveCompleteBonus = 50;

        [Tooltip("每波完成奖励随波次递增")]
        [SerializeField] private int _waveBonusIncrement = 10;

        [Tooltip("击杀敌人奖励倍率")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _killRewardMultiplier = 1f;

        [Tooltip("是否启用波次利息（每波结束按当前金币百分比奖励）")]
        [SerializeField] private bool _enableInterest = false;

        [Tooltip("利息百分比（每波）")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _interestRate = 0.05f;

        [Tooltip("利息上限")]
        [SerializeField] private int _interestCap = 100;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        /// <summary>
        /// 敌人被击杀：发放击杀奖励。
        /// </summary>
        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            int reward = Mathf.RoundToInt(evt.RewardGold * _killRewardMultiplier);
            if (reward > 0)
            {
                GameManager.Instance.AddGold(reward);
            }
        }

        /// <summary>
        /// 波次完成：发放波次奖励和利息。
        /// </summary>
        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            // 波次完成奖励
            int bonus = _waveCompleteBonus + (evt.WaveNumber - 1) * _waveBonusIncrement;
            GameManager.Instance.AddGold(bonus);

            // 利息
            if (_enableInterest)
            {
                int interest = Mathf.Min(
                    Mathf.RoundToInt(GameManager.Instance.CurrentGold * _interestRate),
                    _interestCap
                );
                if (interest > 0)
                {
                    GameManager.Instance.AddGold(interest);
                }
            }
        }

        /// <summary>
        /// 检查是否能负担某个花费。
        /// </summary>
        public bool CanAfford(int cost)
        {
            return GameManager.Instance.CurrentGold >= cost;
        }

        /// <summary>
        /// 获取当前金币。
        /// </summary>
        public int GetCurrentGold()
        {
            return GameManager.Instance.CurrentGold;
        }

        /// <summary>
        /// 格式化金币显示（大数字缩写）。
        /// </summary>
        public string FormatGold(int gold)
        {
            if (gold >= 1000000)
                return $"{gold / 1000000f:F1}M";
            if (gold >= 10000)
                return $"{gold / 1000f:F1}K";
            return gold.ToString();
        }
    }
}
