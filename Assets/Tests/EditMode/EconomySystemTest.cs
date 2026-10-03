using NUnit.Framework;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Systems;

namespace TowerDefense.Tests.EditMode
{
    /// <summary>
    /// 经济系统测试：验证金币增加、扣除、格式化等逻辑。
    /// </summary>
    public class EconomySystemTest
    {
        private GameManager _gameManager;
        private EconomySystem _economySystem;

        [SetUp]
        public void SetUp()
        {
            // 创建GameManager实例（Singleton会自动初始化金币）
            var go = new GameObject("GameManager");
            _gameManager = go.AddComponent<GameManager>();

            // 创建EconomySystem实例
            var ecoGo = new GameObject("EconomySystem");
            _economySystem = ecoGo.AddComponent<EconomySystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameManager.gameObject);
            Object.DestroyImmediate(_economySystem.gameObject);
        }

        [Test]
        public void AddGold_ShouldIncreaseCurrentGold()
        {
            // Arrange
            int initialGold = _gameManager.CurrentGold;
            int addAmount = 50;

            // Act
            _gameManager.AddGold(addAmount);

            // Assert
            Assert.AreEqual(initialGold + addAmount, _gameManager.CurrentGold,
                $"增加{addAmount}金币后，当前金币应为{initialGold + addAmount}");
        }

        [Test]
        public void TrySpendGold_WithEnoughGold_ShouldDecreaseGoldAndReturnTrue()
        {
            // Arrange
            int initialGold = _gameManager.CurrentGold;
            int spendAmount = 50;
            Assert.IsTrue(initialGold >= spendAmount, "初始金币应足够花费");

            // Act
            bool result = _gameManager.TrySpendGold(spendAmount);

            // Assert
            Assert.IsTrue(result, "金币足够时应返回true");
            Assert.AreEqual(initialGold - spendAmount, _gameManager.CurrentGold,
                $"花费{spendAmount}金币后，当前金币应为{initialGold - spendAmount}");
        }

        [Test]
        public void TrySpendGold_WithInsufficientGold_ShouldReturnFalseAndNotChangeGold()
        {
            // Arrange
            int initialGold = _gameManager.CurrentGold;
            int spendAmount = initialGold + 100; // 超过当前金币

            // Act
            bool result = _gameManager.TrySpendGold(spendAmount);

            // Assert
            Assert.IsFalse(result, "金币不足时应返回false");
            Assert.AreEqual(initialGold, _gameManager.CurrentGold,
                "金币不足时不应扣除金币");
        }

        [Test]
        public void TrySpendGold_WithZeroOrNegativeAmount_ShouldReturnFalse()
        {
            // Arrange
            int initialGold = _gameManager.CurrentGold;

            // Act & Assert
            Assert.IsFalse(_gameManager.TrySpendGold(0), "花费0应返回false");
            Assert.IsFalse(_gameManager.TrySpendGold(-10), "花费负数应返回false");
            Assert.AreEqual(initialGold, _gameManager.CurrentGold, "金币不应变化");
        }

        [Test]
        public void CanAfford_ShouldReturnCorrectResult()
        {
            // Arrange
            int currentGold = _gameManager.CurrentGold;

            // Act & Assert
            Assert.IsTrue(_economySystem.CanAfford(currentGold), "花费等于当前金币时应能负担");
            Assert.IsTrue(_economySystem.CanAfford(currentGold - 1), "花费小于当前金币时应能负担");
            Assert.IsFalse(_economySystem.CanAfford(currentGold + 1), "花费大于当前金币时应不能负担");
        }

        [Test]
        public void FormatGold_ShouldFormatLargeNumbers()
        {
            // Act & Assert
            Assert.AreEqual("100", _economySystem.FormatGold(100), "100应直接显示");
            Assert.AreEqual("10.0K", _economySystem.FormatGold(10000), "10000应显示为10.0K");
            Assert.AreEqual("1.0M", _economySystem.FormatGold(1000000), "1000000应显示为1.0M");
            Assert.AreEqual("9999", _economySystem.FormatGold(9999), "9999应直接显示");
        }

        [Test]
        public void AddGold_MultipleTimes_ShouldAccumulate()
        {
            // Arrange
            int initialGold = _gameManager.CurrentGold;

            // Act
            _gameManager.AddGold(30);
            _gameManager.AddGold(20);
            _gameManager.AddGold(50);

            // Assert
            Assert.AreEqual(initialGold + 100, _gameManager.CurrentGold,
                "多次增加金币应累加");
        }
    }
}
