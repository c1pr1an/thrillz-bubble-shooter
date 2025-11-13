using System.Collections.Generic;
using System.Linq;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;
using UnityEngine.Events;

namespace Brain.Managers
{
    public class ColorTrackerManager : UnitySingleton<ColorTrackerManager>
    {
        // Events
        public UnityAction<BallColor> OnColorExhausted;
        public UnityAction OnAllColorsExhausted;

        // Configuration (hardcoded)
        private const int RECENT_COLOR_MEMORY = 2;
        private const int MIN_COLORS_FOR_RANDOM = 3;

        // Color tracking
        private Dictionary<BallColor, int> _colorCounts = new Dictionary<BallColor, int>();
        private Queue<BallColor> _recentColors = new Queue<BallColor>();

        /// <summary>
        /// Adds a color to tracking (called when ball added to grid)
        /// </summary>
        public void AddColor(BallColor color)
        {
            if (_colorCounts.ContainsKey(color))
            {
                _colorCounts[color]++;
            }
            else
            {
                _colorCounts[color] = 1;
            }
        }

        /// <summary>
        /// Removes a color from tracking (called when ball destroyed)
        /// </summary>
        public void RemoveColor(BallColor color)
        {
            if (!_colorCounts.ContainsKey(color))
            {
                Debug.LogWarning($"Attempted to remove color {color} that isn't being tracked");
                return;
            }

            _colorCounts[color]--;

            if (_colorCounts[color] <= 0)
            {
                _colorCounts.Remove(color);

                // Fire event when color is exhausted from grid
                OnColorExhausted?.Invoke(color);

                // Check if all colors are exhausted
                if (_colorCounts.Count == 0)
                {
                    OnAllColorsExhausted?.Invoke();
                }
            }
        }

        /// <summary>
        /// Tries to generate a color based on what's available in the grid
        /// </summary>
        public bool TryGenerateColor(out BallColor color)
        {
            color = BallColor.Yellow; // Default

            // No colors available
            if (_colorCounts.Count == 0)
            {
                return false;
            }

            // Only one color available - no choice
            if (_colorCounts.Count == 1)
            {
                color = _colorCounts.Keys.First();
                UpdateRecentColors(color);
                return true;
            }

            // Build weighted list (colors with more balls have higher chance)
            List<BallColor> weightedList = new List<BallColor>();
            foreach (var kvp in _colorCounts)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    weightedList.Add(kvp.Key);
                }
            }

            // If we have enough color variety, try to avoid recent colors
            if (_colorCounts.Count >= MIN_COLORS_FOR_RANDOM && _recentColors.Count >= RECENT_COLOR_MEMORY)
            {
                // Filter out recent colors if possible
                var filteredList = weightedList.Where(c => !_recentColors.Contains(c)).ToList();

                // Use filtered list if it has options, otherwise use full list
                if (filteredList.Count > 0)
                {
                    weightedList = filteredList;
                }
            }
            // Pick random color from weighted list
            int randomIndex = Random.Range(0, weightedList.Count);
            color = weightedList[randomIndex];

            UpdateRecentColors(color);
            return true;
        }

        /// <summary>
        /// Checks if a specific color is available in the grid
        /// </summary>
        public bool IsColorAvailable(BallColor color)
        {
            return _colorCounts.ContainsKey(color) && _colorCounts[color] > 0;
        }

        /// <summary>
        /// Checks if any colors are available
        /// </summary>
        public bool HasAvailableColors()
        {
            return _colorCounts.Count > 0;
        }

        /// <summary>
        /// Updates the queue of recently generated colors
        /// </summary>
        private void UpdateRecentColors(BallColor color)
        {
            _recentColors.Enqueue(color);

            // Keep queue at max size
            while (_recentColors.Count > RECENT_COLOR_MEMORY)
            {
                _recentColors.Dequeue();
            }
        }

        /// <summary>
        /// Gets the current color counts for debugging
        /// </summary>
        public Dictionary<BallColor, int> GetColorCounts()
        {
            return new Dictionary<BallColor, int>(_colorCounts);
        }
    }
}