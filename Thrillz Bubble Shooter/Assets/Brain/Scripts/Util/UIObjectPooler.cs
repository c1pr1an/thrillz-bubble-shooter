using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brain.Util
{
    public enum UIPooledObjectTag
    {
        AddScoreText,
    }

    public class UIObjectPooler : MonoBehaviour
    {
        // Nested Classes
        [Serializable]
        public class UIPool
        {
            public UIPooledObjectTag tag;
            public int initialSize;
            public GameObject prefab;
        }

        // Public Fields
        public List<UIPool> pools;
        public Dictionary<UIPooledObjectTag, Queue<GameObject>> poolDictionary;

        // Public Methods
        public void Init()
        {
            poolDictionary = new Dictionary<UIPooledObjectTag, Queue<GameObject>>();

            foreach (UIPool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();
                poolDictionary.Add(pool.tag, objectPool);
                for (int i = 0; i < pool.initialSize; i++)
                {
                    GameObject obj = Instantiate(pool.prefab, transform);
                    Release(obj, pool.tag);
                }
            }

            gameObject.SetActive(false);
        }

        public GameObject Get(UIPooledObjectTag tag)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
                return null;
            }

            if (poolDictionary[tag].Count == 0)
                GrowPool(tag);

            GameObject objectToSpawn = poolDictionary[tag].Dequeue();
            objectToSpawn.SetActive(true);

            return objectToSpawn;
        }

        public void Release(GameObject obj, UIPooledObjectTag poolTag)
        {
            obj.SetActive(false);
            obj.transform.localScale = Vector3.one;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(transform, false);

            poolDictionary[poolTag].Enqueue(obj);
        }

        // Private Methods
        private void GrowPool(UIPooledObjectTag tag)
        {
            int growSize = 2;
            UIPool pool = pools.Find(p => p.tag == tag);

            for (int i = 0; i < growSize; i++)
            {
                GameObject obj;
                obj = Instantiate(pool.prefab, transform);

                Release(obj, tag);
            }
        }
    }
}
