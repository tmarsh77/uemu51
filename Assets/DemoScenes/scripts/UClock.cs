using System;
using System.Threading;

using UnityEngine;
using Unity8051Emu.Wrapper;

namespace Uemu.Demo
{
    public class UClock : MonoBehaviour, IClock
    {
        public event EventHandler<EventArgs> Tick;
        public int SpeedMode { get; private set; }

        private int[] _rates;
        private bool _active;
        private bool _paused;
        private long _speed;
        private Thread _clockThread;

        public double MsPerTick
        {
            get
            {
                return 1000D / _rates[SpeedMode];
            }
        }

        public void Init()
        {
            _rates = new int[]
            { 1000000, 1000, 100, 10, 1 }; // uS ms 1/100s 1/10s 1s

            SpeedMode = 0;
            _speed = TimeSpan.TicksPerSecond / _rates[SpeedMode];

            _clockThread = new Thread(Timer);
            _clockThread.Priority = System.Threading.ThreadPriority.Highest;

            _active = true;
            _clockThread.Start();
        }

        private void OnDestroy()
        {
            _active = false;
            _clockThread.Abort();
        }

        public void OnApplicationFocus(bool focus)
        {
            _paused = !focus;
        }

        private void Timer()
        {
            while (_active)
            {
                Thread.Sleep(TimeSpan.FromTicks(_speed));
                DoTick();
            }
        }

        public void AdjustSpeed(char mode)
        {
            if (mode == '+')
            {
                if (SpeedMode > 0)
                    SpeedMode--;
            }
            else if (mode == '-')
            {
                if (SpeedMode < _rates.Length - 1)
                    SpeedMode++;
            }
            _speed = TimeSpan.TicksPerSecond / _rates[SpeedMode];
        }

        public void Impulse()
        {
            DoTick();
        }

        private void DoTick()
        {
            if (_paused) return;
            if (Tick != null) Tick(this, EventArgs.Empty);
        }
    }
}