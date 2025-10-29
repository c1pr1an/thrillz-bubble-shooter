using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Lightning bonus ball.
    /// Lightning balls destroy all balls horizontally (4 left and 4 right of impact).
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class LightningBall : MonoBehaviour
    {
        private Ball _ball;

        [Header("Visual Settings")]
        [SerializeField] private bool _enableElectricEffect = true;
        [SerializeField] private float _sparkInterval = 0.5f;
        [SerializeField] private float _glowIntensity = 1.5f;

        [Header("Lightning Settings")]
        [SerializeField] private int _horizontalRange = 4; // Balls to destroy left and right

        private SpriteRenderer _spriteRenderer;
        private float _sparkTimer = 0f;
        private float _glowTimer = 0f;

        private void Awake()
        {
            _ball = GetComponent<Ball>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // Optional electric visual effect
            if (_enableElectricEffect && _spriteRenderer != null)
            {
                // Glow effect
                _glowTimer += Time.deltaTime * 3f;
                float glow = 1f + Mathf.Sin(_glowTimer) * 0.2f * _glowIntensity;

                // Spark effect (periodic brightness spike)
                _sparkTimer += Time.deltaTime;
                if (_sparkTimer > _sparkInterval)
                {
                    _sparkTimer = 0f;
                    glow = _glowIntensity * 2f; // Brief flash
                }

                // Apply yellow-white electric color with glow
                Color electricColor = Color.Lerp(Color.yellow, Color.white, glow - 1f);
                electricColor *= glow;
                _spriteRenderer.color = electricColor;
            }
        }

        /// <summary>
        /// Check if this is a lightning ball
        /// </summary>
        public bool IsLightning()
        {
            return enabled && _ball != null;
        }

        /// <summary>
        /// Get the horizontal range (how many balls left and right)
        /// </summary>
        public int GetHorizontalRange()
        {
            return _horizontalRange;
        }

        private void OnEnable()
        {
            Debug.Log($"[LightningBall] Lightning ball activated! Horizontal range: {_horizontalRange} balls left and right");
        }

        private void OnDisable()
        {
            // Reset color when disabled
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
            _sparkTimer = 0f;
            _glowTimer = 0f;
        }
    }
}