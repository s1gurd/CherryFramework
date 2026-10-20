using System.Collections;
using CherryFramework.TickDispatcher;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class TickerBehaviourTests
    {
        private class CountingTickable : ITickable
        {
            public int Ticks;

            public void Tick(float deltaTime) => Ticks++;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var behaviour in Object.FindObjectsByType<TickerBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(behaviour.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator RegisteredTickable_IsTickedEveryFrame()
        {
            var ticker = new Ticker();
            var tickable = new CountingTickable();
            ticker.Register(tickable);

            yield return null;
            yield return null;

            Assert.GreaterOrEqual(tickable.Ticks, 1);

            ticker.UnRegister(tickable);
        }

        [UnityTest]
        public IEnumerator UnregisteredTickable_IsNotTicked()
        {
            var ticker = new Ticker();
            var tickable = new CountingTickable();
            ticker.Register(tickable);

            yield return null;

            ticker.UnRegister(tickable);
            var ticksBefore = tickable.Ticks;

            yield return null;
            yield return null;

            Assert.AreEqual(ticksBefore, tickable.Ticks);
        }
    }
}
