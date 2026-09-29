using UnityEngine;

namespace JuiceKing
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public CharacterController cc;
        public Carrier carrier;
        public Chainsaw saw;
        public CharacterAnim anim;

        Vector3 _vel;
        float _yVel;
        float _trailT;

        void Awake()
        {
            if (cc == null) cc = GetComponent<CharacterController>();
            if (anim == null) anim = GetComponent<CharacterAnim>();
        }

        void Start()
        {
            UnlockManager.ZoneUnlocked += OnUnlocked;
            if (GameManager.I != null) GameManager.I.UpgradesChanged += OnUpgraded;
        }

        void OnDestroy()
        {
            UnlockManager.ZoneUnlocked -= OnUnlocked;
            if (GameManager.I != null) GameManager.I.UpgradesChanged -= OnUpgraded;
        }

        void OnUnlocked(UnlockZone z)
        {
            if (anim != null) anim.Cheer();
        }

        void OnUpgraded()
        {
            if (anim != null) anim.Cheer();
            Fx.Stars(transform.position + Vector3.up * 1.2f, 14);
            Fx.Ring(transform.position, new Color(1f, 0.85f, 0.3f, 0.8f), 3.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var d = GameManager.I.data;
            carrier.capacity = Balance.BagCapacity(d.bagLevel);
            saw.dps = Balance.SawDps(d.sawLevel) * Mathf.Lerp(1f, Boosts.WorkMult, 0.5f);
            float maxSpeed = Balance.MoveSpeed(d.speedLevel) * Boosts.MoveMult;

            Vector2 inp = InputJoystick.Direction;
            Vector3 dir = new Vector3(inp.x, 0f, inp.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            _vel = Vector3.MoveTowards(_vel, dir * maxSpeed, 45f * dt);
            _yVel = cc.isGrounded ? -2f : _yVel - 25f * dt;
            cc.Move((_vel + Vector3.up * _yVel) * dt);

            Vector3 face = Vector3.zero;
            if (dir.sqrMagnitude > 0.01f) face = dir;
            else if (saw.Target != null)
            {
                face = saw.Target.transform.position - transform.position;
                face.y = 0f;
            }

            if (face.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 1f - Mathf.Exp(-16f * dt));

            // Turbo: glowing speed trail.
            if (Boosts.IsActive(BoostKind.Turbo) && _vel.sqrMagnitude > 4f)
            {
                _trailT -= dt;
                if (_trailT <= 0f)
                {
                    _trailT = 0.03f;
                    Fx.Trail(transform.position + Vector3.up * (0.3f + Random.value * 0.6f) - _vel.normalized * 0.3f,
                        new Color(0.45f, 0.85f, 1f, 0.7f));
                }
            }
        }
    }
}
