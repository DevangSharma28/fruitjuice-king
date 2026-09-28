using UnityEngine;

namespace JuiceKing
{
    /// <summary>Sales shelf. Juice is stocked from behind, customers buy from the front.</summary>
    public class Counter : MonoBehaviour, IItemReceiver
    {
        public ItemPile display;
        public Transform servePoint;
        public DropZone dropZone;

        public bool Accepts(ItemType t) => t.IsJuice();
        public bool HasSpace => display.HasSpace;

        public void Receive(StackItem item, Carrier from)
        {
            display.Add(item, 1.0f, 0.3f);
            if (from != null && from.isPlayer) Sfx.Play(SfxId.Drop, 0.35f, 1.2f);
        }

        public bool Has(FruitKind k) => display.Contains(t => t == ItemTypes.Juice(k));

        public StackItem TakeJuice(FruitKind k) => display.TakeLast(t => t == ItemTypes.Juice(k));
    }
}
