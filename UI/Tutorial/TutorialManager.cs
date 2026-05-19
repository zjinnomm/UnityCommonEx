using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public class TutorialManager : Singleton<TutorialManager>, ITickable
    {

        Func<TutorialUIController> ShowUIFunc;
        Action HideUIFunc;

        public struct ActivatedEntry
        {
            public int Index;
            public float ElapsedTime;
            public Rect Rect;
        }

        TutorialTemplate Tutorial;
        public bool HasActiveTutorial => Tutorial != null;
        public uint MaskId { get; private set; }
        Dictionary<int, Rect> RectOverrides;
        Action OnTutorialEndCallback;
        bool PausedGameplay;
        List<ActivatedEntry> ActivatedEntries = new List<ActivatedEntry>();
        HashSet<int> ToBeDeactivatedIndexes = new HashSet<int>();
        int NextEntryIndex = 0;
        float ElapsedTime;

        public void Initialize(Func<TutorialUIController> showFunc, Action hideFunc)
        {
            Tutorial = null;
            MaskId = 0;
            RectOverrides = null;
            OnTutorialEndCallback = null;
            PausedGameplay = false;
            ActivatedEntries.Clear();
            ToBeDeactivatedIndexes.Clear();
            NextEntryIndex = 0;
            ElapsedTime = 0;
            ShowUIFunc = showFunc;
            HideUIFunc = hideFunc;
        }

        public bool IsTicking() => true;

        public TickType GetTickType() => TickType.UI;

        public void Tick(float delta)
        {
            if (Tutorial == null || (ActivatedEntries.Count == 0 && ToBeDeactivatedIndexes.Count == 0 && NextEntryIndex >= Tutorial.Entries.Length))
            {
                EndTutorial();
                return;
            }
            bool blocked = false;
            if (ActivatedEntries.Count > 0)
            {
                blocked = Tutorial.Entries[ActivatedEntries[ActivatedEntries.Count - 1].Index].IsBlocking;
            }

            if (!blocked)
            {
                ElapsedTime += delta;
                while (NextEntryIndex < Tutorial.Entries.Length)
                {
                    TutorialEntry entry = Tutorial.Entries[NextEntryIndex];
                    if (ElapsedTime < entry.Delay)
                    {
                        break;
                    }
                    ActivateNext();
                    blocked = entry.IsBlocking;
                    if (blocked)
                    {
                        break;
                    }
                }
            }

            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                ActivatedEntry record = ActivatedEntries[i];
                record.ElapsedTime += delta;
                TutorialEntry entry = Tutorial.Entries[record.Index];
                entry.OnUpdate(delta, ref record);
                ActivatedEntries[i] = record;
                if (entry.Duration > 0 && record.ElapsedTime >= entry.Duration)
                {
                    Deactivate(record.Index);
                }
            }

            foreach (var index in ToBeDeactivatedIndexes)
            {
                for (int i = 0; i < ActivatedEntries.Count; i++)
                {
                    ActivatedEntry record = ActivatedEntries[i];
                    if (record.Index == index)
                    {
                        TutorialEntry entry = Tutorial.Entries[record.Index];
                        entry.OnDeactivate(ref record);
                        ActivatedEntries.RemoveAt(i);
                        break;
                    }
                }
            }
            ToBeDeactivatedIndexes.Clear();
        }

        public void ActivateNext()
        {
            Rect rect = ResolveRect(Tutorial.Entries[NextEntryIndex]);
            ActivatedEntry record = new ActivatedEntry {
                Index = NextEntryIndex,
                ElapsedTime = 0,
                Rect = rect,
            };
            TutorialEntry entry = Tutorial.Entries[NextEntryIndex];
            entry.OnActivate(ref record);
            ActivatedEntries.Add(record);
            NextEntryIndex ++;
        }

        public void Deactivate(int index)
        {
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                if (ActivatedEntries[i].Index == index)
                {
                    ToBeDeactivatedIndexes.Add(index);
                    return;
                }
            }
        }

        public void DeactivateAll()
        {
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                ToBeDeactivatedIndexes.Add(ActivatedEntries[i].Index);
            }
        }

        public bool SkipActiveEntries()
        {
            if (Tutorial == null || ActivatedEntries.Count == 0)
            {
                return false;
            }

            bool skipped = false;
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                ActivatedEntry record = ActivatedEntries[i];
                TutorialEntry entry = Tutorial.Entries[record.Index];
                if (entry != null && entry.CanSkip)
                {
                    ToBeDeactivatedIndexes.Add(record.Index);
                    skipped = true;
                }
            }

            return skipped;
        }

        public void StartTutorial(TutorialTemplate tutorial, Action onTutorialEndCallback = null, Dictionary<int, Rect> rectOverrides = null)
        {
            if (Tutorial != null)
            {
                EndTutorial();
            }

            if (tutorial == null)
            {
                return;
            }
            
            Tutorial = tutorial;
            OnTutorialEndCallback = onTutorialEndCallback;
            RectOverrides = rectOverrides;

            NextEntryIndex = 0;
            ElapsedTime = 0;

            TutorialUIController ui = ShowUIFunc();
            if (ui == null)
            {
                LogUtil.Error("Tutorial UI is not available. StartTutorial aborted.");
                Tutorial = null;
                OnTutorialEndCallback = null;
                RectOverrides = null;
                return;
            }
            if (tutorial.WithVeil)
            {
                ui.SetVeilEnabled(true);
                MaskId = InteractionModel.MaskRegion(new Rect(0, 0, 1, 1));
            }
            else
            {
                ui.SetVeilEnabled(false);
            }
            if (tutorial.PauseGameplay)
            {
                MainGameController<HubbleBubbleGame>.PushGameplayPause();
                PausedGameplay = true;
            }

            TickingManager.Register(this);
        }

        public void StartTutorial(string tutorial, Action onTutorialEndCallback = null, Dictionary<int, Rect> rectOverrides = null)
        {
            StartTutorial(DataTemplateManager.Get<TutorialTemplate>(tutorial), onTutorialEndCallback, rectOverrides);
        }

        public void StartTutorial(TutorialTemplate tutorial, Dictionary<int, RectTransform> rectTransformOverride, Action onTutorialEndCallback = null)
        {
            StartTutorial(tutorial, onTutorialEndCallback, ConvertRectTransforms(rectTransformOverride));
        }

        public void StartTutorial(string tutorial, Dictionary<int, RectTransform> rectTransformOverride, Action onTutorialEndCallback = null)
        {
            StartTutorial(DataTemplateManager.Get<TutorialTemplate>(tutorial), rectTransformOverride, onTutorialEndCallback);
        }

        public void StartTutorial(TutorialTemplate tutorial, Dictionary<int, Bounds> worldBoundsOverride, Camera worldCamera = null, Action onTutorialEndCallback = null)
        {
            StartTutorial(tutorial, onTutorialEndCallback, ConvertWorldBounds(worldBoundsOverride, worldCamera));
        }

        public void StartTutorial(string tutorial, Dictionary<int, Bounds> worldBoundsOverride, Camera worldCamera = null, Action onTutorialEndCallback = null)
        {
            StartTutorial(DataTemplateManager.Get<TutorialTemplate>(tutorial), worldBoundsOverride, worldCamera, onTutorialEndCallback);
        }

        public void StartTutorial(TutorialTemplate tutorial, params TutorialRectSource[] rectSources)
        {
            StartTutorial(tutorial, (IEnumerable<TutorialRectSource>)rectSources, null);
        }

        public void StartTutorial(string tutorial, params TutorialRectSource[] rectSources)
        {
            StartTutorial(DataTemplateManager.Get<TutorialTemplate>(tutorial), rectSources);
        }

        public void StartTutorial(TutorialTemplate tutorial, Action onTutorialEndCallback, params TutorialRectSource[] rectSources)
        {
            StartTutorial(tutorial, (IEnumerable<TutorialRectSource>)rectSources, onTutorialEndCallback);
        }

        public void StartTutorial(string tutorial, Action onTutorialEndCallback, params TutorialRectSource[] rectSources)
        {
            StartTutorial(DataTemplateManager.Get<TutorialTemplate>(tutorial), onTutorialEndCallback, rectSources);
        }

        public void StartTutorial(TutorialTemplate tutorial, IEnumerable<TutorialRectSource> rectSources, Action onTutorialEndCallback = null)
        {
            StartTutorial(tutorial, onTutorialEndCallback, ConvertRectSources(rectSources));
        }

        public void StartTutorial(string tutorial, IEnumerable<TutorialRectSource> rectSources, Action onTutorialEndCallback = null)
        {
            StartTutorial(DataTemplateManager.Get<TutorialTemplate>(tutorial), rectSources, onTutorialEndCallback);
        }
        
        public void EndTutorial()
        {
            TickingManager.Unregister(this);
            HideUIFunc();
            if (MaskId > 0)
            {
                InteractionModel.RemoveMask(MaskId);
                MaskId = 0;
            }
            if (PausedGameplay)
            {
                MainGameController<HubbleBubbleGame>.PopGameplayPause();
                PausedGameplay = false;
            }

            if (Tutorial == null)
            {
                return;
            }
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                ActivatedEntry record = ActivatedEntries[i];
                TutorialEntry entry = Tutorial.Entries[record.Index];
                entry.OnDeactivate(ref record);
            }
            ActivatedEntries.Clear();
            ToBeDeactivatedIndexes.Clear();

            OnTutorialEndCallback?.Invoke();
            OnTutorialEndCallback = null;
            RectOverrides = null;
            Tutorial = null;
        }

        Rect ResolveRect(TutorialEntry entry)
        {
            if (entry == null)
            {
                return Rect.zero;
            }

            if (RectOverrides != null && RectOverrides.TryGetValue(entry.RectIndex, out Rect overrideRect))
            {
                return overrideRect;
            }

            TutorialRect[] templateRects = Tutorial?.Rects;
            if (templateRects != null &&
                entry.RectIndex >= 0 &&
                entry.RectIndex < templateRects.Length &&
                templateRects[entry.RectIndex] != null)
            {
                return templateRects[entry.RectIndex].ToRect();
            }

            return Rect.zero;
        }

        static Dictionary<int, Rect> ConvertRectTransforms(Dictionary<int, RectTransform> rectTransformOverride)
        {
            if (rectTransformOverride == null)
            {
                return null;
            }

            Dictionary<int, Rect> result = new Dictionary<int, Rect>(rectTransformOverride.Count);
            foreach (var pair in rectTransformOverride)
            {
                if (!UIUtil.TryGetNormalizedScreenRect(pair.Value, out Rect normalizedRect))
                {
                    continue;
                }
                result[pair.Key] = normalizedRect;
            }
            return result;
        }

        static Dictionary<int, Rect> ConvertWorldBounds(Dictionary<int, Bounds> worldBoundsOverride, Camera worldCamera)
        {
            if (worldBoundsOverride == null)
            {
                return null;
            }

            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null)
            {
                return null;
            }

            Dictionary<int, Rect> result = new Dictionary<int, Rect>(worldBoundsOverride.Count);
            foreach (var pair in worldBoundsOverride)
            {
                if (UIUtil.TryGetNormalizedScreenRect(pair.Value, camera, out Rect normalizedRect))
                {
                    result[pair.Key] = normalizedRect;
                }
            }
            return result;
        }

        static Dictionary<int, Rect> ConvertRectSources(IEnumerable<TutorialRectSource> rectSources)
        {
            if (rectSources == null)
            {
                return null;
            }

            Dictionary<int, Rect> result = new Dictionary<int, Rect>();
            foreach (TutorialRectSource source in rectSources)
            {
                result[source.RectIndex] = source.NormalizedRect;
            }
            return result;
        }

    }

}
