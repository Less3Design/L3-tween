using System.Collections.Generic;
using NUnit.Framework;

namespace Less3.Tweening.Tests
{
    /// <summary>
    /// Callbacks may cancel or start tweens, including their own, while the manager is mid tick.
    /// </summary>
    public class TweenReentrancyTests : TweenTestFixture
    {
        [Test]
        public void RestartingSelf_InsideOnUpdate_DoesNotCompleteTheNewTween()
        {
            int newCompletes = 0;
            Tween t = default;
            t = Tween.Create(0.1f).SetOnUpdate(_ =>
            {
                // the old tween would complete on this very tick; the new one must not inherit that
                t = t.Cancel().New(1f).SetOnComplete(() => newCompletes++);
            });

            Step(0.1f);
            Assert.AreEqual(0, newCompletes, "new tween completed on the tick that completed the old one");
            Assert.IsTrue(t.IsActive);
            Assert.AreEqual(1, manager.ActiveCount);

            Step(0.5f);
            Assert.AreEqual(0, newCompletes);
            Step(0.5f);
            Assert.AreEqual(1, newCompletes);
            AssertNoLeak();
        }

        [Test]
        public void TweenStartedInsideOnComplete_TicksFromTheNextFrame()
        {
            int chainedUpdates = 0, chainedCompletes = 0;
            Tween.Create(1f).SetOnComplete(() =>
            {
                Tween.Create(1f).SetOnUpdate(_ => chainedUpdates++).SetOnComplete(() => chainedCompletes++);
            });

            Step(1f);
            Assert.AreEqual(0, chainedUpdates, "a tween created mid tick must not tick in the same frame");
            Assert.AreEqual(1, manager.ActiveCount);

            Step(1f);
            Assert.AreEqual(1, chainedUpdates);
            Assert.AreEqual(1, chainedCompletes);
            AssertNoLeak();
        }

        [Test]
        public void Callback_CancellingOtherTweens_IsSafe()
        {
            const int others = 6;
            var handles = new List<Tween>();
            int otherCancels = 0, otherUpdates = 0;
            for (int i = 0; i < others; i++)
            {
                handles.Add(Tween.Create(1f).SetOnUpdate(_ => otherUpdates++).SetOnCancel(() => otherCancels++));
            }
            // this one is allocated last so it ticks after the others in frame one
            Tween.Create(1f).SetOnUpdate(_ =>
            {
                foreach (var h in handles) h.Cancel();
            });

            Step(0.5f);
            Assert.AreEqual(others, otherCancels);
            Assert.AreEqual(others, otherUpdates, "others ticked once before the canceller ran");
            Assert.AreEqual(1, manager.ActiveCount);
            AssertPoolInvariant();

            Step(0.5f);
            Assert.AreEqual(others, otherUpdates, "cancelled tweens kept ticking");
            AssertNoLeak();
        }

        [Test]
        public void FirstTween_CancellingLaterOnes_SkipsThemThisFrame()
        {
            var later = new List<Tween>();
            int laterUpdates = 0;
            Tween.Create(1f).SetOnUpdate(_ =>
            {
                foreach (var h in later) h.Cancel();
            });
            for (int i = 0; i < 5; i++) later.Add(Tween.Create(1f).SetOnUpdate(_ => laterUpdates++));

            Step(0.5f);
            Assert.AreEqual(0, laterUpdates, "a tween cancelled earlier in the same tick still ran");
            Assert.AreEqual(1, manager.ActiveCount);
            AssertPoolInvariant();

            Step(0.5f);
            AssertNoLeak();
        }

        [Test]
        public void CompletingTweens_CreatingNewOnes_WhilePoolIsFull_GrowsInsteadOfAliasing()
        {
            CreateManager(2, 8);
            int chained = 0;
            for (int i = 0; i < 2; i++)
            {
                Tween.Create(1f).SetOnComplete(() => Tween.Create(1f).SetOnComplete(() => chained++));
            }

            Step(1f);// both complete and each starts another; slots freed mid tick are not reusable yet
            Assert.AreEqual(2, manager.ActiveCount);
            Assert.AreEqual(4, manager.PoolSize);
            AssertPoolInvariant();

            Step(1f);
            Assert.AreEqual(2, chained);
            AssertNoLeak();
        }
    }
}
