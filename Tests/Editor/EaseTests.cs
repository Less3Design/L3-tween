using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Less3.Tweening.Tests
{
    // Note: These are a bit extra, it creates almost 200 tests. But I always felt like I was just blindly trusting that
    // these functions worked. Tiny bugs like a function not evaluating properly at t=1 would be really really bad.

    /// <summary>
    /// Property-based checks on <see cref="EaseExtensions.Evaluate"/>. These avoid restating the
    /// formulas: each test asserts a shape property that any correct easing curve must satisfy,
    /// so a flipped sign, wrong constant, or bad piecewise threshold shows up without the test
    /// having to know the "right" value.
    /// </summary>
    public class EaseTests
    {
        const float Tolerance = 1e-4f;
        const int GridSteps = 1000;

        static readonly Ease[] AllEases = (Ease[])Enum.GetValues(typeof(Ease));

        /// <summary>Eases that overshoot [0,1] by design.</summary>
        static readonly Ease[] Overshooting =
        {
            Ease.EaseInBack, Ease.EaseOutBack, Ease.EaseInOutBack,
            Ease.EaseInElastic, Ease.EaseOutElastic, Ease.EaseInOutElastic,
        };

        /// <summary>Bounce stays in [0,1] but is not monotonic (it bounces).</summary>
        static readonly Ease[] Bouncing =
        {
            Ease.EaseInBounce, Ease.EaseOutBounce, Ease.EaseInOutBounce,
        };

        static IEnumerable<Ease> Bounded => AllEases.Except(Overshooting);
        static IEnumerable<Ease> Monotonic => Bounded.Except(Bouncing);

        /// <summary>(In, Out, InOut) triples. InOut is null where it uses its own constants.</summary>
        static readonly (Ease In, Ease Out, Ease? InOut)[] Families =
        {
            (Ease.EaseInSine,    Ease.EaseOutSine,    Ease.EaseInOutSine),
            (Ease.EaseInQuad,    Ease.EaseOutQuad,    Ease.EaseInOutQuad),
            (Ease.EaseInCubic,   Ease.EaseOutCubic,   Ease.EaseInOutCubic),
            (Ease.EaseInQuart,   Ease.EaseOutQuart,   Ease.EaseInOutQuart),
            (Ease.EaseInQuint,   Ease.EaseOutQuint,   Ease.EaseInOutQuint),
            (Ease.EaseInExpo,    Ease.EaseOutExpo,    Ease.EaseInOutExpo),
            (Ease.EaseInCirc,    Ease.EaseOutCirc,    Ease.EaseInOutCirc),
            (Ease.EaseInBounce,  Ease.EaseOutBounce,  Ease.EaseInOutBounce),
            // Back/Elastic InOut variants use different constants (c2, c5) so they are not
            // a pure composition of their In curve; only the In/Out mirror applies to them.
            (Ease.EaseInBack,    Ease.EaseOutBack,    null),
            (Ease.EaseInElastic, Ease.EaseOutElastic, null),
        };

        static IEnumerable<Ease> InOutPairs => Families.Select(f => f.In);
        static IEnumerable<Ease> Composable => Families.Where(f => f.InOut.HasValue).Select(f => f.In);

        static (Ease In, Ease Out, Ease? InOut) Family(Ease inEase) => Families.First(f => f.In == inEase);

        static IEnumerable<float> Grid()
        {
            for (int i = 0; i <= GridSteps; i++)
                yield return i / (float)GridSteps;
        }

        // ------------------------------------------------------------------
        // 1. Endpoints
        // ------------------------------------------------------------------

        [TestCaseSource(nameof(AllEases))]
        public void StartsAtZero(Ease ease)
        {
            Assert.AreEqual(0f, ease.Evaluate(0f), Tolerance);
        }

        [TestCaseSource(nameof(AllEases))]
        public void EndsAtOne(Ease ease)
        {
            Assert.AreEqual(1f, ease.Evaluate(1f), Tolerance);
        }

        // ------------------------------------------------------------------
        // 2. In/Out mirror: In(t) == 1 - Out(1 - t)
        // ------------------------------------------------------------------

        [TestCaseSource(nameof(InOutPairs))]
        public void InIsMirrorOfOut(Ease inEase)
        {
            var (_, outEase, _) = Family(inEase);
            foreach (float t in Grid())
            {
                float expected = 1f - outEase.Evaluate(1f - t);
                Assert.AreEqual(expected, inEase.Evaluate(t), Tolerance,
                    $"{inEase}({t}) should equal 1 - {outEase}({1f - t})");
            }
        }

        // ------------------------------------------------------------------
        // 3. InOut composition: first half is In squeezed, second half is mirrored
        // ------------------------------------------------------------------

        [TestCaseSource(nameof(Composable))]
        public void InOutIsCompositionOfIn(Ease inEase)
        {
            var (_, _, inOut) = Family(inEase);
            foreach (float t in Grid())
            {
                float expected = t < 0.5f
                    ? inEase.Evaluate(2f * t) / 2f
                    : 1f - inEase.Evaluate(2f - 2f * t) / 2f;
                Assert.AreEqual(expected, inOut.Value.Evaluate(t), Tolerance,
                    $"{inOut}({t}) should be composed from {inEase}");
            }
        }

        // ------------------------------------------------------------------
        // 4. Continuity: no jumps between neighbouring samples
        // ------------------------------------------------------------------

        [TestCaseSource(nameof(AllEases))]
        public void IsContinuous(Ease ease)
        {
            // Steepest slope among these curves is InOutElastic around t=0.5 (~20/unit),
            // which gives a step of ~0.02 at this grid resolution. 0.05 leaves headroom
            // while still catching any piecewise seam that does not line up.
            const float maxStep = 0.05f;
            float prev = ease.Evaluate(0f);
            foreach (float t in Grid().Skip(1))
            {
                float cur = ease.Evaluate(t);
                Assert.LessOrEqual(Mathf.Abs(cur - prev), maxStep, $"{ease} jumps at t={t}");
                prev = cur;
            }
        }

        // ------------------------------------------------------------------
        // 5. Range and monotonicity (slope never reverses direction)
        // ------------------------------------------------------------------

        [TestCaseSource(nameof(Bounded))]
        public void StaysWithinUnitRange(Ease ease)
        {
            foreach (float t in Grid())
            {
                float v = ease.Evaluate(t);
                Assert.GreaterOrEqual(v, -Tolerance, $"{ease}({t}) below 0");
                Assert.LessOrEqual(v, 1f + Tolerance, $"{ease}({t}) above 1");
            }
        }

        [TestCaseSource(nameof(Monotonic))]
        public void IsMonotonicNonDecreasing(Ease ease)
        {
            float prev = ease.Evaluate(0f);
            foreach (float t in Grid().Skip(1))
            {
                float cur = ease.Evaluate(t);
                Assert.GreaterOrEqual(cur, prev - Tolerance, $"{ease} decreases at t={t}");
                prev = cur;
            }
        }

        [TestCaseSource(nameof(Overshooting))]
        public void OvershootsUnitRange(Ease ease)
        {
            bool overshoots = Grid().Any(t =>
            {
                float v = ease.Evaluate(t);
                return v < -Tolerance || v > 1f + Tolerance;
            });
            Assert.IsTrue(overshoots, $"{ease} should leave [0,1] somewhere");
        }

        // ------------------------------------------------------------------
        // 6. Golden values (hand-checkable against easings.net)
        // ------------------------------------------------------------------

        [TestCase(Ease.Linear,         0.5f, 0.5f)]
        [TestCase(Ease.EaseInQuad,     0.5f, 0.25f)]
        [TestCase(Ease.EaseOutQuad,    0.5f, 0.75f)]
        [TestCase(Ease.EaseInCubic,    0.5f, 0.125f)]
        [TestCase(Ease.EaseInOutSine,  0.5f, 0.5f)]
        [TestCase(Ease.EaseInExpo,     0.5f, 0.03125f)]      // 2^-5
        [TestCase(Ease.EaseInCirc,     0.5f, 0.1339746f)]    // 1 - sqrt(0.75)
        [TestCase(Ease.EaseInBack,     0.5f, -0.0876975f)]   // (1.70158+1)*0.125 - 1.70158*0.25
        [TestCase(Ease.EaseOutBack,    0.5f, 1.0876975f)]
        [TestCase(Ease.EaseOutElastic, 0.5f, 1.015625f)]     // 2^-5 * sin(150deg) + 1
        [TestCase(Ease.EaseOutBounce,  0.5f, 0.765625f)]     // 7.5625*(0.5-1.5/2.75)^2 + 0.75
        public void MatchesGoldenValue(Ease ease, float t, float expected)
        {
            Assert.AreEqual(expected, ease.Evaluate(t), Tolerance);
        }

        // ------------------------------------------------------------------
        // 7. Default branch
        // ------------------------------------------------------------------

        [Test]
        public void UnknownEase_LogsErrorAndReturnsInput()
        {
            const Ease bogus = (Ease)200;
            LogAssert.Expect(LogType.Error, new Regex("Unsupported ease type"));

            Assert.AreEqual(0.37f, bogus.Evaluate(0.37f), Tolerance);
        }
    }
}
