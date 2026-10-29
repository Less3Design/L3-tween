using UnityEngine;

namespace Less3.Tweening
{
    /// <summary>
    /// A soft reference to a tween. The handle becomes invalid as soon as the tween completes or is cancelled.
    /// </summary>
    public readonly struct Tween
    {
        public readonly ushort id;
        public readonly ushort version;// 0 = invalid

        public Tween(ushort id, ushort version)
        {
            this.id = id;
            this.version = version;
        }

        public int key { get { return (version << 16) | id; } }

        /// <summary>
        /// True while the tween this handle points to is still running.
        /// </summary>
        public bool IsActive => version != 0 && TweenManager.Instance != null && TweenManager.Instance.TryGetTweenObject(this, out _);

        /// <summary>
        /// Start a new tween from 0 to 1. The returned handle replaces this one, so assign it: <c>t = t.Cancel().New(1f)</c>
        /// </summary>
        public Tween New(float duration)
        {
            return New(0f, 1f, duration);
        }

        /// <summary>
        /// Start a new tween between the values. The returned handle replaces this one, so assign it: <c>t = t.Cancel().New(0f, 1f, 1f)</c>
        /// </summary>
        public Tween New(float from, float to, float duration)
        {
            return TweenManager.Instance.AllocateTween(from, to, duration);
        }

        /// <summary>
        /// Cancel the tween if it is still running. Returns an invalid handle.
        /// </summary>
        public Tween Cancel()
        {
            if (version != 0 && TweenManager.Instance != null)
            {
                TweenManager.Instance.CancelTween(this);
            }
            return default;
        }

        // * Set Functions

        public Tween SetEase(Ease ease)
        {
            if (TweenManager.Instance.TryGetTweenObject(this, out TweenObject obj))
            {
                obj.easeType = ease;
            }
            else
            {
                Debug.LogWarning($"Tween: SetEase failed to find TweenObject for id {id} version {version}");
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
            else
            {
                Debug.LogWarning($"Tween: SetOnComplete failed to find TweenObject for id {id} version {version}");
            }
            return this;
        }

        public Tween SetOnCancel(System.Action onCancel)
        {
            if (TweenManager.Instance.TryGetTweenObject(this, out TweenObject obj))
            {
                obj.onCancel = onCancel;
            }
            else
            {
                Debug.LogWarning($"Tween: SetOnCancel failed to find TweenObject for id {id} version {version}");
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
