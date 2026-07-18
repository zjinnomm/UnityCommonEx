using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{
    public readonly struct SpriteVisualResource
    {
        public Sprite Sprite { get; }
        public Vector4 AtlasUVRect { get; }

        public SpriteVisualResource(Sprite sprite)
        {
            Sprite = sprite;
            AtlasUVRect = SpriteUVRectUtility.GetAtlasUVRect(sprite);
        }
    }

    public static class SpriteUVRectUtility
    {
        public static Vector4 GetAtlasUVRect(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return new Vector4(0f, 0f, 1f, 1f);

            var rect = sprite.textureRect;
            var texture = sprite.texture;
            return new Vector4(
                rect.x / texture.width,
                rect.y / texture.height,
                rect.width / texture.width,
                rect.height / texture.height);
        }
    }

    /// <summary>
    /// Maps Sprite names to reusable visual resources.
    /// </summary>
    [CreateAssetMenu(fileName = "NamedSpriteResourceConfig", menuName = "UnityCommonEx/Named Sprite Resource")]
    public class NamedSpriteResourceConfig : ScriptableObject
    {
        public List<Sprite> Sprites = new List<Sprite>();
        public Sprite DefaultSprite;

        private Dictionary<string, Sprite> spriteCache;

        public void Initialize()
        {
            spriteCache = new Dictionary<string, Sprite>();
            foreach (var sprite in Sprites)
            {
                if (sprite != null && !string.IsNullOrEmpty(sprite.name))
                    spriteCache[sprite.name] = sprite;
            }
        }

        public Sprite GetSprite(string name)
        {
            return GetVisualResource(name).Sprite;
        }

        public SpriteVisualResource GetVisualResource(string name)
        {
            if (spriteCache == null)
                Initialize();
            if (string.IsNullOrEmpty(name))
                return new SpriteVisualResource(DefaultSprite);
            if (spriteCache.TryGetValue(name, out var sprite))
                return new SpriteVisualResource(sprite);
            return new SpriteVisualResource(DefaultSprite);
        }
    }
}
