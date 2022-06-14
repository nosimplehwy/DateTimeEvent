using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Logging;
using Crestron.RAD.Common.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DateTimeEvent
{
    class DateTimeEventTransport : ATransportDriver
    {
        public DateTimeEventTransport()
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "CustomTileTransport", "constructor");
        }
        public override void SendMethod(string message, object[] paramaters)
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "SendMethod", message);
        }

        public override void Start()
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Start", "Start method");
        }

        public override void Stop()
        {
            DriverLog.Log(EnableLogging, Log, LoggingLevel.Debug, "Stop", "Stop method");
        }
    }
}
