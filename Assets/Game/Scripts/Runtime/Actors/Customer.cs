using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Walks in, queues, buys juice, pays and walks away happy (or grumpy if kept waiting). Pooled.</summary>
    public class Customer : MonoBehaviour
    {
        public enum State { Entering, Queueing, Leaving }

        public Carrier hands;
        public OrderBubble bubble;
        public CharacterAnim anim;
        public float speed = 3.8f;

        public FruitKind want;
        public int wantCount;
        public int got;

        public State state { get; private set; }
        public int slot = -1;
        public bool AtSlot { get; private set; }
        public bool Done => got >= wantCount;
        /// <summary>Waited too long at the front without receiving anything new.</summary>
        public bool OutOfPatience => _wait >= Balance.CustomerPatience;

        CustomerManager _mgr;
        readonly List<Vector3> _path = new List<Vector3>();
        int _pi;
        int _curSlot;
        Vector3 _face;
        bool _hasFace;
        float _wait;

        Vector3 _bubbleScale = Vector3.one;

        void Awake()
        {
            if (bubble != null) _bubbleScale = bubble.transform.localScale;
            if (anim == null) anim = GetComponent<CharacterAnim>();
        }

        public void Init(CustomerManager mgr, FruitKind kind, int count, IList<Vector3> entryPath, int queueSlot)
        {
            _mgr = mgr;
            want = kind;
            wantCount = count;
            got = 0;
            slot = queueSlot;
            state = State.Entering;
            AtSlot = false;
            _hasFace = false;
            _wait = 0f;
            speed = Random.Range(Balance.CustomerSpeed.x, Balance.CustomerSpeed.y);
            _path.Clear();
            _path.AddRange(entryPath);
            _pi = 0;
            if (bubble != null)
            {
                bubble.transform.localScale = _bubbleScale;
                bubble.SetOrder(GameRefs.I.fruitIcons[(int)kind], wantCount);
                bubble.SetPatience(0f);
                bubble.gameObject.SetActive(false);
            }
        }

        public void Give(StackItem juice)
        {
            got++;
            _wait = 0f;
            hands.Add(juice, 1f, 0.3f, false);
            if (bubble != null)
            {
                bubble.SetCount(wantCount - got);
                bubble.SetPatience(0f);
            }
        }

        public void Leave(IList<Vector3> exitPath, bool happy = true)
        {
            state = State.Leaving;
            AtSlot = false;
            slot = -1;
            _path.Clear();
            _path.AddRange(exitPath);
            _pi = 0;
            _hasFace = false;
            if (bubble != null)
            {
                if (happy) bubble.ShowHappy();
                else bubble.ShowSad();
            }
            if (happy)
            {
                if (anim != null) anim.Cheer();
                Fx.Hearts(transform.position + Vector3.up * 2.2f, 3);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (state)
            {
                case State.Entering:
                    if (FollowPath(dt))
                    {
                        state = State.Queueing;
                        _curSlot = _mgr.SlotCount - 1;
                        if (bubble != null)
                        {
                            bubble.gameObject.SetActive(true);
                            Tweener.Scale(bubble.transform, Vector3.zero, _bubbleScale, 0.35f, Ease.OutBack);
                        }
                    }
                    break;

                case State.Queueing:
                    if (_curSlot < slot) _curSlot = slot;
                    Vector3 target = _mgr.SlotPosition(_curSlot);
                    if (MoveTo(target, dt))
                    {
                        if (_curSlot > slot)
                        {
                            _curSlot--;
                            AtSlot = false;
                        }
                        else
                        {
                            AtSlot = true;
                            _face = slot == 0 ? _mgr.counter.transform.position : _mgr.SlotPosition(slot - 1);
                            _hasFace = true;
                        }
                    }
                    else AtSlot = false;

                    // Patience only drains while being served at the front.
                    if (AtSlot && slot == 0)
                    {
                        _wait += dt;
                        if (bubble != null) bubble.SetPatience(_wait / Balance.CustomerPatience);
                    }
                    break;

                case State.Leaving:
                    if (FollowPath(dt))
                    {
                        hands.ClearAll();
                        state = State.Entering;
                        Pool.Despawn(gameObject);
                        return;
                    }
                    break;
            }

            if (_hasFace && AtSlot)
            {
                Vector3 d = _face - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * dt));
            }
        }

        bool FollowPath(float dt)
        {
            while (_pi < _path.Count)
            {
                if (!MoveTo(_path[_pi], dt)) return false;
                _pi++;
            }
            return true;
        }

        /// <summary>Returns true once at the target.</summary>
        bool MoveTo(Vector3 target, float dt)
        {
            Vector3 p = transform.position;
            Vector3 d = target - p;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < 0.05f) return true;
            float step = speed * Mathf.Lerp(1f, Boosts.WorkMult, 0.5f) * dt;
            Vector3 dir = d / dist;
            transform.position = step >= dist ? new Vector3(target.x, p.y, target.z) : p + dir * step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-12f * dt));
            return step >= dist;
        }
    }
}
