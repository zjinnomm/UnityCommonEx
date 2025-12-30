using System;
using System.Collections.Generic;
using System.Linq;
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
            public Rect RectOverride;
        }

        TutorialTemplate Tutorial;
        public uint MaskId { get; private set; }
        Dictionary<int, Rect> RectOverride;
        Action OnTutorialEndCallback;
        List<ActivatedEntry> ActivatedEntries = new List<ActivatedEntry>();
        HashSet<int> ToBeDeactivatedIndexes = new HashSet<int>();
        int NextEntryIndex = 0;
        float ElapsedTime;

        public void Initialize(Func<TutorialUIController> showFunc, Action hideFunc)
        {
            ShowUIFunc = showFunc;
            HideUIFunc = hideFunc;
        }

        public bool IsTicking() => true;

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
                blocked = Tutorial.Entries[ActivatedEntries.Last().Index].IsBlocking;
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
            Rect rect = Rect.zero;
            RectOverride?.TryGetValue(NextEntryIndex, out rect);
            ActivatedEntry record = new ActivatedEntry {
                Index = NextEntryIndex,
                ElapsedTime = 0,
                RectOverride = rect,
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

        public void StartTutorial(TutorialTemplate tutorial, Action onTutorialEndCallback = null, Dictionary<int, Rect> rectOverride = null)
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
            RectOverride = rectOverride;

            NextEntryIndex = 0;
            ElapsedTime = 0;

            TutorialUIController ui = ShowUIFunc();
            if (tutorial.WithVeil)
            {
                ui.SetVeilEnabled(true);
                MaskId = InteractionModel.MaskRegion(new Rect(0, 0, 1, 1));
            }
            else
            {
                ui.SetVeilEnabled(false);
            }

            TickingManager.Register(this);
        }

        public void StartTutorial(string tutorial, Action onTutorialEndCallback = null, Dictionary<int, Rect> rectOverride = null)
        {
            StartTutorial(DataTemplateManager.Get<TutorialTemplate>(tutorial), onTutorialEndCallback, rectOverride);
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
            RectOverride = null;
            Tutorial = null;
        }

    }

}