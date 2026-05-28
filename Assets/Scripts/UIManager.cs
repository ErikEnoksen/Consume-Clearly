using System.Collections.Generic;
using UnityEngine;

public interface IUILockable
{
    void SetLocked(bool locked);
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private readonly Dictionary<UILockType, int> lockCounters = new Dictionary<UILockType, int>();
    private readonly List<IUILockable> allLockableUIs = new List<IUILockable>();
    private Dictionary<UILockType, System.Func<IUILockable, bool>> lockRules;

    public enum UILockType
    {
        Shopmenu,
        Inventory,
        Workshop,
        Pausemenu,
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLockRules();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLockRules()
    {
        lockRules = new Dictionary<UILockType, System.Func<IUILockable, bool>>
        {
            { UILockType.Shopmenu,  (ui) => !(ui is ShopMenu) },
            { UILockType.Inventory, (ui) => ui is QuestUI },
            { UILockType.Workshop,  (ui) => !(ui is WorkshopStation) },
            { UILockType.Pausemenu, (ui) => true },
        };
    }

    public void RegisterUI(IUILockable ui)
    {
        if (!allLockableUIs.Contains(ui))
            allLockableUIs.Add(ui);
    }

    public void UnregisterUI(IUILockable ui)
    {
        allLockableUIs.Remove(ui);
    }

    public void AddLock(UILockType lockType)
    {
        if (!lockCounters.ContainsKey(lockType))
            lockCounters[lockType] = 0;

        lockCounters[lockType]++;

        if (lockCounters[lockType] == 1 && lockRules.ContainsKey(lockType))
            ApplyLockForType(lockType, true);
    }

    public void RemoveLock(UILockType lockType)
    {
        if (!lockCounters.ContainsKey(lockType) || lockCounters[lockType] <= 0) return;

        lockCounters[lockType]--;

        if (lockCounters[lockType] == 0 && lockRules.ContainsKey(lockType))
            ApplyLockForType(lockType, false);
    }

    private void ApplyLockForType(UILockType lockType, bool locked)
    {
        var rule = lockRules[lockType];

        foreach (var ui in allLockableUIs)
        {
            if (!rule(ui)) continue;

            if (locked)
                ui.SetLocked(true);
            else if (!IsUILocked(ui))
                ui.SetLocked(false);
        }
    }

    public bool IsUILocked(IUILockable ui)
    {
        foreach (var kvp in lockCounters)
        {
            if (kvp.Value > 0 && lockRules.ContainsKey(kvp.Key) && lockRules[kvp.Key](ui))
                return true;
        }
        return false;
    }
}