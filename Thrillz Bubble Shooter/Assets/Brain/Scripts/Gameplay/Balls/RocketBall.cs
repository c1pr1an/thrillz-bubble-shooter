using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Rocket bonus ball.
    /// Rocket balls destroy balls in the shooting direction (2 per row, max 4 rows).
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class RocketBall : MonoBehaviour
    {
        private Ball _ball;

        [Header("Visual Settings")]
        [SerializeField] private bool _enableThrustEffect = true;
        [SerializeField] private float _rotationSpeed = 90f;
        [SerializeField] private float _thrustPulseSpeed = 3f;

        [Header("Rocket Settings")]
        [SerializeField] private int _ballsPerRow = 2; // Balls to destroy per row
        [SerializeField] private int _maxRows = 4; // Maximum rows to clear

        private Transform _transform;
        private SpriteRenderer _spriteRenderer;
        private float _thrustTimer = 0f;
        private Vector2 _lastVelocity; // Store the last velocity for impact direction

        private void Awake()
        {
            _ball = GetComponent<Ball>();
            _transform = transform;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // Optional thrust visual effect
            if (_enableThrustEffect && _spriteRenderer != null)
            {
                // Rotation effect (like a spinning rocket)
                if (_ball != null && !_ball.HasFlag(BallFlags.Pinned))
                {
                    _transform.Rotate(0, 0, _rotationSpeed * Time.deltaTime);
                }

                // Thrust pulse effect
                _thrustTimer += _thrustPulseSpeed * Time.deltaTime;
                float thrust = 1f + Mathf.Sin(_thrustTimer) * 0.1f;

                // Apply red-orange rocket color with thrust effect
                Color rocketColor = Color.Lerp(Color.red, new Color(1f, 0.5f, 0f), thrust - 0.9f);
                rocketColor *= thrust;
                _spriteRenderer.color = rocketColor;
            }
        }

        /// <summary>
        /// Check if this is a rocket ball
        /// </summary>
        public bool IsRocket()
        {
            return enabled && _ball != null;
        }

        /// <summary>
        /// Get the number of balls to destroy per row
        /// </summary>
        public int GetBallsPerRow()
        {
            return _ballsPerRow;
        }

        /// <summary>
        /// Get the maximum number of rows to clear
        /// </summary>
        public int GetMaxRows()
        {
            return _maxRows;
        }

        /// <summary>
        /// Store the velocity for impact direction calculation
        /// </summary>
        public void SetLastVelocity(Vector2 velocity)
        {
            _lastVelocity = velocity;
        }

        /// <summary>
        /// Get the last velocity (impact direction)
        /// </summary>
        public Vector2 GetLastVelocity()
        {
            return _lastVelocity;
        }

        private void OnEnable()
        {
            Debug.Log($"[RocketBall] Rocket ball activated! Clears {_ballsPerRow} balls per row, max {_maxRows} rows");
        }

        private void OnDisable()
        {
            // Reset visual state when disabled
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
            if (_transform != null)
            {
                _transform.rotation = Quaternion.identity;
            }
            _thrustTimer = 0f;
            _lastVelocity = Vector2.zero;
        }
    }
}