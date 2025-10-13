using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TUDarmstadt.SeriousGames.UIElements
{
    public class Radarchart : VisualElement
    {
        private class ValueLabel
        {
            public float Value;
            public VisualElement Container;
            public Label Label;
        }

        static readonly string backgroundUssClassName = "radarchart__background";
        static readonly string valueLabelContainerUssClassName = "radarchart__value-label-container";
        static readonly string valueLabelUssClassName = "radarchart__value-label";
        public new class UxmlFactory : UxmlFactory<Radarchart, UxmlTraits> { }
        private Dictionary<string, ValueLabel> m_Values = new();
        public float LowValue = 0;
        public float HighValue = 1;
        private PickingMode targetPickingMode = PickingMode.Position;

        public PickingMode TargetPickingMode
        {
            get => targetPickingMode;
            set
            {
                targetPickingMode = value;
                ChangePickingMode(this);
            }
        }

        private void ChangePickingMode(VisualElement element)
        {
            element.pickingMode = targetPickingMode;
            foreach (var child in element.Children())
            {
                ChangePickingMode(child);
            }
        }

        public Radarchart() : base() => Initialize();
        public void AddValuePair(string key, float value)
        {
            if (m_Values.ContainsKey(key))
                return;
            var container = new VisualElement();
            container.AddToClassList(valueLabelContainerUssClassName);
            var label = new Label(key);
            container.Add(label);
            label.AddToClassList(valueLabelUssClassName);
            this.Add(container);
            container.pickingMode = targetPickingMode;
            label.pickingMode = targetPickingMode;
            var valueLabel = new ValueLabel { Value = value, Container = container, Label = label };
            m_Values.Add(key, valueLabel);
            MarkDirtyRepaint();
        }
        public void RemoveKey(string key)
        {
            m_Values.Remove(key);
            MarkDirtyRepaint();
        }
        public void ChangeValue(string key, float value)
        {
            if (m_Values.ContainsKey(key))
            {
                m_Values[key].Value = value;
            }
        }
        private void Initialize()
        {
            var styleSheet = Resources.Load<StyleSheet>("de.tu-darmstadt.serious-games.uitoolkitelements/USS-Files/Radarchart");
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
            this.AddToClassList(backgroundUssClassName);


            this.generateVisualContent += Draw;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Now bounds are valid
            schedule.Execute(() => MarkDirtyRepaint()).StartingIn(0);
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        // Helper to get a normalized 0-1 value
        private float ClampedValue(float value)
        {
            return (value - LowValue) / (HighValue - LowValue);
        }
        void Draw(MeshGenerationContext ctx)
        {
            if (m_Values?.Count < 3)
                return;
            var angle_dif = 2 * Mathf.PI / m_Values.Count;
            var borderWidth = this.resolvedStyle.borderLeftWidth;
            var maxWidth = this.resolvedStyle.width - 2 * borderWidth;
            var maxHeight = this.resolvedStyle.height - 2 * borderWidth;
            var smallerSide = 0f;
            if (maxWidth >= maxHeight)
            {
                smallerSide = maxHeight;
            }
            else
            {
                smallerSide = maxWidth;
            }

            var midpoint = new Vector2(borderWidth + maxWidth / 2, borderWidth + maxHeight / 2);
            var maxRadius = smallerSide / 2;
            List<Vector2> directions = new List<Vector2>();
            for (int i = 0; i < m_Values.Count; i++)
            {
                var item = m_Values.ElementAt(i);
                var angle = angle_dif * i - Mathf.PI / 2;
                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);
                var direction = new Vector2(x, y);
                directions.Add(direction);
                maxRadius = Mathf.Min(maxRadius, FindMaxRadius(midpoint, direction, this.localBound, item.Value));
            }
            for (int i = 0; i < m_Values.Count; i++)
            {
                var item = m_Values.ElementAt(i);
                var target = midpoint + directions[i] * maxRadius;
                item.Value.Container.style.left = target.x;
                item.Value.Container.style.top = target.y;
            }

            maxRadius = maxRadius * 0.99f;

            var lineWidth = maxRadius / 40;
            var painter = ctx.painter2D;


            painter.lineWidth = lineWidth / 2;

            foreach (var direction in directions)
            {
                painter.BeginPath();
                painter.MoveTo(midpoint);
                painter.LineTo(midpoint + direction * maxRadius);
                painter.Stroke();
            }

            maxRadius = maxRadius * 0.9f;

            painter.BeginPath();
            painter.Arc(midpoint, lineWidth, 0, 360);
            painter.Fill();
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                painter.BeginPath();
                painter.Arc(midpoint, maxRadius * quarter / 4, 0, 360);
                painter.Stroke();
            }
            painter.fillColor = new Color(0.2f, 0.2f, 0.2f, 0.2f);
            painter.Fill();


            painter.lineWidth = lineWidth;
            painter.BeginPath();
            for (int i = 0; i < directions.Count; i++)
            {
                Vector2 direction = directions[i];
                var item = m_Values.ElementAt(i);
                var point = midpoint + direction * maxRadius * ClampedValue(item.Value.Value);
                if (i == 0)
                {
                    painter.MoveTo(point);
                }
                else
                {
                    painter.LineTo(point);
                }
            }
            painter.ClosePath();
            painter.Stroke();
            painter.fillColor = new Color(0.2f, 0.53f, 0.94f, 0.6f);
            painter.Fill();
        }

        private float FindMaxRadius(Vector2 midpoint, Vector2 direction, Rect outerBounds, ValueLabel valueLabel)
        {
            var labelBounds = valueLabel.Label.localBound;
            float radius = float.MaxValue;
            bool checkX = Mathf.Abs(direction.x) > 0.01f;
            bool checkY = Mathf.Abs(direction.y) > 0.01f;
            if (checkX)
            {
                bool isLeft = direction.x < 0;
                var x = outerBounds.x;
                if (isLeft)
                {
                    x = labelBounds.width;
                    valueLabel.Container.style.alignItems = Align.FlexEnd;
                }
                else
                {
                    x = outerBounds.width - labelBounds.width;
                    valueLabel.Container.style.alignItems = Align.FlexStart;
                }
                var t = (x - midpoint.x) / direction.x;
                radius = Mathf.Min(radius, (direction * t).magnitude);
            }
            else
            {
                valueLabel.Container.style.alignItems = Align.Center;
            }
            if (checkY)
            {
                bool isUp = direction.y < 0;
                var y = outerBounds.y;
                if (isUp)
                {
                    y = labelBounds.height;
                    valueLabel.Container.style.justifyContent = Justify.FlexEnd;
                }
                else
                {
                    y = outerBounds.height - labelBounds.height;
                    valueLabel.Container.style.justifyContent = Justify.FlexStart;
                }
                var t = (y - midpoint.y) / direction.y;
                radius = Mathf.Min(radius, (direction * t).magnitude);
            }
            else
            {
                valueLabel.Container.style.justifyContent = Justify.Center;
            }

            return radius;
        }
    }
}