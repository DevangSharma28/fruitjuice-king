using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    public class PoolTag : MonoBehaviour
    {
        public GameObject prefab;
    }

    /// <summary>Simple prefab-keyed object pool.</summary>
    public static class Pool
    {
        static readonly Dictionary<GameObject, Stack<GameObject>> Free = new Dictionary<GameObject, Stack<GameObject>>();
        static Transform _root;

        static Transform Root
        {
            get
            {
                if (_root == null) _root = new GameObject("[Pool]").transform;
                return _root;
            }
        }

        public static T Spawn<T>(T prefab, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
        {
            var go = Spawn(prefab.gameObject, pos, rot, parent);
            return go.GetComponent<T>();
        }

        public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            GameObject go = null;
            if (Free.TryGetValue(prefab, out var stack))
            {
                while (stack.Count > 0 && go == null) go = stack.Pop();
            }

            if (go == null)
            {
                go = Object.Instantiate(prefab, pos, rot, parent);
                var tag = go.GetComponent<PoolTag>();
                if (tag == null) tag = go.AddComponent<PoolTag>();
                tag.prefab = prefab;
            }
            else
            {
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(pos, rot);
                go.transform.localScale = prefab.transform.localScale;
                go.SetActive(true);
            }
            return go;
        }

        public static void Despawn(GameObject go)
        {
            if (go == null) return;
            Tweener.Kill(go.transform);
            var tag = go.GetComponent<PoolTag>();
            if (tag == null || tag.prefab == null)
            {
                Object.Destroy(go);
                return;
            }
            go.SetActive(false);
            go.transform.SetParent(Root, false);
            if (!Free.TryGetValue(tag.prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                Free[tag.prefab] = stack;
            }
            stack.Push(go);
        }
    }
}

