using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Grid-shaped pile of items resting on a surface (juicer trays, counter shelf, cash pile).</summary>
    public class ItemPile : MonoBehaviour
    {
        public int columns = 3;
        public int rows = 2;
        public int layers = 4;
        public Vector2 spacing = new Vector2(0.45f, 0.45f);
        public float layerHeight = 0.3f;

        public readonly List<StackItem> items = new List<StackItem>(64);
        public event Action OnChanged;

        public int Capacity => columns * rows * layers;
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public bool HasSpace => items.Count < Capacity;

        public Vector3 SlotLocal(int i)
        {
            int perLayer = columns * rows;
            int layer = i / perLayer;
            int idx = i % perLayer;
            float x = (idx % columns - (columns - 1) * 0.5f) * spacing.x;
            float z = (idx / columns - (rows - 1) * 0.5f) * spacing.y;
            return new Vector3(x, layer * layerHeight, z);
        }

        public Vector3 SlotWorld(int i) => transform.TransformPoint(SlotLocal(i));

        public void Add(StackItem item, float arcHeight = 1f, float duration = 0.3f, Action onArrive = null)
        {
            items.Add(item);
            item.inTransit = true;
            item.onGround = false;
            item.transform.SetParent(transform, true);
            Tweener.Arc(item.transform, () => transform.TransformPoint(SlotLocalOf(item)), arcHeight, duration, () =>
            {
                item.inTransit = false;
                onArrive?.Invoke();
            }, () => transform.rotation);
            OnChanged?.Invoke();
        }

        /// <summary>Place without animation (e.g. when loading).</summary>
        public void AddInstant(StackItem item)
        {
            items.Add(item);
            item.inTransit = false;
            item.transform.SetParent(transform, false);
            item.transform.localPosition = SlotLocal(items.Count - 1);
            item.transform.localRotation = Quaternion.identity;
            OnChanged?.Invoke();
        }

        public StackItem TakeLast(Predicate<ItemType> filter = null)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var it = items[i];
                if (it.inTransit) continue;
                if (filter != null && !filter(it.type)) continue;
                items.RemoveAt(i);
                it.transform.SetParent(null, true);
                OnChanged?.Invoke();
                return it;
            }
            return null;
        }

        public bool Contains(Predicate<ItemType> filter)
        {
            for (int i = 0; i < items.Count; i++)
                if (!items[i].inTransit && filter(items[i].type)) return true;
            return false;
        }

        public int CountOf(Predicate<ItemType> filter)
        {
            int c = 0;
            for (int i = 0; i < items.Count; i++)
                if (filter(items[i].type)) c++;
            return c;
        }

        Vector3 SlotLocalOf(StackItem item)
        {
            int idx = items.IndexOf(item);
            return SlotLocal(idx < 0 ? items.Count : idx);
        }

        void LateUpdate()
        {
            // Settle items smoothly toward their slot (handles removal from the middle).
            float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null || it.inTransit) continue;
                var tr = it.transform;
                Vector3 goal = SlotLocal(i);
                if ((tr.localPosition - goal).sqrMagnitude > 0.00001f)
                    tr.localPosition = Vector3.Lerp(tr.localPosition, goal, k);
            }
        }
    }
}
