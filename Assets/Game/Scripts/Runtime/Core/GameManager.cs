using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuiceKing
{
    [Serializable]
    public class SaveData
    {
        public long money;
        public List<string> unlocked = new List<string>();
        public List<string> paidIds = new List<string>();
        public List<int> paidAmounts = new List<int>();
        public int sawLevel, bagLevel, speedLevel, priceLevel, counterLevel;
        public int tutorialStep;
        public int totalSold;
        public long lastSeenTicks;
    }

    public enum UpgradeKind { Saw, Bag, Speed, Price, Counter }

    /// <summary>Owns money, progression and persistence.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        const string SaveKey = "juiceking_save_v1";

        public static GameManager I { get; private set; }

        public long startMoney = 0;
        public SaveData data = new SaveData();

        public event Action<long, long> MoneyChanged; // (newValue, delta)
        public event Action UpgradesChanged;
        public event Action<string> Unlocked;
        public event Action JuiceSold;

        readonly HashSet<FruitKind> _activeJuicers = new HashSet<FruitKind>();
        readonly List<FruitKind> _activeJuicerList = new List<FruitKind>();
        readonly HashSet<FruitKind> _activeFields = new HashSet<FruitKind>();
        readonly List<FruitKind> _orderable = new List<FruitKind>();
        bool _dirty;
        float _saveTimer;

        public long Money => data.money;
        public IReadOnlyList<FruitKind> ActiveJuicers => _activeJuicerList;

        /// <summary>Fruit kinds customers may order: the juicer and its field are both open.</summary>
        public IReadOnlyList<FruitKind> OrderableKinds => _orderable;

        /// <summary>Seconds the player was away (0 on first launch), measured when the save loaded.</summary>
        public double AwaySeconds { get; private set; }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Load();
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
            _dirty = true;
            JuiceSold?.Invoke();
        }

        /// <summary>Sale price of one juice cup, including the recipe upgrade.</summary>
        public int JuicePrice(FruitKind k) => Mathf.RoundToInt(Balance.JuicePrice[(int)k] * Balance.PriceMult(data.priceLevel));

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

        void RefreshOrderable()
        {
            _orderable.Clear();
            foreach (var k in _activeJuicerList)
                if (_activeFields.Contains(k)) _orderable.Add(k);
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
            switch (k)
            {
                case UpgradeKind.Saw: data.sawLevel++; break;
                case UpgradeKind.Bag: data.bagLevel++; break;
                case UpgradeKind.Speed: data.speedLevel++; break;
                case UpgradeKind.Price: data.priceLevel++; break;
                default: data.counterLevel++; break;
            }
            _dirty = true;
            Save();
            UpgradesChanged?.Invoke();
            return true;
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

        static readonly string[] HelperIds = { "hire_waiter", "farmer_orange", "farmer_melon", "farmer_pine" };
        static readonly string[] JuicerIds = { "melon_juicer", "pine_juicer" };

        /// <summary>Money earned while away, based on hired helpers. Zero when nothing is owed.</summary>
        public long OfflineEarnings()
        {
            if (AwaySeconds < Balance.OfflineMinSeconds) return 0;
            int helpers = 0, juicers = 1;
            foreach (var id in HelperIds) if (IsUnlocked(id)) helpers++;
            foreach (var id in JuicerIds) if (IsUnlocked(id)) juicers++;
            double secs = Math.Min(AwaySeconds, Balance.OfflineMaxSeconds);
            return (long)(secs * Balance.OfflineRate(juicers, helpers) * Balance.PriceMult(data.priceLevel));
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
