namespace Save
{
    public interface ISaveable
    {
        string GetUniqueId();
        InteractableObjectState SaveState();
        void LoadState(InteractableObjectState state);
    }
}
