using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// A delivery vehicle. Drives a waypoint path to the bay, parks, opens its cargo door, swallows juice cups
    /// (its cargo fills up visibly), then closes up and drives off. One instance per truck type is reused (pooled).
    /// </summary>
    public class DeliveryTruck : MonoBehaviour
    {
        public enum State { Hidden, Arriving, Parked, Departing }

        public TruckKind kind;
        public Transform body;
        public Transform[] wheels;
        public float wheelRadius = 0.35f;
        [Tooltip("Scaled on Y from 0 to 1 as the order fills.")]
        public Transform cargoFill;
        public Transform door;
        public Vector3 doorOpenEuler = new Vector3(0f, -100f, 0f);
        public Transform dropPoint;
        public Transform exhaust;
        public Renderer[] lights;
        public Material lightOn, lightOff;
        public AudioSource engine;
        public TruckBoard board;
        public float maxSpeed = 7f;
        [Tooltip("Red tail lights that glow while braking (optional).")]
        public Renderer[] brakeLights;
        public Material brakeOn, brakeOff;

        public State state { get; private set; } = State.Hidden;
        public event Action Arrived;
        public event Action Gone;

        readonly List<Vector3> _path = new List<Vector3>();
        int _pi;
        float _speed;
        float _smokeT;
        float _door;
        Vector3 _bodyBase;
        Quaternion _doorClosed;
        Vector3 _fillScale;
        float _fillShown, _fillTarget;
        Quaternion _bodyRot;
        float _pitch, _pitchVel, _roll, _lastSpeed, _lastYaw;
        bool _braking;

        void Awake()
        {
            if (body != null)
            {
                _bodyBase = body.localPosition;
                _bodyRot = body.localRotation;
            }
            if (door != null) _doorClosed = door.localRotation;
            if (cargoFill != null) _fillScale = cargoFill.localScale;
            if (engine != null)
            {
                engine.clip = Sfx.EngineClip;
                engine.loop = true;
                engine.volume = 0f;
            }
            SetFill(0f, true);
        }

        public void Arrive(IList<Vector3> path, float fill)
        {
            gameObject.SetActive(true);
            _path.Clear();
            _path.AddRange(path);
            _pi = 1;
            transform.position = _path[0];
            if (_path.Count > 1) transform.rotation = Quaternion.LookRotation(Flat(_path[1] - _path[0]));
            _speed = maxSpeed;
            state = State.Arriving;
            SetFill(fill, true);
            SetLights(true);
            if (engine != null && !engine.isPlaying) engine.Play();
            if (board != null) board.gameObject.SetActive(false);
        }

        /// <summary>Appear already parked (loading a save mid-order).</summary>
        public void ParkAt(Vector3 pos, Quaternion rot, float fill)
        {
            gameObject.SetActive(true);
            transform.SetPositionAndRotation(pos, rot);
            state = State.Parked;
            _speed = 0f;
            _door = 1f;
            SetFill(fill, true);
            SetLights(false);
            if (engine != null && !engine.isPlaying) engine.Play();
            if (board != null) board.gameObject.SetActive(true);
        }

        public void Depart(IList<Vector3> path)
        {
            _path.Clear();
            _path.Add(transform.position);
            _path.AddRange(path);
            _pi = 1;
            _speed = 0.5f;
            state = State.Departing;
            SetLights(true);
            Sfx.Play(SfxId.Depart, 0.5f);
            Tweener.Delay(0.2f, () => { if (this != null && state == State.Departing) Sfx.Play(SfxId.Horn, 0.35f, 1.1f); });
            for (int i = 0; i < 6; i++) Fx.Smoke(exhaust != null ? exhaust.position : transform.position, 0.4f, 1);
        }

        public void SetFill(float f, bool instant = false)
        {
            _fillTarget = Mathf.Clamp01(f);
            if (instant) _fillShown = _fillTarget;
            ApplyFill();
        }

        void ApplyFill()
        {
            if (cargoFill == null) return;
            cargoFill.gameObject.SetActive(_fillShown > 0.01f);
            cargoFill.localScale = new Vector3(_fillScale.x, _fillScale.y * Mathf.Max(0.001f, _fillShown), _fillScale.z);
        }

        /// <summary>A cup flies into the cargo door.</summary>
        public void Receive(StackItem item, Action onLanded)
        {
            item.inTransit = true;
            item.onGround = false;
            item.transform.SetParent(null, true);
            var target = dropPoint != null ? dropPoint : transform;
            Tweener.Arc(item.transform, () => target.position, 1.3f, 0.32f, () =>
            {
                item.Despawn();
                Fx.Glint(target.position, new Color(1f, 0.95f, 0.6f), 1);
                if (body != null) Tweener.Punch(body, 0.025f, 0.15f, Vector3.one);
                Sfx.Play(SfxId.Load, 0.32f, UnityEngine.Random.Range(0.92f, 1.12f));
                onLanded?.Invoke();
            }, null, Vector3.one * 0.5f);
        }

        /// <summary>A heavy parcel was loaded aboard: the suspension dips and settles.</summary>
        public void TakeCargo()
        {
            _pitchVel -= 40f;
            SetFill(1f);
            Fx.Dust(transform.position, 4);
        }

        void SetLights(bool on)
        {
            if (lights == null) return;
            foreach (var l in lights)
                if (l != null) l.sharedMaterial = on ? lightOn : lightOff;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.0001f ? Vector3.forward : v;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (state == State.Arriving || state == State.Departing) Drive(dt);

            // Cargo door swings open while parked.
            float doorGoal = state == State.Parked ? 1f : 0f;
            _door = Mathf.MoveTowards(_door, doorGoal, dt * 2.2f);
            if (door != null) door.localRotation = _doorClosed * Quaternion.Euler(doorOpenEuler * Ease.OutBack(_door));

            if (Mathf.Abs(_fillShown - _fillTarget) > 0.001f)
            {
                _fillShown = Mathf.MoveTowards(_fillShown, _fillTarget, dt * 1.5f);
                ApplyFill();
            }

            // Suspension: the body pitches with acceleration (nose dips when braking), leans into turns, and settles
            // on a damped spring; plus an idle engine rumble.
            if (body != null && state != State.Hidden && dt > 0f)
            {
                float accel = (_speed - _lastSpeed) / dt;
                _lastSpeed = _speed;
                float yaw = transform.eulerAngles.y;
                float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
                _lastYaw = yaw;
                float pitchGoal = Mathf.Clamp(-accel * 0.9f, -4f, 4f);
                _pitchVel += ((pitchGoal - _pitch) * 90f - _pitchVel * 9f) * dt;
                _pitch += _pitchVel * dt;
                _roll = Mathf.Lerp(_roll, Mathf.Clamp(-yawRate * 0.05f * Mathf.Clamp01(_speed / 3f), -4f, 4f), 1f - Mathf.Exp(-6f * dt));
                float rumble = state == State.Parked ? 0.006f : 0.012f;
                body.localPosition = _bodyBase + Vector3.up * (Mathf.Sin(Time.time * 32f) * rumble);
                body.localRotation = _bodyRot * Quaternion.Euler(_pitch, 0f, _roll);

                bool braking = state == State.Arriving && accel < -0.5f;
                if (braking != _braking && brakeLights != null)
                {
                    _braking = braking;
                    foreach (var l in brakeLights)
                        if (l != null) l.sharedMaterial = braking ? brakeOn : brakeOff;
                }
            }
            if (state != State.Hidden && exhaust != null)
            {
                _smokeT -= dt;
                if (_smokeT <= 0f)
                {
                    _smokeT = state == State.Parked ? 0.45f : 0.12f;
                    Fx.Smoke(exhaust.position, 0.5f, 1);
                }
            }
            if (engine != null && GameRefs.I != null && GameRefs.I.player != null)
            {
                float d = Vector3.Distance(GameRefs.I.player.transform.position, transform.position);
                float vol = Mathf.Clamp01(1f - d / 22f) * (state == State.Parked ? 0.06f : 0.16f);
                engine.volume = Mathf.MoveTowards(engine.volume, vol, dt);
                engine.pitch = 0.85f + Mathf.Clamp01(_speed / maxSpeed) * 0.5f;
            }
        }

        void Drive(float dt)
        {
            if (_pi >= _path.Count)
            {
                Finish();
                return;
            }
            Vector3 pos = transform.position;
            Vector3 target = _path[_pi];
            Vector3 d = target - pos;
            d.y = 0f;
            float dist = d.magnitude;

            // Remaining distance to the end of the path, for easing in / out.
            float remaining = dist;
            for (int i = _pi; i < _path.Count - 1; i++) remaining += Vector3.Distance(_path[i], _path[i + 1]);
            float goal = maxSpeed;
            // Arriving: brake on a constant deceleration so the truck glides to a stop exactly at the bay.
            if (state == State.Arriving) goal = Mathf.Min(maxSpeed, Mathf.Sqrt(2f * 3.2f * remaining) + 0.15f);
            _speed = Mathf.MoveTowards(_speed, goal, dt * (state == State.Departing ? 3.2f : 8f));

            float step = _speed * dt;
            if (dist <= step)
            {
                transform.position = new Vector3(target.x, pos.y, target.z);
                _pi++;
            }
            else transform.position = pos + d / dist * step;

            if (dist > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-5f * dt));

            if (wheels != null)
                foreach (var w in wheels)
                    if (w != null) w.Rotate(Vector3.right, step / wheelRadius * Mathf.Rad2Deg, Space.Self);
        }

        void Finish()
        {
            if (state == State.Arriving)
            {
                state = State.Parked;
                _speed = 0f;
                SetLights(false);
                Sfx.Play(SfxId.Horn, 0.45f);
                if (board != null)
                {
                    board.gameObject.SetActive(true);
                    Tweener.Scale(board.transform, Vector3.zero, Vector3.one, 0.45f, Ease.OutBack);
                }
                Fx.Ring(transform.position, new Color(1f, 0.9f, 0.4f, 0.8f), 7f);
                Arrived?.Invoke();
            }
            else if (state == State.Departing)
            {
                state = State.Hidden;
                if (engine != null) engine.Stop();
                gameObject.SetActive(false);
                Gone?.Invoke();
            }
        }
    }
}
