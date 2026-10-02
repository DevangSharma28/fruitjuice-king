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
        /// <summary>This desk has completed a delivery in this world (the scripted easy first order is not repeated). v4.</summary>
        public bool anyDone;
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
        // v4
        /// <summary>Money customers paid that was still lying on the cash piles at the last save (re-deposited on load).</summary>
        public long pendingCash;
        /// <summary>Smoothed sales income per minute (customers + trucks). Drives offline earnings and Free Cash.</summary>
        public float incomePerMin;
        /// <summary>Offline earnings computed but not claimed yet (app closed with the Welcome Back card up).</summary>
        public long offlinePending;
        /// <summary>Seconds left on the 2x CASH / TURBO boosts and the free cash / unlock assist cooldowns.</summary>
        public float boostCash, boostTurbo, freeCashCooldown, assistCooldown;

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
        /// <summary>The last world (Berry Blast) was completed and its reward granted. v4.</summary>
        public bool world2Complete;
        public LifetimeStats stats = new LifetimeStats();
        /// <summary>Premium currency, kept across every world. Saves written before it existed start with the welcome gift.</summary>
        public int goldenApples = Economy.StartingApples;
        /// <summary>Ad Tickets (bought in the shop): each one claims a rewarded-ad reward without watching the ad.</summary>
        public int adTickets;
        /// <summary>VIP: Skip Ads purchased (<c>jk_remove_ads</c>): every rewarded offer is granted without a video.</summary>
        public bool noAds;
        /// <summary>Store transaction ids already granted (newest last, capped): a re-delivered purchase never grants twice. v4.</summary>
        public List<string> iapTransactions = new List<string>();
        /// <summary>The first-launch analytics event was sent. v4.</summary>
        public bool firstLaunchLogged;
    }

    public enum UpgradeKind { Saw, Bag, Speed, Price, Counter }

    /// <summary>Owns money, progression and persistence.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        const string SaveKey = "juiceking_save_v1";
        /// <summary>Debug world switcher: each world's parked progress (separate key, never part of the real save).</summary>
        public const string DebugWorldsKey = "juiceking_debug_worlds";
        public const int SaveVersion = 4;

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
        /// <summary>(newValue, delta)</summary>
        public event Action<int, int> TicketsChanged;
        public event Action NoAdsChanged;

        readonly HashSet<FruitKind> _activeJuicers = new HashSet<FruitKind>();
        readonly List<FruitKind> _activeJuicerList = new List<FruitKind>();
        readonly HashSet<FruitKind> _activeFields = new HashSet<FruitKind>();
        readonly List<FruitKind> _orderable = new List<FruitKind>();
        readonly HashSet<FruitKind> _cakeMixers = new HashSet<FruitKind>();
        readonly HashSet<FruitKind> _ovens = new HashSet<FruitKind>();
        readonly List<FruitKind> _orderableCakes = new List<FruitKind>();
        bool _dirty;
        float _saveTimer;
        float _backupTimer;
        // Set once this scene's world was reset or swapped (expansion / debug switch): its cash piles and boosts no
        // longer belong to the save, which now describes the next world.
        bool _worldReset;
        long _incomeAccum;
        float _incomeT;
        const float IncomeSample = 10f;
        const float IncomeTau = 180f;

        public long Money => data.money;
        public IReadOnlyList<FruitKind> ActiveJuicers => _activeJuicerList;

        /// <summary>Fruit kinds customers may order: the juicer and its field are both open.</summary>
        public IReadOnlyList<FruitKind> OrderableKinds => _orderable;

        /// <summary>Cakes customers may order: the berry's field, cake mixer and oven are all open.</summary>
        public IReadOnlyList<FruitKind> OrderableCakes => _orderableCakes;

        public IReadOnlyList<FruitKind> Orderable(ProductLine line) => line == ProductLine.Cake ? _orderableCakes : _orderable;

        public int Apples => data.goldenApples;
        public int Tickets => data.adTickets;
        public bool NoAds => data.noAds;

        /// <summary>Seconds the player was away (0 on first launch), measured when the save loaded.</summary>
        public double AwaySeconds { get; private set; }

        /// <summary>Copy of the save outside PlayerPrefs (written atomically), used when the PlayerPrefs entry is corrupt.</summary>
        static string BackupPath => System.IO.Path.Combine(Application.persistentDataPath, "juiceking_save_backup.json");

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
            // A world scene starts clean: no popup of the previous scene can keep the player frozen or the game paused.
            InputJoystick.ClearBlocks();
            Platform.ResetPauses();
            CashPile.ResetRestored();
        }

        void Start()
        {
            if (Redirecting) return;
            if (!data.firstLaunchLogged)
            {
                data.firstLaunchLogged = true;
                _dirty = true;
                Analytics.Log(Analytics.FirstLaunch);
            }
            // Purchases the store reported while the boot screen was up are granted now that a world runs.
            Iap.FlushQueued();
        }

        void Update()
        {
            _saveTimer += Time.unscaledDeltaTime;
            if (_dirty && _saveTimer > 2f) Save();

            // Sales income per minute, smoothed over a few minutes (offline earnings and Free Cash scale with it).
            // Clamped: the first frame after the app comes back from the background must not count as a long idle sample.
            _incomeT += Mathf.Min(Time.unscaledDeltaTime, 0.5f);
            if (_incomeT >= IncomeSample && Platform.GameplayRunning || _incomeT >= IncomeSample * 3f)
            {
                float rate = _incomeAccum * (60f / _incomeT);
                float k = 1f - Mathf.Exp(-_incomeT / IncomeTau);
                data.incomePerMin = data.incomePerMin <= 0f ? rate : Mathf.Lerp(data.incomePerMin, rate, k);
                _incomeAccum = 0;
                _incomeT = 0f;
            }

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

        /// <summary>Saves and refreshes the backup file now (purchases: the backup must never be older than paid goods).</summary>
        public void SaveWithBackup()
        {
            _backupTimer = 0f;
            Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            _backupTimer = 0f;
            Save();
        }

        void OnApplicationQuit()
        {
            _backupTimer = 0f;
            Save();
        }

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

        /// <summary>Money the business earned (a sale or a truck payout), counted when it is paid, not when collected.</summary>
        public void NotifyIncome(long amount)
        {
            if (amount <= 0) return;
            _incomeAccum += amount;
            SessionIncome += amount;
        }

        /// <summary>Everything the business earned since this scene started (QA / balance measurements).</summary>
        public long SessionIncome { get; private set; }

        /// <summary>Smoothed income per minute; never negative.</summary>
        public float IncomePerMin => Mathf.Max(0f, data.incomePerMin);

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

        // ---------------- Ad Tickets / Remove Ads (shop) ----------------

        public void AddTickets(int n)
        {
            if (n == 0) return;
            data.adTickets = Mathf.Max(0, data.adTickets + n);
            _dirty = true;
            Save();
            TicketsChanged?.Invoke(data.adTickets, n);
        }

        public bool TrySpendTicket()
        {
            if (data.adTickets <= 0) return false;
            AddTickets(-1);
            return true;
        }

        public void SetNoAds(bool on)
        {
            if (data.noAds == on) return;
            data.noAds = on;
            _dirty = true;
            Save();
            NoAdsChanged?.Invoke();
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

        /// <summary>
        /// Money owed for the time away (plus anything computed earlier but never claimed). Helpers keep the business
        /// running, so it scales with the measured income and how many helpers are hired (<see cref="Economy.OfflinePerSecond"/>),
        /// capped at <see cref="Economy.OfflineMaxSeconds"/>. Computed once per launch: call <see cref="ConsumeOffline"/>
        /// when the Welcome Back card opens and <see cref="ClaimOffline"/> when it is collected.
        /// </summary>
        public long OfflineEarnings()
        {
            long owed = Math.Max(0, data.offlinePending);
            if (AwaySeconds < Balance.OfflineMinSeconds) return owed;
            int helpers = 0, mixers = 1;
            foreach (var id in Economy.HelperIds) if (IsUnlocked(id)) helpers++;
            foreach (var id in Economy.MixerIds) if (IsUnlocked(id)) mixers++;
            double secs = Math.Min(AwaySeconds, Economy.OfflineMaxSeconds);
            long fresh = (long)(secs * Economy.OfflinePerSecond(mixers, helpers, Economy.HelperIds.Length));
            return owed + Math.Max(0, fresh);
        }

        /// <summary>Seconds away measured when the save loaded (not consumed: fox regrowth also reads it).</summary>
        public double LoadedAwaySeconds { get; private set; }

        /// <summary>
        /// Takes the offline amount for the Welcome Back card: it is parked in the save until claimed, so closing the
        /// app with the card up does not lose it, and it can never be paid twice.
        /// </summary>
        public void ConsumeOffline(long amount)
        {
            AwaySeconds = 0;
            data.offlinePending = Math.Max(0, amount);
            Save();
        }

        /// <summary>The Welcome Back card was collected.</summary>
        public void ClaimOffline(bool doubled)
        {
            if (data.offlinePending > 0)
                Analytics.Log(Analytics.OfflineClaimed, "amount", data.offlinePending, "doubled", doubled);
            data.offlinePending = 0;
            _dirty = true;
        }

        // ---------------- Persistence ----------------

        public void Save()
        {
            _dirty = false;
            _saveTimer = 0f;
            if (!_worldReset && !Redirecting)
            {
                // Uncollected bills on the cash piles and the running boosts are part of the save.
                if (CashPile.Restored) data.pendingCash = CashPile.TotalAll();
                if (Boosts.I != null) Boosts.I.WriteTo(data);
            }
            long now = DateTime.UtcNow.Ticks;
            // Clock set backwards: keep the later timestamp so moving the clock back and forth cannot farm offline
            // earnings (a clock more than two days "in the future" is assumed fixed and is accepted).
            if (now >= data.lastSeenTicks || data.lastSeenTicks - now > TimeSpan.TicksPerDay * 2) data.lastSeenTicks = now;
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            _backupTimer -= 1f;
            if (_backupTimer <= 0f)
            {
                _backupTimer = 5f;
                WriteBackup(json);
            }
        }

        static void WriteBackup(string json)
        {
            try
            {
                string path = BackupPath, tmp = path + ".tmp";
                System.IO.File.WriteAllText(tmp, json);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                System.IO.File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] backup failed: " + e.Message);
            }
        }

        static void DeleteBackup()
        {
            try
            {
                if (System.IO.File.Exists(BackupPath)) System.IO.File.Delete(BackupPath);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Reads the save: PlayerPrefs first, the backup file when the PlayerPrefs entry is missing or unreadable.
        /// A corrupt PlayerPrefs entry is kept under its own key for support instead of being silently overwritten.
        /// Returns null when there is no save at all (first launch).
        /// </summary>
        static SaveData ReadSave()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var d = JsonUtility.FromJson<SaveData>(json);
                    if (d != null) return d;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Save] save unreadable, trying the backup: " + e.Message);
                }
                PlayerPrefs.SetString(SaveKey + "_corrupt", json);
            }
            try
            {
                if (System.IO.File.Exists(BackupPath))
                {
                    var d = JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(BackupPath));
                    if (d != null)
                    {
                        Debug.LogWarning("[Save] restored progress from the backup file");
                        return d;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] backup unreadable: " + e.Message);
            }
            return null;
        }

        void Load()
        {
            data = ReadSave() ?? new SaveData { money = startMoney };

            if (data.stats == null) data.stats = new LifetimeStats();
            if (data.delivery == null) data.delivery = new DeliverySave();
            if (data.delivery2 == null) data.delivery2 = new DeliverySave();
            if (data.upIds == null) data.upIds = new List<string>();
            if (data.upLevels == null) data.upLevels = new List<int>();
            if (data.foxKinds == null) data.foxKinds = new List<int>();
            if (data.foxTimers == null) data.foxTimers = new List<float>();
            if (data.iapTransactions == null) data.iapTransactions = new List<string>();
            if (data.paidIds == null) data.paidIds = new List<string>();
            if (data.paidAmounts == null) data.paidAmounts = new List<int>();
            if (data.unlocked == null) data.unlocked = new List<string>();
            // Parallel lists must stay the same length (a hand-edited or truncated save could break them).
            if (data.upLevels.Count != data.upIds.Count)
            {
                int n = Mathf.Min(data.upIds.Count, data.upLevels.Count);
                data.upIds.RemoveRange(n, data.upIds.Count - n);
                data.upLevels.RemoveRange(n, data.upLevels.Count - n);
            }
            if (data.paidAmounts.Count != data.paidIds.Count)
            {
                int n = Mathf.Min(data.paidIds.Count, data.paidAmounts.Count);
                data.paidIds.RemoveRange(n, data.paidIds.Count - n);
                data.paidAmounts.RemoveRange(n, data.paidAmounts.Count - n);
            }
            if (data.foxTimers.Count != data.foxKinds.Count)
            {
                data.foxKinds.Clear();
                data.foxTimers.Clear();
            }
            // Currencies can never be negative, whatever the save says.
            data.goldenApples = Mathf.Max(0, data.goldenApples);
            data.adTickets = Mathf.Max(0, data.adTickets);
            if (data.money < 0) data.money = 0;
            if (data.pendingCash < 0) data.pendingCash = 0;
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
            // v4 (release pass) only added fields with safe defaults (pending cash, income, boosts, IAP ledger).
            data.saveVersion = SaveVersion;

            AwaySeconds = 0;
            if (data.lastSeenTicks > 0)
            {
                var away = DateTime.UtcNow - new DateTime(data.lastSeenTicks, DateTimeKind.Utc);
                if (away.TotalSeconds > 0) AwaySeconds = away.TotalSeconds;
            }
            LoadedAwaySeconds = AwaySeconds;
        }

        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(DebugWorldsKey);
            PlayerPrefs.Save();
            DeleteBackup();
            data = new SaveData { money = startMoney };
            _dirty = false;
            _worldReset = true;
            LoadingScreen.LoadSavedWorld();
        }

        /// <summary>Which world the save is in, read without loading a scene (the boot loading screen uses it).</summary>
        public static int PeekSavedExpansion()
        {
            var d = ReadSave();
            return d != null ? Mathf.Clamp(d.expansion, 0, ExpansionManager.WorldCount - 1) : 0;
        }

        /// <summary>Deletes the save and its backup file (Editor menu "Reset Save Data", tests).</summary>
        public static void DeleteSaveFiles()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(DebugWorldsKey);
            PlayerPrefs.Save();
            DeleteBackup();
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
            ResetWorldProgress(expansion, startingMoney);
            Save();
        }

        /// <summary>Per-world progress back to a fresh start in <paramref name="expansion"/> (meta data is kept).</summary>
        void ResetWorldProgress(int expansion, long startingMoney)
        {
            _worldReset = true;
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
            data.pendingCash = 0;
            data.incomePerMin = 0f;
            data.offlinePending = 0;
            // Boosts the player watched ads for carry over into the next world.
            if (Boosts.I != null) Boosts.I.WriteTo(data);
        }

        // ---------------- debug world switcher (Settings > DEBUG, Editor and development builds) ----------------

        [Serializable]
        class DebugWorlds
        {
            public List<string> worlds = new List<string>();
        }

        /// <summary>
        /// Jumps to another world for testing. The current world's progress is parked under <see cref="DebugWorldsKey"/>
        /// and restored when you come back; Golden Apples, Ad Tickets, Remove Ads, lifetime stats and world archives are
        /// shared. A world never visited starts fresh (as on entering it), without marking any world complete.
        /// </summary>
        public void DebugSwitchWorld(int world)
        {
            world = Mathf.Clamp(world, 0, ExpansionManager.WorldCount - 1);
            if (world == data.expansion || Redirecting) return;

            DebugWorlds slots = null;
            try { slots = JsonUtility.FromJson<DebugWorlds>(PlayerPrefs.GetString(DebugWorldsKey, "")); }
            catch { }
            if (slots == null) slots = new DebugWorlds();
            while (slots.worlds.Count < ExpansionManager.WorldCount) slots.worlds.Add("");
            slots.worlds[Mathf.Clamp(data.expansion, 0, ExpansionManager.WorldCount - 1)] = JsonUtility.ToJson(data);

            SaveData parked = null;
            if (!string.IsNullOrEmpty(slots.worlds[world]))
            {
                try { parked = JsonUtility.FromJson<SaveData>(slots.worlds[world]); }
                catch { }
            }
            if (parked != null)
            {
                // Shared (meta) data always comes from the live save.
                parked.goldenApples = data.goldenApples;
                parked.adTickets = data.adTickets;
                parked.noAds = data.noAds;
                parked.stats = data.stats;
                parked.world0Complete = data.world0Complete;
                parked.world0Archive = data.world0Archive;
                parked.world1Complete = data.world1Complete;
                parked.world1Archive = data.world1Archive;
                parked.world2Complete = data.world2Complete;
                parked.saveVersion = data.saveVersion;
                parked.iapTransactions = data.iapTransactions;
                parked.firstLaunchLogged = data.firstLaunchLogged;
                parked.expansion = world;
                _worldReset = true;
                data = parked;
            }
            else ResetWorldProgress(world, Economy.StartMoney(world));

            PlayerPrefs.SetString(DebugWorldsKey, JsonUtility.ToJson(slots));
            Save();
            LoadingScreen.LoadSavedWorld();
        }
    }
}
