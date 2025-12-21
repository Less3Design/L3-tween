using UnityEngine;

namespace Less3.Tweening
{
    public enum Ease : byte
    {
        Linear = 0,
        EaseInSine = 1,
        EaseOutSine = 2,
        EaseInOutSine = 3,
        EaseInQuad = 4,
        EaseOutQuad = 5,
        EaseInOutQuad = 6,
        EaseInCubic = 7,
        EaseOutCubic = 8,
        EaseInOutCubic = 9,
        EaseInQuart = 10,
        EaseOutQuart = 11,
        EaseInOutQuart = 12,
        EaseInQuint = 13,
        EaseOutQuint = 14,
        EaseInOutQuint = 15,
        EaseInExpo = 16,
        EaseOutExpo = 17,
        EaseInOutExpo = 18,
        EaseInCirc = 19,
        EaseOutCirc = 20,
        EaseInOutCirc = 21,
        EaseInBack = 22,
        EaseOutBack = 23,
        EaseInOutBack = 24,
        EaseInElastic = 25,
        EaseOutElastic = 26,
        EaseInOutElastic = 27,
        EaseInBounce = 28,
        EaseOutBounce = 29,
        EaseInOutBounce = 30,
    }

    public static class EaseExtensions
    {
        public static float Evaluate(this Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.Linear:
                    return t;
                case Ease.EaseInSine:
                    return 1f - Mathf.Cos((t * Mathf.PI) / 2f);
                case Ease.EaseOutSine:
                    return Mathf.Sin((t * Mathf.PI) / 2f);
                case Ease.EaseInOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.EaseInQuad:
                    return t * t;
                case Ease.EaseOutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.EaseInOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.EaseInCubic:
                    return t * t * t;
                case Ease.EaseOutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.EaseInOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case Ease.EaseInQuart:
                    return t * t * t * t;
                case Ease.EaseOutQuart:
                    return 1f - Mathf.Pow(1f - t, 4f);
                case Ease.EaseInOutQuart:
                    return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;
                case Ease.EaseInQuint:
                    return t * t * t * t * t;
                case Ease.EaseOutQuint:
                    return 1f - Mathf.Pow(1f - t, 5f);
                case Ease.EaseInOutQuint:
                    return t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;
                case Ease.EaseInExpo:
                    return t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case Ease.EaseOutExpo:
                    return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Ease.EaseInOutExpo:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                case Ease.EaseInCirc:
                    return 1f - Mathf.Sqrt(1f - Mathf.Pow(t, 2f));
                case Ease.EaseOutCirc:
                    return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
                case Ease.EaseInOutCirc:
                    return t < 0.5f ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;
                case Ease.EaseInBack:
                    {
                        const float c1 = 1.70158f;
                        const float c3 = c1 + 1f;
                        return c3 * t * t * t - c1 * t * t;
                    }
                case Ease.EaseOutBack:
                    {
                        const float c1 = 1.70158f;
                        const float c3 = c1 + 1f;
                        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                    }
                case Ease.EaseInOutBack:
                    {
                        const float c1 = 1.70158f;
                        const float c2 = c1 * 1.525f;
                        if (t < 0.5f)
                        {
                            float x = Mathf.Pow(2f * t, 2f);
                            float y = (c2 + 1f) * 2f * t - c2;
                            return (x * y) / 2f;
                        }
                        else
                        {
                            float x = Mathf.Pow(2f * t - 2f, 2f);
                            float y = (c2 + 1f) * (t * 2f - 2f) + c2;
                            return (x * y + 2f) / 2f;
                        }
                    }
                case Ease.EaseInElastic:
                    {
                        if (t == 0f) return 0f;
                        if (t == 1f) return 1f;
                        const float c4 = (2f * Mathf.PI) / 3f;
                        return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
                    }
                case Ease.EaseOutElastic:
                    {
                        if (t == 0f) return 0f;
                        if (t == 1f) return 1f;
                        const float c4 = (2f * Mathf.PI) / 3f;
                        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                    }
                case Ease.EaseInOutElastic:
                    {
                        if (t == 0f) return 0f;
                        if (t == 1f) return 1f;
                        const float c5 = (2f * Mathf.PI) / 4.5f;
                        return t < 0.5f
                            ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f
                            : (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
                    }
                case Ease.EaseInBounce:
                    return 1f - Ease.EaseOutBounce.Evaluate(1f - t);
                case Ease.EaseOutBounce:
                    {
                        const float n1 = 7.5625f;
                        const float d1 = 2.75f;

                        if (t < 1f / d1)
                        {
                            return n1 * t * t;
                        }
                        else if (t < 2f / d1)
                        {
                            t -= 1.5f / d1;
                            return n1 * t * t + 0.75f;
                        }
                        else if (t < 2.5f / d1)
                        {
                            t -= 2.25f / d1;
                            return n1 * t * t + 0.9375f;
                        }
                        else
                        {
                            t -= 2.625f / d1;
                            return n1 * t * t + 0.984375f;
                        }
                    }
                case Ease.EaseInOutBounce:
                    return t < 0.5f
                        ? (1f - Ease.EaseOutBounce.Evaluate(1f - 2f * t)) / 2f
                        : (1f + Ease.EaseOutBounce.Evaluate(2f * t - 1f)) / 2f;
                default:
                    {
                        Debug.LogError($"Unsupported ease type: {ease}");
                        return t;
                    }
            }
        }
    }
}
