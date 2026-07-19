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


        private void OnDestroy()
        {
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
                gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }


        protected virtual void OnInit() { }
        protected virtual void OnRelease() { }
        

    }
}
