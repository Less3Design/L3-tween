using NUnit.Framework;

namespace Less3.Tweening.Tests
{
    /// <summary>
    /// Odd time inputs: zero and negative durations, negative and zero deltas, and paused scaled time.
    /// </summary>
    public class TweenTimeTests : TweenTestFixture
    {
        [TestCase(0f)]
        [TestCase(-1f)]
        public void NonPositiveDuration_CompletesOnFirstTick_WithToValue(float duration)
        {
            float last = float.NaN;
            int completes = 0;
            Tween.Create(2f, 5f, duration).SetOnUpdate(v => last = v).SetOnComplete(() => completes++);

            Step(0.016f);

            Assert.AreEqual(5f, last);
            Assert.AreEqual(1, completes);
            AssertNoLeak();
        }

        [Test]
        public void NegativeDeltaTime_HoldsAtStart_AndDoesNotProduceNaN()
        {
            float last = float.NaN;
            int completes = 0;
            Tween.Create(1f, 2f, 1f).SetOnUpdate(v => last = v).SetOnComplete(() => completes++);

            Step(-0.5f);

            Assert.AreEqual(1f, last, "value should clamp to from");
            Assert.IsFalse(float.IsNaN(last));
            Assert.AreEqual(0, completes);
            Assert.AreEqual(1, manager.ActiveCount);

            // the negative step pushed elapsed to -0.5, so it takes 1.5 to finish
            Step(1f);
            Assert.AreEqual(0, completes);
            Step(0.5f);
            Assert.AreEqual(1, completes);
            Assert.AreEqual(2f, last);
        }

        [Test]
        public void ZeroDeltaTime_DoesNotAdvance_OrComplete()
        {
            float last = float.NaN;
            int completes = 0;
            Tween.Create(0f, 1f, 1f).SetOnUpdate(v => last = v).SetOnComplete(() => completes++);

            Step(0f, 10);

            Assert.AreEqual(0f, last);
            Assert.AreEqual(0, completes);
            Assert.AreEqual(1, manager.ActiveCount);
        }

        [Test]
        public void PausedTimeScale_ScaledTweenHolds_UnscaledTweenRuns()
        {
            float scaledLast = -1f, unscaledLast = -1f;
            int scaledCompletes = 0, unscaledCompletes = 0;
            Tween.Create(1f).SetOnUpdate(v => scaledLast = v).SetOnComplete(() => scaledCompletes++);
            Tween.Create(1f).SetUpdateType(TweenUpdateType.UnscaledDeltaTime).SetOnUpdate(v => unscaledLast = v).SetOnComplete(() => unscaledCompletes++);

            // Time.timeScale == 0 means deltaTime is 0 while unscaledDeltaTime keeps flowing
            manager.Step(0f, 0.5f);
            manager.Step(0f, 0.5f);

            Assert.AreEqual(0f, scaledLast);
            Assert.AreEqual(0, scaledCompletes);
            Assert.AreEqual(1f, unscaledLast);
            Assert.AreEqual(1, unscaledCompletes);
            Assert.AreEqual(1, manager.ActiveCount);

            // unpause
            manager.Step(1f, 1f);
            Assert.AreEqual(1, scaledCompletes);
            AssertNoLeak();
        }

        [Test]
        public void HugeDeltaTime_DoesNotOvershootValues()
        {
            float last = float.NaN;
            Tween.Create(-3f, 3f, 1f).SetEase(Ease.EaseOutBack).SetOnUpdate(v => last = v);

            Step(1e6f);

            // Back eases overshoot mid way but every ease evaluates to exactly 1 at t = 1
            Assert.AreEqual(3f, last, 1e-4f);
        }
    }
}
