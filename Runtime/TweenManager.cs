using UnityEngine;

namespace Less3.Tweening
{
    public class TweenManager : MonoBehaviour
    {
        public static TweenManager Instance { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Bootstrap()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("L3 Tween Manager");
                Instance = obj.AddComponent<TweenManager>();
                DontDestroyOnLoad(obj);
                Instance.Initialize();
            }
        }

        // * ---------------------

        private const int TWEEN_POOL = 2048;

        // * ---------------------

        /// Allocate into the array linearly. Looking for a free slot one index at a time.
        private int allocationIndex = 0;
        private TweenObject[] tweenPool;
        [SerializeField]
        private bool showVisualizer = false;// navigate to the dontDestroyonload scene and flip this to see a texture visualizing the state of all tweens.

        public void Initialize()
        {
            tweenPool = new TweenObject[TWEEN_POOL];
            for (int i = 0; i < TWEEN_POOL; i++)
            {
                tweenPool[i] = new TweenObject((ushort)i);
            }
        }

        private void Update()
        {
            for (int i = 0; i < tweenPool.Length; i++)
            {
                TweenObject obj = tweenPool[i];
                if (obj.active && obj.deltaTimeType != TweenUpdateType.FixedDeltaTime)
                {
                    Tick(obj, obj.deltaTimeType == TweenUpdateType.DeltaTime ? Time.deltaTime : Time.unscaledDeltaTime);
                }
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < tweenPool.Length; i++)
            {
                TweenObject obj = tweenPool[i];
                if (obj.active && obj.deltaTimeType == TweenUpdateType.FixedDeltaTime)
                {
                    Tick(obj, Time.fixedDeltaTime);
                }
            }
        }

        private void Tick(TweenObject obj, float deltaTime)
        {
            ushort version = obj.version;
            bool completed = obj.Update(deltaTime);
            // onUpdate may have cancelled this tween, or cancelled and re-allocated the same slot.
            // either way it is no longer the tween we were ticking.
            if (!obj.active || obj.version != version) return;
            if (!completed) return;

            // release before invoking so the callback sees a dead handle and can safely cancel / allocate
            System.Action onComplete = obj.onComplete;
            obj.Release();
            try
            {
                onComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TweenObject: Exception in onComplete callback: {e}");
            }
        }

        public Tween AllocateTween(float from, float to, float duration)
        {
            TweenObject obj;

            for (int i = 0; i < tweenPool.Length; i++)
            {
                int index = (allocationIndex + i) % tweenPool.Length;
                obj = tweenPool[index];
                if (!obj.active)
                {
                    allocationIndex = (index + 1) % tweenPool.Length;
                    obj.Allocate(from, to, duration);
                    return new Tween(obj.id, obj.version);
                }
            }
            Debug.LogError($"{nameof(TweenManager)}: No free tween slots available!");
            return default;
        }

        public void CancelTween(Tween tween)
        {
            if (TryGetTweenObject(tween, out TweenObject obj))
            {
                System.Action onCancel = obj.onCancel;
                obj.Release();
                try
                {
                    onCancel?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"TweenObject: Exception in onCancel callback: {e}");
                }
            }
        }

        /// <summary>
        /// Resolve a handle to its live TweenObject. Fails for default handles, stale handles and finished tweens.
        /// </summary>
        public bool TryGetTweenObject(Tween tween, out TweenObject obj)
        {
            if (tween.version != 0 && tween.id < tweenPool.Length)
            {
                obj = tweenPool[tween.id];
                if (obj.active && obj.version == tween.version)
                {
                    return true;
                }
            }
            obj = null;
            return false;
        }

        // * --------------------- Debug UI ---------------------

        private RenderTexture tweenPoolVisualizer;

        private void InitializeTextureIfNeeded()
        {
            //if it exists and is correct size
            if (tweenPoolVisualizer != null && tweenPoolVisualizer.width * tweenPoolVisualizer.height >= TWEEN_POOL)
            {
                return;
            }

            if (tweenPoolVisualizer != null)
            {
                tweenPoolVisualizer.Release();
                tweenPoolVisualizer = null;
            }

            //pick a square resolution that will fit all tweens, e.g. 64x32 = 2048
            int texWidth = Mathf.NextPowerOfTwo((int)Mathf.Ceil(Mathf.Sqrt(TWEEN_POOL)));
            int texHeight = Mathf.NextPowerOfTwo((int)Mathf.Ceil((float)TWEEN_POOL / texWidth));
            tweenPoolVisualizer = new RenderTexture(texWidth, texHeight, 0);
            tweenPoolVisualizer.filterMode = FilterMode.Point;
        }

        private void WriteTweensToTexture()
        {
            InitializeTextureIfNeeded();

            RenderTexture.active = tweenPoolVisualizer;
            Texture2D tex = new Texture2D(tweenPoolVisualizer.width, tweenPoolVisualizer.height, TextureFormat.RGBAFloat, false);
            for (int i = 0; i < tweenPool.Length; i++)
            {
                TweenObject obj = tweenPool[i];
                Color col;
                if (obj.active)
                {
                    col = Color.Lerp(Color.red, Color.green, obj.t);
                }
                else
                {
                    col = new Color(0f, 0f, 0f, 1f);
                }
                int x = i % tweenPoolVisualizer.width;
                int y = i / tweenPoolVisualizer.width;
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            Graphics.Blit(tex, tweenPoolVisualizer);
            RenderTexture.active = null;
            Object.Destroy(tex);
        }

        private void OnGUI()
        {
            if (!showVisualizer) return;

            int pixelSize = 8;
            int padding = 4;
            WriteTweensToTexture();
            int w = tweenPoolVisualizer.width * pixelSize;
            int h = tweenPoolVisualizer.height * pixelSize;
            //black square
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(padding, padding, w + padding * 2, h + padding * 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(padding * 2, padding * 2, w, h), tweenPoolVisualizer);
        }
    }
}
