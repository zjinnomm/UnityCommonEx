using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{
    [Serializable]
    public struct LocalizedSpriteEntry
    {
        public LanguageType LanguageType;
        public Sprite Sprite;
    }

    public class LocalizedImage : NodeController
    {
        public Image TargetImage;
        public List<LocalizedSpriteEntry> LocalizedSprite = new List<LocalizedSpriteEntry>();
        public Sprite DefaultSprite;

        private Sprite initialSprite;

        protected override void OnInit()
        {
            base.OnInit();
            if (TargetImage == null)
            {
                TargetImage = GetComponent<Image>();
            }
            if (TargetImage != null)
            {
                initialSprite = TargetImage.sprite;
            }
        }

        private void OnEnable()
        {
            RefreshLocalizedSprite();
        }

        public void RefreshLocalizedSprite()
        {
            if (TargetImage == null)
            {
                return;
            }

            var targetSprite = GetSprite(LanguageTypeEx.GetCurrentLanguageType());
            if (targetSprite != null)
            {
                TargetImage.sprite = targetSprite;
            }
        }

        private Sprite GetSprite(LanguageType languageType)
        {
            for (int i = 0; i < LocalizedSprite.Count; i++)
            {
                var entry = LocalizedSprite[i];
                if (entry.LanguageType == languageType && entry.Sprite != null)
                {
                    return entry.Sprite;
                }
            }

            if (DefaultSprite != null)
            {
                return DefaultSprite;
            }

            return initialSprite;
        }
    }
}
