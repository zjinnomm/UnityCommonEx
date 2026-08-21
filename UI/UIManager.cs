using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityCommonEx
{

    public class UIConfigTableRow<T> : BaseDataTableRow<T> where T : Enum
    {

        public override T RowKey => Name;

        public T Name;
        public string Path;
        public bool IsDialog;
        public int SortOrder = 0;
        public InputScope InputScope = UnityCommonEx.InputScope.UI;

    }

    public interface IRegisterUI<T> where T : Enum
    {

        public ICollection<T> RegisterUI();

    }

    public class UIManager<TE, TR> : SingletonController<UIManager<TE, TR>> where TE : Enum where TR : UIConfigTableRow<TE>, new()
    {

        public Transform UIRoot;
        public Transform DialogRoot;
        public string UIRootResPath;

        DataTable<TE, TR> uiConfigTable;
        Dictionary<int, Transform> uiRootBySortOrder = new Dictionary<int, Transform>();
        Dictionary<TE, NodeController> uiControllers = new Dictionary<TE, NodeController>();
        Dictionary<TE, NodeController> dialogControllers = new Dictionary<TE, NodeController>();
        Dictionary<TE, AsyncOperationHandle<GameObject>> handles = new Dictionary<TE, AsyncOperationHandle<GameObject>>();
        TE currentDialogType;
        NodeController currentDialogController = null;
        uint dialogMaskId; // 交互遮罩ID

        protected override bool IsPersistent => true;

        public void Initialize()
        {
            uiConfigTable = DataTableManager.Get<TE, TR>();
            if (uiConfigTable == null)
            {
                LogUtil.Error("lack ui config data");
            }
            DontDestroyOnLoad(this);
            DontDestroyOnLoad(EventSystem.current);
            DialogRoot.gameObject.SetActive(false);
        }

        public NodeController OpenDialog(TE pageType)
        {
            DialogRoot.gameObject.SetActive(true);
            
            // 创建全屏交互遮罩
            if (dialogMaskId == 0)
            {
                dialogMaskId = InteractionModel.MaskRegion(new Rect(0, 0, 1, 1));
            }
            
            if (currentDialogController != null)
            {
                if (currentDialogType.Equals(pageType))
                {
                    currentDialogController.Activate();
                    ActivateInputScope(currentDialogController, GetConfig(pageType));
                    return currentDialogController;
                }
                InputScopeRegistry.DeactivateUI(currentDialogController);
                currentDialogController.Deactivate();
                currentDialogController = null;
            }
            currentDialogType = pageType;
            if (dialogControllers.TryGetValue(pageType, out currentDialogController))
            {
                currentDialogController.Activate();
                ActivateInputScope(currentDialogController, GetConfig(pageType));
                return currentDialogController;
            }
            
            // 检查uiConfigTable是否为空
            if (uiConfigTable == null)
            {
                LogUtil.Error("UI config table is null, cannot open dialog {0}", pageType);
                return null;
            }
            
            var config = uiConfigTable.GetRow(pageType);
            if (config == null)
            {
                LogUtil.Error("page {0} is not defined", pageType);
                return null;
            }
            if (!config.IsDialog)
            {
                LogUtil.Error("page {0} is not a dialog, use Open instead", pageType);
                return null;
            }
            string path = $"{UIRootResPath}/{config.Path}";
            var result = ResourceUtil.LoadSync<GameObject>(path);
            if (!result.IsValid)
            {
                LogUtil.Warn("ui prefab {0}'s path {1} is not valid", config.Name, config.Path);
                return null;
            }
            handles[pageType] = result.Handle;
            currentDialogController = Create<NodeController>(result.Asset, DialogRoot);
#if UNITY_EDITOR
            currentDialogController.gameObject.name += $"_{Convert.ChangeType(pageType, pageType.GetTypeCode())}";
#endif
            dialogControllers.Add(pageType, currentDialogController);
            ActivateInputScope(currentDialogController, config);
            return currentDialogController;
        }

        public NodeController Open(TE pageType)
        {
            NodeController controller;
            if (uiControllers.TryGetValue(pageType, out controller))
            {
                controller.Activate();
                ActivateInputScope(controller, GetConfig(pageType));
                return controller;
            }
            
            // 检查uiConfigTable是否为空
            if (uiConfigTable == null)
            {
                LogUtil.Error("UI config table is null, cannot open page {0}", pageType);
                return null;
            }
            
            var config = uiConfigTable.GetRow(pageType);
            if (config == null)
            {
                LogUtil.Error("page {0} is not defined", pageType);
                return null;
            }
            if (config.IsDialog)
            {
                LogUtil.Error("page {0} is a dialog, use OpenDialog instead", pageType);
                return null;
            }
            string path = $"{UIRootResPath}/{config.Path}";
            var result = ResourceUtil.LoadSync<GameObject>(path);
            if (!result.IsValid)
            {
                LogUtil.Warn("ui prefab {0}'s path {1} is not valid", config.Name, config.Path);
                return null;
            }
            handles[pageType] = result.Handle;

            Transform uiRoot;
            if (!uiRootBySortOrder.TryGetValue(config.SortOrder, out uiRoot))
            {
                uiRoot = Instantiate(UIRoot, transform);
                uiRoot.GetComponent<Canvas>().sortingOrder = config.SortOrder;
                uiRoot.gameObject.SetActive(true);
                uiRootBySortOrder.Add(config.SortOrder, uiRoot);
            }

            controller = Create<NodeController>(result.Asset, uiRoot);
#if UNITY_EDITOR
            controller.gameObject.name += $"_{Convert.ChangeType(pageType, pageType.GetTypeCode())}";
#endif
            uiControllers.Add(pageType, controller);
            ActivateInputScope(controller, config);
            return controller;
        }

        public NodeController TryGet(TE pageType)
        {
            NodeController controller;
            if (uiControllers.TryGetValue(pageType, out controller))
            {
                if (controller.Activated)
                {
                    return controller;
                }
            }
            return null;
        }

        public C OpenDialog<C>(TE pageType) where C : NodeController
        {
            NodeController controller = OpenDialog(pageType);
            if (controller != null)
            {
                if (controller is C c)
                {
                    return c;
                }
                LogUtil.Error("page {0}'s prefab has not been attached by a controller of type {1}", pageType, typeof(C));
            }
            return null;
        }

        public C Open<C>(TE pageType) where C : NodeController
        {
            NodeController controller = Open(pageType);
            if (controller != null)
            {
                if (controller is C c)
                {
                    return c;
                }
                LogUtil.Error("page {0}'s prefab has not been attached by a controller of type {1}", pageType, typeof(C));
            }
            return null;
        }

        public C TryGet<C>(TE pageType) where C : NodeController
        {
            NodeController controller = TryGet(pageType);
            if (controller != null)
            {
                if (controller is C c)
                {
                    return c;
                }
                LogUtil.Error("page {0}'s prefab has not been attached by a controller of type {1}", pageType, typeof(C));
            }
            return null;
        }

        public void Close(TE pageType)
        {
            if (uiControllers.TryGetValue(pageType, out var controller))
            {
                if (controller != null)
                {
                    InputScopeRegistry.DeactivateUI(controller);
                    controller.Deactivate();
                }
            }
        }

        public void CloseDialog()
        {
            if (currentDialogController != null)
            {
                InputScopeRegistry.DeactivateUI(currentDialogController);
                currentDialogController.Deactivate();
                currentDialogController = null;
            }
            
            // 移除交互遮罩并关闭DialogRoot
            if (dialogMaskId > 0)
            {
                InteractionModel.RemoveMask(dialogMaskId);
                dialogMaskId = 0;
            }
            DialogRoot.gameObject.SetActive(false);
        }

        public void CloseDialog(TE pageType)
        {
            if (currentDialogController != null && currentDialogType.Equals(pageType))
            {
                InputScopeRegistry.DeactivateUI(currentDialogController);
                currentDialogController.Deactivate();
                currentDialogController = null;
                
                // 移除交互遮罩并关闭DialogRoot
                if (dialogMaskId > 0)
                {
                    InteractionModel.RemoveMask(dialogMaskId);
                    dialogMaskId = 0;
                }
                DialogRoot.gameObject.SetActive(false);
            }
        }

        public void SwapUIBetweenStates(IRegisterUI<TE> SwapIn, IRegisterUI<TE> SwapOut)
        {
            var swapIn = SwapIn?.RegisterUI();
            var swapOut = SwapOut?.RegisterUI();
            if (swapIn != null)
            {
                foreach (var i in swapIn)
                {
                    Open(i);
                }
            }
            if (swapOut != null)
            {
                foreach (var i in swapOut)
                {
                    if (swapIn == null || !swapIn.Contains(i))
                    {
                        Close(i);
                    }
                }
            }
        }

        void Update()
        {
            if (currentDialogController != null && !currentDialogController.Activated)
            {
                CloseDialog();
            }
        }

        private TR GetConfig(TE pageType)
        {
            return uiConfigTable?.GetRow(pageType);
        }

        private static void ActivateInputScope(NodeController controller, TR config)
        {
            if (controller == null || config == null)
                return;
            InputScopeRegistry.ActivateUI(controller, controller.transform, config.InputScope);
        }

        protected override void OnRelease()
        {
            foreach (var controller in uiControllers.Values)
                InputScopeRegistry.DeactivateUI(controller);
            foreach (var controller in dialogControllers.Values)
                InputScopeRegistry.DeactivateUI(controller);

            foreach (var handle in handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            handles.Clear();
            base.OnRelease();
        }

    }

}
