using UnityEngine;

namespace Brain.Util
{
    public class UnitySingleton<T> : MonoBehaviour where T : Component
    {
        private static T s_Instance;

        protected virtual void Awake()
        {
            if (FindObjectsOfType<T>().Length > 1 && this != s_Instance)
            {
                Debug.LogWarning(this.name + " - Instance already created!");
                DestroyImmediate(gameObject);
            }
        }

        public static bool Exists()
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType<T>();
            }
            if (s_Instance != null)
                return true;

            return false;
        }

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<T>();
                    if (s_Instance == null)
                        Debug.LogWarningFormat(
                            "Could not find [{0}] Singleton! Make sure one is present in your game before using it!", typeof(T).Name);
                }

                return s_Instance;
            }
        }
    }
}