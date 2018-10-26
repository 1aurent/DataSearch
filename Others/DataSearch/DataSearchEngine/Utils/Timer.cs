using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DataSearchEngine.Utils
{
    public class Timer : IDisposable
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryPerformanceCounter(out long time);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryPerformanceFrequency(out long frequency);

        private static readonly double SFrequency;
        static Timer()
        {
            long frequency;
            SFrequency = !QueryPerformanceFrequency(out frequency) ? 0 : frequency;
        }

        private readonly long _startTime;
        private readonly string _reason;

        public Timer(string reason)
        {
            _reason = reason;
            if (SFrequency != 0)
                if (!QueryPerformanceCounter(out _startTime)) _startTime = 0;
            log.DebugFormat("{0}: Starting", _reason);
        }

        public void Dispose()
        {
            ShowStatus();
        }

        public void ShowStatus()
        {
            if (_startTime == 0) return;
            long endTime;
            if (!QueryPerformanceCounter(out endTime))
            {
                log.DebugFormat("{0} : Unable to compute time (ERR={1})", _reason, Marshal.GetLastWin32Error());
                return;
            }

            var perfInSec = (endTime - _startTime) / SFrequency;
            if (perfInSec > 60)
            {
                var perfInMin = Math.Floor(perfInSec / 60);
                perfInSec = perfInSec - (perfInMin * 60F);
                if (perfInMin > 60)
                {
                    var perfInHrs = Math.Floor(perfInMin / 60);
                    perfInMin = perfInMin - (perfInHrs * 60F);

                    log.DebugFormat("{0}: Elapsed {1}h {2}mn {3:0.000000}s", _reason, perfInHrs, perfInMin, perfInSec);
                }
                else
                {
                    log.DebugFormat("{0}: Elapsed {1}mn {2:0.000000}s", _reason, perfInMin, perfInSec);
                }
            }
            else
            {
                log.DebugFormat("{0}: Elapsed {1:0.000000}s", _reason, perfInSec);
            }

        }
    }
}
