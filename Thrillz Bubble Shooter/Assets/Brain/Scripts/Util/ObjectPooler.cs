using System;
using System.Collections;
using System.Collections.Generic;
using Brain.Gameplay;
using UnityEngine;

namespace Brain.Util
{
    public enum PooledObjectTag
    {
        AddScoreText,
        BallYellow,
        BallBlue,
        BallRed,
        BallGreen,
        BallPurple,
        BallPink,
        BallRed_VFX,
    }

    public class ObjectPooler : UnitySingleton<ObjectPooler>
    {
        // Nested Classes
        [Serializable]
        public class Pool
        {
            public PooledObjectTag tag;
            public int initialSize;
            public GameObject prefab;
        }

        // Public Fields
        public List<Pool> pools;
        public Dictionary<PooledObjectTag, Queue<GameObject>> poolDictionary;

        // Public Methods
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

        public GameObject Get(BallColor ballColor)
        {
            PooledObjectTag tag = GetPoolTag(ballColor);
            return Get(tag);
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

        // Private Methods
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

        public void Release(GameObject obj, BallColor color)
        {
            PooledObjectTag poolTag = GetPoolTag(color);
            Release(obj, poolTag);
        }

        public void Release(GameObject obj, PooledObjectTag poolTag)
        {
            obj.SetActive(false);
            obj.transform.localScale = Vector3.one;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(transform, false);

            poolDictionary[poolTag].Enqueue(obj);
        }

        public static PooledObjectTag GetPoolTag(BallColor color)
        {
            switch (color)
            {
                case BallColor.Yellow:
                    return PooledObjectTag.BallYellow;
                case BallColor.Blue:
                    return PooledObjectTag.BallBlue;
                case BallColor.Red:
                    return PooledObjectTag.BallRed;
                case BallColor.Green:
                    return PooledObjectTag.BallGreen;
                case BallColor.Purple:
                    return PooledObjectTag.BallPurple;
                case BallColor.Pink:
                    return PooledObjectTag.BallPink;
                default:
                    Debug.LogError($"No pool tag defined for color {color}");
                    return PooledObjectTag.BallRed; // Fallback
            }
        }
    }
}