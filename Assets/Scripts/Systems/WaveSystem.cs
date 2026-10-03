using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 波次配置。每波包含多个敌人生成组。
    /// </summary>
    [System.Serializable]
    public class WaveConfig
    {
        [Tooltip("波次编号")]
        public int WaveNumber;

        [Tooltip("该波的敌人生成组列表")]
        public List<EnemySpawnGroup> SpawnGroups = new List<EnemySpawnGroup>();

        [Tooltip("波次开始前的准备时间（秒）")]
        public float PreparationTime = 5f;
    }

    /// <summary>
    /// 敌人生成组。一组同类型敌人按间隔生成。
    /// </summary>
    [System.Serializable]
    public class EnemySpawnGroup
    {
        [Tooltip("敌人数据")]
        public EnemyData EnemyData;

        [Tooltip("生成数量")]
        [Min(1)] public int Count = 5;

        [Tooltip("生成间隔（秒）")]
        [Range(0.1f, 5f)] public float SpawnInterval = 1f;

        [Tooltip("该组开始前的延迟（秒）")]
        public float StartDelay = 0f;
    }

    /// <summary>
    /// 波次系统。管理波次配置、敌人生成、波次进度。
    /// 支持自动波次和手动开始波次。
    /// </summary>
    public class WaveSystem : MonoBehaviour
    {
        [Header("波次设置")]
        [Tooltip("波次配置列表")]
        [SerializeField] private List<WaveConfig> _waves = new List<WaveConfig>();

        [Tooltip("是否自动开始下一波")]
        [SerializeField] private bool _autoStartNextWave = true;

        [Tooltip("路径点")]
        [SerializeField] private Transform[] _pathPoints;

        // 运行时状态
        private int _currentWaveIndex = 0;
        private int _enemiesRemainingInWave = 0;
        private int _totalEnemiesInWave = 0;
        private bool _isWaveActive = false;
        private Coroutine _waveCoroutine;
        private float _waveCountdown = 0f;
        public float WaveCountdown => _waveCountdown;
        private bool _countdownActive = false;

        public int CurrentWaveNumber => _currentWaveIndex + 1;
        public int TotalWaves => _waves.Count;
        public bool IsWaveActive => _isWaveActive;
        public int EnemiesRemaining => _enemiesRemainingInWave;
        public IReadOnlyList<WaveConfig> Waves => _waves;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<EnemyReachedEndEvent>(OnEnemyReachedEnd);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<EnemyReachedEndEvent>(OnEnemyReachedEnd);
        }

        /// <summary>
        /// 设置路径点。
        /// </summary>
        public void SetPath(Transform[] pathPoints)
        {
            _pathPoints = pathPoints;
        }

        /// <summary>
        /// 设置波次配置（由启动器程序化生成）。
        /// </summary>
        public void SetWaves(List<WaveConfig> waves)
        {
            _waves = waves;
        }

        /// <summary>
        /// 游戏开始时自动倒计时并开始第一波。
        /// </summary>
        public void StartFirstWaveCountdown()
        {
            _waveCountdown = GameManager.Instance.WaveInterval;
            _countdownActive = true;
        }

        /// <summary>
        /// 开始下一波。
        /// </summary>
        public void StartNextWave()
        {
            if (_isWaveActive)
            {
                Debug.LogWarning("[WaveSystem] 当前波次尚未结束，无法开始下一波。");
                return;
            }

            if (_currentWaveIndex >= _waves.Count)
            {
                return;
            }

            StartWave(_currentWaveIndex);
        }

        /// <summary>
        /// 开始指定波次。
        /// </summary>
        private void StartWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _waves.Count) return;

            _currentWaveIndex = waveIndex;
            var wave = _waves[waveIndex];

            // 统计总敌人数
            _totalEnemiesInWave = 0;
            foreach (var group in wave.SpawnGroups)
            {
                _totalEnemiesInWave += group.Count;
            }
            _enemiesRemainingInWave = _totalEnemiesInWave;
            _isWaveActive = true;

            GameManager.Instance.AdvanceWave();
            GameManager.Instance.ChangeState(GameState.InWave);

            EventBus.Publish(new WaveStartedEvent
            {
                WaveNumber = wave.WaveNumber,
                TotalEnemies = _totalEnemiesInWave
            });


            _waveCoroutine = StartCoroutine(SpawnWaveCoroutine(wave));
        }

        /// <summary>
        /// 波次敌人生成协程。
        /// </summary>
        private IEnumerator SpawnWaveCoroutine(WaveConfig wave)
        {
            foreach (var group in wave.SpawnGroups)
            {
                if (group.StartDelay > 0)
                {
                    yield return new WaitForSeconds(group.StartDelay);
                }

                for (int i = 0; i < group.Count; i++)
                {
                    if (group.EnemyData != null)
                    {
                        EnemySpawner.Instance.SpawnEnemy(group.EnemyData, _pathPoints);
                    }
                    yield return new WaitForSeconds(group.SpawnInterval);
                }
            }

            // 所有敌人已生成完毕，等待全部被消灭或到达终点
            // （通过EnemyKilled/EnemyReachedEnd事件计数，在CheckWaveComplete中处理）
        }

        /// <summary>
        /// 敌人被击杀回调。
        /// </summary>
        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!_isWaveActive) return;
            _enemiesRemainingInWave--;
            EventBus.Publish(new EnemiesRemainingChangedEvent { Remaining = _enemiesRemainingInWave });
            CheckWaveComplete();
        }

        /// <summary>
        /// 敌人到达终点回调。
        /// </summary>
        private void OnEnemyReachedEnd(EnemyReachedEndEvent evt)
        {
            if (!_isWaveActive) return;
            _enemiesRemainingInWave--;
            // 敌人到达终点，扣除玩家生命值
            GameManager.Instance.LoseLives(evt.Damage);
            EventBus.Publish(new EnemiesRemainingChangedEvent { Remaining = _enemiesRemainingInWave });
            CheckWaveComplete();
        }

        /// <summary>
        /// 检查波次是否完成。
        /// </summary>
        private void CheckWaveComplete()
        {
            if (_enemiesRemainingInWave <= 0 && _isWaveActive)
            {
                CompleteWave();
            }
        }

        /// <summary>
        /// 完成当前波次。
        /// </summary>
        private void CompleteWave()
        {
            _isWaveActive = false;
            var wave = _waves[_currentWaveIndex];

            EventBus.Publish(new WaveCompletedEvent { WaveNumber = wave.WaveNumber });

            _currentWaveIndex++;

            if (_currentWaveIndex >= _waves.Count)
            {
                // 所有波次完成
                GameManager.Instance.ChangeState(GameState.Victory);
            }
            else
            {
                GameManager.Instance.ChangeState(GameState.BetweenWaves);

                if (_autoStartNextWave)
                {
                    _waveCountdown = GameManager.Instance.WaveInterval;
                    _countdownActive = true;
            }
        }
        }

        private void Update()
        {
            if (_countdownActive)
            {
                _waveCountdown -= Time.deltaTime;
                if (_waveCountdown <= 0)
                {
                    _waveCountdown = 0;
                    _countdownActive = false;
                    StartNextWave();
                }
            }
        }
        /// <summary>
        /// 跳过波次间隙，立即开始下一波（给予奖励金币）。
        /// </summary>
        public void SkipToNextWave()
        {
            if (_isWaveActive) return;
            if (_currentWaveIndex >= _waves.Count) return;

            // 提前开始波次奖励
            int bonus = 20 + _currentWaveIndex * 5;
            GameManager.Instance.AddGold(bonus);

            StartNextWave();
        }

        /// <summary>
        /// 重置波次系统。
        /// </summary>
        public void ResetWaves()
        {
            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
            }
            _currentWaveIndex = 0;
            _enemiesRemainingInWave = 0;
            _isWaveActive = false;
        }
    }
}


