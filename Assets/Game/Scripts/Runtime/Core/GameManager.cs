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
        public int sawLevel, bagLevel, speedLevel;
        public int tutorialStep;
        public int totalSold;
    }

    public enum UpgradeKind { Saw, Bag, Speed }

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
        bool _dirty;
        float _saveTimer;

        public long Money => data.money;
        public IReadOnlyList<FruitKind> ActiveJuicers => _activeJuicerList;

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
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

        // ---------------- Unlocks ----------------

        public bool IsUnlocked(string id) => data.unlocked.Contains(id);

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
        }

        public void UnregisterJuicer(FruitKind kind)
        {
            if (_activeJuicers.Remove(kind)) _activeJuicerList.Remove(kind);
        }

        // ---------------- Upgrades ----------------

        public int GetLevel(UpgradeKind k) => k switch
        {
            UpgradeKind.Saw => data.sawLevel,
            UpgradeKind.Bag => data.bagLevel,
            _ => data.speedLevel
        };

        public int GetCost(UpgradeKind k)
        {
            int lvl = GetLevel(k);
            if (lvl >= Balance.MaxUpgradeLevel) return -1;
            return k switch
            {
                UpgradeKind.Saw => Balance.SawCosts[lvl],
                UpgradeKind.Bag => Balance.BagCosts[lvl],
                _ => Balance.SpeedCosts[lvl]
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
                default: data.speedLevel++; break;
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

        // ---------------- Persistence ----------------

        public void Save()
        {
            _dirty = false;
            _saveTimer = 0f;
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
