using UnityEngine;
using UnityEngine.UIElements;

namespace TUDarmstadt.SeriousGames.UIElements
{
    public class Gauge : Slider
    {
        static readonly string drawingContainerUssClassName = "gauge__drawing-container";
        static readonly string percentageLabelUssClassName = "gauge__percentage-label";
        static readonly string gaugeUssClassName = "gauge";
        readonly float widthToHeight = 0.5f;
        public new class UxmlFactory : UxmlFactory<Gauge, UxmlTraits> { }
        VisualElement m_DrawingContainer;
        Label m_PercentageLabel;

        public Gauge() : base() => Initialize();

        // Made this constructor functional by calling the base and our initializer
        public Gauge(string label, float start, float end, SliderDirection direction, float pageSize)
            : base(label, start, end, direction, pageSize)
        {
            Initialize();
        }

        private void Initialize()
        {
            value = 0.3f;
            var styleSheet = Resources.Load<StyleSheet>("SG_AG/USS-Files/Gauge");
            if (styleSheet != null)
                styleSheets.Add(styleSheet);

            // Remove the default slider input visual element
            var sliderInput = this.Q(className: Slider.inputUssClassName);
            if (sliderInput != null)
                this.Remove(sliderInput);

            m_DrawingContainer = new VisualElement();
            m_DrawingContainer.AddToClassList(drawingContainerUssClassName);
            this.Add(m_DrawingContainer);

            m_PercentageLabel = new Label();
            m_PercentageLabel.AddToClassList(percentageLabelUssClassName);
            this.Add(m_PercentageLabel);

            AddToClassList(gaugeUssClassName);

            this.generateVisualContent += Draw;
            this.RegisterValueChangedCallback(OnValueChanged);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Unregister the callback so it doesn't run again if re-parented.
            UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            UpdateLabel();
        }

        private void OnValueChanged(ChangeEvent<float> evt)
        {
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (m_PercentageLabel == null)
                return;
            m_PercentageLabel.text = $"{Mathf.Round(ClampedValue() * 100)}%";
        }

        // Helper to get a normalized 0-1 value
        private float ClampedValue()
        {
            return (value - lowValue) / (highValue - lowValue);
        }

        void Draw(MeshGenerationContext ctx)
        {
            var curWidth = m_DrawingContainer.resolvedStyle.width;
            var curHeight = m_DrawingContainer.resolvedStyle.height;

            if (curWidth <= 0 || curHeight <= 0)
                return;

            var usableHeight = curHeight;
            var usableWidth = curWidth;
            var lineWidth = 0f;

            if (curWidth * widthToHeight >= curHeight)
            {
                lineWidth = curHeight / 3f;
                usableHeight = curHeight - lineWidth / 2;
                usableWidth = 2 * usableHeight;
            }
            else
            {
                lineWidth = (curWidth / 2) / 3f;
                usableWidth = curWidth - lineWidth;
                usableHeight = usableWidth / 2;
            }

            var horizontalPadding = (curWidth - usableWidth) / 2;

            var midPoint = new Vector2(horizontalPadding + usableWidth / 2, curHeight + m_PercentageLabel.resolvedStyle.height);
            var radius = usableHeight;

            var painter = ctx.painter2D;
            painter.lineWidth = lineWidth;
            painter.lineCap = LineCap.Butt;

            // --- Draw Foreground (Value) Arc ---
            painter.BeginPath();
            painter.strokeGradient = new Gradient()
            {
                colorKeys = new GradientColorKey[] {
                new GradientColorKey() { color = Color.red, time = 0.0f },
                new GradientColorKey() { color = Color.yellow, time = 0.5f },
                new GradientColorKey() { color = Color.green, time = 1.0f }
            }
            };
            // Draw the arc up to the current value
            painter.Arc(midPoint, radius, 180, 360);
            painter.Stroke();

            // --- Draw Background Arc ---
            painter.BeginPath();
            painter.strokeColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // A dark gray for the background
            painter.Arc(midPoint, radius, 180 + 180 * ClampedValue(), 360);
            painter.Stroke();

            // 1. Set needle properties
            painter.lineWidth = lineWidth / 8;
            painter.strokeColor = Color.black;
            painter.lineCap = LineCap.Round;

            // 2. Calculate needle angle
            // Map the 0-1 value to an angle from 180 degrees (left) to 0 degrees (right)
            float angleDegrees = 180f * (1f - ClampedValue());
            float angleRadians = angleDegrees * Mathf.Deg2Rad;

            // 3. Define needle length and direction
            float needleLength = radius * 0.9f; // Slightly shorter than the radius
            Vector2 direction = new Vector2(Mathf.Cos(angleRadians), -Mathf.Sin(angleRadians)); // Negative Sin because Y is inverted in UI

            Vector2 needleEnd = midPoint + direction * needleLength;

            // 4. Draw the needle line
            painter.BeginPath();
            painter.MoveTo(midPoint);
            painter.LineTo(needleEnd);
            painter.Stroke();

        }
    }
}