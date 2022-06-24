using System;
using Crestron.RAD.Common.Attributes.Programming;

namespace DateTimeEvent
{
    public class ScheduledEvent
    {
        private static Scheduler _scheduler;
        private bool _enabled;
        private readonly bool _recurring;

        public event EventHandler TriggerScheduledEvent;

        public event EventHandler<bool> SchedulerEnabled; 
        
        public DateTime ScheduledDateTime { get; private set; }

        public bool Enable
        {
            get => _enabled;
            set
            {
                switch (value)
                {
                    case true when _enabled == false:
                        EnableScheduler();
                        break;
                    case false:
                        DisableScheduler();
                        break;
                }
            }
        }

        public ScheduledEvent(DateTime dateTime, bool recurring)
        {
            ScheduledDateTime = dateTime;
            _recurring = recurring;
        }

        private void EnableScheduler()
        {
            OnEnabledChanged(true);
            _scheduler = new Scheduler(ScheduledDateTime);
            _scheduler.SchedulerElapsed += SchedulerOnSchedulerElapsed;
        }

        private void DisableScheduler()
        {
            OnEnabledChanged(false);
            if(_scheduler == null) return;
            _scheduler.SchedulerElapsed -= SchedulerOnSchedulerElapsed;
            _scheduler.Dispose();
        }
        
        private void OnEnabledChanged(bool enabled)
        {
            _enabled = enabled;
            SchedulerEnabled?.Invoke(this, _enabled);
        }
        private void SchedulerOnSchedulerElapsed(object sender, EventArgs e)
        {
            TriggerScheduledEvent?.Invoke(this, EventArgs.Empty);

            if (_recurring != true) return;
            ScheduledDateTime = ScheduledDateTime.AddYears(1);
            DisableScheduler();
            EnableScheduler();
        }


    }
}