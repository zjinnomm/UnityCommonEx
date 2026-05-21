using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityCommonEx
{

    public class TutorialUIController : SingletonController<TutorialUIController>
    {

        enum DisplaySide
        {
            Left,
            Right,
        }

        enum DisplayAlign
        {
            Top,
            Bottom,
        }

        struct ActivatedEntry
        {
            public int Index;
            public TutorialEntryType Type;
            public Component RelatedComponent;
        }

        public GameObject VeilMaskPrefab;
        public GameObject TextPrefab;
        public Sprite CircleMaskSprite;
        public float TextPadding = 10f;

        public GameObject Veil;
        public Transform VeilMaskRoot;
        public Transform TextRoot;

        EventTrigger skipEventTrigger;
        EventTrigger.Entry skipPointerClickEntry;

        List<Image> CachedVeilMasks = new List<Image>();
        List<TMP_Text> CachedTexts = new List<TMP_Text>();
        List<ActivatedEntry> ActivatedEntries = new List<ActivatedEntry>();

        protected override void OnInit()
        {
            base.OnInit();
            SetupSkipInteraction();
        }

        protected override void OnRelease()
        {
            TeardownSkipInteraction();
            base.OnRelease();
        }

        public void SetVeilEnabled(bool enabled)
        {
            if (Veil == null)
            {
                LogUtil.Error("TutorialUIController.Veil is not assigned.");
                return;
            }
            Veil.SetActive(enabled);
        }

        public void AddVeilMask(int key, Rect rect, HighlightTutorialEntry.ShapeType shape)
        {
            if (VeilMaskPrefab == null || VeilMaskRoot == null || Veil == null)
            {
                LogUtil.Error("TutorialUIController veil mask references are not fully assigned.");
                return;
            }

            Image image;
            if (CachedVeilMasks.Count != 0)
            {
                int cachedIndex = CachedVeilMasks.Count - 1;
                image = CachedVeilMasks[cachedIndex];
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
            if (TutorialManager.Instance.MaskId > 0)
            {
                InteractionModel.ExcludeRegion(TutorialManager.Instance.MaskId, key, rect);
            }
            ActivatedEntries.Add(new ActivatedEntry{
                Index = key,
                Type = TutorialEntryType.Highlight,
                RelatedComponent = image,
            });
        }

        public void AddText(int key, Rect rect, string s)
        {
            TMP_Text text = CreateOrReuseText();
            if (text == null)
            {
                return;
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

        public void AddTextNearRegion(int key, Rect rect, string s)
        {
            TMP_Text text = CreateOrReuseText();
            if (text == null)
            {
                return;
            }

            text.gameObject.SetActive(true);
            text.text = s;
            PrepareTextLayout(text);
            PositionTextNearRegion(text, rect);

            ActivatedEntries.Add(new ActivatedEntry{
                Index = key,
                Type = TutorialEntryType.Text,
                RelatedComponent = text,
            });
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
                        if (TutorialManager.Instance.MaskId > 0)
                        {
                            InteractionModel.RemoveExcludeRegion(TutorialManager.Instance.MaskId, key);
                        }
                        if (component is Image image)
                        {
                            CachedVeilMasks.Add(image);
                        }
                    }
                    else if (entry.Type == TutorialEntryType.Text)
                    {
                        CachedTexts.Add(component as TMP_Text);
                    }
                    return;
                }
            }
        }

        DisplaySide DetermineDisplaySide(Rect screenRect, Vector2 textScreenPixelSize)
        {
            if (screenRect.x > textScreenPixelSize.x + TextPadding)
            {
                return DisplaySide.Left;
            }
            return DisplaySide.Right;
        }

        DisplayAlign DetermineDisplayAlign(Rect screenRect, Vector2 textScreenPixelSize)
        {
            float rectTop = screenRect.y + screenRect.height;
            if (rectTop > textScreenPixelSize.y + TextPadding)
            {
                return DisplayAlign.Top;
            }
            return DisplayAlign.Bottom;
        }

        void PositionTextNearRegion(TMP_Text text, Rect normalizedRect)
        {
            RectTransform textRect = text.rectTransform;
            Canvas canvas = textRect.GetComponentInParent<Canvas>();
            RectTransform parentRect = textRect.parent as RectTransform;
            if (canvas == null || parentRect == null)
            {
                return;
            }

            textRect.ForceRebuildLayout();
            Camera eventCam = canvas.GetEventCamera();

            Rect screenRect = UIUtil.NormalizedRectToScreenRect(normalizedRect);
            Vector2 textScreenPixelSize = textRect.GetScreenPixelSize(eventCam);
            DisplaySide side = DetermineDisplaySide(screenRect, textScreenPixelSize);
            DisplayAlign align = DetermineDisplayAlign(screenRect, textScreenPixelSize);

            Vector2 targetScreenPos = Vector2.zero;
            if (side == DisplaySide.Left)
            {
                targetScreenPos.x = screenRect.xMin - TextPadding;
                textRect.pivot = new Vector2(1f, 0.5f);
            }
            else
            {
                targetScreenPos.x = screenRect.xMax + TextPadding;
                textRect.pivot = new Vector2(0f, 0.5f);
            }

            if (align == DisplayAlign.Top)
            {
                targetScreenPos.y = screenRect.yMax;
                textRect.pivot = new Vector2(textRect.pivot.x, 1f);
            }
            else
            {
                targetScreenPos.y = screenRect.yMin;
                textRect.pivot = new Vector2(textRect.pivot.x, 0f);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPos, eventCam, out Vector2 localInParent);
            textRect.anchoredPosition = localInParent - parentRect.rect.center;
            ClampToScreenEdges(textRect, parentRect, eventCam);
        }

        TMP_Text CreateOrReuseText()
        {
            if (CachedTexts.Count != 0)
            {
                int cachedIndex = CachedTexts.Count - 1;
                TMP_Text text = CachedTexts[cachedIndex];
                CachedTexts.RemoveAt(CachedTexts.Count - 1);
                return text;
            }

            if (TextPrefab == null || TextRoot == null)
            {
                LogUtil.Error("TutorialUIController text references are not fully assigned.");
                return null;
            }

            return Instantiate(TextPrefab, TextRoot).GetComponent<TMP_Text>();
        }

        void SetupSkipInteraction()
        {
            GameObject target = Veil != null ? Veil : gameObject;
            if (target == null)
            {
                return;
            }

            skipEventTrigger = target.GetComponent<EventTrigger>();
            if (skipEventTrigger == null)
            {
                skipEventTrigger = target.AddComponent<EventTrigger>();
            }
            if (skipEventTrigger.triggers == null)
            {
                skipEventTrigger.triggers = new List<EventTrigger.Entry>();
            }

            skipPointerClickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            skipPointerClickEntry.callback.AddListener(_ => OnSkipPointerClick());
            skipEventTrigger.triggers.Add(skipPointerClickEntry);
        }

        void TeardownSkipInteraction()
        {
            if (skipEventTrigger == null || skipPointerClickEntry == null)
            {
                return;
            }

            skipPointerClickEntry.callback.RemoveAllListeners();
            skipEventTrigger.triggers.Remove(skipPointerClickEntry);
            skipPointerClickEntry = null;
            skipEventTrigger = null;
        }

        void OnSkipPointerClick()
        {
            TutorialManager.Instance?.SkipCurrentStage();
        }

        void PrepareTextLayout(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }
            const float maxWidth = 420f;
            Vector2 preferred = text.GetPreferredValues(text.text, maxWidth, 0f);
            text.rectTransform.sizeDelta = new Vector2(
                Mathf.Clamp(preferred.x + 24f, 120f, maxWidth),
                Mathf.Max(preferred.y + 16f, 44f));
        }

        void ClampToScreenEdges(RectTransform rectTransform, RectTransform parentRect, Camera eventCam)
        {
            const float edgePad = 4f;
            float minX = edgePad;
            float maxX = Screen.width - edgePad;
            float minY = edgePad;
            float maxY = Screen.height - edgePad;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            float sMinX = float.PositiveInfinity;
            float sMaxX = float.NegativeInfinity;
            float sMinY = float.PositiveInfinity;
            float sMaxY = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(eventCam, corners[i]);
                if (sp.x < sMinX) sMinX = sp.x;
                if (sp.x > sMaxX) sMaxX = sp.x;
                if (sp.y < sMinY) sMinY = sp.y;
                if (sp.y > sMaxY) sMaxY = sp.y;
            }

            float dx = 0f;
            float dy = 0f;
            if (sMinX < minX) dx = minX - sMinX;
            else if (sMaxX > maxX) dx = maxX - sMaxX;
            if (sMinY < minY) dy = minY - sMinY;
            else if (sMaxY > maxY) dy = maxY - sMaxY;

            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
            {
                return;
            }

            Vector2 screenRef = RectTransformUtility.WorldToScreenPoint(eventCam, corners[0]);
            Vector2 screenRef2 = screenRef + new Vector2(dx, dy);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenRef, eventCam, out Vector2 local0))
            {
                return;
            }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenRef2, eventCam, out Vector2 local1))
            {
                return;
            }

            rectTransform.anchoredPosition += local1 - local0;
        }
        
        
    }


}
