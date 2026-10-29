using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Less3.Tweening.Tests
{
    /// <summary>
    /// Pool growth, limits and leak checks under churn.
    /// </summary>
    public class TweenPoolTests : TweenTestFixture
    {
        [Test]
        public void Initialize_ClampsSizes()
        {
            CreateManager(0, 0);
            Assert.AreEqual(1, manager.PoolSize);
            Assert.AreEqual(1, manager.MaxPoolSize);

            CreateManager(100, 10);
            Assert.AreEqual(10, manager.PoolSize, "initial must not exceed max");

            CreateManager(1, int.MaxValue);
            Assert.AreEqual(TweenManager.ABSOLUTE_MAX_POOL_SIZE, manager.MaxPoolSize);
        }

        [Test]
        public void Pool_DoublesWhenExhausted_UpToMax()
        {
            CreateManager(2, 8);

            Tween.Create(1f);
            Tween.Create(1f);
            Assert.AreEqual(2, manager.PoolSize);

            Tween.Create(1f);
            Assert.AreEqual(4, manager.PoolSize);

            for (int i = 0; i < 2; i++) Tween.Create(1f);// 5 total
            Assert.AreEqual(8, manager.PoolSize);

            for (int i = 0; i < 3; i++) Tween.Create(1f);// 8 total, full
            Assert.AreEqual(8, manager.PoolSize);
            Assert.AreEqual(8, manager.ActiveCount);
            AssertPoolInvariant();
        }

        [Test]
        public void Allocation_AtMax_FailsWithInvalidHandle_AndDoesNotThrow()
        {
            CreateManager(2, 2);
            Tween.Create(1f);
            Tween.Create(1f);

            LogAssert.Expect(LogType.Error, new Regex("No free tween slots"));
            Tween failed = Tween.Create(1f);

            Assert.IsFalse(failed.IsActive);
            // chaining on the failed handle must be harmless
            LogAssert.Expect(LogType.Warning, new Regex("SetOnUpdate failed"));
            failed.SetOnUpdate(_ => { });
            Assert.AreEqual(2, manager.ActiveCount);
        }

        [Test]
        public void EveryAllocatedTween_CompletesExactlyOnce()
        {
            CreateManager(4, 64);
            const int count = 50;
            var completions = new int[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Tween.Create(1f).SetOnComplete(() => completions[index]++);
            }

            Step(0.5f, 2);

            for (int i = 0; i < count; i++) Assert.AreEqual(1, completions[i], $"tween {i}");
            AssertNoLeak();
        }

        [Test]
        public void Churn_LeavesNothingStuck()
        {
            CreateManager(8, 256);
            var rng = new System.Random(1234);
            var live = new List<Tween>();
            int completes = 0, cancels = 0, created = 0;

            for (int frame = 0; frame < 2000; frame++)
            {
                int spawn = rng.Next(0, 6);
                for (int i = 0; i < spawn; i++)
                {
                    Tween t = Tween.Create((float)rng.NextDouble() * 0.5f)
                        .SetOnComplete(() => completes++)
                        .SetOnCancel(() => cancels++);
                    Assert.IsTrue(t.IsActive, "pool should be large enough for this churn");
                    created++;
                    live.Add(t);
                }
                if (live.Count > 0 && rng.Next(0, 4) == 0)
                {
                    int index = rng.Next(0, live.Count);
                    live[index].Cancel();
                    live.RemoveAt(index);
                }

                Step(0.016f);
                AssertPoolInvariant();
                live.RemoveAll(t => !t.IsActive);
                Assert.AreEqual(live.Count, manager.ActiveCount, $"frame {frame}: live handles disagree with ActiveCount");
            }

            foreach (var t in live) t.Cancel();
            Step(1f);

            Assert.AreEqual(created, completes + cancels, "every created tween must end exactly once");
            AssertNoLeak();
        }

        [Test]
        public void SlotReuse_IsRoundRobin()
        {
            CreateManager(4, 4);
            Tween first = Tween.Create(1f);
            first.Cancel();

            // the freed slot goes to the back of the ring, so the next three allocations use fresh ids
            Tween a = Tween.Create(1f);
            Tween b = Tween.Create(1f);
            Tween c = Tween.Create(1f);
            Tween d = Tween.Create(1f);

            Assert.AreNotEqual(first.id, a.id);
            Assert.AreNotEqual(first.id, b.id);
            Assert.AreNotEqual(first.id, c.id);
            Assert.AreEqual(first.id, d.id);
            Assert.AreNotEqual(first.version, d.version, "reused slot must carry a new version");
        }
    }
}
