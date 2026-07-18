using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    [RequireComponent(typeof(Image))]
    public class SpriteLocalUVImageModifier : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            var image = graphic as Image;
            var sprite = image != null ? image.sprite : null;
            if (sprite == null || vertexHelper.currentVertCount == 0)
                return;

            var atlasUVRect = SpriteUVRectUtility.GetAtlasUVRect(sprite);
            var size = Vector2.Max(new Vector2(atlasUVRect.z, atlasUVRect.w), new Vector2(0.0001f, 0.0001f));
            for (var index = 0; index < vertexHelper.currentVertCount; index++)
            {
                var vertex = new UIVertex();
                vertexHelper.PopulateUIVertex(ref vertex, index);
                vertex.uv1 = new Vector2(
                    (vertex.uv0.x - atlasUVRect.x) / size.x,
                    (vertex.uv0.y - atlasUVRect.y) / size.y);
                vertex.uv2 = new Vector2(1f, 0f);
                vertexHelper.SetUIVertex(vertex, index);
            }

            EnsureCanvasChannels();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannels();
        }

        private void EnsureCanvasChannels()
        {
            var canvas = graphic != null ? graphic.canvas : null;
            if (canvas == null)
                return;

            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.TexCoord2;
        }
    }
}
