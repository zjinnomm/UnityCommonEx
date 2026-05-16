using System;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public abstract class ButtonInputActionWrapper<TAction> : NodeController, IInputActionWrapper<TAction> where TAction : struct, Enum
    {
        public TAction Action;
        public Button Button;

        TAction IInputActionWrapper<TAction>.Action => Action;

        protected abstract InputManager<TAction> GetInputManager();

        protected override void OnActivate()
        {
            base.OnActivate();
            GetInputManager()?.RegisterWrapper(this);
        }

        protected override void OnDeactivate()
        {
            GetInputManager()?.UnregisterWrapper(this);
            base.OnDeactivate();
        }

        public virtual bool CanTrigger()
        {
            return isActiveAndEnabled &&
                gameObject.activeInHierarchy &&
                Button != null &&
                Button.gameObject.activeInHierarchy &&
                Button.interactable;
        }

        public virtual void Trigger()
        {
            if (!CanTrigger())
                return;
            Button.onClick.Invoke();
        }
    }
}
