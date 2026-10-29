using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Less3.Tweening.Tests
{
    /// <summary>
    /// onUpdate / onComplete / onCancel fire at the right moments, exactly once, and never after the tween is dead.
    /// </summary>
    public class TweenCallbackTests : TweenTestFixture
    {
        [Test]
        public void OnUpdate_ReceivesInterpolatedValues_EndingExactlyOnTo()
        {
            var values = new List<float>();
            Tween.Create(0f, 10f, 1f).SetOnUpdate(values.Add);

            Step(0.25f, 4);

            Assert.AreEqual(4, values.Count);
            Assert.AreEqual(2.5f, values[0], 1e-4f);
            Assert.AreEqual(5f, values[1], 1e-4f);
            Assert.AreEqual(7.5f, values[2], 1e-4f);
            Assert.AreEqual(10f, values[3], "final onUpdate must land exactly on the to value");
        }

        [Test]
        public void OnComplete_FiresOnceAtTheEnd_AndNothingAfter()
        {
            int updates = 0, completes = 0;
            Tween.Create(1f).SetOnUpdate(_ => updates++).SetOnComplete(() => completes++);

            Step(0.5f);
            Assert.AreEqual(0, completes, "completed early");

            Step(0.5f);
            Assert.AreEqual(1, completes);
            Assert.AreEqual(2, updates);

            Step(0.5f, 5);
            Assert.AreEqual(1, completes, "onComplete fired again after the tween was dead");
            Assert.AreEqual(2, updates, "onUpdate fired after the tween was dead");
            AssertNoLeak();
        }

        [Test]
        public void OnComplete_FiresAfterTheFinalOnUpdate()
        {
            var order = new List<string>();
            Tween.Create(1f).SetOnUpdate(_ => order.Add("update")).SetOnComplete(() => order.Add("complete"));

            Step(1f);

            CollectionAssert.AreEqual(new[] { "update", "complete" }, order);
        }

        [Test]
        public void Overshoot_CompletesInOneTick_WithExactToValue()
        {
            float last = float.NaN;
            int completes = 0;
            Tween.Create(3f, 7f, 0.1f).SetOnUpdate(v => last = v).SetOnComplete(() => completes++);

            Step(100f);

            Assert.AreEqual(7f, last);
            Assert.AreEqual(1, completes);
            AssertNoLeak();
        }

        [Test]
        public void Cancel_FiresOnCancel_NotOnComplete_AndStopsUpdates()
        {
            int updates = 0, completes = 0, cancels = 0;
            Tween t = Tween.Create(1f)
                .SetOnUpdate(_ => updates++)
                .SetOnComplete(() => completes++)
                .SetOnCancel(() => cancels++);

            Step(0.5f);
            t = t.Cancel();
            Step(0.5f, 3);

            Assert.AreEqual(1, cancels);
            Assert.AreEqual(0, completes);
            Assert.AreEqual(1, updates);
            Assert.IsFalse(t.IsActive);
            AssertNoLeak();
        }

        [Test]
        public void Cancel_AfterComplete_DoesNotFireOnCancel()
        {
            int cancels = 0;
            Tween t = Tween.Create(1f).SetOnCancel(() => cancels++);

            Step(1f);
            t.Cancel();

            Assert.AreEqual(0, cancels);
        }

        [Test]
        public void Cancel_Twice_FiresOnCancelOnce()
        {
            int cancels = 0;
            Tween t = Tween.Create(1f).SetOnCancel(() => cancels++);

            t.Cancel();
            t.Cancel();

            Assert.AreEqual(1, cancels);
            AssertNoLeak();
        }

        [Test]
        public void CancellingSelf_InsideOnComplete_DoesNotFireOnCancel()
        {
            int cancels = 0;
            Tween t = default;
            t = Tween.Create(1f).SetOnCancel(() => cancels++);
            t = t.SetOnComplete(() => t.Cancel());

            Step(1f);

            Assert.AreEqual(0, cancels, "a completed tween must not also report a cancel");
            AssertNoLeak();
        }

        [Test]
        public void Ease_IsApplied()
        {
            float last = 0f;
            Tween.Create(1f).SetEase(Ease.EaseInQuad).SetOnUpdate(v => last = v);

            Step(0.5f);

            Assert.AreEqual(0.25f, last, 1e-4f);
        }

        [Test]
        public void FixedUpdateType_OnlyTicksInStepFixed()
        {
            int fixedUpdates = 0, frameUpdates = 0;
            Tween.Create(1f).SetUpdateType(TweenUpdateType.FixedDeltaTime).SetOnUpdate(_ => fixedUpdates++);
            Tween.Create(1f).SetOnUpdate(_ => frameUpdates++);

            Step(0.1f);
            Assert.AreEqual(0, fixedUpdates);
            Assert.AreEqual(1, frameUpdates);

            manager.StepFixed(0.02f);
            Assert.AreEqual(1, fixedUpdates);
            Assert.AreEqual(1, frameUpdates);
        }

        [Test]
        public void ThrowingCallback_IsLogged_AndTweenStillCompletes()
        {
            int completes = 0;
            Tween.Create(1f)
                .SetOnUpdate(_ => throw new System.InvalidOperationException("boom"))
                .SetOnComplete(() => completes++);

            LogAssert.Expect(LogType.Error, new Regex("onUpdate callback.*boom"));
            Step(1f);

            Assert.AreEqual(1, completes);
            AssertNoLeak();
        }

        [Test]
        public void ThrowingOnComplete_IsLogged_AndSlotIsReleased()
        {
            Tween.Create(1f).SetOnComplete(() => throw new System.InvalidOperationException("boom"));

            LogAssert.Expect(LogType.Error, new Regex("onComplete callback.*boom"));
            Step(1f);

            AssertNoLeak();
        }
    }
}
