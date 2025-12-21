using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Less3.Tweening.Editor
{
    [CustomPropertyDrawer(typeof(Ease))]
    public class EasePropertyDrawer : PropertyDrawer
    {
        // imgui
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);
            var enumRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(enumRect, property, label);
            EditorGUI.EndProperty();
        }

        const float PreviewHeight = 16f;
        const float MainPadding = 6f;
        const float OvershootPadding = 24f;
        const float DotRadius = 4f;
        const float Speed = .8f; // cycles per second

        private static string EDITOR_PREFS_KEY = "L3Tween_EasePreviewEnabled";

        public static System.Action SettingChanged;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement
            {
                style =
            {
                flexDirection = FlexDirection.Column
            }
            };

            // Add a right click option to the property
            root.AddManipulator(new ContextualMenuManipulator(menuEvent =>
            {
                menuEvent.menu.AppendAction("Show Ease Preview", actionEvent =>
                {
                    bool current = EditorPrefs.GetBool(EDITOR_PREFS_KEY, true);
                    EditorPrefs.SetBool(EDITOR_PREFS_KEY, !current);
                    SettingChanged?.Invoke();
                },
                actionEvent =>
                {
                    bool current = EditorPrefs.GetBool(EDITOR_PREFS_KEY, true);
                    return current ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });
            }));

            // Enum field
            root.Add(new PropertyField(property));

            // Animated preview
            var preview = new EasePreviewElement(property);
            root.Add(preview);
            preview.style.height = PreviewHeight;
            preview.style.marginTop = -2f;
            root.Add(preview);

            preview.style.display = EditorPrefs.GetBool(EDITOR_PREFS_KEY, true) ? DisplayStyle.Flex : DisplayStyle.None;

            return root;
        }

        class EasePreviewElement : VisualElement
        {
            readonly SerializedProperty property;
            float time;
            bool flip;

            public EasePreviewElement(SerializedProperty property)
            {
                this.property = property;
                generateVisualContent += OnGenerateVisualContent;

                EditorApplication.update += Tick;
                RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    EditorApplication.update -= Tick;
                    EasePropertyDrawer.SettingChanged -= SettingChanged;
                });

                EasePropertyDrawer.SettingChanged += SettingChanged;
            }

            void SettingChanged()
            {
                style.display = EditorPrefs.GetBool(EDITOR_PREFS_KEY, true) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void Tick()
            {
                time = (Time.realtimeSinceStartup * Speed) % 1f;
                flip = (int)(Time.realtimeSinceStartup * Speed) % 2 == 1;
                MarkDirtyRepaint();
            }

            void OnGenerateVisualContent(MeshGenerationContext ctx)
            {
                var rect = contentRect;
                if (rect.width <= 0f || rect.height <= 0f)
                    return;

                var painter = ctx.painter2D;

                float t = time;
                float eased = ((Ease)property.enumValueIndex).Evaluate(t);

                float xMin = rect.xMin + MainPadding;
                float xMax = rect.xMax - MainPadding;
                float xMinNorm = xMin + OvershootPadding;
                float xMaxNorm = xMax - OvershootPadding;
                float yMid = rect.center.y;

                float x = Mathf.LerpUnclamped(xMinNorm + DotRadius, xMaxNorm - DotRadius, eased);
                if (flip)
                    x = xMaxNorm - (x - xMinNorm);

                // Track line
                painter.lineCap = LineCap.Round;
                painter.strokeColor = new Color(1f, 1f, 1f, 0.1f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(xMin, yMid));
                painter.LineTo(new Vector2(xMax, yMid));
                painter.Stroke();

                painter.strokeColor = new Color(1f, 1f, 1f, 0.2f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(xMinNorm, yMid));
                painter.LineTo(new Vector2(xMaxNorm, yMid));
                painter.Stroke();

                painter.strokeColor = new Color(1f, 1f, 1f, 1f);
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x - DotRadius, yMid));
                painter.LineTo(new Vector2(x + DotRadius, yMid));
                painter.Stroke();
            }
        }
    }
}
