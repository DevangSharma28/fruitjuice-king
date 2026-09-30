using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuiceKing
{
    /// <summary>Lifetime statistics: never reset by an expansion.</summary>
    [Serializable]
    public class LifetimeStats
    {
        public long earned;
        public int juiceMade;
        public int fruitHarvested;
        public int customersServed;
        public int deliveries;
        public long deliveryEarned;
        public int cakesBaked;
        public int cakesSold;
        public int foxRaids;
        public int applesEarned;
    }

    /// <summary>The truck currently at (or due at) the delivery bay.</summary>
    [Serializable]
    public class DeliverySave
    {
        public bool active;
        public int truck;
        public int kind;
        public int qty;
        public int delivered;
        public long reward;
        public string client;
        public float cooldown = 20f;
        public float waited;
        /// <summary>What the truck wants (<see cref="ProductLine"/>): 0 = juice (every save before the Berry Blast), 1 = cake.</summary>
        public int line;
    }

    [Serializable]
    public class SaveData
    {
        // ---- current world (reset when entering the next expansion)
        public long money;
        public List<string> unlocked = new List<string>();
        public List<string> paidIds = new List<string>();
        public List<int> paidAmounts = new List<int>();
        public int sawLevel, bagLevel, speedLevel, priceLevel, counterLevel;
        public int tutorialStep;
        public int totalSold;
        public long lastSeenTicks;
        /// <summary>Upgrade levels of data-driven upgrade trees (Expansion 1+), keyed by id.</summary>
        public List<string> upIds = new List<string>();
        public List<int> upLevels = new List<int>();
        public DeliverySave delivery = new DeliverySave();
        /// <summary>Second delivery desk (Berry Blast).</summary>
        public DeliverySave delivery2 = new DeliverySave();
        public bool introSeen;
        /// <summary>Cakes sold in this world (drives cake order sizes).</summary>
        public int cakesSold;
        /// <summary>Fox raids (Berry Blast): the first one plays a cutscene; damaged farms regrow on these timers.</summary>
        public bool foxIntroSeen;
        public float foxNext = -1f;
        public List<int> foxKinds = new List<int>();
        public List<float> foxTimers = new List<float>();

        // ---- permanent / meta
        /// <summary>0 in saves written before Expansion 1 (field absent), <see cref="GameManager.SaveVersion"/> after migration.</summary>
        public int saveVersion;
        /// <summary>0 = original farm, 1 = Tropical Farm.</summary>
        public int expansion;
        public bool world0Complete;
        /// <summary>JSON snapshot of the finished original farm.</summary>
        public string world0Archive;
        public bool world1Complete;
        public string world1Archive;
        public LifetimeStats stats = new LifetimeStats();
        /// <summary>Premium currency, kept across every world. Saves written before it existed start with the welcome gift.</summary>
        public int goldenApples = Economy.StartingApples;
    }

    public enum UpgradeKind { Saw, Bag, Speed, Price, Counter }

    /// <summary>Owns money, progression and persistence.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        const string SaveKey = "juiceking_save_v1";
        public const int SaveVersion = 3;

        public static GameManager I { get; private set; }

        public long startMoney = 0;
        [Tooltip("Which expansion this scene hosts (0 = original farm, 1 = Tropical Farm).")]
        public int sceneExpansion;
        public SaveData data = new SaveData();

        /// <summary>True while this scene is handing over to the scene of the saved expansion.</summary>
        public static bool Redirecting { get; private set; }

        public event Action<long, long> MoneyChanged; // (newValue, delta)
        public event Action UpgradesChanged;
        public event Action<string> Unlocked;
        public event Action JuiceSold;
        /// <summary>(newValue, delta)</summary>
        public event Action<int, int> ApplesChanged;

        readonly HashSet<FruitKind> _activeJuicers = new HashSet<FruitKind>();
        readonly List<FruitKind> _activeJuicerList = new List<FruitKind>();
        readonly HashSet<FruitKind> _activeFields = new HashSet<FruitKind>();
        readonly List<FruitKind> _orderable = new List<FruitKind>();
        readonly HashSet<FruitKind> _cakeMixers = new HashSet<FruitKind>();
        readonly HashSet<FruitKind> _ovens = new HashSet<FruitKind>();
        readonly List<FruitKind> _orderableCakes = new List<FruitKind>();
        bool _dirty;
        float _saveTimer;

        public long Money => data.money;
        public IReadOnlyList<FruitKind> ActiveJuicers => _activeJuicerList;

        /// <summary>Fruit kinds customers may order: the juicer and its field are both open.</summary>
        public IReadOnlyList<FruitKind> OrderableKinds => _orderable;

        /// <summary>Cakes customers may order: the berry's field, cake mixer and oven are all open.</summary>
        public IReadOnlyList<FruitKind> OrderableCakes => _orderableCakes;

        public IReadOnlyList<FruitKind> Orderable(ProductLine line) => line == ProductLine.Cake ? _orderableCakes : _orderable;

        public int Apples => data.goldenApples;

        /// <summary>Seconds the player was away (0 on first launch), measured when the save loaded.</summary>
        public double AwaySeconds { get; private set; }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Load();

            // The save belongs to another world: switch scenes before anything else in this one wakes up.
            if (data.expansion != sceneExpansion)
            {
                Redirecting = true;
                foreach (var go in gameObject.scene.GetRootGameObjects())
                    if (go != gameObject && go.GetComponent<Camera>() == null) go.SetActive(false);
                foreach (var mb in GetComponents<MonoBehaviour>())
                    if (mb != this && !(mb is GameRefs)) mb.enabled = false;
                enabled = false;
                SceneManager.LoadScene(ExpansionManager.SceneName(data.expansion));
                return;
            }
            Redirecting = false;
        }

        void Update()
        {
            _saveTimer += Time.unscaledDeltaTime;
            if (_dirty && _saveTimer > 2f) Save();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.mKey.wasPressedThisFrame) AddMoney(500);
                if (kb.f9Key.wasPressedThisFrame) ResetProgress();
                if (kb.f10Key.wasPressedThisFrame) ExpansionManager.DebugComplete();
            }
