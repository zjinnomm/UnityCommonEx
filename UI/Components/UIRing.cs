using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public class UIRing : MaskableGraphic
    {

        public float OuterRadius;
        public float InnerRadius;
        public float Progress;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            vh.Clear();
            const float step = 0.02f;
            float left = 0f;
            int offset = 0;
            while (left < Progress)
            {
                float right = Mathf.Clamp(left + step, 0f, Progress);
                vh.AddVert(new Vector3(InnerRadius * Mathf.Sin(left * Mathf.PI * 2), InnerRadius * Mathf.Cos(left * Mathf.PI * 2), 0), color, Vector4.zero);
                vh.AddVert(new Vector3(OuterRadius * Mathf.Sin(left * Mathf.PI * 2), OuterRadius * Mathf.Cos(left * Mathf.PI * 2), 0), color, Vector4.zero);
                vh.AddVert(new Vector3(OuterRadius * Mathf.Sin(right * Mathf.PI * 2), OuterRadius * Mathf.Cos(right * Mathf.PI * 2), 0), color, Vector4.zero);
                vh.AddVert(new Vector3(InnerRadius * Mathf.Sin(right * Mathf.PI * 2), InnerRadius * Mathf.Cos(right * Mathf.PI * 2), 0), color, Vector4.zero);
                vh.AddTriangle(offset, offset + 1, offset + 2);
                vh.AddTriangle(offset, offset + 2, offset + 3);
                offset += 4;
                left += step;
            }
        }

    }
}