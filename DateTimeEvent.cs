using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.Common.Interfaces.ExtensionDevice;
using Crestron.RAD.Common.Logging;
using Crestron.RAD.DeviceTypes.ExtensionDevice;
using System;
using System.Globalization;
using Crestron.RAD.Common.Attributes.Programming;
using static System.String;

namespace DateTimeEvent
{
    public class DateTimeEvent : AExtensionDevice, ICloudConnected
    {
        #region Constants
        
        //UI Definition
        private const string EventSetTimeKey = "EventSetTime";
        private const string EventStatusTextKey = "EventStatusText";
        private const string EventSetTimeErrorKey = "EditEventSetTimeErrorMessage";
        private const string EventEnableStatusKey = "EventEnableButtonStatus";
        private const string EventDisableStatusKey = "EventDisableButtonStatus";
        
        //Settings
        private const string ScheduledEventSettingKey = "ScheduledEventSetting";

        #endregion Constants

        #region Fields

        private DateTimeEventProtocol _protocol;
        private ScheduledEvent _scheduledEvent;
        private PropertyValue<string> _eventSetTime;
        private PropertyValue<string> _eventStatusText;
        private PropertyValue<string> _eventSetTimeError;
        private PropertyValue<bool> _eventEnableButtonStatus;
        private PropertyValue<bool> _eventDisableButtonStatus;

        #endregion Fields
        
        #region AExtensionDevice Members

        protected override IOperationResult DoCommand(string command, string[] parameters)
        {

            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "DoCommand", command);

            if (IsNullOrEmpty(command))
                return new OperationResult(OperationResultCode.Error, "command string is empty");

