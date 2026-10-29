using UnityEngine;
using System;

namespace Less3.Tweening
{
    public class TweenObject
    {
        public ushort id;// maps to the index in the tween pool
        public ushort version;// increments each time the tween is released, so stale handles stop matching. never 0.
        public bool active;

        public float fromValue;
        public float toValue;
        public float duration;
        public float elapsedTime;
        public Ease easeType;
        public TweenUpdateType deltaTimeType;

        public Action<float> onUpdate;
        public Action onComplete;
        public Action onCancel;

        public float t => active ? Progress() : 0f;

        public TweenObject(ushort id)
        {
            this.id = id;
            this.version = 1;
        }

        public void Allocate(float from, float to, float duration)
        {
            active = true;
            this.fromValue = from;
            this.toValue = to;
            this.duration = duration;
            this.elapsedTime = 0f;

            // default values
            this.easeType = Ease.Linear;
            this.deltaTimeType = TweenUpdateType.DeltaTime;
            onUpdate = null;
            onComplete = null;
            onCancel = null;
        }

        /// <summary>
        /// Advance the tween and invoke onUpdate. Returns true when the tween has reached its end.
        /// Does not release the tween; the manager does that so callbacks see a dead handle.
        /// </summary>
        public bool Update(float deltaTime)
        {
            elapsedTime += deltaTime;
            float t = Progress();
            float value = Mathf.LerpUnclamped(fromValue, toValue, easeType.Evaluate(t));
            try
            {
                onUpdate?.Invoke(value);
            }
            catch (Exception e)
            {
                Debug.LogError($"TweenObject: Exception in onUpdate callback: {e}");
            }
            return t >= 1f;
        }

        /// <summary>
        /// Mark the tween dead and invalidate every handle pointing at it.
        /// </summary>
        public void Release()
        {
            active = false;
            version++;
            if (version == 0) version = 1; // 0 is reserved for invalid handles
            onUpdate = null;
            onComplete = null;
            onCancel = null;
        }

        private float Progress()
        {
            // a zero/negative duration completes on its first tick instead of producing NaN forever
            if (duration <= 0f) return 1f;
            return Mathf.Clamp01(elapsedTime / duration);
        }
    }

    public enum TweenUpdateType : byte
    {
        DeltaTime,
        FixedDeltaTime,
        UnscaledDeltaTime
    }
}
