using UnityEngine;

namespace UnityCommonEx
{
    public static partial class InteractionModel
    {

        // relative pos from (0,0) to (1,1)
        public static Vector2 Pos;
        public static Vector2 Delta;
        public static float ScrollDelta;

        public static bool JustPressed;
        public static bool JustReleased;
        public static bool IsClicked;
        public static bool IsPressed;
        public static bool IsRightClicked;
        
        // 新增：Ctrl键状态
        public static bool IsCtrlPressed;

        const float ClickInterval = 0.2f;
        static float LastDownTime = 0f;
        static float LastDownTimeRight = 0f;

        public static Vector2 GetMouseWorldPos()
        {
            return Camera.main.ScreenToWorldPoint(Pos* new Vector2(Screen.width, Screen.height));
        }

        public static void Update()
        {
            Vector2 pos = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
            bool masked = IsPointMasked(pos);
            if (!masked)
            {
                Delta = pos - Pos;
                Pos = pos;
                ScrollDelta = Input.mouseScrollDelta.y;
                if (Input.GetMouseButtonDown(0))
                {
                    LastDownTime = Time.realtimeSinceStartup;
                    Delta = Vector2.zero;
                }
                if (Input.GetMouseButtonDown(1))
                {
                    LastDownTimeRight = Time.realtimeSinceStartup;
                }
                JustPressed = Input.GetMouseButtonDown(0);
                JustReleased = Input.GetMouseButtonUp(0);
                IsClicked = Input.GetMouseButtonUp(0) && Time.realtimeSinceStartup - LastDownTime < ClickInterval;
                IsPressed = Input.GetMouseButton(0);
                IsRightClicked = Input.GetMouseButtonUp(1) && Time.realtimeSinceStartup - LastDownTimeRight < ClickInterval;
                
                // 检测Ctrl键状态
                IsCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            }
            else
            {
                Pos = pos;
                Delta = Vector2.zero;
                ScrollDelta = 0;
                JustPressed = false;
                JustReleased = false;
                IsClicked = false;
                IsPressed = false;
                IsCtrlPressed = false;
            }
        }

    }
}