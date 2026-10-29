using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Less3.Tweening.Tests
{
    /// <summary>
    /// The real player loop: bootstrap, Update / FixedUpdate driving, and Time.timeScale.
    /// Everything about tween logic itself is covered by the EditMode tests; these only prove the wiring.
    /// </summary>
    public class TweenPlayModeTests
    {
        private float savedTimeScale;

        [SetUp]
        public void SetUp()
        {
            savedTimeScale = Time.timeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = savedTimeScale;
        }

        [UnityTest]
        public IEnumerator Bootstrap_CreatesAPersistentManager()
        {
            yield return null;

            Assert.IsNotNull(TweenManager.Instance);
            Assert.AreEqual("L3 Tween Manager", TweenManager.Instance.gameObject.name);
            Assert.IsTrue(TweenManager.Instance.PoolSize > 0);
        }

        [UnityTest]
        public IEnumerator Tween_CompletesOverRealFrames()
        {
            int completes = 0;
            float last = -1f;
            Tween t = Tween.Create(0.1f).SetOnUpdate(v => last = v).SetOnComplete(() => completes++);

            yield return WaitUntilOrTimeout(() => completes > 0, 2f);

            Assert.AreEqual(1, completes);
            Assert.AreEqual(1f, last);
            Assert.IsFalse(t.IsActive);
        }

        [UnityTest]
        public IEnumerator FixedUpdateTween_CompletesOverPhysicsSteps()
        {
            int completes = 0;
            Tween.Create(0.1f).SetUpdateType(TweenUpdateType.FixedDeltaTime).SetOnComplete(() => completes++);

            yield return WaitUntilOrTimeout(() => completes > 0, 2f);

            Assert.AreEqual(1, completes);
        }

        [UnityTest]
        public IEnumerator TimeScaleZero_ScaledTweenHolds_UnscaledTweenCompletes()
        {
            Time.timeScale = 0f;
            float scaledLast = -1f;
            int scaledCompletes = 0, unscaledCompletes = 0;
            Tween scaled = Tween.Create(0.1f).SetOnUpdate(v => scaledLast = v).SetOnComplete(() => scaledCompletes++);
            Tween.Create(0.1f).SetUpdateType(TweenUpdateType.UnscaledDeltaTime).SetOnComplete(() => unscaledCompletes++);

            yield return WaitUntilOrTimeout(() => unscaledCompletes > 0, 2f);

            Assert.AreEqual(1, unscaledCompletes);
            Assert.AreEqual(0, scaledCompletes);
            Assert.AreEqual(0f, scaledLast, "scaled tween advanced while time was paused");
            Assert.IsTrue(scaled.IsActive);

            Time.timeScale = 1f;
            yield return WaitUntilOrTimeout(() => scaledCompletes > 0, 2f);
            Assert.AreEqual(1, scaledCompletes);
        }

        private static IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
