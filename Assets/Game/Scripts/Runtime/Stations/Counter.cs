using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>
    /// Sales shelf. Stock goes in from behind, customers buy from the front. Sells one <see cref="ProductLine"/>: juice
    /// (stack grows with the Counter upgrade) or cakes (the Berry Cake Shop's pastry case, Pastry Case upgrade).
    /// </summary>
    public class Counter : MonoBehaviour, IItemReceiver
    {
        public ProductLine line;
        public ItemPile display;
        public Transform servePoint;
        public DropZone dropZone;
        [Tooltip("Optional \"12/24\" readout above the counter.")]
        public TextMeshPro capacityText;
        public Transform body;

        int _level = -1;
        int _shownCount = -1, _shownCap = -1;
        Vector3 _bodyScale = Vector3.one;

        void Awake()
        {
            if (body != null) _bodyScale = body.localScale;
        }

        void Start()
        {
            if (GameManager.I != null) GameManager.I.UpgradesChanged += OnUpgrade;
        }

        void OnDestroy()
        {
            if (GameManager.I != null) GameManager.I.UpgradesChanged -= OnUpgrade;
        }

        int Layers => line == ProductLine.Cake ? Economy.CakeCaseLayers : Economy.CounterLayers;

        void OnUpgrade()
        {
            int lvl = Layers;
            if (lvl == _level) return;
            bool grew = _level >= 0;
            int added = grew ? (lvl - _level) * display.columns * display.rows : 0;
            SyncLayers();
            if (!grew) return;
            if (body != null) Tweener.Punch(body, 0.08f, 0.35f, _bodyScale);
            Fx.Stars(display.transform.position + Vector3.up * 0.8f, 14);
            Fx.Ring(transform.position, new Color(1f, 0.9f, 0.4f, 0.8f), 6f);
            FloatingText.Show("STACK +" + added, display.transform.position + Vector3.up * 1.6f, new Color(1f, 0.9f, 0.3f), 1.1f, 1.2f, 1.1f);
        }

        void SyncLayers()
        {
            _level = Layers;
            display.layers = _level;
        }

        void Update()
        {
            if (_level < 0 && GameManager.I != null) SyncLayers();
            if (capacityText == null) return;
            int n = display.Count, cap = display.Capacity;
            if (n == _shownCount && cap == _shownCap) return;
            _shownCount = n;
            _shownCap = cap;
            bool full = n >= cap;
            capacityText.text = full ? "FULL" : n + "/" + cap;
            capacityText.color = full ? new Color(1f, 0.4f, 0.35f) : Color.white;
        }

        public bool Accepts(ItemType t) => t.IsProduct(line);
        public bool HasSpace => display.HasSpace;

        public void Receive(StackItem item, Carrier from)
        {
            bool player = from != null && from.isPlayer;
            display.Add(item, 1.0f, 0.3f, () =>
            {
                Tweener.Punch(item.transform, 0.18f, 0.2f, Vector3.one);
                if (player) Sfx.Play(SfxId.Tink, 0.32f, Random.Range(1.0f, 1.2f));
            });
        }

        public ItemType ProductOf(FruitKind k) => ItemTypes.Product(line, k);

        public bool Has(FruitKind k)
        {
            var want = ProductOf(k);
            return display.Contains(t => t == want);
        }

        /// <summary>Hand one item of this kind to a customer (juice or cake, whatever this counter sells).</summary>
        public StackItem TakeProduct(FruitKind k)
        {
            var want = ProductOf(k);
            return display.TakeLast(t => t == want);
        }
    }
}
