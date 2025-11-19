using System;
using UnityEngine;
using Brain.Util;
using Brain.Gameplay.Containers;
using Brain.Gameplay;

using UnityEngine.EventSystems;

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
        private bool _inputBlockedByUI = false;

        // Cached references to containers for click detection
        private BallPreviewContainer _previewContainer;
        private BonusBallContainer _bonusContainer;

        // Cached colliders for optimization
        private Collider2D _previewCollider;
        private Collider2D _bonusCollider;

        public void Init(BallPreviewContainer _previewContainer, BonusBallContainer _bonusContainer)
        {
            this._previewContainer = _previewContainer;
            this._bonusContainer = _bonusContainer;

            if (this._previewContainer != null)
                _previewCollider = this._previewContainer.GetComponent<Collider2D>();

            if (this._bonusContainer != null)
                _bonusCollider = this._bonusContainer.GetComponent<Collider2D>();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            bool isInputActive = false;
            Vector2 inputPosition = Vector2.zero;
            bool isPointerOverUI = false;
            bool isNewInput = false;

            // Prioritize Touch input (mobile)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    isPointerOverUI = true;
                }

                Vector3 touchWorldPos = Cameras.Instance.MainCam.ScreenToWorldPoint(touch.position);
                inputPosition = new Vector2(touchWorldPos.x, touchWorldPos.y);
                isInputActive = (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled);
                isNewInput = (touch.phase == TouchPhase.Began);
            }
            // Check for mouse input (desktop)
            else if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    isPointerOverUI = true;
                }

                Vector3 mouseWorldPos = Cameras.Instance.MainCam.ScreenToWorldPoint(Input.mousePosition);
                inputPosition = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
                isInputActive = Input.GetMouseButton(0);
                isNewInput = Input.GetMouseButtonDown(0);
            }

            // If input just started and we are over UI, block this entire input session
            if (isNewInput && isPointerOverUI)
            {
                _inputBlockedByUI = true;
            }

            // If physical input stopped, reset the block
            // We check the raw isInputActive (before blocking logic) to know if physical input ended
            if (!isInputActive && !isNewInput)
            {
                _inputBlockedByUI = false;
            }

            // If blocked by UI, force input inactive
            if (_inputBlockedByUI)
            {
                isInputActive = false;
            }

            // If we are over UI and we weren't already holding, ignore this input
            // This prevents starting a drag/aim when clicking on UI buttons
            if (isPointerOverUI && !_wasHolding)
            {
                isInputActive = false;
                // We don't update inputPosition here to avoid jumping
            }

            // Only update current position if we have valid input and it's not blocked by UI
            if (inputPosition != Vector2.zero && (!isPointerOverUI || _wasHolding) && !_inputBlockedByUI)
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

        private GameObject _initialDownObject;

        private void OnInputDown(Vector2 position)
        {
            // Store what we initially clicked on
            _initialDownObject = GetObjectAtPosition(position);

            // Always start aiming on input down
            _currentState = InputState.Aiming;
            OnAimingStarted?.Invoke(position);
        }

        private void OnInputHold(Vector2 position)
        {
            if (_currentState == InputState.Aiming)
            {
                // Always update aiming while holding
                OnAimingUpdated?.Invoke(position);
            }
            else if (_currentState == InputState.Cancelled)
            {
                // Stay cancelled until input is released
                // Do nothing
            }
        }

        private void OnInputUp(Vector2 position)
        {
            // Check for container clicks first
            if (CheckContainerClicks(position))
            {
                // If a container was clicked, we don't shoot
                _currentState = InputState.Idle;
                OnAimingCancelled?.Invoke();
                _initialDownObject = null;
                return;
            }

            if (_currentState == InputState.Aiming)
            {
                // Released - shoot!
                OnAimingReleased?.Invoke(position);
            }

            // Reset state
            _currentState = InputState.Idle;
            _initialDownObject = null;
        }

        private bool CheckContainerClicks(Vector2 position)
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
                        // Only trigger if we started clicking on this container
                        if (_initialDownObject == _previewContainer.gameObject ||
                            (_initialDownObject != null && _initialDownObject.transform.IsChildOf(_previewContainer.transform)))
                        {
                            OnPreviewContainerClicked?.Invoke(_previewContainer);
                            return true; // Found container
                        }
                    }
                }

                // Check if we hit the bonus container or its child ball
                if (_bonusContainer != null)
                {
                    if (hit.collider.gameObject == _bonusContainer.gameObject ||
                        hit.collider.transform.IsChildOf(_bonusContainer.transform))
                    {
                        // Only trigger if we started clicking on this container
                        if (_initialDownObject == _bonusContainer.gameObject ||
                            (_initialDownObject != null && _initialDownObject.transform.IsChildOf(_bonusContainer.transform)))
                        {
                            OnBonusContainerClicked?.Invoke(_bonusContainer);
                            return true; // Found container
                        }
                    }
                }
            }

            return false;
        }

        private GameObject GetObjectAtPosition(Vector2 position)
        {
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero);
            return hit.collider != null ? hit.collider.gameObject : null;
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
        /// Check if the pointer is currently over any interactive container
        /// </summary>
        public bool IsPointerOverContainer(Vector2 position)
        {
            // Optimization: Containers are always below the limit line
            if (position.y >= GetLimitLineY())
            {
                return false;
            }

            // Check overlap with preview container
            if (_previewCollider != null && _previewCollider.OverlapPoint(position))
            {
                return true;
            }

            // Check overlap with bonus container
            if (_bonusCollider != null && _bonusCollider.OverlapPoint(position))
            {
                return true;
            }

            return false;
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