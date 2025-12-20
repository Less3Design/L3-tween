using UnityEngine;
using System;

public class TweenObject
{
    public ushort id;// maps to the index in the tween pool
    public ushort version;// increments each time the tween is reused
    public bool active;

    public float fromValue;
    public float toValue;
    public float duration;
    public float elapsedTime;
    public Ease easeType;
    public TweenUpdateType deltaTimeType;

    public float t => active ? Mathf.Clamp01(elapsedTime / duration) : 0f;

    // OnUpdate is guaranteed to be called OnComplete with the final value
    public Action<float> onUpdate;
    public Action onComplete;
    public Action onCancel;

    public TweenObject(ushort id)
    {
        this.id = id;
    }

    public void Allocate(float from, float to, float duration)
    {
        active = true;
        version++;
        version += (version == 0) ? (ushort)1 : (ushort)0; // avoid zero version
        this.fromValue = from;
        this.toValue = to;
        this.duration = duration;
        this.elapsedTime = 0f;

        // default values
        this.easeType = Ease.Linear;
        this.deltaTimeType = TweenUpdateType.DeltaTime;
    }

    public void Update(float deltaTime)
    {
        // if (!active) return; // checked in TweenManager
        elapsedTime += deltaTime;
        float t = Mathf.Clamp01(elapsedTime / duration);
        float value = Mathf.LerpUnclamped(fromValue, toValue, t);
        onUpdate?.Invoke(value);

        if (t >= 1f)
        {
            onComplete?.Invoke();
            active = false;
            Reset();
        }
    }

    public void Reset()
    {
        // release actions
        // other fields don't need to be reset
        onUpdate = null;
        onComplete = null;
        onCancel = null;
    }
}

public enum TweenUpdateType : byte
{
    DeltaTime,
    FixedDeltaTime,
    UnscaledDeltaTime
}
