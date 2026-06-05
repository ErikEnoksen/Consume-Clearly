// =============================================================================
// ISaveable.cs — Save System Contract
// 
//
// PURPOSE:
//   Any scene object that needs its state saved and restored implements this
//   interface. GameManager finds all ISaveable objects at save time, calls
//   SaveState() on each, and stores the results. On load it matches them by
//   uniqueId and calls LoadState() to restore each one.
//
// CONTRACT:
//   • GetUniqueId()  — must return a stable, scene-unique string ID.
//                      Usually a GUID generated in OnValidate/Awake and
//                      stored as a serialized field so it survives recompiles.
//   • SaveState()    — pack current runtime state into an InteractableObjectState
//   • LoadState()    — read from that snapshot and apply it back to the object
//
// NOTE:
//   IDs must be unique per scene. Duplicate IDs are skipped with a warning
//   during load (see GameManager.LoadInteractableStates).
// =============================================================================

namespace Save
{
    public interface ISaveable
    {
        string GetUniqueId();
        InteractableObjectState SaveState();
        void LoadState(InteractableObjectState state);
    }
}