            switch (command)
            {
                case "EventEnable":
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", command);

                    if(!DateTime.TryParse(_eventSetTime.Value, new CultureInfo("en-US"), DateTimeStyles.None, out var verifiedTime))
                    {
                        _eventSetTimeError.Value = "The text entered is not in the correct format";
                        Commit();
                        return new OperationResult(OperationResultCode.Success);
                    }
                    if(DateTime.Now >= verifiedTime)
                    {
                        _eventSetTimeError.Value = "The date entered is in the past.";
                        Commit();
                        return new OperationResult(OperationResultCode.Success);
                    }
                    
                    EventEnable(verifiedTime);
                    return new OperationResult(OperationResultCode.Success);                    
                }
                case "EventDisable":
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", command);
                    EventDisable();
                    return new OperationResult(OperationResultCode.Success);                    
                }
                default:
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Switch", "Unhandled command!");
                    return new OperationResult(OperationResultCode.Error, "Unhandled command!");
                }
            }

        }

        protected override IOperationResult SetDriverPropertyValue<T>(string propertyKey, T value)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "SetDriverPropertyValue", propertyKey);

            switch (propertyKey)
            {
                case "EventSetTime":
                {
                    if (value is string setTime)
                    {
                        DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "SetDriverPropertyValue", Format($"EventSetTime: {setTime}"));
                        _eventSetTime.Value = setTime;
                        _eventSetTimeError.Value = Empty;
                    }
                    else
                    {
                        _eventSetTimeError.Value = "The text entered is invalid.";
                    }

                    Commit();
                    return new OperationResult(OperationResultCode.Success);
                }
                default:
                {
                    return new OperationResult(OperationResultCode.Error, "The property with object does not exist.");
                }
            }
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string objectId, string propertyKey, T value)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "SetDriverPropertyValueWithObject", propertyKey);
            return new OperationResult(OperationResultCode.Error, "The property with object does not exist.");
        }

        public override void Dispose()
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Dispose", Empty);
            if (_scheduledEvent != null)
                _scheduledEvent.Enable = false;
        }

        #endregion AExtensionDevice Members

        #region ICloudConnected Members

        public void Initialize()
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Initialize", Empty);
            var transport = new DateTimeEventTransport
            {
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger,
                EnableRxDebug = InternalEnableRxDebug,
                EnableTxDebug = InternalEnableTxDebug
            };
            ConnectionTransport = transport;

            _protocol = new DateTimeEventProtocol(transport, Id)
            {
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger
            };

            _protocol.Initialize(DriverData);
            DeviceProtocol = _protocol;
            
            CreateDeviceDefinition();

        }


        #endregion ICloudConnected Members

        #region IConnection Members

        public override void Connect()
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Connect", "Connect");

            Connected = true;

        }


        #endregion IConnection Members

        #region Programmable Operations



        #endregion Programmable Operations


        #region Programmable Events


        [ProgrammableEvent("SchedulerTimeElapsed")] public event EventHandler SchedulerTriggered;


        #endregion Programmable Events


        #region Private Methods

        private void CreateDeviceDefinition()
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition", "");

            _eventSetTime = CreateProperty<string>(new PropertyDefinition(EventSetTimeKey, Empty,
                DevicePropertyType.String));
            _eventStatusText = CreateProperty<string>(new PropertyDefinition(EventStatusTextKey, Empty,
                DevicePropertyType.String));
            _eventSetTimeError = CreateProperty<string>(new PropertyDefinition(EventSetTimeErrorKey, Empty,
                DevicePropertyType.String));
            _eventEnableButtonStatus = CreateProperty<bool>(new PropertyDefinition(EventEnableStatusKey, Empty,
                DevicePropertyType.Boolean));
            _eventDisableButtonStatus = CreateProperty<bool>(new PropertyDefinition(EventDisableStatusKey, Empty,
                DevicePropertyType.Boolean));
            
            RestoreSettings();
        }

        private void SetStatus(bool status)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "SetStatus", status.ToString());
            _eventEnableButtonStatus.Value = !status;
            _eventDisableButtonStatus.Value = status;
            
            // ReSharper disable once RedundantBoolCompare, 
            if (_scheduledEvent != null && status == true)
            {
                _eventSetTime.Value = Format($"{_scheduledEvent.ScheduledDateTime:MM/dd/yyyy h:mm tt}");
                _eventStatusText.Value = Format($"Enabled: {_eventSetTime.Value}");
            }
            else
            {
                _eventSetTime.Value = Empty;
                _eventStatusText.Value = "Disabled";
                                  
            }
            Commit();
            StoreSettings();            
        }

       
                private void EventEnable(DateTime setTime)
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "EventEnable", Empty);
                    _scheduledEvent = new ScheduledEvent(setTime,true);
                    _scheduledEvent.SchedulerEnabled += ScheduledEventOnSchedulerEnabled;
                    _scheduledEvent.TriggerScheduledEvent += ScheduledEventOnTriggerScheduledEvent;
                    _scheduledEvent.Enable = true;
                }

                private void ScheduledEventOnTriggerScheduledEvent(object sender, EventArgs eventArgs)
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ScheduledEventOnTriggerScheduledEvent", "Event Triggered");
                    TriggerEvent();
                }

                private void ScheduledEventOnSchedulerEnabled(object sender, bool e1)
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ScheduledEventOnSchedulerEnabled", e1.ToString());
                    SetStatus(e1);
                }

                private void TriggerEvent()
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "TriggerEvent", "Event Trigger Invoked");
                    SchedulerTriggered?.Invoke(this, EventArgs.Empty);
                   
                }

                private void EventDisable()
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "EventDisable", Empty);
                    _scheduledEvent.Enable = false;
                }
                #endregion Private Methods

                private void StoreSettings()
                {
                    if (_scheduledEvent == null) return;
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "StoreSettings", "Save Settings");
                    SaveSetting(ScheduledEventSettingKey, new StoredSettings(_scheduledEvent.ScheduledDateTime,_eventSetTime.Value,_scheduledEvent.Enable));
                }
                private void RestoreSettings()
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "RestoreSettings", "Save Settings");
                    var storedEvent = (StoredSettings)GetSetting(ScheduledEventSettingKey);
                    if (storedEvent == null)
                    {
                        DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition",
                            $"No event settings stored.");
                        SetStatus(false);
                        return;
                    }

                    if (storedEvent.SetEnabled)
                    {
                            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition",
                                "Stored event is enabled.");
                            _eventSetTime.Value = storedEvent.SetText;
                            if (storedEvent.SetDateTime > DateTime.Now)
                                EventEnable(storedEvent.SetDateTime);
                            //assume that the system was not running when the even was supposed to fire and fire it when it starts up
                            else
                            {
                                TriggerEvent();
                                var time = storedEvent.SetDateTime;
                                var setTime = new DateTime(DateTime.Now.Year + 1, time.Month, time.Day, time.Hour,
                                    time.Minute, time.Second);
                                EventEnable(setTime);
                            }
                    }
                    else
                    {
                            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition",
                                "Stored event is disabled.");
                            SetStatus(false);
                    }
                }
                
        private class StoredSettings
        {
            public StoredSettings(DateTime setDateTime, string setText, bool setEnabled)
            {
                SetDateTime = setDateTime;
                SetText = setText;
                SetEnabled = setEnabled;
            }
            public DateTime SetDateTime { get; }
            public string SetText { get; }
            public bool SetEnabled { get; }
        }
    }

  
}

