using UnityEngine;

namespace JuiceKing
{
    /// <summary>Anything that can be carried, piled or sold: fruit slices, juice cups, money bills.</summary>
    public class StackItem : MonoBehaviour
    {
        public ItemType type;
        [Tooltip("Vertical space this item takes in a stack.")]
        public float height = 0.14f;
        [Tooltip("Money value (bills only).")]
        public int value;

        [System.NonSerialized] public bool inTransit;
        [System.NonSerialized] public bool onGround;

        public void Despawn()
        {
            inTransit = false;
            onGround = false;
            Pool.Despawn(gameObject);
        }
    }
}

