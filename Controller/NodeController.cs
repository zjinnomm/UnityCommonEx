using UnityEngine;

namespace UnityCommonEx
{

    public class NodeController : MonoBehaviour
    {

        public static T Create<T>(GameObject prefab, Transform parent = null) where T : NodeController
        {
            var obj = Instantiate(prefab, parent);
            var controller = obj.GetComponent<T>();
            if (controller == null)
            {
                LogUtil.Error("prefab {0} is not attached by a {1}", prefab, typeof(T));
            }
            return controller;
        }

        private void Awake()
        {
            OnInit();
        }

        private void Start()
        {
            OnActivate();
        }

        private void OnDestroy()
        {
            OnDeactivate();
            OnRelease();
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public bool Activated => gameObject.activeInHierarchy;

        public void Activate()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                if (Activated)
                {
                    OnActivate();
                    foreach (NodeController controller in GetComponentsInChildren<NodeController>())
                    {
                        if (controller != this && controller.Activated)
                        {
                            controller.OnActivate();
                        }
                    }
                }
            }
        }

        public void Deactivate()
        {
            if (gameObject.activeSelf)
            {
                if (Activated)
                {
                    OnDeactivate();
                    foreach (NodeController controller in GetComponentsInChildren<NodeController>())
                    {
                        if (controller != this && controller.Activated)
                        {
                            controller.OnDeactivate();
                        }
                    }
                }
                gameObject.SetActive(false);
            }
        }


        protected virtual void OnInit() { }
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnRelease() { }
        

    }
}
