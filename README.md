<img width="6904" height="1380" alt="Frame 57" src="https://github.com/user-attachments/assets/e40c77f2-d796-474d-881f-fd9876092b24" />

## Install
Add to your Unity project as a [git package using the package manager](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-ui-giturl.html).
```
https://github.com/Less3Design/L3-tween.git
```
## Usage
L3 Tween is based around exclusively supporting the way I like to write tweens:
```c#
private Tween moveTween;

void DoAnimation()
{
    Vector3 startPos = transform.position;
    Vector3 endPos = Random.insideUnitSphere;
    //cancel previous tween, start a new one
    moveTween = moveTween.Cancel().New(1f).SetOnUpdate((t) => {
      transform.position = Vector3.Lerp(startPos,endPos,t);
    });
}
```
`Tween` is a readonly struct handle, so always assign the result of `Cancel()` / `New()`. A handle goes dead the moment its tween completes or is cancelled; `tween.IsActive` tells you which. Setting a callback on a dead handle logs a warning and does nothing.

Supporting just a single use case lets us keep the business logic to only a couple hundred lines. Here is all the state we mainatin:
```csharp
public class TweenObject
{
    public ushort id;
    public ushort version;
    public bool active;
    public int activeIndex;

    public float fromValue;
    public float toValue;
    public float duration;
    public float elapsedTime;
    public Ease easeType;
    public TweenUpdateType deltaTimeType;

    public Action<float> onUpdate;
    public Action onComplete;
    public Action onCancel;
}
```

## Pool size
The pool starts at 512 tweens and doubles whenever it runs out, up to a max of 4096 (hard limit 65536, ids are ushorts). Growth allocates a batch of objects, so set the initial size to your usual peak if you want to avoid that at runtime. A log in the editor reports each growth.

Configure it from **Project Settings > Less3 > Tween**, which creates `Assets/Resources/L3 Tween Settings.asset` in your project (not in the package, so it survives package updates). Or from code, before the first tween:
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void ConfigureTweens() => TweenManager.Configure(initialPoolSize: 1024, maxPoolSize: 8192);
```

## Performance
It's super good enough. If you need to animate many **thousands** of objects at once I reccomend treating that like a particle system, don't use tweens.
- TweenObjects are pooled; allocation is O(1) off a free list
- Only active tweens are iterated. A frame with no tweens running costs a single int compare
- The biggest cost will be whatever you allocate and execute inside tweens
- No async job burst etc... because the goal is to keep code as simple as possible

>Video showing the system allocating tweens through its pool

https://github.com/user-attachments/assets/805f029c-1e62-4172-bcfb-62bd22df7682
