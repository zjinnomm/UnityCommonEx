using UnityEngine;

namespace UnityCommonEx
{

    public class ScrollCameraController : SingletonController<ScrollCameraController>
    {

        const float DefaultSize = 5f;
        const float MinSize = 2f;
        const float MaxSize = 15f;
        const float CameraZ = -10f;

        Camera targetCamera;
        readonly RangeAttribute sizeRange = new RangeAttribute(MinSize, MaxSize);
        bool locked = true;
        Rect posRange;
        Vector2 lastMouseViewportPos;
        bool hasLastMouseViewportPos;

        protected override void OnInit()
        {
            base.OnInit();
            targetCamera = GetComponent<Camera>();
            DontDestroyOnLoad(gameObject);
        }

        public void SetLocked(bool locked)
        {
            this.locked = locked;
        }

        public void Set(Vector2 pos, float size = DefaultSize)
        {
            gameObject.transform.localPosition = new Vector3(pos.x, pos.y, CameraZ);
            targetCamera.orthographicSize = size;
        }

        public void SetRect(float x, float y, float width, float height)
        {
            posRange = new Rect(x, y, width, height);
        }

        private void Update()
        {
            if (locked)
            {
                hasLastMouseViewportPos = false;
                return;
            }
            float scrollDelta = Input.mouseScrollDelta.y;
            targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize + scrollDelta, sizeRange.min, sizeRange.max);

            Vector2 currentMouseViewportPos = InteractionModel.GetMouseNormalizedPos();
            if (Input.GetMouseButton(0))
            {
                Vector2 viewportDelta = hasLastMouseViewportPos ? currentMouseViewportPos - lastMouseViewportPos : Vector2.zero;
                Vector2 pos = gameObject.transform.localPosition;
                Vector2 deltaPos = viewportDelta * targetCamera.orthographicSize * 2 / targetCamera.pixelHeight;
                pos -= deltaPos;
                gameObject.transform.localPosition = new Vector3(Mathf.Clamp(pos.x, posRange.xMin, posRange.xMax), Mathf.Clamp(pos.y, posRange.yMin, posRange.yMax), CameraZ);
            }

            lastMouseViewportPos = currentMouseViewportPos;
            hasLastMouseViewportPos = true;
        }

    }

}
