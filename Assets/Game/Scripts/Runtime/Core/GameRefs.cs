using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Scene-wide references wired up by the scene builder.</summary>
    [DefaultExecutionOrder(-200)]
    public class GameRefs : MonoBehaviour
    {
        public static GameRefs I { get; private set; }

        [Header("Item prefabs (index = FruitKind)")]
        public StackItem[] slicePrefabs;
        public StackItem[] juicePrefabs;
        public StackItem moneyPrefab;

        [Header("Art")]
        public Sprite[] fruitIcons;
        public Sprite juiceIcon;
        public Sprite moneyIcon;
        public Material particleMaterial;
        [Tooltip("Particle materials indexed by FxShape.")]
        public Material[] fxMaterials;
        public TMP_FontAsset font;
        public FloatingText floatingTextPrefab;
        public GameObject[] customerPrefabs;

        [Header("Scene")]
        public Camera mainCamera;
        public Carrier player;
        public Counter counter;
        public CashPile cashPile;

        void Awake() => I = this;

        public StackItem SpawnItem(ItemType type, Vector3 pos, Quaternion rot)
        {
            StackItem prefab = type == ItemType.Money ? moneyPrefab
                : type.IsSlice() ? slicePrefabs[(int)type.Fruit()]
                : juicePrefabs[(int)type.Fruit()];
            var it = Pool.Spawn(prefab, pos, rot);
            it.inTransit = false;
            it.onGround = false;
            return it;
        }
    }
}
