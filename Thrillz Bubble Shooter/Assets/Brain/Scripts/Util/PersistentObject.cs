using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brain.Util
{
    public class PersistentObject : MonoBehaviour
    {
        // Static Fields
        private static PersistentObject s_Instance = null;

        // Unity Lifecycle
        void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (s_Instance != this)
                Destroy(gameObject);
        }
    }
}