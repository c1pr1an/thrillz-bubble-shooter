using System.Collections.Generic;
using UnityEngine;
using Brain.Managers;
using DG.Tweening;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Rocket bonus ball.
    /// Rocket balls destroy balls in a runway pattern in the shooting direction.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class RocketBall : MonoBehaviour, IBonusBall
    {
        private Ball _ball;

        [Header("Visual Settings")]
        [SerializeField] private Transform _modelTransform;
        [SerializeField] private Transform _rocketParticles;
        [SerializeField] private Transform _rocketParticlesIdle;
        [SerializeField] private float _rotationSmoothness = 10f; // Smoothness for rotation transitions
        [SerializeField] private bool _instantBounceRotation = true; // Instant rotation on bounce

        [Header("Rocket Settings")]
        [SerializeField] private float _runwayLength;
        [SerializeField] private float _runwayWidth;

        private float _thrustTimer = 0f;
        private Vector2 _lastVelocity; // Store the last velocity for impact direction

        // Launcher rotation tracking
        private bool _isInLauncher = false;
        private bool _isAiming = false;
        private Quaternion _targetRotation = Quaternion.identity;
        private bool _isFlying = false;
        private static readonly Quaternion _defaultRotation = Quaternion.Euler(0, 0, -45f);

        private void Awake()
        {
            _ball = GetComponent<Ball>();

            // Set initial default rotation
            _modelTransform.rotation = _defaultRotation;
            _targetRotation = _defaultRotation;
        }

        private void Update()
        {
            // Handle rotation based on state
            if (_isInLauncher)
            {
                // In launcher: follow aim direction or stay at default
                if (_isAiming)
                {
                    // Smoothly rotate to target rotation
                    _modelTransform.rotation = Quaternion.Slerp(_modelTransform.rotation, _targetRotation,
                        _rotationSmoothness * Time.deltaTime);
                }
                else
                {
                    // Return to default rotation when not aiming
                    _modelTransform.rotation = Quaternion.Slerp(_modelTransform.rotation, _defaultRotation,
                        _rotationSmoothness * Time.deltaTime);
                }
            }
            else if (_isFlying)
            {
                // During flight: rotation is handled by BallLaunch component
                // Just apply smooth rotation to target
                _modelTransform.rotation = Quaternion.Slerp(_modelTransform.rotation, _targetRotation,
                    _rotationSmoothness * Time.deltaTime);
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

        /// <summary>
        /// Set whether the rocket is in the launcher container
        /// </summary>
        public void SetInLauncher(bool inLauncher)
        {
            _isInLauncher = inLauncher;
            _rocketParticlesIdle.gameObject.SetActive(inLauncher);
            if (!inLauncher)
            {
                _isAiming = false;
            }
        }

        /// <summary>
        /// Update the rocket's rotation to point in a specific direction (for aiming)
        /// </summary>
        public void SetAimDirection(Vector2 direction, bool isAiming)
        {
            _isAiming = isAiming;

            if (isAiming && direction != Vector2.zero)
            {
                // Calculate rotation angle from direction
                // -90 degrees offset because sprites usually face upward by default
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                _targetRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>
        /// Set the rocket to flying state and update rotation for flight direction
        /// </summary>
        public void SetFlying(bool flying)
        {
            _rocketParticles.gameObject.SetActive(flying);
            _isFlying = flying;
            _isInLauncher = false;
            _isAiming = false;
        }

        /// <summary>
        /// Update rotation during flight to match movement direction
        /// </summary>
        public void UpdateFlightRotation(Vector2 direction, bool isBounce = false)
        {
            if (_isFlying && direction != Vector2.zero)
            {
                // Calculate rotation angle from direction
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                _targetRotation = Quaternion.Euler(0, 0, angle);

                // Apply instant rotation on bounce if enabled
                if (isBounce && _instantBounceRotation)
                {
                    _modelTransform.rotation = _targetRotation;
                }
            }
        }

        #region IBonusBallDetector Implementation

        /// <summary>
        /// Get all balls that would be affected by the rocket's runway pattern
        /// </summary>
        public List<Ball> GetAffectedBalls(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            List<Ball> affectedBalls = new List<Ball>();

            // Use stored velocity if no direction provided
            if (impactDirection == default)
                impactDirection = _lastVelocity;

            // If still no direction, can't determine path
            if (impactDirection == Vector2.zero)
            {
                Debug.LogWarning("[RocketBall] No impact direction available!");
                return affectedBalls;
            }

            // Normalize direction
            impactDirection = impactDirection.normalized;

            // Get grid manager for ball dimensions
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
                return affectedBalls;

            // Calculate runway dimensions
            float ballDiameter = gridManager.BallWidth;
            float ballHeight = gridManager.BallHeight;
            float runwayLength = ballHeight * _runwayLength;
            float runwayWidth = ballDiameter * _runwayWidth;

            // Calculate the center of the runway (offset forward from impact position)
            Vector2 runwayCenter = impactPosition + (impactDirection * runwayLength * 0.5f);

            // Calculate rotation angle for the box
            float angle = Mathf.Atan2(impactDirection.y, impactDirection.x) * Mathf.Rad2Deg;

            // Use OverlapBox to detect all balls in the runway area
            int ballLayer = LayerMask.NameToLayer("Default");
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(1 << ballLayer);
            contactFilter.useLayerMask = true;

            // Get all colliders in the runway area
            Collider2D[] hitColliders = new Collider2D[100];
            Vector2 boxSize = new Vector2(runwayLength, runwayWidth);
            int numHits = Physics2D.OverlapBox(runwayCenter, boxSize, angle, contactFilter, hitColliders);

            // Process all hit balls
            for (int i = 0; i < numHits; i++)
            {
                if (hitColliders[i] != null)
                {
                    Ball ball = hitColliders[i].GetComponent<Ball>();

                    // Check if it's a valid ball (not the rocket itself, pinned, and not already destroying)
                    if (ball != null && ball != _ball &&
                        ball.HasFlag(BallFlags.Pinned) && !ball.HasFlag(BallFlags.Destroying))
                    {
                        // Additional check: make sure the ball is in front of the rocket
                        Vector2 toBall = (Vector2)ball.transform.position - impactPosition;
                        float dotProduct = Vector2.Dot(toBall.normalized, impactDirection);

                        // Only include balls that are in front (dot product > 0)
                        if (dotProduct > 0)
                        {
                            affectedBalls.Add(ball);
                            ball.Flags |= BallFlags.MarkedForMatch;
                        }
                    }
                }
            }

            // Add the rocket ball itself to be destroyed
            if (_ball != null && !affectedBalls.Contains(_ball))
            {
                affectedBalls.Add(_ball);
                _ball.Flags |= BallFlags.MarkedForMatch;
            }

            return affectedBalls;
        }

        /// <summary>
        /// Draw debug visualization for the rocket runway
        /// </summary>
        public void DrawDebugVisualization(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            // Use stored velocity if no direction provided
            if (impactDirection == default)
                impactDirection = _lastVelocity;

            // If still no direction, can't draw
            if (impactDirection == Vector2.zero)
                return;

            // Normalize direction
            impactDirection = impactDirection.normalized;

            // Get grid manager for ball dimensions
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
                return;

            // Calculate runway dimensions
            float ballDiameter = gridManager.BallWidth;
            float ballHeight = gridManager.BallHeight;
            float runwayLength = ballHeight * _runwayLength;
            float runwayWidth = ballDiameter * _runwayWidth;

            // Calculate the center of the runway
            Vector2 runwayCenter = impactPosition + (impactDirection * runwayLength * 0.5f);

            // Calculate rotation angle
            float angle = Mathf.Atan2(impactDirection.y, impactDirection.x) * Mathf.Rad2Deg;

            // Convert angle to rotation matrix
            float angleRad = angle * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Vector2 up = new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));

            // Calculate the four corners of the rotated box
            Vector2 halfSize = new Vector2(runwayLength * 0.5f, runwayWidth * 0.5f);
            Vector3[] corners = new Vector3[4];
            corners[0] = runwayCenter + right * halfSize.x + up * halfSize.y;
            corners[1] = runwayCenter - right * halfSize.x + up * halfSize.y;
            corners[2] = runwayCenter - right * halfSize.x - up * halfSize.y;
            corners[3] = runwayCenter + right * halfSize.x - up * halfSize.y;

            // Draw the box outline using Debug.DrawLine for runtime visualization
            Color redColor = Color.red;
            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(corners[i], corners[(i + 1) % 4], redColor);
            }

            // Draw diagonal lines to show it's a filled area
            Color orangeColor = new Color(1f, 0.5f, 0f, 0.5f);
            Debug.DrawLine(corners[0], corners[2], orangeColor);
            Debug.DrawLine(corners[1], corners[3], orangeColor);

            // Draw center point (approximated as a cross since Debug doesn't have DrawWireSphere)
            Color yellowColor = Color.yellow;
            Debug.DrawLine(runwayCenter + Vector2.left * 0.1f, runwayCenter + Vector2.right * 0.1f, yellowColor);
            Debug.DrawLine(runwayCenter + Vector2.up * 0.1f, runwayCenter + Vector2.down * 0.1f, yellowColor);

            // Draw direction arrow
            Vector3 arrowEnd = (Vector3)runwayCenter + (Vector3)(right * halfSize.x * 0.8f);
            Color greenColor = Color.green;
            Debug.DrawLine(runwayCenter, arrowEnd, greenColor);
        }

        #endregion

        /// <summary>
        /// Animate the rocket moving forward after impact
        /// </summary>
        public void AnimateForwardMovement()
        {
            Vector2 direction = _lastVelocity.normalized;
            float ballHeight = GridManager.Instance.BallHeight;
            float forwardDistance = ballHeight * _runwayLength;
            Vector3 targetPosition = transform.position + (Vector3)(direction * forwardDistance);

            transform.DOMove(targetPosition, 0.25f)
                .SetEase(Ease.Linear)
                .OnComplete(() => SetFlying(false));
        }

        private void OnEnable()
        {
            Debug.Log($"[RocketBall] Rocket ball activated! Runway: {_runwayLength} balls forward, {_runwayWidth} balls wide");
        }

        private void OnDisable()
        {
            _thrustTimer = 0f;
            _lastVelocity = Vector2.zero;

            // Reset launcher states
            _isInLauncher = false;
            _isAiming = false;
            _isFlying = false;
            _targetRotation = _defaultRotation;
        }
    }
}