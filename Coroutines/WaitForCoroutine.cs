using System;
using System.Windows.Forms;

namespace DirectXEngine
{
    public class WaitForCoroutine : Coroutine
    {
        public WaitForCoroutine(TimeSpan delay)
        {
            _Delay = delay;
            _StartTime = DateTime.Now;
        }

        private TimeSpan _Delay;
        private DateTime _StartTime;
        public override bool CanMoveNext => DateTime.Now - _StartTime >= _Delay;
    }
}
