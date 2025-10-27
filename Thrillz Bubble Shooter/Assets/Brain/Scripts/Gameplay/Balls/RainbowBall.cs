using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Rainbow bonus ball.
    /// Rainbow balls match with ANY color they touch.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class RainbowBall : MonoBehaviour
    {
        private Ball _ball;

        [Header("Visual Settings")]
        [SerializeField] private bool _enableRainbowEffect = true;
        [SerializeField] private float _colorCycleSpeed = 2f;

        private SpriteRenderer _spriteRenderer;
        private float _hue = 0f;

        private void Awake()
        {
            _ball = GetComponent<Ball>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // Optional rainbow color cycling effect
            if (_enableRainbowEffect && _spriteRenderer != null)
            {
                _hue += _colorCycleSpeed * Time.deltaTime;
                if (_hue > 1f) _hue -= 1f;

                Color rainbowColor = Color.HSVToRGB(_hue, 0.7f, 1f);
                _spriteRenderer.color = rainbowColor;
            }
        }

        /// <summary>
        /// Check if this ball should match with another ball regardless of color
        /// </summary>
        public bool CanMatchAnyColor()
        {
            return enabled && _ball != null;
        }

        private void OnEnable()
        {
            Debug.Log("[RainbowBall] Rainbow ball activated!");
        }

        private void OnDisable()
        {
            // Reset color when disabled
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
        }
    }
}