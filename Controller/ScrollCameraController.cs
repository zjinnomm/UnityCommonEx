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
                return;
            }
            targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize + InteractionModel.ScrollDelta, sizeRange.min, sizeRange.max);
            if (InteractionModel.IsPressed)
            {
                Vector2 pos = gameObject.transform.localPosition;
                Vector2 deltaPos = InteractionModel.Delta * targetCamera.orthographicSize * 2 / targetCamera.pixelHeight;
                pos -= deltaPos;
                gameObject.transform.localPosition = new Vector3(Mathf.Clamp(pos.x, posRange.xMin, posRange.xMax), Mathf.Clamp(pos.y, posRange.yMin, posRange.yMax), CameraZ);
            }
        }

    }

}