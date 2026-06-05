// This script defines a UIManager class that manages the locking and unlocking of various UI elements in a Unity game.
//  
// Purpose:
// The UIManager allows different parts of the game to request locks on specific types of UI (e.g., Shopmenu, Inventory)
// and ensures that the appropriate UI elements are locked or unlocked based on the current locks in place.
// ===============================================================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

// Interface for UI elements that can be locked by the UIManager
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

    // Define the rules for which UI elements should be locked for each UILockType
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
        // Only apply the lock if this is the first lock of this type
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
        // Apply the lock or unlock to all relevant UIs based on the rule
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
        // Check if any active lock applies to this UI element
        foreach (var kvp in lockCounters)
        {
            if (kvp.Value > 0 && lockRules.ContainsKey(kvp.Key) && lockRules[kvp.Key](ui))
                return true;
        }
        return false;
    }
}