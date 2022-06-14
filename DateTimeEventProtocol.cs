using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Logging;
using Crestron.RAD.Common.Transports;

namespace DateTimeEvent
{
    public class DateTimeEventProtocol : ABaseDriverProtocol
    {
        public DateTimeEventProtocol(ISerialTransport transport, byte id) : base(transport, id)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CustomTileProtocol", "constructor");
        }

        protected override void ConnectionChangedEvent(bool connection)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Error, "ConnectionChangedEvent", connection.ToString());
        }

        protected override void ChooseDeconstructMethod(ValidatedRxData validatedData)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Error, "ChooseDeconstructMethod", validatedData.Data);
        }

        public override void SetUserAttribute(string attributeId, string attributeValue)
        {
            if (!string.IsNullOrEmpty(attributeValue)) return;
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Error, "SetUserAttribute",
                "Attribute value is null or empty");

        }

        public override void SetUserAttribute(string attributeId, bool attributeValue)
        {
        }

        public override void Dispose()
        {
            // Do nothing for now, this is due to a bug in the base class Dispose method
        }

        
    }
}