using UnityEngine;

namespace Less3.Tweening
{
    public class TweenManager : MonoBehaviour
    {
        public static TweenManager Instance { get; private set; }

        /// Set by Configure(). Takes priority over the settings asset.
        private static int? configuredInitialSize;
        private static int? configuredMaxSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Bootstrap()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("L3 Tween Manager");
                Instance = obj.AddComponent<TweenManager>();
                DontDestroyOnLoad(obj);

                int initial = DEFAULT_INITIAL_POOL_SIZE;
                int max = DEFAULT_MAX_POOL_SIZE;
                TweenSettings settings = TweenSettings.Load();
                if (settings != null)
                {
                    initial = settings.initialPoolSize;
                    max = settings.maxPoolSize;
                }
                Instance.Initialize(configuredInitialSize ?? initial, configuredMaxSize ?? max);
            }
        }

        /// <summary>
        /// Code alternative to the settings asset. Call it before the first tween, e.g. from a
        /// <c>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]</c>.
        /// Calling it later rebuilds the pool only while no tweens are running.
        /// </summary>
        public static void Configure(int initialPoolSize, int maxPoolSize)
        {
            configuredInitialSize = initialPoolSize;
            configuredMaxSize = maxPoolSize;
            if (Instance == null) return;

            if (Instance.activeCount > 0)
            {
                Debug.LogWarning($"{nameof(TweenManager)}: Configure called while {Instance.activeCount} tweens are running; the new sizes apply on the next bootstrap.");
                return;
            }
            Instance.Initialize(initialPoolSize, maxPoolSize);
        }

        // * ---------------------

        public const int DEFAULT_INITIAL_POOL_SIZE = 512;
        public const int DEFAULT_MAX_POOL_SIZE = 4096;
        /// Ids are ushorts, so the pool can never hold more than this.
        public const int ABSOLUTE_MAX_POOL_SIZE = ushort.MaxValue + 1;

        // * ---------------------

        /// Every TweenObject ever created, indexed by id. Handles point into this.
        private TweenObject[] tweenPool;
        /// FIFO ring of ids that are not in use. Round robin reuse keeps stale handles from colliding for as long as possible.
        private ushort[] freeIds;
        private int freeHead;
        private int freeCount;
        /// Dense list of the ids currently running. Only these are ticked.
        private ushort[] activeIds;
        private int activeCount;
        /// While ticking, releases only mark the entry stale; the list is compacted once the tick is over.
        private bool ticking;
        private bool needsCompact;

        [SerializeField]
        private bool showVisualizer = false;// navigate to the dontDestroyonload scene and flip this to see a texture visualizing the state of all tweens.

        private int maxPoolSize;

        public int PoolSize => tweenPool.Length;
        public int MaxPoolSize => maxPoolSize;
        public int ActiveCount => activeCount;
        /// Ids waiting in the free ring. ActiveCount + FreeCount == PoolSize whenever no tick is in progress.
        internal int FreeCount => freeCount;

        /// <summary>
        /// Make a manually created manager the one the Tween API talks to. Tests use this to run
        /// without the bootstrap. Pass null to clear it.
        /// </summary>
        internal static void InstallInstance(TweenManager manager)
        {
            Instance = manager;
        }

        public void Initialize()
        {
            Initialize(DEFAULT_INITIAL_POOL_SIZE, DEFAULT_MAX_POOL_SIZE);
        }

        /// <summary>
        /// Build the pool. The pool starts at initialSize and doubles whenever it runs out, up to maxSize.
        /// </summary>
        public void Initialize(int initialSize, int maxSize)
        {
            maxPoolSize = Mathf.Clamp(maxSize, 1, ABSOLUTE_MAX_POOL_SIZE);
            initialSize = Mathf.Clamp(initialSize, 1, maxPoolSize);

            tweenPool = new TweenObject[initialSize];
            freeIds = new ushort[initialSize];
            activeIds = new ushort[initialSize];
            for (int i = 0; i < initialSize; i++)
            {
                tweenPool[i] = new TweenObject((ushort)i);
                freeIds[i] = (ushort)i;
            }
            freeHead = 0;
            freeCount = initialSize;
            activeCount = 0;
        }

        /// <summary>
        /// Double the pool, up to maxPoolSize. Only called when the free ring is empty, so the ring is simply refilled.
        /// </summary>
        private bool TryGrow()
        {
            int oldSize = tweenPool.Length;
            if (oldSize >= maxPoolSize) return false;
            int newSize = Mathf.Min(oldSize * 2, maxPoolSize);

            System.Array.Resize(ref tweenPool, newSize);
            System.Array.Resize(ref freeIds, newSize);
            System.Array.Resize(ref activeIds, newSize);
            for (int i = oldSize; i < newSize; i++)
            {
                tweenPool[i] = new TweenObject((ushort)i);
            }
            // the ring is empty, so restart it at 0 and fill it with the new ids
            freeHead = 0;
            freeCount = 0;
            for (int i = oldSize; i < newSize; i++)
            {
                freeIds[freeCount++] = (ushort)i;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{nameof(TweenManager)}: Pool grew from {oldSize} to {newSize} (max {maxPoolSize}). Raise the initial pool size in the tween settings to avoid this at runtime.");
#endif
            return true;
        }

        private void Update()
        {
            Step(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            StepFixed(Time.fixedDeltaTime);
        }

        /// <summary>
        /// One frame of the DeltaTime and UnscaledDeltaTime tweens. Update() calls this with the engine's times;
        /// tests call it directly to step deterministically.
        /// </summary>
        internal void Step(float deltaTime, float unscaledDeltaTime)
        {
            if (activeCount == 0) return;
            TickActive(fixedStep: false, deltaTime, unscaledDeltaTime);
        }

        /// <summary>
        /// One physics step of the FixedDeltaTime tweens.
        /// </summary>
        internal void StepFixed(float fixedDeltaTime)
        {
            if (activeCount == 0) return;
            TickActive(fixedStep: true, fixedDeltaTime, fixedDeltaTime);
        }

        /// <summary>
        /// Tick every active tween that belongs to this step.
        /// Tweens created during the tick are appended past the captured count and first tick next frame.
        /// </summary>
        private void TickActive(bool fixedStep, float deltaTime, float unscaledDeltaTime)
        {
            ticking = true;
            int count = activeCount;
            for (int i = 0; i < count; i++)
            {
                TweenObject obj = tweenPool[activeIds[i]];
                if (obj.activeIndex != i) continue;// released earlier in this tick
                bool isFixed = obj.deltaTimeType == TweenUpdateType.FixedDeltaTime;
                if (isFixed != fixedStep) continue;

                Tick(obj, obj.deltaTimeType == TweenUpdateType.UnscaledDeltaTime ? unscaledDeltaTime : deltaTime);
            }
            ticking = false;
            if (needsCompact) Compact();
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
            Release(obj);
            try
            {
                onComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TweenObject: Exception in onComplete callback: {e}");
            }
        }

        /// <summary>
        /// Drop the entries released during the last tick from the active list and return their ids to the free ring.
        /// </summary>
        private void Compact()
        {
            int write = 0;
            for (int read = 0; read < activeCount; read++)
            {
                ushort id = activeIds[read];
                TweenObject obj = tweenPool[id];
                if (obj.activeIndex != read)
                {
                    PushFree(id);
                    continue;
                }
                obj.activeIndex = write;
                activeIds[write] = id;
                write++;
            }
            activeCount = write;
            needsCompact = false;
        }

        private void PushFree(ushort id)
        {
            freeIds[(freeHead + freeCount) % freeIds.Length] = id;
            freeCount++;
        }

        public Tween AllocateTween(float from, float to, float duration)
        {
            if (freeCount == 0 && !TryGrow())
            {
                Debug.LogError($"{nameof(TweenManager)}: No free tween slots available! Pool is at its max of {maxPoolSize}.");
                return default;
            }

            ushort id = freeIds[freeHead];
            freeHead = (freeHead + 1) % freeIds.Length;
            freeCount--;

            TweenObject obj = tweenPool[id];
            obj.Allocate(from, to, duration);
            obj.activeIndex = activeCount;
            activeIds[activeCount++] = id;
            return new Tween(id, obj.version);
        }

        /// <summary>
        /// Kill the tween, invalidate its handles and return its id to the free ring.
        /// </summary>
        private void Release(TweenObject obj)
        {
            int index = obj.activeIndex;
            obj.Release();
            obj.activeIndex = -1;

            if (ticking)
            {
                // leave the entry in place so the tick loop's indices stay valid. the loop skips it and
                // Compact drops it (and frees the id) once the tick is over, so an id is never listed twice.
                needsCompact = true;
                return;
            }

            // swap remove
            activeCount--;
            if (index != activeCount)
            {
                ushort moved = activeIds[activeCount];
                activeIds[index] = moved;
                tweenPool[moved].activeIndex = index;
            }
            PushFree(obj.id);
        }

        public void CancelTween(Tween tween)
        {
            if (TryGetTweenObject(tween, out TweenObject obj))
            {
                System.Action onCancel = obj.onCancel;
                Release(obj);
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
            if (tweenPoolVisualizer != null && tweenPoolVisualizer.width * tweenPoolVisualizer.height >= tweenPool.Length)
            {
                return;
            }

            if (tweenPoolVisualizer != null)
            {
                tweenPoolVisualizer.Release();
                tweenPoolVisualizer = null;
            }

            //pick a square resolution that will fit all tweens, e.g. 64x32 = 2048
            int texWidth = Mathf.NextPowerOfTwo((int)Mathf.Ceil(Mathf.Sqrt(tweenPool.Length)));
            int texHeight = Mathf.NextPowerOfTwo((int)Mathf.Ceil((float)tweenPool.Length / texWidth));
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
            GUI.Label(new Rect(padding * 2, padding * 3 + h, w, 20), $"{activeCount} / {tweenPool.Length} tweens active");
        }
    }
}
