using UnityEngine;
using TowerDefense.Systems;

namespace TowerDefense.Core
{
    /// <summary>
    /// 游戏主管理器。负责全局状态机、游戏配置、系统协调。
    /// 作为单例存在，其他系统通过它访问全局配置与状态。
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("游戏配置")]
        [Tooltip("初始金币")]
        [SerializeField] private int _startingGold = 300;

        [Tooltip("初始生命值")]
        [SerializeField] private int _startingLives = 10;

        [Tooltip("总波次数")]
        [SerializeField] private int _totalWaves = 15;

        [Tooltip("波次间隙时间（秒）")]
        [SerializeField] private float _waveInterval = 5f;

        // 运行时状态
        private GameState _currentState = GameState.Initializing;
        private GameState _previousStateBeforePause;
        private int _currentGold;
        private int _currentLives;
        private int _currentWave = 0;
        private float _timeScaleBeforePause = 1f;

        // 子系统引用
        public ObjectPool ObjectPool { get; private set; }
        public EconomySystem Economy { get; private set; }
        public WaveSystem WaveSystem { get; private set; }

        // 公共访问属性
        public GameState CurrentState => _currentState;
        public int CurrentGold => _currentGold;
        public int CurrentLives => _currentLives;
        public int CurrentWave => _currentWave;
        public int TotalWaves => _totalWaves;

        public void SetTotalWaves(int count) => _totalWaves = count;
        public float WaveInterval => _waveInterval;
        public int StartingGold => _startingGold;
        public int StartingLives => _startingLives;

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            _currentGold = _startingGold;
            _currentLives = _startingLives;
        }

        /// <summary>
        /// 注册子系统引用（由启动器在初始化时调用）。
        /// </summary>
        public void RegisterSystems(ObjectPool pool, EconomySystem economy, WaveSystem waveSystem)
        {
            ObjectPool = pool;
            Economy = economy;
            WaveSystem = waveSystem;
        }

        /// <summary>
        /// 切换游戏状态。
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            var previous = _currentState;
            _currentState = newState;

            EventBus.Publish(new GameStateChangedEvent { NewState = newState, PreviousState = previous });

            // 状态进入逻辑
            switch (newState)
            {
                case GameState.Preparation:
                    // 仅在首次准备阶段重置为1倍速，后续不强制重置（保留用户倍速选择）
                    if (previous == GameState.Initializing)
                    {
                        Time.timeScale = 1f;
                    }
                    break;
                case GameState.InWave:
                    // 不重置timeScale，保留用户选择的倍速
                    break;
                case GameState.BetweenWaves:
                    break;
                case GameState.Victory:
                    Time.timeScale = 0f;
                    EventBus.Publish(new GameVictoryEvent());
                    break;
                case GameState.Defeat:
                    Time.timeScale = 0f;
                    EventBus.Publish(new GameDefeatEvent());
                    break;
                case GameState.Paused:
                    _previousStateBeforePause = previous;
                    _timeScaleBeforePause = Time.timeScale > 0 ? Time.timeScale : 1f;
                    Time.timeScale = 0f;
                    break;
            }
        }

        /// <summary>
        /// 从暂停恢复。
        /// </summary>
        public void ResumeFromPause()
        {
            if (_currentState == GameState.Paused)
            {
                var stateToRestore = _previousStateBeforePause;
                ChangeState(stateToRestore);
                // 恢复暂停前的倍速（ChangeState不会修改timeScale，所以这里手动还原）
                Time.timeScale = _timeScaleBeforePause;
            }
        }

        /// <summary>
        /// 切换暂停状态。
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Paused)
            {
                ResumeFromPause();
            }
            else if (_currentState == GameState.Preparation ||
                     _currentState == GameState.InWave ||
                     _currentState == GameState.BetweenWaves)
            {
                ChangeState(GameState.Paused);
            }
        }

        /// <summary>
        /// 添加金币。
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _currentGold += amount;
            EventBus.Publish(new GoldChangedEvent { CurrentGold = _currentGold, Delta = amount });
        }

        /// <summary>
        /// 尝试花费金币。余额不足返回 false。
        /// </summary>
        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || _currentGold < amount) return false;
            _currentGold -= amount;
            EventBus.Publish(new GoldChangedEvent { CurrentGold = _currentGold, Delta = -amount });
            return true;
        }

        /// <summary>
        /// 扣除生命值。
        /// </summary>
        public void LoseLives(int amount)
        {
            if (amount <= 0) return;
            _currentLives = Mathf.Max(0, _currentLives - amount);
            EventBus.Publish(new LivesChangedEvent { CurrentLives = _currentLives, Delta = -amount });

            if (_currentLives <= 0)
            {
                ChangeState(GameState.Defeat);
            }
        }

        /// <summary>
        /// 进入下一波。
        /// </summary>
        public void AdvanceWave()
        {
            _currentWave++;
            if (_currentWave > _totalWaves)
            {
                ChangeState(GameState.Victory);
            }
        }

        /// <summary>
        /// 重置游戏到初始状态（用于重新开始）。
        /// 注意：不调用 EventBus.ClearAll()，因为UI和系统组件仍在场景中订阅着事件。
        /// </summary>
        public void ResetGame()
        {
            _currentGold = _startingGold;
            _currentLives = _startingLives;
            _currentWave = 0;
            ChangeState(GameState.Preparation);

            EventBus.Publish(new GoldChangedEvent { CurrentGold = _currentGold, Delta = 0 });
            EventBus.Publish(new LivesChangedEvent { CurrentLives = _currentLives, Delta = 0 });
        }
    }
}

