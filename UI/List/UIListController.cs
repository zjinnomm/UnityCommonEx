using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public class UIListController : NodeController
    {

        public GameObject ChildPrefab;
        public Action<int> OnChildClickImpl;

        public int Length => controllers.Count;
        public UIListItemController this[int index] => controllers[index];

        protected readonly List<UIListItemController> controllers = new List<UIListItemController>();



        public virtual void SetItems<T>(IList<T> items, int minSize = 0)
        {
            if (items == null)
            {
                foreach (var controller in controllers)
                {
                    controller.Destroy();
                }
                controllers.Clear();
                return;
            }
            int expectedNum = Mathf.Max(minSize, items.Count);
            while (controllers.Count < expectedNum)
            {
                var controller = Create<UIListItemController>(ChildPrefab, transform);
                controller.Parent = this;
                controllers.Add(controller);
            }
            while (controllers.Count > expectedNum)
            {
                var controller = controllers[controllers.Count - 1];
                controllers.RemoveAt(controllers.Count - 1);
                controller.Destroy();
            }
            for (int i = 0; i < controllers.Count; i++)
            {
                if (i < items.Count)
                {
                    controllers[i].SetItem(items[i]);
                }
                else
                {
                    controllers[i].Unset();
                }
            }
        }

        public UIListItemController GetChild(int index)
        {
            return controllers.Count > index ? controllers[index] : null;
        }

        protected virtual void OnChildClick(int index)
        {
            OnChildClickImpl?.Invoke(index);
        }

        public void OnChildClick(UIListItemController child)
        {
            for(int i = 0; i < controllers.Count; i++)
            {
                if (controllers[i] == child)
                {
                    OnChildClick(i);
                    return;
                }
            }
        }

    }

    public abstract class UIListItemController : NodeController
    {

        [HideInInspector]
        public UIListController Parent;

        public virtual void OnClick()
        {
            Parent?.OnChildClick(this);
        }

        public virtual void Unset() { }

        public virtual void SetItem(object item) { }

    }

    public abstract class UIListItemController<T> : UIListItemController
    {

        public override void SetItem(object item)
        {
            SetItem((T)item);
        }

        public abstract void SetItem(T item);

    }



}