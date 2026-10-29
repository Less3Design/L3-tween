using NUnit.Framework;
using UnityEngine;

namespace Less3.Tweening.Tests
{
    /// <summary>
    /// Base for EditMode tests. Builds a private TweenManager with a small pool, installs it as the
    /// instance the Tween API talks to, and steps it by hand so every test is deterministic.
    /// Nothing here depends on play mode, Time.deltaTime or frame timing.
    /// </summary>
    public abstract class TweenTestFixture
    {
        protected TweenManager manager;
        private GameObject go;
        private TweenManager previousInstance;

        protected virtual int InitialPoolSize => 8;
        protected virtual int MaxPoolSize => 64;

        [SetUp]
        public void SetUpManager()
        {
            previousInstance = TweenManager.Instance;
            CreateManager(InitialPoolSize, MaxPoolSize);
        }

        [TearDown]
        public void TearDownManager()
        {
            DestroyManager();
            TweenManager.InstallInstance(previousInstance);
        }

        /// <summary>
        /// Rebuild the manager with a specific pool, for tests that care about growth.
        /// </summary>
        protected void CreateManager(int initial, int max)
        {
            DestroyManager();
            go = new GameObject("Tween Test Manager") { hideFlags = HideFlags.HideAndDontSave };
            manager = go.AddComponent<TweenManager>();
            manager.Initialize(initial, max);
            TweenManager.InstallInstance(manager);
        }

        private void DestroyManager()
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
                go = null;
                manager = null;
            }
        }

        /// <summary>One frame where scaled and unscaled time advance by the same amount.</summary>
        protected void Step(float deltaTime) => manager.Step(deltaTime, deltaTime);

        protected void Step(float deltaTime, int times)
        {
            for (int i = 0; i < times; i++) Step(deltaTime);
        }

        /// <summary>Every slot is back in the free ring and nothing is ticking.</summary>
        protected void AssertNoLeak()
        {
            Assert.AreEqual(0, manager.ActiveCount, "tweens still active");
            Assert.AreEqual(manager.PoolSize, manager.FreeCount, "free ring does not account for every slot");
        }

        protected void AssertPoolInvariant()
        {
            Assert.AreEqual(manager.PoolSize, manager.ActiveCount + manager.FreeCount, "active + free must equal the pool size between ticks");
        }
    }
}
