using System;
using System.Timers;

namespace DateTimeEvent
{
    public class Scheduler
    {
        private static Timer _timer;
        public event EventHandler SchedulerElapsed;
            
        public Scheduler(DateTime scheduledTime)
        {
            SetupTimer(scheduledTime);
        }

        private void SetupTimer(DateTime dateTime)
        {
            if (dateTime < DateTime.Now)
                throw new ArgumentOutOfRangeException(nameof(dateTime), string.Format("The date entered is in the past."));
            _timer = new Timer((dateTime - DateTime.Now).TotalMilliseconds);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            OnEventFired();
            Dispose();
        }

        public void Dispose()
        {
            if (_timer == null) return;
            _timer.Stop();
            _timer.Dispose();
        }

        private void OnEventFired()
        {
            SchedulerElapsed?.Invoke(this, EventArgs.Empty);
        }
    }
}