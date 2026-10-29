using UnityEngine;

namespace Less3.Tweening
{
    /// <summary>
    /// Optional per project configuration for the tween pool.
    /// Lives in the consuming project (not the package) at <c>Assets/.../Resources/L3 Tween Settings.asset</c>
    /// so it survives package updates. Create it from Project Settings > Less3 > Tween.
    /// When no asset exists the defaults on TweenManager are used.
    /// </summary>
    public class TweenSettings : ScriptableObject
    {
        public const string RESOURCE_NAME = "L3 Tween Settings";

        [Tooltip("Number of tweens allocated up front. Growth at runtime allocates a batch of objects, so size this to your usual peak.")]
        [Min(1)]
        public int initialPoolSize = TweenManager.DEFAULT_INITIAL_POOL_SIZE;

        [Tooltip("The pool doubles when it runs out, up to this many tweens. Allocations past this fail with an error. Hard limit is 65536.")]
        [Min(1)]
        public int maxPoolSize = TweenManager.DEFAULT_MAX_POOL_SIZE;

        private void OnValidate()
        {
            maxPoolSize = Mathf.Clamp(maxPoolSize, 1, TweenManager.ABSOLUTE_MAX_POOL_SIZE);
            initialPoolSize = Mathf.Clamp(initialPoolSize, 1, maxPoolSize);
        }

        /// <summary>
        /// The settings asset in Resources, or null when the project has not created one.
        /// </summary>
        public static TweenSettings Load()
        {
            return Resources.Load<TweenSettings>(RESOURCE_NAME);
        }
    }
}
