using UnityEngine;

namespace Less3.Tweening
{
    /// <summary>
    /// A soft reference to a tween
    /// </summary>
    public struct Tween
    {
        public ushort id;
        public ushort version;// 0 = invalid

        public int key { get { return (version << 16) | id; } }

        public Tween New(float duration)
        {
            return New(0f, 1f, duration);
        }

        static Tween t;// avoid some gc (i think?)

        public Tween New(float from, float to, float duration)
        {
            t = TweenManager.Instance.AllocateTween(from, to, duration);
            this.id = t.id;
            this.version = t.version;
            return this;
        }

        public Tween Cancel()
        {
            TweenManager.Instance.CancelTween(this);
            id = 0;
            version = 0;
            return this;
        }

        // * Set Functions

        public Tween SetEase(Ease ease)
        {
            if (TweenManager.Instance.TryGetTweenObject(this, out TweenObject obj))
            {
                obj.easeType = ease;
            }
            return this;
        }

        public Tween SetUpdateType(TweenUpdateType updateType)
        {
            if (TweenManager.Instance.TryGetTweenObject(this, out TweenObject obj))
            {
                obj.deltaTimeType = updateType;
            }
            else
            {
                Debug.LogWarning($"Tween: SetUpdateType failed to find TweenObject for id {id} version {version}");
            }
            return this;
        }

        public Tween SetOnUpdate(System.Action<float> onUpdate)
        {
            if (TweenManager.Instance.TryGetTweenObject(this, out TweenObject obj))
            {
                obj.onUpdate = onUpdate;
            }
            else
            {
                Debug.LogWarning($"Tween: SetOnUpdate failed to find TweenObject for id {id} version {version}");
            }
            return this;
        }

        public Tween SetOnComplete(System.Action onComplete)
        {
            if (TweenManager.Instance.TryGetTweenObject(this, out TweenObject obj))
            {
                obj.onComplete = onComplete;
            }
            return this;
        }

        public Tween SetOnCancel(System.Action onCancel)
        {
            if (TweenManager.Instance.TryGetTweenObject(this, out TweenObject obj))
            {
                obj.onCancel = onCancel;
            }
            return this;
        }

        //* static bits

        /// <summary>
        /// Start a tween from 0 to 1 over the given duration
        /// </summary>
        public static Tween Create(float duration)
        {
            return Create(0f, 1f, duration);
        }

        /// <summary>
        /// Start a tween that will fade between the values over the duration
        /// </summary>
        public static Tween Create(float from, float to, float duration)
        {
            return TweenManager.Instance.AllocateTween(from, to, duration);
        }
    }
}
