using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brain.Util
{
    public class PersistentObject : MonoBehaviour
    {
        private static PersistentObject Instance = null;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
                Destroy(gameObject);
        }
    }
}