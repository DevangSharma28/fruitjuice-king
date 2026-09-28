using UnityEngine;

namespace JuiceKing
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public CharacterController cc;
        public Carrier carrier;
        public Chainsaw saw;

        Vector3 _vel;
        float _yVel;

        void Awake()
        {
            if (cc == null) cc = GetComponent<CharacterController>();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var d = GameManager.I.data;
            carrier.capacity = Balance.BagCapacity(d.bagLevel);
            saw.dps = Balance.SawDps(d.sawLevel);
            float maxSpeed = Balance.MoveSpeed(d.speedLevel);

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
        }
    }
}
