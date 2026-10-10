using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace UnityCommonEx
{
    public class BorderHighlightFocusTarget : ControllerFocusTarget
    {
        [Serializable]
        public class HighlightBinding
        {
            public Image TargetImage;
            public Image BackgroundImage;
            [NonSerialized] internal Material Material;
        }

        public bool ShowHighlight = true;
        public HighlightBinding[] Highlights = Array.Empty<HighlightBinding>();
        [Min(1f)] public float SizeMultiplier = 1.2f;
        public Vector2 PixelPadding = Vector2.zero;
        public Color HighlightColor = new Color(0.3f, 0.9f, 1f, 0.8f);
        [Min(0f)] public float FadeDuration = 0.1f;
        [Range(0f, 0.5f)] public float PulseAmount = 0.1f;
        [Min(0f)] public float PulseSpeed = 3f;

        private float visibility;
        private static readonly int SpriteUVRectId = Shader.PropertyToID("_SpriteUVRect");
        private static readonly int InnerScaleId = Shader.PropertyToID("_InnerScale");

        protected override void OnFocus(bool focused)
        {
            if (focused) Refresh();
        }

        private void LateUpdate()
        {
            visibility = !ShowHighlight ? 0f : FadeDuration > 0f
                ? Mathf.MoveTowards(visibility, IsFocused ? 1f : 0f, Time.unscaledDeltaTime / FadeDuration)
                : IsFocused ? 1f : 0f;
            Refresh();
        }

        private void Refresh()
        {
            if (Highlights == null) return;
            float pulse = 1f - PulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed));
            foreach (var binding in Highlights)
            {
                var source = binding?.TargetImage;
                var image = binding?.BackgroundImage;
                if (image == null) continue;
                bool visible = ShowHighlight && visibility > 0f && source != null && source.isActiveAndEnabled && source.color.a > 0.001f;
                image.enabled = visible;
                if (!visible) continue;

                image.sprite = source.overrideSprite;
                image.type = source.type;
                image.preserveAspect = source.preserveAspect;
                image.fillCenter = source.fillCenter;
                image.fillMethod = source.fillMethod;
                image.fillAmount = source.fillAmount;
                image.fillClockwise = source.fillClockwise;
                image.fillOrigin = source.fillOrigin;
                image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                var color = HighlightColor;
                color.a *= visibility * pulse * source.color.a;
                image.color = color;

                var sourceRect = source.rectTransform;
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = sourceRect.rect.size * Mathf.Max(1f, SizeMultiplier) + Vector2.Max(PixelPadding, Vector2.zero) * 2f;
                bool child = rect.parent == sourceRect;
                rect.localScale = child ? Vector3.one : sourceRect.localScale;
                rect.localRotation = child ? Quaternion.identity : sourceRect.localRotation;
                rect.position = sourceRect.TransformPoint(sourceRect.rect.center);

                if (binding.Material == null)
                {
                    binding.Material = new Material(image.material) { hideFlags = HideFlags.HideAndDontSave };
                    image.material = binding.Material;
                }
                var sprite = source.overrideSprite;
                var uv = sprite != null ? DataUtility.GetOuterUV(sprite) : new Vector4(0f, 0f, 1f, 1f);
                var size = rect.rect.size;
                var innerScale = new Vector4(sourceRect.rect.width / Mathf.Max(0.001f, size.x),
                    sourceRect.rect.height / Mathf.Max(0.001f, size.y), 0f, 0f);
                binding.Material.SetVector(SpriteUVRectId, uv);
                binding.Material.SetVector(InnerScaleId, innerScale);
                var renderMaterial = image.materialForRendering;
                if (renderMaterial != binding.Material)
                {
                    renderMaterial.SetVector(SpriteUVRectId, uv);
                    renderMaterial.SetVector(InnerScaleId, innerScale);
                }
            }
        }

        private void OnDestroy()
        {
            if (Highlights == null) return;
            foreach (var binding in Highlights)
            {
                if (binding?.Material == null) continue;
                if (Application.isPlaying) Destroy(binding.Material);
                else DestroyImmediate(binding.Material);
                binding.Material = null;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            visibility = 0f;
            if (Highlights == null) return;
            foreach (var binding in Highlights)
                if (binding?.BackgroundImage != null) binding.BackgroundImage.enabled = false;
        }
    }
}
