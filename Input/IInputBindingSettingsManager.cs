using System.Collections.Generic;

namespace UnityCommonEx
{
    public interface IInputBindingSettingsManager
    {
        bool HasPendingChanges { get; }
        bool IsRebinding { get; }
        int DisplayRevision { get; }

        IReadOnlyList<InputBinding> GetPendingBindings(string actionName);
        void ResetPendingBindings(string actionName);
        void ResetAllPendingBindings();
        void ApplyChanges();
        void RevertChanges();

        InputBindingSetResult TrySetPendingBinding(string actionName, int slotIndex, InputBinding binding);
        InputBindingSetResult ClearPendingBinding(string actionName, int slotIndex);

        bool BeginRebind(string actionName, int slotIndex);
        void CancelRebind();
        bool IsRebindingTarget(string actionName, int slotIndex);
        bool TryFinishActiveRebindThisFrame(out InputBindingSetResult result);
    }
}
