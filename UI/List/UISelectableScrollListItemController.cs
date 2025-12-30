using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityCommonEx
{
    /// <summary>
    /// 支持 ScrollView 的可选择列表控制器
    /// 用于解决 Item 中的 Button 等控件拦截 Drag 事件导致 ScrollView 无法滚动的问题
    /// </summary>
    public class UISelectableScrollListController : UISelectableListController
    {
        [Header("Scroll Configuration")]
        public ScrollRect TargetScrollRect;
        
        /// <summary>
        /// 处理来自子项的 Drag 事件
        /// </summary>
        /// <param name="dragData">拖拽数据</param>
        public virtual void OnItemDrag(PointerEventData dragData)
        {
            if (TargetScrollRect != null)
            {
                // 将拖拽事件传递给 ScrollRect
                TargetScrollRect.OnDrag(dragData);
            }
        }
        
        /// <summary>
        /// 处理来自子项的 BeginDrag 事件
        /// </summary>
        /// <param name="dragData">拖拽数据</param>
        public virtual void OnItemBeginDrag(PointerEventData dragData)
        {
            if (TargetScrollRect != null)
            {
                TargetScrollRect.OnBeginDrag(dragData);
            }
        }
        
        /// <summary>
        /// 处理来自子项的 EndDrag 事件
        /// </summary>
        /// <param name="dragData">拖拽数据</param>
        public virtual void OnItemEndDrag(PointerEventData dragData)
        {
            if (TargetScrollRect != null)
            {
                TargetScrollRect.OnEndDrag(dragData);
            }
        }
        
        /// <summary>
        /// 处理来自子项的 Scroll 事件
        /// </summary>
        /// <param name="scrollData">滚轮数据</param>
        public virtual void OnItemScroll(PointerEventData scrollData)
        {
            if (TargetScrollRect != null)
            {
                TargetScrollRect.OnScroll(scrollData);
            }
        }
    }

    /// <summary>
    /// 支持 ScrollView 的可选择列表项控制器
    /// 实现 Drag 语义并将 Drag 操作数据传递给父控制器
    /// </summary>
    public abstract class UISelectableScrollListItemController<T> : UISelectableListItemController<T>, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        /// <summary>
        /// 获取父 Scroll 控制器
        /// </summary>
        protected UISelectableScrollListController ScrollParent => Parent as UISelectableScrollListController;
        
        /// <summary>
        /// 是否允许拖拽
        /// </summary>
        public bool AllowDrag = true;
        
        /// <summary>
        /// 是否允许滚轮
        /// </summary>
        public bool AllowScroll = true;
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!AllowDrag) return;
            
            var scrollParent = ScrollParent;
            if (scrollParent != null)
            {
                scrollParent.OnItemBeginDrag(eventData);
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!AllowDrag) return;
            
            var scrollParent = ScrollParent;
            if (scrollParent != null)
            {
                scrollParent.OnItemDrag(eventData);
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!AllowDrag) return;
            
            var scrollParent = ScrollParent;
            if (scrollParent != null)
            {
                scrollParent.OnItemEndDrag(eventData);
            }
        }
        
        public void OnScroll(PointerEventData eventData)
        {
            if (!AllowScroll) return;
            
            var scrollParent = ScrollParent;
            if (scrollParent != null)
            {
                scrollParent.OnItemScroll(eventData);
            }
        }
    }
} 