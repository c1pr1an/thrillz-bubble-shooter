using UnityEngine;
using Brain.Util;

namespace Brain.Gameplay
{
    public class AutoReturnToPool : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 1.5f;
        [SerializeField] private PooledObjectTag _poolTag;

        private float _spawnTime;

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= _lifetime)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            ObjectPooler.Instance.Release(gameObject, _poolTag);
        }
    }
}
