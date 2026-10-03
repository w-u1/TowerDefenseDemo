using System;
using System.Collections.Generic;

namespace TowerDefense.Core
{
    /// <summary>
    /// 轻量级事件总线。支持按事件类型订阅/取消订阅/派发，解耦各系统间的直接引用。
    /// 事件参数使用 struct，通过泛型方法派发。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        /// <summary>
        /// 订阅指定类型的事件。
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                // 防止重复订阅同一个handler
                bool alreadySubscribed = false;
                foreach (var del in existing.GetInvocationList())
                {
                    if (del == (Delegate)handler)
                    {
                        alreadySubscribed = true;
                        break;
                    }
                }
                if (!alreadySubscribed)
                {
                    _handlers[type] = Delegate.Combine(existing, handler);
                }
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        /// <summary>
        /// 取消订阅指定类型的事件。
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var newDelegate = Delegate.Remove(existing, handler);
                if (newDelegate == null)
                {
                    _handlers.Remove(type);
                }
                else
                {
                    _handlers[type] = newDelegate;
                }
            }
        }

        /// <summary>
        /// 派发事件，通知所有订阅者。
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var handler))
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[EventBus] 事件 {typeof(T).Name} 派发异常: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 清空所有订阅（用于游戏重置/场景切换）。
        /// </summary>
        public static void ClearAll()
        {
            _handlers.Clear();
        }

        /// <summary>
        /// 获取当前订阅的事件数量（调试用）。
        /// </summary>
        public static int GetSubscriberCount()
        {
            return _handlers.Count;
        }
    }

    // ===== 游戏中使用的事件定义 =====

    /// <summary>金币数量变化事件</summary>
    public struct GoldChangedEvent
    {
        public int CurrentGold;
        public int Delta;
    }

    /// <summary>生命值变化事件</summary>
    public struct LivesChangedEvent
    {
        public int CurrentLives;
        public int Delta;
    }

    /// <summary>波次开始事件</summary>
    public struct WaveStartedEvent
    {
        public int WaveNumber;
        public int TotalEnemies;
    }

    /// <summary>波次结束事件</summary>
    public struct WaveCompletedEvent
    {
        public int WaveNumber;
    }

    /// <summary>敌人被击杀事件</summary>
    public struct EnemyKilledEvent
    {
        public int RewardGold;
        public UnityEngine.Vector3 Position;
    }

    /// <summary>敌人到达终点事件</summary>
    public struct EnemyReachedEndEvent
    {
        public int Damage;
    }

    /// <summary>防御塔建造事件</summary>
    public struct TowerBuiltEvent
    {
        public string TowerType;
        public UnityEngine.Vector3 Position;
    }

    /// <summary>游戏状态变化事件</summary>
    public struct GameStateChangedEvent
    {
        public GameState NewState;
        public GameState PreviousState;
    }

    /// <summary>游戏胜利事件</summary>
    public struct GameVictoryEvent { }

    /// <summary>游戏失败事件</summary>
    public struct GameDefeatEvent { }

    /// <summary>剩余敌人数量变化事件</summary>
    public struct EnemiesRemainingChangedEvent
    {
        public int Remaining;
    }

    /// <summary>游戏重置事件</summary>
    public struct GameResetEvent { }
}
