using System;
using UnityEngine;
using Brain.Util;
using Brain.Gameplay.Containers;
using Brain.Gameplay;

namespace Brain.Managers
{
    public enum InputState
    {
        Idle,
        Aiming,
        Cancelled
    }

    /// <summary>
    /// Centralized input manager that handles both mouse and touch inputs
    /// Provides unified events for game interactions
    /// </summary>
    public class InputManager : UnitySingleton<InputManager>
    {
        // Events for different input actions
        public static event Action<Vector2> OnAimingStarted;
        public static event Action<Vector2> OnAimingUpdated;
        public static event Action<Vector2> OnAimingReleased;
        public static event Action OnAimingCancelled;
        public static event Action<BallPreviewContainer> OnPreviewContainerClicked;
        public static event Action<BonusBallContainer> OnBonusContainerClicked;

        private InputState _currentState = InputState.Idle;
        private bool _wasHolding = false;
        private Vector2 _currentInputPosition;

        // Cached references to containers for click detection
        private BallPreviewContainer _previewContainer;
        private BonusBallContainer _bonusContainer;

        public void Init(BallPreviewContainer _previewContainer, BonusBallContainer _bonusContainer)
        {
            this._previewContainer = _previewContainer;
            this._bonusContainer = _bonusContainer;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            bool isInputActive = false;
            Vector2 inputPosition = Vector2.zero;

            // Check for mouse input (desktop)
            if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                Vector3 mouseWorldPos = Cameras.Instance.MainCam.ScreenToWorldPoint(Input.mousePosition);
                inputPosition = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
                isInputActive = Input.GetMouseButton(0);
            }
            // Check for touch input (mobile)
            else if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector3 touchWorldPos = Cameras.Instance.MainCam.ScreenToWorldPoint(touch.position);
                inputPosition = new Vector2(touchWorldPos.x, touchWorldPos.y);
                isInputActive = (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled);
            }

            // Only update current position if we have valid input
            if (inputPosition != Vector2.zero)
            {
                _currentInputPosition = inputPosition;
            }

            // Handle input state transitions
            if (isInputActive && !_wasHolding)
            {
                // Input just started
                OnInputDown(_currentInputPosition);
            }
            else if (isInputActive && _wasHolding)
            {
                // Input is being held
                OnInputHold(_currentInputPosition);
            }
            else if (!isInputActive && _wasHolding)
            {
                // Input just released - use the last valid position
                OnInputUp(_currentInputPosition);
            }

            _wasHolding = isInputActive;
        }

        private void OnInputDown(Vector2 position)
        {
            float limitLineY = GetLimitLineY();

            if (position.y < limitLineY)
            {
                // Below limit line - check for container clicks
                CheckContainerClicks(position);
            }
            else
            {
                // Above limit line - start aiming
                _currentState = InputState.Aiming;
                OnAimingStarted?.Invoke(position);
            }
        }

        private void OnInputHold(Vector2 position)
        {
            if (_currentState == InputState.Aiming)
            {
                float limitLineY = GetLimitLineY();

                if (position.y >= limitLineY)
                {
                    // Still above limit line - continue aiming
                    OnAimingUpdated?.Invoke(position);
                }
                else
                {
                    // Crossed below limit line - cancel aiming
                    _currentState = InputState.Cancelled;
                    OnAimingCancelled?.Invoke();
                }
            }
            else if (_currentState == InputState.Cancelled)
            {
                // Stay cancelled until input is released
                // Do nothing
            }
        }

        private void OnInputUp(Vector2 position)
        {
            if (_currentState == InputState.Aiming)
            {
                float limitLineY = GetLimitLineY();

                if (position.y >= limitLineY)
                {
                    // Released above limit line - shoot!
                    OnAimingReleased?.Invoke(position);
                }
                // If below limit line, do nothing (cancelled)
            }

            // Reset state
            _currentState = InputState.Idle;
        }

        private void CheckContainerClicks(Vector2 position)
        {
            // Perform a raycast and get all hits to handle overlapping colliders
            RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.zero);

            // Check all hits for containers
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                // Check if we hit the preview container or its child ball
                if (_previewContainer != null)
                {
                    if (hit.collider.gameObject == _previewContainer.gameObject ||
                        hit.collider.transform.IsChildOf(_previewContainer.transform))
                    {
                        OnPreviewContainerClicked?.Invoke(_previewContainer);
                        return; // Found container, stop checking
                    }
                }

                // Check if we hit the bonus container or its child ball
                if (_bonusContainer != null)
                {
                    if (hit.collider.gameObject == _bonusContainer.gameObject ||
                        hit.collider.transform.IsChildOf(_bonusContainer.transform))
                    {
                        OnBonusContainerClicked?.Invoke(_bonusContainer);
                        return; // Found container, stop checking
                    }
                }
            }
        }

        private float GetLimitLineY()
        {
            // Get limit line Y position from GridManager
            if (GridManager.Instance != null && GridManager.Instance.LimitLine != null)
            {
                return GridManager.Instance.LimitLine.position.y;
            }

            // Fallback to a default value if not set
            return -6f; // Adjust this default based on your scene
        }

        /// <summary>
        /// Get current input position (for trajectory calculation)
        /// </summary>
        public Vector2 GetCurrentInputPosition()
        {
            return _currentInputPosition;
        }

        /// <summary>
        /// Check if currently in aiming state
        /// </summary>
        public bool IsAiming()
        {
            return _currentState == InputState.Aiming;
        }
    }
}