using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TUDarmstadt.SeriousGames.UIElements
{
    public class CenteredScrollView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<CenteredScrollView, UxmlTraits> { }

        //private readonly List<LabelWithResolvedHeight> _labels = new List<LabelWithResolvedHeight>();
        private readonly List<VisualElementWithResolvedHeight> _choices = new List<VisualElementWithResolvedHeight>();
        private readonly List<VisualElement> _carouselPoints = new List<VisualElement>();
        private int _selectedIndex;
        public int SelectedIndex => _selectedIndex;
        public float Spacing = 10f;
        //private List<string> _texts;
        private List<VisualElement> _rawChoices;
        public Action<int> Textchosen;

        private VisualElement _answerContainer;
        private VisualElement _carouselContainer;

        public CenteredScrollView()
        {
            var styleSheet = Resources.Load<StyleSheet>("SG_AG/USS-Files/CenteredScrollViewStyles");
            styleSheets.Add(styleSheet);
            style.flexDirection = FlexDirection.Row;

            _answerContainer = new VisualElement { name = "answer-container" };
            _answerContainer.AddToClassList("answer-container");
            Add(_answerContainer);

            _carouselContainer = new VisualElement { name = "carousel-container" };
            _carouselContainer.AddToClassList("carousel-container");
            Add(_carouselContainer);

            var previousButton = CreateChoiceButton("choice-button", "choice-button--rotate-left");
            previousButton.clicked += PreviousIndex;
            _carouselContainer.Add(previousButton);

            // Create carousel points.
            for (int i = 0; i < 50; i++)
            {
                var point = new VisualElement { name = "carousel-point" };
                point.AddToClassList("carousel-point");
                point.style.display = DisplayStyle.None;
                var index = i;
                point.RegisterCallback<MouseDownEvent>((_) =>
                {
                    if (_selectedIndex < index)
                    {
                        this.schedule.Execute(NextIndex).Every(75).Until(() => index <= _selectedIndex);
                    }
                    else
                    {
                        this.schedule.Execute(PreviousIndex).Every(75).Until(() => index >= _selectedIndex);
                    }
                });
                _carouselPoints.Add(point);
                _carouselContainer.Add(point);
            }

            var nextButton = CreateChoiceButton("choice-button", "choice-button--rotate-right");
            nextButton.clicked += NextIndex;
            _carouselContainer.Add(nextButton);

            RegisterCallback<WheelEvent>(OnScroll);
        }

        public void NextIndex()
        {
            if (_selectedIndex < _choices.Count - 1)
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex + 1, 0, _choices.Count - 1);
                UpdateVisuals();
            }
        }

        public void PreviousIndex()
        {
            if (_selectedIndex > 0)
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex - 1, 0, _choices.Count - 1);
                UpdateVisuals();
            }
        }

        private Button CreateChoiceButton(string baseClass, string rotationClass)
        {
            var button = new Button();
            button.AddToClassList(baseClass);
            button.AddToClassList(rotationClass);
            return button;
        }

        public void SetChoices(List<VisualElement> visualElements)
        {
            // Hide all carousel points.
            foreach (var point in _carouselPoints)
            {
                point.style.display = DisplayStyle.None;
            }
            // Show only the points corresponding to texts.
            for (int i = 0; i < visualElements.Count; i++)
            {
                _carouselPoints[i].style.display = DisplayStyle.Flex;
            }
            // Remove previous labels.
            foreach (var labelEntry in _choices)
            {
                _answerContainer.Remove(labelEntry.Element);
            }
            _choices.Clear();

            _rawChoices = visualElements;
            UpdateChoices();
        }


        private void UpdateChoices()
        {
            if (_rawChoices == null)
                return;

            StyleList<TimeValue> transitionDuration = null;
            for (int i = 0; i < _rawChoices.Count; i++)
            {
                var choice = _rawChoices[i];
                choice.AddToClassList("picker-item");
                int index = i;
                choice.RegisterCallback<MouseDownEvent>(_ =>
                {
                    if (index != _selectedIndex)
                    {
                        _selectedIndex = index;
                        UpdateVisuals();
                    }
                    else
                    {
                        Textchosen?.Invoke(index);
                    }
                }, TrickleDown.TrickleDown);
                if (transitionDuration == null)
                {
                    transitionDuration = choice.style.transitionDuration;
                }
                choice.style.transitionDuration = StyleKeyword.Initial;
                _choices.Add(new VisualElementWithResolvedHeight(choice, 0));
                _answerContainer.Add(choice);
            }
            _selectedIndex = _choices.Count / 2;
            style.opacity = 0f;
            schedule.Execute(() =>
            {
                UpdateResolvedHeights();
                UpdateVisuals();
                style.opacity = 1f;
                foreach (var entry in _choices)
                {
                    entry.Element.style.transitionDuration = transitionDuration;
                }
            }).ExecuteLater(10);
        }

        private void UpdateResolvedHeights()
        {
            foreach (var entry in _choices)
            {
                entry.Height = entry.Element.resolvedStyle.height;
            }
        }

        private void OnScroll(WheelEvent evt)
        {
            evt.StopPropagation();
            if (evt.delta.y > 0)
                NextIndex();
            else
                PreviousIndex();
        }

        private void UpdateVisuals()
        {
            float containerCenter = resolvedStyle.height / 2f;
            var selectedEntry = _choices[_selectedIndex];
            var selectedLabel = selectedEntry.Element;
            float selectedHeight = selectedEntry.Height;
            float selectedTop = containerCenter - selectedHeight * 0.5f;

            // Update carousel points.
            foreach (var point in _carouselPoints)
            {
                point.RemoveFromClassList("carousel-point--selected");
            }
            _carouselPoints[_selectedIndex].AddToClassList("carousel-point--selected");

            // Update labels.
            foreach (var entry in _choices)
            {
                entry.Element.RemoveFromClassList("picker-item--selected");
            }
            selectedLabel.style.top = selectedTop;
            selectedLabel.AddToClassList("picker-item--selected");
            selectedLabel.style.opacity = 1f;
            selectedLabel.style.scale = new Scale(new Vector2(1, 1));
            selectedLabel.BringToFront();

            // Update labels above.
            float lastPosition = selectedTop;
            for (int i = _selectedIndex - 1; i >= 0; i--)
            {
                lastPosition -= (_choices[i].Height + Spacing) * 0.8f;
                UpdateLabelStyle(_choices[i].Element, lastPosition, i);
            }

            // Update labels below.
            lastPosition = selectedTop;
            for (int i = _selectedIndex + 1; i < _choices.Count; i++)
            {
                lastPosition += (_choices[i - 1].Height + Spacing) * 0.8f;
                UpdateLabelStyle(_choices[i].Element, lastPosition, i);
            }
        }

        private void UpdateLabelStyle(VisualElement label, float topPosition, int index)
        {
            label.style.top = topPosition;
            float alpha = 1f - (Mathf.Abs(index - _selectedIndex) * 0.3f);
            label.style.opacity = Mathf.Clamp01(alpha);
            label.style.scale = new Scale(new Vector2(alpha, alpha));
        }

        private class VisualElementWithResolvedHeight
        {
            public VisualElement Element;
            public float Height;
            public VisualElementWithResolvedHeight(VisualElement element, float height)
            {
                this.Element = element;
                this.Height = height;
            }
        }
        private class LabelWithResolvedHeight
        {
            public Label Label;
            public float Height;
            public LabelWithResolvedHeight(Label label, float height)
            {
                this.Label = label;
                this.Height = height;
            }
        }
    }
}