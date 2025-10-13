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

using System;
using System.Collections.Generic;
using System.Linq;
using BubbleShooterGameToolkit.Scripts.CommonUI.Popups;
using BubbleShooterGameToolkit.Scripts.System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BubbleShooterGameToolkit.Scripts.CommonUI
{
    public class MenuManager : SingletonBehaviour<MenuManager>
    {
        public Fader fader;
        private List<Popup> popupStack = new();
        [SerializeField]
        private Canvas canvas;

        private static Dictionary<Type, GameObject> popupPrefabCache;
        
        private static void InitializePopupCache()
        {
            if (popupPrefabCache != null)
                return;
                
            popupPrefabCache = new Dictionary<Type, GameObject>();
            
            GameObject[] popupPrefabs = Resources.LoadAll<GameObject>("Popups");
            
            foreach (var prefab in popupPrefabs)
            {
                Popup popupComponent = prefab.GetComponent<Popup>();
                if (popupComponent != null)
                {
                    Type popupType = popupComponent.GetType();
                    popupPrefabCache[popupType] = prefab;
                }
            }
        }

        public override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
        }

        private void OnEnable()
        {
            fader.FadeAfterLoadingScene();
            Popup.OnClosePopup += ClosePopup;
            SceneLoader.OnSceneLoadedCallback += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene)
        {
            if (canvas == null && this != null)
            {
                canvas = GetComponent<Canvas>();
            }

            canvas.worldCamera = Camera.main;
        }

        private void OnDisable()
        {
            Popup.OnClosePopup -= ClosePopup;
            SceneLoader.OnSceneLoadedCallback -= OnSceneLoaded;
        }

        public T ShowPopup<T>(Action onShow = null, Action<EPopupResult> onClose = null) where T : Popup
        {
            Type popupType = typeof(T);
            
            if (popupPrefabCache == null)
            {
                InitializePopupCache();
            }
            
            GameObject popupPrefab = null;
            if (!popupPrefabCache.TryGetValue(popupType, out popupPrefab))
            {
                T resourcePopup = Resources.Load<T>("Popups/" + popupType.Name);
                if (resourcePopup == null)
                {
                    Debug.LogError($"Popup prefab for type '{popupType.Name}' not found in Resources/Popups folder!");
                    return null;
                }
                
                popupPrefab = (resourcePopup as Component)?.gameObject;
                
                if (popupPrefab != null)
                {
                    popupPrefabCache[popupType] = popupPrefab;
                }
            }
            
            var popup = Instantiate(popupPrefab, transform).GetComponent<T>();
            if (popup == null)
            {
                Debug.LogError($"Failed to instantiate popup of type '{popupType.Name}'!");
                return null;
            }
            
            if (popupStack.Count > 0)
            {
                popupStack.Last().Hide();
            }
            
            popupStack.Add(popup);
            popup.Show<T>(onShow, onClose);
            var rectTransform = popup.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            
            if(fader != null && popupStack.Count > 0 && popup.fade)
                fader.FadeIn();
                
            return popup;
        }

        private void ClosePopup(Popup popupClose)
        {
            if (popupStack.Count > 0)
            {
                popupStack.Remove(popupClose);
                if (popupStack.Count > 0)
                {
                    var popup = popupStack.Last();
                    popup.Show();
                }
            }
            if(fader != null && popupStack.Count == 0 && fader.IsFaded())
                fader.FadeOut();
        }

        public void ShowPurchased(GameObject imagePrefab, string boostName)
        {
            var menu = ShowPopup<PurchasedMenu>();
            menu.GetComponent<PurchasedMenu>().SetIconSprite(imagePrefab, boostName);
        }

        void Update()
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer) {
                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    if (popupStack is { Count: > 0 })
                    {
                        var closeButton = popupStack.Last().closeButton;
                        if (closeButton != null)
                        {
                            closeButton.onClick?.Invoke();
                        }
                    }
                    else if (SceneManager.GetActiveScene().name == "map")
                    {
                        SceneLoader.instance.GoMain();
                    }
                }
            }
        }

        public T GetPopupOpened<T>() where T : Popup
        {
            foreach (var popup in popupStack)
            {
                if (popup.GetType() == typeof(T))
                    return (T)popup;
            }
            return null;
        }

        public void CloseAllPopups()
        {
            for (var i = 0; i < popupStack.Count; i++)
            {
                var popup = popupStack[i];
                popup.Close();
            }
            popupStack.Clear();
        }

    }
}
