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
        private const string EventSetTimeSettingKey = "EventSetTime";
        private const string ScheduledEventSettingKey = "ScheduledEvent";

        #endregion Constants

        #region Fields

        private DateTimeEventProtocol _protocol;
        private ScheduledEvent _scheduledEvent;
        private PropertyValue<string> _eventSetTime;
        private string _eventEnteredTime;
        private PropertyValue<string> _eventStatusText;
        private PropertyValue<string> _eventSetTimeError;
        private PropertyValue<bool> _eventEnableButtonStatus;
        private PropertyValue<bool> _eventDisableButtonStatus;

        #endregion Fields

        #region Constructor

        public DateTimeEvent()
        {
            //TODO remove after debugging 
            EnableLogging = true;
            CreateDeviceDefinition();

        }

        #endregion Constructor

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

                    if(!DateTime.TryParse(_eventEnteredTime, new CultureInfo("en-US"), DateTimeStyles.None, out var verifiedTime))
                    {
                        _eventSetTimeError.Value = "The text entered is not in the correct format";
                        Commit();
                        return new OperationResult(OperationResultCode.Success);
                    }
                    if(DateTime.Now < verifiedTime)
                    {
                        _eventSetTimeError.Value = "The date entered is in the past.";
                        Commit();
                        return new OperationResult(OperationResultCode.Success);
                    }
                    _eventSetTime.Value = _eventEnteredTime;
                    _eventSetTimeError.Value = Empty;
                    Commit();
                    
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
                        _eventEnteredTime = setTime;
                    }
                    else
                    {
                        _eventSetTimeError.Value = "The text entered is invalid.";
                        Commit();
                    }

                    break;
                }
                default:
                {
                    return new OperationResult(OperationResultCode.Error, "The property with object does not exist.");
                }
            }
            
            return new OperationResult(OperationResultCode.Success);
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string objectId, string propertyKey, T value)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "SetDriverPropertyValueWithObject", propertyKey);
            return new OperationResult(OperationResultCode.Error, "The property with object does not exist.");
        }

        public override void Dispose()
        {
            if (_scheduledEvent != null)
                _scheduledEvent.Enable = false;
        }

        #endregion AExtensionDevice Members

        #region ICloudConnected Members

        public void Initialize()
        {
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
            
            try
            {
                var storedEvent = (ScheduledEvent)GetSetting(ScheduledEventSettingKey);
                if (storedEvent.Enable)
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition", "Stored event is enabled.");
                    EventEnable(storedEvent.ScheduledDateTime);
                }
                else
                {
                    DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition", "Stored event is disabled.");
                    SetStatus(false);
                }
            }
            catch (Exception exception)
            {
                DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition", $"No event settings stored.{exception}");
                SetStatus(false);
            }

            try
            {
                _eventSetTime.Value = (string)GetSetting(EventSetTimeSettingKey) ?? Empty;
            }
            catch (Exception exception)
            {
                DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CreateDeviceDefinition", $"No time settings stored.{exception}");
                _eventSetTime.Value = Empty;
            }
        }

        private void SetStatus(bool status)
        {
            _eventEnableButtonStatus.Value = !status;
            _eventDisableButtonStatus.Value = status;
            //save event to settings
            SaveSetting(ScheduledEventSettingKey, _scheduledEvent);

            if (status == false)
            {
                _eventSetTime.Value = Empty;
                _eventStatusText.Value = "Disabled";
            }
            else
            {
                _eventStatusText.Value = Format($"Enabled: {_eventSetTime.Value}");
            }
            Commit();
        }

       
        private void EventEnable(DateTime setTime)
        {
            _scheduledEvent = new ScheduledEvent(setTime,true);
            _scheduledEvent.SchedulerEnabled += ScheduledEventOnSchedulerEnabled;
            _scheduledEvent.TriggerScheduledEvent += ScheduledEventOnTriggerScheduledEvent;
            _scheduledEvent.Enable = true;
        }

        private void ScheduledEventOnTriggerScheduledEvent(object sender, EventArgs eventArgs)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ScheduledEventOnTriggerScheduledEvent", "Event Triggered");
            SchedulerTriggered?.Invoke(this, EventArgs.Empty);
        }

        private void ScheduledEventOnSchedulerEnabled(object sender, bool e1)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "ScheduledEventOnSchedulerEnabled", e1.ToString());
            SetStatus(e1);
            SaveSetting(EventSetTimeSettingKey,_eventSetTime.Value);

        }

        private void EventDisable()
        {
            _scheduledEvent.Enable = false;
        }
        #endregion Private Methods
        
        
    }
}

