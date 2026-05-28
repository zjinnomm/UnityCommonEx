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
            public int Key;
            public int StageIndex;
            public int EntryIndex;
            public float ElapsedTime;
            public Rect Rect;
        }

        TutorialTemplate Tutorial;
        public bool HasActiveTutorial => Tutorial != null;
        public uint MaskId { get; private set; }
        Dictionary<int, Rect> RectOverrides;
        Action OnTutorialEndCallback;
        bool PausedGameplay;
        readonly List<ActivatedEntry> ActivatedEntries = new List<ActivatedEntry>();
        readonly HashSet<int> ToBeDeactivatedKeys = new HashSet<int>();
        int CurrentStageIndex;
        int NextEntryIndex;
        float StageElapsedTime;

        public void Initialize(Func<TutorialUIController> showFunc, Action hideFunc)
        {
            Tutorial = null;
            MaskId = 0;
            RectOverrides = null;
            OnTutorialEndCallback = null;
            PausedGameplay = false;
            ActivatedEntries.Clear();
            ToBeDeactivatedKeys.Clear();
            CurrentStageIndex = 0;
            NextEntryIndex = 0;
            StageElapsedTime = 0;
            ShowUIFunc = showFunc;
            HideUIFunc = hideFunc;
        }

        public bool IsTicking() => true;

        public TickType GetTickType() => TickType.UI;

        public void OnTickStopped()
        {
            EndTutorial();
        }

        public void Tick(float delta)
        {
            if (Tutorial == null)
            {
                EndTutorial();
                return;
            }

            TutorialStage stage = GetCurrentStage();
            if (stage == null)
            {
                EndTutorial();
                return;
            }

            StageElapsedTime += delta;
            ActivateDueEntries(stage);
            UpdateActivatedEntries(delta);
            FlushPendingDeactivations();

            while (Tutorial != null && IsCurrentStageComplete())
            {
                if (!AdvanceToNextStage())
                {
                    EndTutorial();
                    return;
                }
            }
        }

        void ActivateDueEntries(TutorialStage stage)
        {
            TutorialEntry[] entries = stage?.Entries;
            if (entries == null)
            {
                return;
            }

            while (NextEntryIndex < entries.Length)
            {
                TutorialEntry entry = entries[NextEntryIndex];
                if (entry == null)
                {
                    NextEntryIndex++;
                    continue;
                }

                if (StageElapsedTime < entry.Delay)
                {
                    break;
                }

                ActivateNext(stage);
            }
        }

        void UpdateActivatedEntries(float delta)
        {
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                ActivatedEntry record = ActivatedEntries[i];
                record.ElapsedTime += delta;
                TutorialEntry entry = GetEntry(record.StageIndex, record.EntryIndex);
                entry?.OnUpdate(delta, ref record);
                ActivatedEntries[i] = record;
                if (entry != null && entry.Duration > 0 && record.ElapsedTime >= entry.Duration)
                {
                    Deactivate(record.Key);
                }
            }
        }

        void FlushPendingDeactivations()
        {
            if (ToBeDeactivatedKeys.Count == 0)
            {
                return;
            }

            for (int i = ActivatedEntries.Count - 1; i >= 0; i--)
            {
                ActivatedEntry record = ActivatedEntries[i];
                if (!ToBeDeactivatedKeys.Contains(record.Key))
                {
                    continue;
                }

                TutorialEntry entry = GetEntry(record.StageIndex, record.EntryIndex);
                entry?.OnDeactivate(ref record);
                ActivatedEntries.RemoveAt(i);
            }

            ToBeDeactivatedKeys.Clear();
        }

        void ActivateNext(TutorialStage stage)
        {
            TutorialEntry entry = stage.Entries[NextEntryIndex];
            ActivatedEntry record = new ActivatedEntry {
                Key = ComposeEntryKey(CurrentStageIndex, NextEntryIndex),
                StageIndex = CurrentStageIndex,
                EntryIndex = NextEntryIndex,
                ElapsedTime = 0,
                Rect = ResolveRect(entry),
            };
            entry.OnActivate(ref record);
            ActivatedEntries.Add(record);
            NextEntryIndex++;
        }

        public void Deactivate(int key)
        {
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                if (ActivatedEntries[i].Key == key)
                {
                    ToBeDeactivatedKeys.Add(key);
                    return;
                }
            }
        }

        public void DeactivateAll()
        {
            for (int i = 0; i < ActivatedEntries.Count; i++)
            {
                ToBeDeactivatedKeys.Add(ActivatedEntries[i].Key);
            }
        }

        public bool SkipCurrentStage()
        {
            if (Tutorial == null)
            {
                return false;
            }

            if (!AdvanceToNextStage(forceSkip: true))
            {
                EndTutorial();
            }
            return true;
        }

        bool AdvanceToNextStage(bool forceSkip = false)
        {
            TutorialStage[] stages = Tutorial?.Stages;
            if (stages == null || CurrentStageIndex >= stages.Length)
            {
                return false;
            }

            if (!forceSkip && !IsCurrentStageComplete())
            {
                return false;
            }

            ClearActivatedEntries();

            CurrentStageIndex++;
            NextEntryIndex = 0;
            StageElapsedTime = 0;

            return CurrentStageIndex < stages.Length;
        }

        void ClearActivatedEntries()
        {
            for (int i = ActivatedEntries.Count - 1; i >= 0; i--)
            {
                ActivatedEntry record = ActivatedEntries[i];
                TutorialEntry entry = GetEntry(record.StageIndex, record.EntryIndex);
                entry?.OnDeactivate(ref record);
            }

            ActivatedEntries.Clear();
            ToBeDeactivatedKeys.Clear();
        }

        bool IsCurrentStageComplete()
        {
            TutorialStage stage = GetCurrentStage();
            TutorialEntry[] entries = stage?.Entries;
            return entries == null || (NextEntryIndex >= entries.Length && ActivatedEntries.Count == 0 && ToBeDeactivatedKeys.Count == 0);
        }

        TutorialStage GetCurrentStage()
        {
            TutorialStage[] stages = Tutorial?.Stages;
            if (stages == null || CurrentStageIndex < 0 || CurrentStageIndex >= stages.Length)
            {
                return null;
            }

            return stages[CurrentStageIndex];
        }

        TutorialEntry GetEntry(int stageIndex, int entryIndex)
        {
            TutorialStage[] stages = Tutorial?.Stages;
            if (stages == null || stageIndex < 0 || stageIndex >= stages.Length)
            {
                return null;
            }

            TutorialEntry[] entries = stages[stageIndex]?.Entries;
            if (entries == null || entryIndex < 0 || entryIndex >= entries.Length)
            {
                return null;
            }

            return entries[entryIndex];
        }

        static int ComposeEntryKey(int stageIndex, int entryIndex)
        {
            return (stageIndex << 16) ^ entryIndex;
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

            TutorialStage[] stages = tutorial.Stages;
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            Tutorial = tutorial;
            OnTutorialEndCallback = onTutorialEndCallback;
            RectOverrides = rectOverrides;

            CurrentStageIndex = 0;
            NextEntryIndex = 0;
            StageElapsedTime = 0;

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

            ClearActivatedEntries();
            CurrentStageIndex = 0;
            NextEntryIndex = 0;
            StageElapsedTime = 0;

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
