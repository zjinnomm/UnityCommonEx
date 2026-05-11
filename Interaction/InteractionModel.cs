using UnityEngine;

namespace UnityCommonEx
{
    public static partial class InteractionModel
    {
        // relative pos from (0,0) to (1,1)
        public static Vector2 Pos { get; private set; }

        public static Vector2 GetMouseNormalizedPos()
        {
            return new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        }

        public static Vector2 GetMouseWorldPos()
        {
            return Camera.main.ScreenToWorldPoint(GetMouseNormalizedPos() * new Vector2(Screen.width, Screen.height));
        }

        public static void Update()
        {
            Pos = GetMouseNormalizedPos();
        }

    }
}
