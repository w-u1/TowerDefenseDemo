using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Towers;

namespace TowerDefense.Tests.EditMode
{
    /// <summary>
    /// 炮塔测试：验证升级后伤害按公式增长。
    /// </summary>
    public class TowerTest
    {
        private Tower _tower;
        private TowerData _towerData;
        private GameManager _gameManager;

        [SetUp]
        public void SetUp()
        {
            // 清理可能残留的Singleton实例
            ClearSingleton<GameManager>();

            // 创建GameManager并给足够金币
            var gmGo = new GameObject("GameManager");
            _gameManager = gmGo.AddComponent<GameManager>();
            _gameManager.AddGold(10000); // 给足够金币用于升级

            // 创建TowerData配置
            _towerData = ScriptableObject.CreateInstance<TowerData>();
            _towerData.Type = TowerType.Archer;
            _towerData.DisplayName = "测试箭塔";
            _towerData.Damage = 10f;
            _towerData.Range = 3f;
            _towerData.AttackInterval = 1f;
            _towerData.BuildCost = 50;

            // 设置3级升级数据
            _towerData.Upgrades = new[]
            {
                new TowerUpgradeData { Cost = 80, DamageMultiplier = 1.5f, RangeBonus = 0.5f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 150, DamageMultiplier = 1.5f, RangeBonus = 0.5f, AttackSpeedBonus = 0.1f },
                new TowerUpgradeData { Cost = 300, DamageMultiplier = 2.0f, RangeBonus = 1.0f, AttackSpeedBonus = 0.15f }
            };

            // 创建Tower组件
            var go = new GameObject("TestTower");
            _tower = go.AddComponent<Tower>();
            _tower.Initialize(_towerData);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tower.gameObject);
            Object.DestroyImmediate(_towerData);
            Object.DestroyImmediate(_gameManager.gameObject);
            ClearSingleton<GameManager>();
        }

        /// <summary>
        /// 清理Singleton的_instance字段。
        /// </summary>
        private void ClearSingleton<T>() where T : MonoBehaviour
        {
            var field = typeof(Singleton<T>).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null) field.SetValue(null, null);
        }

        [Test]
        public void Initialize_ShouldSetLevelZeroAndBaseDamage()
        {
            // Assert
            Assert.AreEqual(0, _tower.CurrentLevel, "初始等级应为0");
            Assert.AreEqual(10f, _tower.CurrentDamage, 0.001f, "初始伤害应为基础伤害10");
        }

        [Test]
        public void Upgrade_FirstLevel_ShouldMultiplyDamageBy1_5()
        {
            // Arrange
            float baseDamage = 10f;
            float expectedDamage = baseDamage * 1.5f; // 15

            // Act
            bool result = _tower.Upgrade();

            // Assert
            Assert.IsTrue(result, "升级应成功");
            Assert.AreEqual(1, _tower.CurrentLevel, "升级后等级应为1");
            Assert.AreEqual(expectedDamage, _tower.CurrentDamage, 0.001f,
                $"1级伤害应为{expectedDamage}（基础10 × 1.5）");
        }

        [Test]
        public void Upgrade_SecondLevel_ShouldMultiplyDamageCumulatively()
        {
            // Arrange
            float baseDamage = 10f;
            float expectedDamage = baseDamage * 1.5f * 1.5f; // 22.5

            // Act
            _tower.Upgrade();
            _tower.Upgrade();

            // Assert
            Assert.AreEqual(2, _tower.CurrentLevel, "两次升级后等级应为2");
            Assert.AreEqual(expectedDamage, _tower.CurrentDamage, 0.001f,
                $"2级伤害应为{expectedDamage}（基础10 × 1.5 × 1.5）");
        }

        [Test]
        public void Upgrade_ThirdLevel_ShouldMultiplyDamageCumulatively()
        {
            // Arrange
            float baseDamage = 10f;
            float expectedDamage = baseDamage * 1.5f * 1.5f * 2.0f; // 45

            // Act
            _tower.Upgrade();
            _tower.Upgrade();
            _tower.Upgrade();

            // Assert
            Assert.AreEqual(3, _tower.CurrentLevel, "三次升级后等级应为3");
            Assert.AreEqual(expectedDamage, _tower.CurrentDamage, 0.001f,
                $"3级伤害应为{expectedDamage}（基础10 × 1.5 × 1.5 × 2.0）");
        }

        [Test]
        public void Upgrade_BeyondMaxLevel_ShouldReturnFalse()
        {
            // Arrange - 升满3级
            _tower.Upgrade();
            _tower.Upgrade();
            _tower.Upgrade();
            int levelBefore = _tower.CurrentLevel;
            float damageBefore = _tower.CurrentDamage;

            // Act
            bool result = _tower.Upgrade();

            // Assert
            Assert.IsFalse(result, "超过最大等级时升级应返回false");
            Assert.AreEqual(levelBefore, _tower.CurrentLevel, "等级不应变化");
            Assert.AreEqual(damageBefore, _tower.CurrentDamage, 0.001f, "伤害不应变化");
        }

        [Test]
        public void GetNextUpgrade_ShouldReturnCorrectUpgradeData()
        {
            // Act & Assert
            var next1 = _tower.GetNextUpgrade();
            Assert.IsNotNull(next1, "0级时应有下一级升级数据");
            Assert.AreEqual(80, next1.Cost, "1级升级花费应为80");
            Assert.AreEqual(1.5f, next1.DamageMultiplier, 0.001f, "1级伤害倍率应为1.5");

            _tower.Upgrade();
            var next2 = _tower.GetNextUpgrade();
            Assert.IsNotNull(next2, "1级时应有下一级升级数据");
            Assert.AreEqual(150, next2.Cost, "2级升级花费应为150");

            _tower.Upgrade();
            _tower.Upgrade();
            var next3 = _tower.GetNextUpgrade();
            Assert.IsNull(next3, "满级时不应有下一级升级数据");
        }

        [Test]
        public void Upgrade_DamageFormula_ShouldBeCumulativeMultiplication()
        {
            // 验证伤害公式是累乘而非累加
            // 基础10，升级1(×1.5)=15，升级2(×1.5)=22.5，升级3(×2.0)=45
            // 如果是累加会是 10+5+5+10=30，明显不同

            // Act
            _tower.Upgrade();
            _tower.Upgrade();
            _tower.Upgrade();

            // Assert
            Assert.AreEqual(45f, _tower.CurrentDamage, 0.001f,
                "伤害应为累乘结果45，而非累加结果30");
            Assert.AreNotEqual(30f, _tower.CurrentDamage,
                "伤害不应是累加结果");
        }
    }
}
