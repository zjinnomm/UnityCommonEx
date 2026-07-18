using System;

namespace UnityCommonEx
{
    public interface IInputActionWrapper<TAction> where TAction : struct, Enum
    {
        TAction Action { get; }
        int Priority { get; }
        bool CanTrigger();
        void Trigger();
    }
}
