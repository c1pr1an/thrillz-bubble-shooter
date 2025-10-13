// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using BubbleShooterGameToolkit.Scripts.Audio;
using BubbleShooterGameToolkit.Scripts.Enums;
using BubbleShooterGameToolkit.Scripts.Gameplay.BubbleContainers;
using BubbleShooterGameToolkit.Scripts.Gameplay.GUI;
using BubbleShooterGameToolkit.Scripts.Gameplay.PlayObjects;
using BubbleShooterGameToolkit.Scripts.System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace BubbleShooterGameToolkit.Scripts.Gameplay.Managers
{
    /// Input manager detects user input, launches the ball and debug stuff
    public partial class LevelManager
    {
        [SerializeField] public LaunchContainer launchContainer;
        [SerializeField] public BallContainerSpawn ballContainerSpawn;
        [SerializeField] private RectTransform switchRect;
        [SerializeField] private AimLine aimLine;
        private Camera uiCamera;
        private RaycastManager raycastManager;
        private RaycastData raycastData;
        private int num;
        private bool isTouchStarted;
        private bool isDragging;
        private int activeTouchId = -1;
        private VirtualMouseInput virtualMouseInput;
        private bool wasVirtualMousePressed;

        public override void Awake()
        {
            base.Awake();
            Canvas canvas = switchRect.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                uiCamera = canvas.worldCamera;
            }
            raycastManager = new RaycastManager();
            
            // Try to find a UIInputModule to get virtual mouse
            virtualMouseInput = FindObjectOfType<VirtualMouseInput>();
        }
        private void Switch()
        {
            ballContainerSpawn.SwitchBalls();
        }

        private void Update()
        {
            if ((EventManager.GameStatus != EStatus.Play && EventManager.GameStatus != EStatus.Tutorial) || 
                MovesTimeManager.instance.GetMoves() <= 0)
            {
                return;
            }
            
            // Handle touch input with new Input System
            if (Touchscreen.current != null)
            {
                // Handle existing active touch
                if (isDragging && activeTouchId != -1)
                {
                    bool foundActiveTouch = false;
                    foreach (var touch in Touchscreen.current.touches)
                    {
                        if (touch.touchId.ReadValue() == activeTouchId)
                        {
                            HandleAiming(touch.position.ReadValue());
                            foundActiveTouch = true;
                    
                            // Check if touch has ended
                            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended || 
                                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                            {
                                EndAiming(touch.position.ReadValue());
                            }
                            break;
                        }
                    }
            
                    if (!foundActiveTouch)
                    {
                        EndAiming(Vector2.zero);
                    }
                }
                // Check for new touches if not already dragging
                else if (!isDragging)
                {
                    foreach (var touch in Touchscreen.current.touches)
                    {
                        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                        {
                            Vector2 touchPosition = touch.position.ReadValue();
                            if (!EventSystem.current.IsPointerOverGameObject())
                            {
                                activeTouchId = touch.touchId.ReadValue();
                                BeginAiming(touchPosition);
                                break;
                            }
                        }
                    }
                }
            }
            
            // Track virtual mouse state if available - works on ALL platforms
            bool virtualMouseHandled = false;
            bool isVirtualMousePressed = false;
            Vector2 virtualMousePosition = Vector2.zero;
            
            if (virtualMouseInput != null && virtualMouseInput.virtualMouse != null)
            {
                isVirtualMousePressed = virtualMouseInput.virtualMouse.leftButton.isPressed;
                virtualMousePosition = virtualMouseInput.virtualMouse.position.ReadValue();
                
                // Handle virtual mouse input
                if (activeTouchId == -1)
                {
                    // Virtual mouse button down this frame
                    if (isVirtualMousePressed && !wasVirtualMousePressed && !isDragging)
                    {
                        if (!EventSystem.current.IsPointerOverGameObject())
                        {
                            BeginAiming(virtualMousePosition);
                            virtualMouseHandled = true;
                        }
                    }
                    // Continue dragging with virtual mouse
                    else if (isVirtualMousePressed && isDragging)
                    {
                        HandleAiming(virtualMousePosition);
                        virtualMouseHandled = true;
                    }
                    // Release with virtual mouse
                    else if (!isVirtualMousePressed && wasVirtualMousePressed && isDragging)
                    {
                        EndAiming(virtualMousePosition);
                        virtualMouseHandled = true;
                    }
                }
                
                wasVirtualMousePressed = isVirtualMousePressed;
            }
            
            // Handle regular mouse input using the new Input System if not already handled
            if (!virtualMouseHandled && activeTouchId == -1)
            {
                if (Mouse.current != null)
                {
                    Vector2 mousePosition = Mouse.current.position.ReadValue();
                    if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
                    {
                        if (!EventSystem.current.IsPointerOverGameObject())
                        {
                            BeginAiming(mousePosition);
                        }
                    }
                    else if (Mouse.current.leftButton.isPressed && isDragging)
                    {
                        HandleAiming(mousePosition);
                    }
                    else if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
                    {
                        EndAiming(mousePosition);
                    }
                }
            }

            // Additional safety check to ensure EndAiming is called if dragging unexpectedly stops
            if (isDragging && activeTouchId == -1 && 
                (Mouse.current == null || !Mouse.current.leftButton.isPressed) &&
                !(virtualMouseInput != null && virtualMouseInput.virtualMouse != null && 
                  virtualMouseInput.virtualMouse.leftButton.isPressed))
            {
                EndAiming(Vector2.zero);
            }
            
            // Debug shortcuts handling
            HandleDebugShortcuts();
        }

        private void HandleDebugShortcuts()
        {
            if(debugSettings.enableShortcuts)
            {
                if (EventManager.GameStatus == EStatus.Play)
                {
                    if (Input.GetKeyDown(debugSettings.Win))
                    {
                        EventManager.SetGameStatus(EStatus.Win);
                    }
                    if (Input.GetKeyDown(debugSettings.Lose))
                    {
                        MovesTimeManager.instance.SetMoves(0);
                        EventManager.SetGameStatus(EStatus.Fail);
                    }
                    if (Input.GetKeyDown(debugSettings.OneMove))
                    {
                        MovesTimeManager.instance.SetMoveToOne();
                    }
                    if (Input.GetKeyDown(debugSettings.fillPowerUp))
                    {
                        var powerCollector = FindObjectOfType<PowerCollector>();
                        powerCollector.power = 1;
                        powerCollector.UpdateEnergyBar();
                        powerCollector.ReleasePower();
                    }
                }
                if (Input.GetKeyDown(debugSettings.Restart))
                {
                    SceneLoader.instance.StartGameScene();
                }
            }
        }

        private void BeginAiming(Vector2 screenPosition)
        {
            isDragging = true;
            if (launchContainer.BallCharged != null)
            {
                Vector3 worldPosition = GameCamera.instance.Camera.ScreenToWorldPoint(screenPosition);
                raycastData = GetRaycastData(launchContainer.BallCharged.gameObject, launchContainer.transform.position, GameCamera.instance.GetControlBounds(worldPosition));
                DrawAimLine(raycastData, launchContainer.BallCharged);
            }
        }
        
        private void HandleAiming(Vector2 screenPosition)
        {
            if (launchContainer.BallCharged != null)
            {
                Vector3 worldPosition = GameCamera.instance.Camera.ScreenToWorldPoint(screenPosition);
                raycastData = GetRaycastData(launchContainer.BallCharged.gameObject, launchContainer.transform.position, GameCamera.instance.GetControlBounds(worldPosition));
                DrawAimLine(raycastData, launchContainer.BallCharged);
            }
        }
        
        private void EndAiming(Vector2 screenPosition)
        {
            isDragging = false;
            activeTouchId = -1;
            
            if (RectTransformUtility.RectangleContainsScreenPoint(switchRect, screenPosition, uiCamera))
            {
                Switch();
                SoundBase.instance.PlaySound(SoundBase.instance.swish[1]);
            }
            else if (launchContainer.BallCharged != null && raycastData != null)
            {
                launchContainer.LaunchBall(raycastData);
            }
            
            HidePoints();
        }

        private bool IsTouchStarted(bool down)
        {
            // For editor, standalone or WebGL
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            if (Mouse.current == null) return false;

            if (down)
            {
                return Mouse.current.leftButton.isPressed && !EventSystem.current.IsPointerOverGameObject();
            }

            return Mouse.current.leftButton.wasReleasedThisFrame && !EventSystem.current.IsPointerOverGameObject();
            #else
            // For mobile platforms
            if (Touchscreen.current == null || Touchscreen.current.touches.Count == 0) return false;

            var touch = Touchscreen.current.touches[0];
            int touchId = touch.touchId.ReadValue();

            if (!EventSystem.current.IsPointerOverGameObject(touchId))
            {
                if (down)
                {
                    if (IsPhaseMoving(touch))  // Pass the touch parameter here
                        isTouchStarted = true;
                    return isTouchStarted;
                }
                if (isTouchStarted)
                {
                    if (IsPhaseTouching(touch))  // Pass the touch parameter here
                        isTouchStarted = false;

                    return IsPhaseTouching(touch);  // Pass the touch parameter here
                }
            }
            return false;
            #endif
        }

        // These methods are updated to use the new Input System
        private static bool IsPhaseTouching(TouchControl touch)
        {
            return touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
                   touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;
        }

        private static bool IsPhaseMoving(TouchControl touch)
        {
            var phase = touch.phase.ReadValue();
            return phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                   phase == UnityEngine.InputSystem.TouchPhase.Stationary ||
                   phase == UnityEngine.InputSystem.TouchPhase.Began;
        }

        public RaycastData GetRaycastData(GameObject ignoreObject, Vector3 position, Vector3 nextPoint)
        {
            Vector3 direction = (nextPoint - position).normalized;
            var raycastFromBall = RaycastFromBall(position, direction, ignoreObject);
            raycastFromBall.screenMousePosition =  GameCamera.instance.Camera.WorldToScreenPoint(nextPoint);
            raycastFromBall.worldMousePosition = nextPoint;
            return raycastFromBall;
        }

        public RaycastData UpdateRaycastData(GameObject ignoreObject, RaycastData data)
        {
            return GetRaycastData(ignoreObject, data.hits[0].point, data.worldMousePosition);
        }

        public RaycastData RaycastFromBall(Vector3 position, Vector3 direction, GameObject ignoreObject)
        {
            var raycastFromBall = raycastManager.RaycastHit2D(position, ignoreObject, direction);
            return raycastFromBall;
        }

        public void DrawAimLine(RaycastData raycastData, Ball ballCharged)
        {
            aimLine.DrawAimLine(ballCharged.GetColorForAim(), raycastData.hits, raycastData.positionNearBall);
        }

        private void HidePoints()
        {
            aimLine.HidePoints();
        }
    }
}