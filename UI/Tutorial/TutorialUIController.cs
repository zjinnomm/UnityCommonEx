using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{

    public class TutorialUIController : SingletonController<TutorialUIController>
    {

        struct ActivatedEntry
        {
            public int Index;
            public TutorialEntryType Type;
            public Component RelatedComponent;
        }

        public GameObject VeilMaskPrefab;
        public GameObject TextPrefab;
        public Sprite CircleMaskSprite;

        public GameObject Veil;
        public Transform VeilMaskRoot;
        public Transform TextRoot;
        

        List<Image> CachedVeilMasks = new List<Image>();
        List<TMP_Text> CachedTexts = new List<TMP_Text>();
        List<ActivatedEntry> ActivatedEntries = new List<ActivatedEntry>();

        public void SetVeilEnabled(bool enabled)
        {
            Veil.SetActive(enabled);
        }

        public void AddVeilMask(int key, Rect rect, HighlightTutorialEntry.ShapeType shape)
        {
            Image image;
            if (CachedVeilMasks.Count != 0)
            {
                image = CachedVeilMasks.Last();
                CachedVeilMasks.RemoveAt(CachedVeilMasks.Count - 1);
            }
            else
            {
                image = Instantiate(VeilMaskPrefab, VeilMaskRoot).GetComponent<Image>();
            }

            RectTransform root = Veil.GetComponent<RectTransform>();
            image.gameObject.SetActive(true);
            image.rectTransform.anchoredPosition = new Vector2((rect.center.x - 0.5f) * root.rect.width, (rect.center.y - 0.5f) * root.rect.height);
            image.rectTransform.sizeDelta = new Vector2(rect.size.x * root.rect.width, rect.size.y * root.rect.height);

            image.sprite = shape switch {
                HighlightTutorialEntry.ShapeType.Circle => CircleMaskSprite,
                _ => null,
            };
            ActivatedEntries.Add(new ActivatedEntry{
                Index = key,
                Type = TutorialEntryType.Highlight,
                RelatedComponent = image,
            });
        }

        public void AddText(int key, Rect rect, string s)
        {
            TMP_Text text;
            if (CachedTexts.Count != 0)
            {
                text = CachedTexts.Last();
                CachedTexts.RemoveAt(CachedTexts.Count - 1);
            }
            else
            {
                text = Instantiate(TextPrefab, TextRoot).GetComponent<TMP_Text>();
            }

            RectTransform root = GetComponent<RectTransform>();
            text.gameObject.SetActive(true);
            text.rectTransform.anchoredPosition = new Vector2((rect.center.x - 0.5f) * root.rect.width, (rect.center.y - 0.5f) * root.rect.height);
            text.rectTransform.sizeDelta = new Vector2(rect.size.x * root.rect.width, rect.size.y * root.rect.height);

            text.text = s;
            ActivatedEntries.Add(new ActivatedEntry{
                Index = key,
                Type = TutorialEntryType.Text,
                RelatedComponent = text,
            });
        }

        public void AddInputRegion(int key, Rect rect)
        {
            if (TutorialManager.Instance.MaskId > 0)
            {
                InteractionModel.ExcludeRegion(TutorialManager.Instance.MaskId, key, rect);
            }
        }

        public void RemoveObject(int key)
        {
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                ActivatedEntry entry = ActivatedEntries[i];
                if (entry.Index == key)
                {
                    ActivatedEntries.RemoveAt(i);
                    Component component = entry.RelatedComponent;
                    component?.gameObject.SetActive(false);
                    if (entry.Type == TutorialEntryType.Highlight)
                    {
                        CachedVeilMasks.Add(component as Image);
                    }
                    else if (entry.Type == TutorialEntryType.Text)
                    {
                        CachedTexts.Add(component as TMP_Text);
                    }
                    else if (entry.Type == TutorialEntryType.Input)
                    {
                        if (TutorialManager.Instance.MaskId > 0)
                        {
                            InteractionModel.RemoveExcludeRegion(TutorialManager.Instance.MaskId, key);
                        }
                    }
                    return;
                }
            }
        }
        
        
    }


}