#endif
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();

        // ---------------- Money ----------------

        public void AddMoney(long amount)
        {
            if (amount == 0) return;
            data.money += amount;
            if (amount > 0) data.stats.earned += amount;
            _dirty = true;
            MoneyChanged?.Invoke(data.money, amount);
        }

        public bool TrySpend(long amount)
        {
            if (data.money < amount) return false;
            AddMoney(-amount);
            return true;
        }

        public void NotifyJuiceSold(int count)
        {
            data.totalSold += count;
            data.stats.customersServed++;
            _dirty = true;
            JuiceSold?.Invoke();
        }

        /// <summary>A cake customer paid for <paramref name="count"/> cakes.</summary>
        public void NotifyCakeSold(int count)
        {
            data.cakesSold += count;
            data.stats.cakesSold += count;
            data.stats.customersServed++;
            _dirty = true;
        }

        public void NotifyCakeBaked() => data.stats.cakesBaked++;

        public void NotifyJuiceMade() => data.stats.juiceMade++;

        // ---------------- Golden Apples (premium) ----------------

        public void AddApples(int n)
        {
            if (n == 0) return;
            data.goldenApples = Mathf.Max(0, data.goldenApples + n);
            if (n > 0) data.stats.applesEarned += n;
            _dirty = true;
            Save();
            ApplesChanged?.Invoke(data.goldenApples, n);
        }

        public bool TrySpendApples(int n)
        {
            if (n <= 0) return true;
            if (data.goldenApples < n) return false;
            AddApples(-n);
            return true;
        }
        public void NotifyFruitHarvested() => data.stats.fruitHarvested++;

        /// <summary>Sale price of one juice cup, including the recipe / juice-price upgrade.</summary>
        public int JuicePrice(FruitKind k) => Mathf.RoundToInt(Balance.JuicePrice[(int)k] * Economy.PriceMult);

        /// <summary>Sale price of one cake, including the Cake Recipe upgrade.</summary>
        public int CakePrice(FruitKind k) => Mathf.RoundToInt(Balance.CakePrice[(int)k] * Economy.CakePriceMult);

        public int ProductPrice(ProductLine line, FruitKind k) => line == ProductLine.Cake ? CakePrice(k) : JuicePrice(k);

        // ---------------- Data-driven upgrades (Expansion 1+) ----------------

        public int GetUpgrade(string id)
        {
            int i = data.upIds.IndexOf(id);
            return i >= 0 ? data.upLevels[i] : 0;
        }

        public void SetUpgrade(string id, int level)
        {
            int i = data.upIds.IndexOf(id);
            if (i < 0)
            {
                data.upIds.Add(id);
                data.upLevels.Add(level);
            }
            else data.upLevels[i] = level;
            _dirty = true;
        }

        /// <summary>Buy the next level of a data-driven upgrade.</summary>
        public bool TryBuy(UpgradeDef def)
        {
            int cost = def.Cost();
            if (cost < 0 || !TrySpend(cost)) return false;
            def.Apply();
            _dirty = true;
            Save();
            UpgradesChanged?.Invoke();
            return true;
        }

        public void RaiseUpgradesChanged() => UpgradesChanged?.Invoke();

        // ---------------- Unlocks ----------------

        public bool IsUnlocked(string id) => data.unlocked.Contains(id);

        public int UnlockedCount => data.unlocked.Count;

        public void MarkUnlocked(string id)
        {
            if (!data.unlocked.Contains(id)) data.unlocked.Add(id);
            SetPaid(id, 0);
            _dirty = true;
            Save();
            Unlocked?.Invoke(id);
        }

        public int GetPaid(string id)
        {
            int i = data.paidIds.IndexOf(id);
            return i >= 0 ? data.paidAmounts[i] : 0;
        }

        public void SetPaid(string id, int amount)
        {
            int i = data.paidIds.IndexOf(id);
            if (i < 0)
            {
                if (amount == 0) return;
                data.paidIds.Add(id);
                data.paidAmounts.Add(amount);
            }
            else data.paidAmounts[i] = amount;
            _dirty = true;
        }

        public void RegisterJuicer(FruitKind kind)
        {
            if (_activeJuicers.Add(kind)) _activeJuicerList.Add(kind);
            RefreshOrderable();
        }

        public void UnregisterJuicer(FruitKind kind)
        {
            if (_activeJuicers.Remove(kind)) _activeJuicerList.Remove(kind);
            RefreshOrderable();
        }

        public void RegisterField(FruitKind kind)
        {
            _activeFields.Add(kind);
            RefreshOrderable();
        }

        public void UnregisterField(FruitKind kind)
        {
            _activeFields.Remove(kind);
            RefreshOrderable();
        }

        public bool HasJuicer(FruitKind kind) => _activeJuicers.Contains(kind);

        public void RegisterCakeMixer(FruitKind kind, bool on)
        {
            if (on) _cakeMixers.Add(kind);
            else _cakeMixers.Remove(kind);
            RefreshOrderable();
        }

        public void RegisterOven(FruitKind kind, bool on)
        {
            if (on) _ovens.Add(kind);
            else _ovens.Remove(kind);
            RefreshOrderable();
        }

        public bool HasCakeMixer(FruitKind kind) => _cakeMixers.Contains(kind);
        public bool HasField(FruitKind kind) => _activeFields.Contains(kind);

        void RefreshOrderable()
        {
            _orderable.Clear();
            foreach (var k in _activeJuicerList)
                if (_activeFields.Contains(k)) _orderable.Add(k);
            _orderableCakes.Clear();
            for (int k = 0; k < ItemTypes.FruitCount; k++)
            {
                var f = (FruitKind)k;
                if (_ovens.Contains(f) && _cakeMixers.Contains(f) && _activeFields.Contains(f)) _orderableCakes.Add(f);
            }
        }

        // ---------------- Upgrades ----------------

        public int GetLevel(UpgradeKind k) => k switch
        {
            UpgradeKind.Saw => data.sawLevel,
            UpgradeKind.Bag => data.bagLevel,
            UpgradeKind.Speed => data.speedLevel,
            UpgradeKind.Price => data.priceLevel,
            _ => data.counterLevel
        };

        public int GetCost(UpgradeKind k)
        {
            int lvl = GetLevel(k);
            if (lvl >= Balance.MaxUpgradeLevel) return -1;
            return k switch
            {
                UpgradeKind.Saw => Balance.SawCosts[lvl],
                UpgradeKind.Bag => Balance.BagCosts[lvl],
                UpgradeKind.Speed => Balance.SpeedCosts[lvl],
                UpgradeKind.Price => Balance.PriceCosts[lvl],
                _ => Balance.CounterCosts[lvl]
            };
        }

        public bool TryBuyUpgrade(UpgradeKind k)
        {
            int cost = GetCost(k);
            if (cost < 0 || !TrySpend(cost)) return false;
            ApplyClassicUpgrade(k);
            _dirty = true;
            Save();
            UpgradesChanged?.Invoke();
            return true;
        }

        /// <summary>Raise one of the original farm's upgrade levels (no payment).</summary>
        public void ApplyClassicUpgrade(UpgradeKind k)
        {
            switch (k)
            {
                case UpgradeKind.Saw: data.sawLevel++; break;
                case UpgradeKind.Bag: data.bagLevel++; break;
                case UpgradeKind.Speed: data.speedLevel++; break;
                case UpgradeKind.Price: data.priceLevel++; break;
                default: data.counterLevel++; break;
            }
            _dirty = true;
        }

        // ---------------- Tutorial ----------------

        public int TutorialStep
        {
            get => data.tutorialStep;
            set
            {
                data.tutorialStep = value;
                _dirty = true;
            }
        }

        // ---------------- Offline earnings ----------------

        /// <summary>Money earned while away, based on hired helpers. Zero when nothing is owed.</summary>
        public long OfflineEarnings()
        {
            if (AwaySeconds < Balance.OfflineMinSeconds) return 0;
            int helpers = 0, juicers = 1;
            foreach (var id in Economy.HelperIds) if (IsUnlocked(id)) helpers++;
            foreach (var id in Economy.MixerIds) if (IsUnlocked(id)) juicers++;
            double secs = Math.Min(AwaySeconds, Balance.OfflineMaxSeconds);
            return (long)(secs * Economy.OfflineRate(juicers, helpers) * Economy.PriceMult);
        }

        /// <summary>Only pay offline earnings once per session.</summary>
        public void ConsumeOffline() => AwaySeconds = 0;

        // ---------------- Persistence ----------------

        public void Save()
        {
            _dirty = false;
            _saveTimer = 0f;
            data.lastSeenTicks = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
                catch { data = new SaveData(); }
            }
            else
            {
                data = new SaveData { money = startMoney };
            }

            if (data.stats == null) data.stats = new LifetimeStats();
            if (data.delivery == null) data.delivery = new DeliverySave();
            if (data.delivery2 == null) data.delivery2 = new DeliverySave();
            if (data.upIds == null) data.upIds = new List<string>();
            if (data.upLevels == null) data.upLevels = new List<int>();
            if (data.foxKinds == null) data.foxKinds = new List<int>();
            if (data.foxTimers == null) data.foxTimers = new List<float>();
            // v1 saves had no lifetime stats: estimate them from cups sold (about $11 a cup, 2 slices per cup, ~3.5 slices
            // per fruit) so the completion screen does not show zeros.
            if (data.saveVersion < 2)
            {
                if (data.stats.customersServed == 0) data.stats.customersServed = data.totalSold / 2;
                if (data.stats.juiceMade == 0) data.stats.juiceMade = data.totalSold;
                if (data.stats.earned == 0) data.stats.earned = data.totalSold * 11L + data.money;
                if (data.stats.fruitHarvested == 0) data.stats.fruitHarvested = Mathf.RoundToInt(data.totalSold * 2f / 3.5f);
            }
            // v3 (Berry Blast) only added fields with safe defaults: Golden Apples start at the welcome gift.
            data.saveVersion = SaveVersion;

            AwaySeconds = 0;
            if (data.lastSeenTicks > 0)
            {
                var away = DateTime.UtcNow - new DateTime(data.lastSeenTicks, DateTimeKind.Utc);
                if (away.TotalSeconds > 0) AwaySeconds = away.TotalSeconds;
            }
        }

        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            data = new SaveData { money = startMoney };
            _dirty = false;
            LoadingScreen.LoadSavedWorld();
        }

        /// <summary>Which world the save is in, read without loading a scene (the boot loading screen uses it).</summary>
        public static int PeekSavedExpansion()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return 0;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                return d != null ? Mathf.Max(0, d.expansion) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Finish the current world and move to the next expansion: archive the finished world, reset per-world progress,
        /// keep lifetime stats and settings.
        /// </summary>
        public void BeginExpansion(int expansion, long startingMoney)
        {
            if (data.expansion == 0)
            {
                var snap = new SaveData
                {
                    money = data.money, unlocked = new List<string>(data.unlocked), sawLevel = data.sawLevel, bagLevel = data.bagLevel,
                    speedLevel = data.speedLevel, priceLevel = data.priceLevel, counterLevel = data.counterLevel,
                    tutorialStep = data.tutorialStep, totalSold = data.totalSold
                };
                data.world0Archive = JsonUtility.ToJson(snap);
                data.world0Complete = true;
            }
            else if (data.expansion == 1)
            {
                var snap = new SaveData
                {
                    money = data.money, unlocked = new List<string>(data.unlocked), upIds = new List<string>(data.upIds),
                    upLevels = new List<int>(data.upLevels), tutorialStep = data.tutorialStep, totalSold = data.totalSold
                };
                data.world1Archive = JsonUtility.ToJson(snap);
                data.world1Complete = true;
            }
            data.expansion = expansion;
            data.money = startingMoney;
            data.unlocked.Clear();
            data.paidIds.Clear();
            data.paidAmounts.Clear();
            data.sawLevel = data.bagLevel = data.speedLevel = data.priceLevel = data.counterLevel = 0;
            data.upIds.Clear();
            data.upLevels.Clear();
            data.tutorialStep = 0;
            data.totalSold = 0;
            data.delivery = new DeliverySave();
            data.delivery2 = new DeliverySave();
            data.introSeen = false;
            data.cakesSold = 0;
            data.foxIntroSeen = false;
            data.foxNext = -1f;
            data.foxKinds.Clear();
            data.foxTimers.Clear();
            Save();
        }
    }
}
