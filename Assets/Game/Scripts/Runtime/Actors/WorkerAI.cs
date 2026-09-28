using UnityEngine;
using UnityEngine.AI;

namespace JuiceKing
{
    /// <summary>
    /// Hired helper. Farmers harvest one field and feed its juicer; waiters carry juice from juicers to the counter.
    /// </summary>
    public class WorkerAI : MonoBehaviour
    {
        public enum Role { Farmer, Waiter }
        enum Task { Idle, Gather, Deliver, Collect }

        public Role role;
        public NavMeshAgent agent;
        public Carrier carrier;
        public Chainsaw saw;

        [Header("Farmer")]
        public FruitField field;
        public Juicer juicer;

        [Header("Waiter")]
        public Juicer[] juicers;
        public Counter counter;

        public Transform idlePoint;

        Task _task;
        float _think;
        Vector3 _dest;
        bool _hasDest;

        void OnEnable()
        {
            _task = Task.Idle;
            _think = 0.3f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
                return;
            }

            _think -= dt;
            if (_think <= 0f)
            {
                _think = 0.25f;
                if (role == Role.Farmer) DecideFarmer();
                else DecideWaiter();
                if (_hasDest) agent.SetDestination(_dest);
            }

            // Face the fruit while sawing.
            if (saw != null && saw.Target != null && agent.velocity.sqrMagnitude < 0.2f)
            {
                Vector3 d = saw.Target.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * dt));
            }
        }

        void SetDest(Vector3 p)
        {
            _dest = p;
            _hasDest = true;
        }

        void DecideFarmer()
        {
            Vector3 pos = transform.position;
            if (_task == Task.Deliver)
            {
                if (carrier.Count == 0) _task = Task.Gather;
                else
                {
                    SetDest(juicer.inputZone.transform.position);
                    return;
                }
            }

            var fruit = field.NearestReady(pos);
            var loose = LooseItems.Nearest(pos, ItemTypes.Slice(field.kind), 9f);
            if (carrier.IsFull || (carrier.Count > 0 && fruit == null && loose == null))
            {
                _task = Task.Deliver;
                SetDest(juicer.inputZone.transform.position);
                return;
            }

            _task = Task.Gather;
            if (loose != null) SetDest(loose.transform.position);
            else if (fruit != null)
            {
                Vector3 away = pos - fruit.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                SetDest(fruit.transform.position + away.normalized * (fruit.radius + 0.45f));
            }
            else if (idlePoint != null) SetDest(idlePoint.position);
        }

        void DecideWaiter()
        {
            if (_task == Task.Deliver)
            {
                if (carrier.Count == 0) _task = Task.Idle;
                else
                {
                    SetDest(counter.dropZone.transform.position);
                    return;
                }
            }

            Juicer best = null;
            foreach (var j in juicers)
            {
                if (j == null || !j.isActiveAndEnabled || j.Available == 0) continue;
                if (best == null || j.Available > best.Available) best = j;
            }

            // Keep collecting at the current tray while it still has cups.
            if (carrier.IsFull || (carrier.Count > 0 && best == null))
            {
                _task = Task.Deliver;
                SetDest(counter.dropZone.transform.position);
                return;
            }

            if (best != null)
            {
                _task = Task.Collect;
                SetDest(best.outputZone.transform.position);
            }
            else if (carrier.Count == 0 && idlePoint != null)
            {
                _task = Task.Idle;
                SetDest(idlePoint.position);
            }
        }
    }
}
