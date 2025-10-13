using UnityEngine;
using UnityEngine.UIElements;

namespace TUDarmstadt.SeriousGames.UIElements
{
    public class CustomSlider : SliderInt
    {
        public new class UxmlFactory : UxmlFactory<CustomSlider, UxmlTraits> { }

        VisualElement _tracker;
        VisualElement _filledTracker;

        public CustomSlider() : base() => Initialize();
        public CustomSlider(int start, int end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = 0)
            : base(start, end, direction, pageSize) => Initialize();
        public CustomSlider(string label, int start = 0, int end = 10, SliderDirection direction = SliderDirection.Horizontal, float pageSize = 0)
            : base(label, start, end, direction, pageSize) => Initialize();

        void Initialize()
        {
            InitializeFilledBar();
            AddToClassList("custom-slider");
            var styleSheet = Resources.Load<StyleSheet>("de.tu-darmstadt.serious-games.uitoolkitelements/USS-Files/CustomSlider");
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
            this.RegisterValueChangedCallback(OnValueChanged);
        }

        private void OnValueChanged(ChangeEvent<int> evt)
        {
            float percent = ((float)evt.newValue) / this.highValue * 100;
            _filledTracker.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
        }

        void InitializeFilledBar()
        {
            _tracker = this.Q<VisualElement>("unity-tracker");
            if (_tracker == null)
                return;

            _filledTracker = new VisualElement { name = "filled-tracker" };
            _tracker.Add(_filledTracker);
        }
    }

}
