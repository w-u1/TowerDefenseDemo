using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Systems;

namespace TowerDefense.Tests.EditMode
{
    /// <summary>
    /// 波次系统测试：验证倒计时结束后正确触发刷怪。
    /// </summary>
    public class WaveSystemTest
    {
        private GameManager _gameManager;
        private WaveSystem _waveSystem;
        private EnemyData _enemyData;

        [SetUp]
        public void SetUp()
        {
            // 创建GameManager
            var gmGo = new GameObject("GameManager");
            _gameManager = gmGo.AddComponent<GameManager>();

            // 创建EnemyData
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            _enemyData.MaxHealth = 100;
            _enemyData.BodyColor = Color.red;

            // 创建WaveSystem
            var wsGo = new GameObject("WaveSystem");
            _waveSystem = wsGo.AddComponent<WaveSystem>();

            // 设置波次配置
            var waves = new List<WaveConfig>
            {
                new WaveConfig
                {
                    WaveNumber = 1,
                    SpawnGroups = new List<EnemySpawnGroup>
                    {
                        new EnemySpawnGroup { EnemyData = _enemyData, Count = 3, SpawnInterval = 1f }
                    }
                },
                new WaveConfig
                {
                    WaveNumber = 2,
                    SpawnGroups = new List<EnemySpawnGroup>
                    {
                        new EnemySpawnGroup { EnemyData = _enemyData, Count = 5, SpawnInterval = 0.8f }
                    }
                }
            };
            _waveSystem.SetWaves(waves);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameManager.gameObject);
            Object.DestroyImmediate(_waveSystem.gameObject);
            Object.DestroyImmediate(_enemyData);
        }

        /// <summary>
        /// 通过反射获取私有字段值。
        /// </summary>
        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field.GetValue(obj);
        }

        /// <summary>
        /// 通过反射设置私有字段值。
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(obj, value);
        }

        /// <summary>
        /// 调用WaveSystem的Update方法。
        /// </summary>
        private void InvokeUpdate()
        {
            var updateMethod = _waveSystem.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod.Invoke(_waveSystem, null);
        }

        [Test]
        public void SetWaves_ShouldStoreWaveConfiguration()
        {
            // Assert
            Assert.AreEqual(2, _waveSystem.TotalWaves, "应有2波配置");
        }

        [Test]
        public void StartFirstWaveCountdown_ShouldSetCountdownActive()
        {
            // Act
            _waveSystem.StartFirstWaveCountdown();

            // Assert
            bool countdownActive = GetPrivateField<bool>(_waveSystem, "_countdownActive");
            Assert.IsTrue(countdownActive, "启动倒计时后_countdownActive应为true");
            Assert.AreEqual(_gameManager.WaveInterval, _waveSystem.WaveCountdown, 0.001f,
                "倒计时应初始化为WaveInterval");
        }

        [Test]
        public void Countdown_WhenReachesZero_ShouldStartWave()
        {
            // Arrange - 启动倒计时，然后手动把倒计时设为接近0
            _waveSystem.StartFirstWaveCountdown();
            SetPrivateField(_waveSystem, "_waveCountdown", 0.01f);

            // Act - 调用Update，倒计时会减到0以下，触发StartNextWave
            // 注意：EditMode下Time.deltaTime可能很小，所以我们直接设为负数模拟倒计时结束
            SetPrivateField(_waveSystem, "_waveCountdown", -0.1f);
            InvokeUpdate();

            // Assert
            bool isWaveActive = GetPrivateField<bool>(_waveSystem, "_isWaveActive");
            bool countdownActive = GetPrivateField<bool>(_waveSystem, "_countdownActive");
            Assert.IsTrue(isWaveActive, "倒计时结束后应开始波次（_isWaveActive=true）");
            Assert.IsFalse(countdownActive, "波次开始后_countdownActive应为false");
            Assert.AreEqual(0f, _waveSystem.WaveCountdown, 0.001f, "倒计时应归零");
        }

        [Test]
        public void Countdown_WhenAboveZero_ShouldNotStartWave()
        {
            // Arrange - 启动倒计时，确保倒计时大于0
            _waveSystem.StartFirstWaveCountdown();
            SetPrivateField(_waveSystem, "_waveCountdown", 3f);

            // Act - 调用Update，但倒计时仍大于0（假设deltaTime很小）
            InvokeUpdate();

            // Assert
            bool countdownActive = GetPrivateField<bool>(_waveSystem, "_countdownActive");
            Assert.IsTrue(countdownActive, "倒计时未结束时_countdownActive应为true");
        }

        [Test]
        public void StartWave_ShouldSetWaveActiveAndAdvanceIndex()
        {
            // Arrange - 直接通过反射调用StartWave
            var startWaveMethod = _waveSystem.GetType().GetMethod("StartWave", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            startWaveMethod.Invoke(_waveSystem, new object[] { 0 });

            // Assert
            bool isWaveActive = GetPrivateField<bool>(_waveSystem, "_isWaveActive");
            int currentWaveIndex = GetPrivateField<int>(_waveSystem, "_currentWaveIndex");
            int enemiesRemaining = GetPrivateField<int>(_waveSystem, "_enemiesRemainingInWave");

            Assert.IsTrue(isWaveActive, "StartWave后_isWaveActive应为true");
            Assert.AreEqual(0, currentWaveIndex, "当前波次索引应为0");
            Assert.AreEqual(3, enemiesRemaining, "第一波应有3个敌人（1组×3个）");
        }

        [Test]
        public void StartWave_SecondWave_ShouldHaveCorrectEnemyCount()
        {
            // Arrange
            var startWaveMethod = _waveSystem.GetType().GetMethod("StartWave", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act - 开始第二波
            startWaveMethod.Invoke(_waveSystem, new object[] { 1 });

            // Assert
            int enemiesRemaining = GetPrivateField<int>(_waveSystem, "_enemiesRemainingInWave");
            Assert.AreEqual(5, enemiesRemaining, "第二波应有5个敌人");
        }

        [Test]
        public void StartNextWave_WhenWaveActive_ShouldNotStart()
        {
            // Arrange - 先开始第一波
            var startWaveMethod = _waveSystem.GetType().GetMethod("StartWave", BindingFlags.NonPublic | BindingFlags.Instance);
            startWaveMethod.Invoke(_waveSystem, new object[] { 0 });

            // Act - 尝试在波次进行中开始下一波
            _waveSystem.StartNextWave();

            // Assert - 波次索引不应增加
            int currentWaveIndex = GetPrivateField<int>(_waveSystem, "_currentWaveIndex");
            Assert.AreEqual(0, currentWaveIndex, "波次进行中不应开始下一波");
        }

        [Test]
        public void TotalWaves_ShouldReturnCorrectCount()
        {
            // Assert
            Assert.AreEqual(2, _waveSystem.TotalWaves, "TotalWaves应返回2");
        }

        [Test]
        public void IsWaveActive_ShouldReflectCurrentState()
        {
            // Arrange - 初始状态
            Assert.IsFalse(_waveSystem.IsWaveActive, "初始状态IsWaveActive应为false");

            // Act - 开始波次
            var startWaveMethod = _waveSystem.GetType().GetMethod("StartWave", BindingFlags.NonPublic | BindingFlags.Instance);
            startWaveMethod.Invoke(_waveSystem, new object[] { 0 });

            // Assert
            Assert.IsTrue(_waveSystem.IsWaveActive, "开始波次后IsWaveActive应为true");
        }
    }
}
