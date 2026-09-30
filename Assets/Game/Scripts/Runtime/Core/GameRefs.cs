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
        [Tooltip("Berry Cake Shop: batter bowls and cakes (index = FruitKind, only berries are set).")]
        public StackItem[] batterPrefabs;
        public StackItem[] cakePrefabs;
        public StackItem moneyPrefab;

        [Header("Art")]
        public Sprite[] fruitIcons;
        public Sprite juiceIcon;
        [Tooltip("Per-juice cup icons (index = FruitKind).")]
        public Sprite[] juiceIcons;
        public Sprite moneyIcon;
        [Tooltip("Cake icons (index = FruitKind, berries only).")]
        public Sprite[] cakeIcons;
        public Sprite appleIcon;
        public Sprite foxIcon;
        [Tooltip("Upgrade icon lookup: keys match UpgradeDef.icon.")]
        public string[] upgradeIconKeys;
        public Sprite[] upgradeIconSprites;
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

        public Sprite JuiceIcon(FruitKind k)
        {
            int i = (int)k;
            return juiceIcons != null && i < juiceIcons.Length && juiceIcons[i] != null ? juiceIcons[i] : juiceIcon;
        }

        public Sprite CakeIcon(FruitKind k)
        {
            int i = (int)k;
            return cakeIcons != null && i < cakeIcons.Length && cakeIcons[i] != null ? cakeIcons[i] : FruitIcon(k);
        }

        /// <summary>Icon of what a product line sells for a kind (juice cup or cake).</summary>
        public Sprite ProductIcon(ProductLine line, FruitKind k) => line == ProductLine.Cake ? CakeIcon(k) : JuiceIcon(k);

        public Sprite FruitIcon(FruitKind k)
        {
            int i = (int)k;
            return fruitIcons != null && i < fruitIcons.Length ? fruitIcons[i] : null;
        }

        public Sprite UpgradeIcon(string key)
        {
            if (upgradeIconKeys != null)
                for (int i = 0; i < upgradeIconKeys.Length; i++)
                    if (upgradeIconKeys[i] == key) return i < upgradeIconSprites.Length ? upgradeIconSprites[i] : null;
            return null;
        }

        public StackItem SpawnItem(ItemType type, Vector3 pos, Quaternion rot)
        {
            int f = (int)type.Fruit();
            StackItem prefab = type == ItemType.Money ? moneyPrefab
                : type.IsSlice() ? slicePrefabs[f]
                : type.IsBatter() ? batterPrefabs[f]
                : type.IsCake() ? cakePrefabs[f]
                : juicePrefabs[f];
            var it = Pool.Spawn(prefab, pos, rot);
            it.inTransit = false;
            it.onGround = false;
            return it;
        }
    }
}
