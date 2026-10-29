using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Less3.Tweening.Tests
{
    /// <summary>
    /// Tween handles go dead when their tween ends and never alias the next tween that reuses the slot.
    /// </summary>
    public class TweenHandleTests : TweenTestFixture
    {
        protected override int InitialPoolSize => 4;
        protected override int MaxPoolSize => 4;

        [Test]
        public void DefaultHandle_IsInactive_AndCancelIsSafe()
        {
            Tween t = default;

            Assert.IsFalse(t.IsActive);
            Assert.DoesNotThrow(() => t.Cancel());
            AssertNoLeak();
        }

        [Test]
        public void Handle_IsActive_UntilComplete()
        {
            Tween t = Tween.Create(1f);

            Assert.IsTrue(t.IsActive);
            Step(0.5f);
            Assert.IsTrue(t.IsActive);
            Step(0.5f);
            Assert.IsFalse(t.IsActive);
        }

        [Test]
        public void SettingCallback_OnDeadHandle_Warns_AndNeverFires()
        {
            int lateCompletes = 0;
            Tween dead = Tween.Create(1f);
            Step(1f);
            Assert.IsFalse(dead.IsActive);

            LogAssert.Expect(LogType.Warning, new Regex("SetOnComplete failed"));
            dead.SetOnComplete(() => lateCompletes++);

            // cycle through the whole pool so the dead handle's slot is reused, then complete everything
            for (int i = 0; i < manager.PoolSize; i++) Tween.Create(1f);
            Step(1f);

            Assert.AreEqual(0, lateCompletes, "callback set on a dead handle fired for the slot's next occupant");
            AssertNoLeak();
        }

        [Test]
        public void StaleHandle_DoesNotCancelTheSlotsNewOccupant()
        {
            Tween stale = Tween.Create(1f);
            Step(1f);

            // reuse every slot, including the stale one's
            int cancels = 0;
            for (int i = 0; i < manager.PoolSize; i++) Tween.Create(1f).SetOnCancel(() => cancels++);

            stale.Cancel();

            Assert.AreEqual(0, cancels);
            Assert.AreEqual(manager.PoolSize, manager.ActiveCount);
        }

        [Test]
        public void RestartPattern_KillsTheOldTween()
        {
            int oldUpdates = 0, oldCancels = 0, newCompletes = 0;
            Tween t = Tween.Create(1f).SetOnUpdate(_ => oldUpdates++).SetOnCancel(() => oldCancels++);
            Step(0.5f);

            t = t.Cancel().New(0.5f).SetOnComplete(() => newCompletes++);
            Step(0.5f);

            Assert.AreEqual(1, oldUpdates);
            Assert.AreEqual(1, oldCancels);
            Assert.AreEqual(1, newCompletes);
            Assert.IsFalse(t.IsActive);
            AssertNoLeak();
        }

        [Test]
        public void Handles_FromBeforeGrowth_StayValid()
        {
            CreateManager(2, 8);
            int completes = 0;
            Tween early = Tween.Create(1f);
            Tween.Create(1f);
            Tween.Create(1f);// forces growth to 4
            Assert.AreEqual(4, manager.PoolSize);

            early.SetOnComplete(() => completes++);
            Step(1f);

            Assert.AreEqual(1, completes);
            AssertNoLeak();
        }
    }
}
