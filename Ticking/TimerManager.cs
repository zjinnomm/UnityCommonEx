using System;
using System.Collections.Generic;

namespace UnityCommonEx
{

    public static class TimerManager
    {

        struct Timer
        {
            public uint Id;
            public float Time;
            public Action Callback;
        }

        static uint NextTimerId = 1;
        static readonly List<Timer> Timers = new List<Timer>();

        public static uint Start(Action callback, float delay)
        {
            Timers.Add(new Timer{
                Id = NextTimerId,
                Time = delay,
                Callback = callback,
            });
            NextTimerId ++;
            return NextTimerId - 1;
        }

        public static void Cancel(uint id)
        {
            for (int i = 0; i < Timers.Count; i++)
            {
                if (Timers[i].Id == id)
                {
                    Timers.RemoveAt(i);
                    return;
                }
            }
        }

        public static void Tick(float delta)
        {
            for (int i = 0; i < Timers.Count; i++)
            {
                Timer timer = Timers[i];
                timer.Time -= delta;
                if (timer.Time <= 0)
                {
                    Timers.RemoveAt(i);
                    i--;
                    timer.Callback();
                }
                else
                {
                    Timers[i] = timer;
                }
            }
        }


    }

}