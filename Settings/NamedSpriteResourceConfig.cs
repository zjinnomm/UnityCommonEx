using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{
    /// <summary>
    /// 以 Sprite.name 为键的通用 Sprite 表（按 Id / 名称查找，缺省用 DefaultSprite）
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
            if (spriteCache == null)
                Initialize();
            if (string.IsNullOrEmpty(name))
                return DefaultSprite;
            if (spriteCache.TryGetValue(name, out var sprite))
                return sprite;
            return DefaultSprite;
        }
    }
}
