using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brain.Util
{
    public enum PooledObjectTag
    {
        AddScoreText
    }

    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance;

        [Serializable]
        public class Pool
        {
            public PooledObjectTag tag;
            public int initialSize;
            public GameObject prefab;
        }

        public List<Pool> pools;
        public Dictionary<PooledObjectTag, Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Init();
            }
            else
            {
                Debug.LogWarning("Multiple instances of ObjectPooler detected. Destroying the new instance.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        public void Init()
        {
            poolDictionary = new Dictionary<PooledObjectTag, Queue<GameObject>>();

            foreach (Pool pool in pools)
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

        public GameObject Get(PooledObjectTag tag)
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

        private void GrowPool(PooledObjectTag tag)
        {
            int growSize = 2;
            Pool pool = pools.Find(p => p.tag == tag);

            for (int i = 0; i < growSize; i++)
            {
                GameObject obj;
                obj = Instantiate(pool.prefab, transform);

                Release(obj, tag);
            }
        }

        public void Release(GameObject obj, PooledObjectTag poolTag)
        {
            obj.SetActive(false);
            obj.transform.localScale = Vector3.one;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(transform, false);

            poolDictionary[poolTag].Enqueue(obj);
        }
    }
}