using System.Collections.Generic;
using UnityEngine;

namespace GitVisualizer.Core
{
    public class CommitNodePool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _available = new Queue<GameObject>();
        private readonly HashSet<GameObject> _inUse = new HashSet<GameObject>();

        public CommitNodePool(GameObject prefab, Transform parent, int initialSize = 32)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initialSize; i++)
                _available.Enqueue(CreateNew());
        }

        private GameObject CreateNew()
        {
            var obj = _prefab != null
                ? Object.Instantiate(_prefab, _parent)
                : CreatePrimitive();
            obj.SetActive(false);
            return obj;
        }

        private GameObject CreatePrimitive()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.SetParent(_parent);
            return obj;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;
            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else
            {
                obj = CreateNew();
            }
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            _inUse.Add(obj);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null || !_inUse.Contains(obj)) return;
            _inUse.Remove(obj);
            obj.SetActive(false);
            obj.transform.SetParent(_parent);
            _available.Enqueue(obj);
        }

        public void ReturnAll()
        {
            foreach (var obj in new List<GameObject>(_inUse))
                Return(obj);
        }

        public void DestroyAll()
        {
            ReturnAll();
            while (_available.Count > 0)
            {
                var obj = _available.Dequeue();
                if (obj != null)
                    Object.Destroy(obj);
            }
        }
    }
}
