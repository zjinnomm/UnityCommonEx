using System;

namespace UnityCommonEx
{
    public interface IInputActionWrapper<TAction> where TAction : Enum
    {
        TAction Action { get; }
        bool CanTrigger();
        void Trigger();
    }
}
