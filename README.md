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
Supporting just a single use case lets us keep the business logic to only a couple hundred lines. Here is all the state we mainatin:
```csharp
public class TweenObject
{
    public ushort id;
    public ushort version;
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
}
```

## Performance
It's super good enough. If you need to animate many **thousands** of objects at once I reccomend treating that like a particle system, don't use tweens.
- TweenObjects are pooled
- Every object is iterated every frame, but if no tweens are active the process takes < `.01ms`
- The biggest cost will be whatever you allocate and execute inside tweens
- No async job burst etc... because the goal is to keep code as simple as possible

>Video showing the system allocating tweens through its pool

https://github.com/user-attachments/assets/805f029c-1e62-4172-bcfb-62bd22df7682

## TODO
- Init custom pool size
- Auto pool expansion
- Probably some perf to improve, could avoid iterating inactive tweens every frame
