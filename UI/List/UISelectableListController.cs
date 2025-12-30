using System.Collections.Generic;

namespace UnityCommonEx
{
    public class UISelectableListController : UIListController
    {

        public int SelectedIndex { get; protected set; } = -1;

        public override void SetItems<T>(IList<T> items, int minSize = 0)
        {
            base.SetItems(items);
            SelectedIndex = -1;
            foreach (var c in controllers)
            {
                if (c is ISelectableListItemController controller)
                {
                    controller.OnDeselected();
                }
            }
        }

        protected override void OnChildClick(int index)
        {
            SetSelectedIndex(index);
            base.OnChildClick(index);
        }

        /// <summary>
        /// 设置选中的索引
        /// </summary>
        /// <param name="index">要选中的索引，-1表示取消所有选择</param>
        public void SetSelectedIndex(int index)
        {
            SelectedIndex = index;
            for (int i = 0; i < controllers.Count; i++)
            {
                ISelectableListItemController controller = controllers[i] as ISelectableListItemController;
                if (controller == null)
                {
                    continue;
                }
                if (i == index)
                {
                    controller.OnSelected();
                }
                else
                {
                    controller.OnDeselected();
                }
            }
        }

    }

    public interface ISelectableListItemController
    {

        public void OnSelected();

        public void OnDeselected();

    }

    public abstract class UISelectableListItemController<T> : UIListItemController<T>, ISelectableListItemController
    {

        public virtual void OnSelected() { }

        public virtual void OnDeselected() { }

    }

}