using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Stack of items carried by an actor (player, workers, customers). Items settle into columns and sway with movement.
    /// </summary>
    public class Carrier : MonoBehaviour
    {
        public static readonly List<Carrier> All = new List<Carrier>();

        public Transform stackRoot;
        public int capacity = 8;
        public int columns = 2;
        public float columnSpacing = 0.34f;
        public bool isPlayer;
        public bool canCollectLoose = true;
        [Tooltip("Customers carry items but must not trigger station zones.")]
        public bool interactsWithZones = true;
        public float swayAmount = 0.07f;

        public readonly List<StackItem> items = new List<StackItem>(32);

        /// <summary>Only pick up items that pass this (null = anything). Helpers use it to stick to one job.</summary>
        [NonSerialized] public Predicate<ItemType> pickupFilter;
        /// <summary>Stop picking up at this many matching items (-1 = no limit besides capacity).</summary>
        [NonSerialized] public int maxPickup = -1;
        public event Action OnChanged;

        Vector3 _lastPos;
        Vector3 _sway, _swayVel;
        float _bounce;
        Vector3 _rootBase;
        bool _rootBaseSet;

        public int Count => items.Count;
        public bool IsFull => items.Count >= capacity;
        public int FreeSpace => Mathf.Max(0, capacity - items.Count);
        public Vector3 Velocity { get; private set; }

        void OnEnable()
        {
            All.Add(this);
            _lastPos = transform.position;
        }

        void OnDisable() => All.Remove(this);

        float _nextFullMsg;

        /// <summary>Feedback when the actor tries to take more than it can carry.</summary>
        public void NotifyFull()
        {
            if (!isPlayer || Time.time < _nextFullMsg) return;
            _nextFullMsg = Time.time + 1.2f;
            FloatingText.Show("MAX", TopWorld() + Vector3.up * 0.6f, new Color(1f, 0.3f, 0.3f), 1.2f, 0.8f, 0.8f);
            Sfx.Play(SfxId.Error, 0.4f);
        }

        public bool Contains(Predicate<ItemType> filter)
        {
            for (int i = 0; i < items.Count; i++)
                if (filter(items[i].type)) return true;
            return false;
        }

        public int CountOf(Predicate<ItemType> filter)
        {
            int c = 0;
            for (int i = 0; i < items.Count; i++)
                if (filter(items[i].type)) c++;
            return c;
        }

        public void Add(StackItem item, float arcHeight = 1.2f, float duration = 0.32f, bool playSound = true)
        {
            items.Add(item);
            item.onGround = false;
            item.inTransit = true;
            item.transform.SetParent(stackRoot, true);
            Tweener.Arc(item.transform, () => stackRoot.TransformPoint(SlotLocalOf(item)), arcHeight, duration, () =>
            {
                item.inTransit = false;
                item.transform.localRotation = Quaternion.identity;
                // Little squash on landing makes the stack feel soft.
                Tweener.Punch(item.transform, 0.25f, 0.2f, Vector3.one);
                _bounce = Mathf.Max(_bounce, 1f);
                if (playSound && isPlayer) Sfx.Play(SfxId.Pop, 0.32f, 1f + Mathf.Min(items.Count, 30) * 0.025f);
            }, () => stackRoot.rotation);
            OnChanged?.Invoke();
        }

        /// <summary>Remove the top-most item that matches the filter.</summary>
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

        public void ClearAll()
        {
            foreach (var it in items)
                if (it != null) it.Despawn();
            items.Clear();
            OnChanged?.Invoke();
        }

        Vector3 SlotLocalOf(StackItem item)
        {
            int idx = items.IndexOf(item);
            if (idx < 0) idx = items.Count;
            return SlotLocal(idx);
        }

        public Vector3 SlotLocal(int index)
        {
            int col = index % columns;
            float y = 0f;
            for (int j = col; j < index && j < items.Count; j += columns) y += items[j].height;
            float x = (col - (columns - 1) * 0.5f) * columnSpacing;
            return new Vector3(x, y, 0f);
        }

        public Vector3 TopWorld()
        {
            float h = 0f;
            for (int c = 0; c < columns; c++)
            {
                float y = 0f;
                for (int j = c; j < items.Count; j += columns) y += items[j].height;
                h = Mathf.Max(h, y);
            }
            return stackRoot.position + Vector3.up * h;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 pos = transform.position;
            Velocity = Vector3.Lerp(Velocity, (pos - _lastPos) / dt, 1f - Mathf.Exp(-12f * dt));
            _lastPos = pos;

            // Spring the sway offset toward the opposite of movement.
            Vector3 localVel = stackRoot.InverseTransformDirection(Velocity);
            localVel.y = 0f;
            Vector3 target = -localVel * swayAmount;
            target = Vector3.ClampMagnitude(target, 0.6f);
            _swayVel += (target - _sway) * (90f * dt);
            _swayVel *= Mathf.Exp(-9f * dt);
            _sway += _swayVel * dt;

            // Springy bounce of the whole stack whenever something lands on it.
            if (!_rootBaseSet)
            {
                _rootBase = stackRoot.localScale;
                _rootBaseSet = true;
            }
            if (_bounce > 0f)
            {
                _bounce = Mathf.Max(0f, _bounce - dt * 5f);
                float b = Mathf.Sin((1f - _bounce) * Mathf.PI * 3f) * _bounce * 0.08f;
                stackRoot.localScale = new Vector3(_rootBase.x * (1f + b), _rootBase.y * (1f - b), _rootBase.z * (1f + b));
            }

            float k = 1f - Mathf.Exp(-22f * dt);
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null || it.inTransit) continue;
                Vector3 slot = SlotLocal(i);
                float f = Mathf.Pow(Mathf.Max(0f, slot.y), 1.3f);
                Vector3 goal = slot + _sway * f;
                var tr = it.transform;
                tr.localPosition = Vector3.Lerp(tr.localPosition, goal, k);
                tr.localRotation = Quaternion.Slerp(tr.localRotation,
                    Quaternion.Euler(_sway.z * f * 25f, 0f, -_sway.x * f * 25f), k);
            }
        }
    }
}
