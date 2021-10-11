using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.XEvent;
using Microsoft.SqlServer.XEvent.Linq;
using System.Diagnostics;

namespace agdiag
{
    //For right now just keep a simple enum of XE Sources, should change later
    public enum DataSourceType : byte
    {
        SystemHealth = 1,
        AlwaysOnHealth,
        AlwaysOnSpServerDiagnostics,
        Unknown
    }

    public struct InterestingEvent
    {
        public string strNodeName;
        public string strInstanceName;
        public DateTime DateTimeOfEvent;
        public string strEventName;
        public string strFieldName;
        public string strFieldValue;
        public string strAdditionalInformation;
        public string strRawData;
    }

    //
    public class XEReader
    {
        public string XEDirectory
        {
            get;
            set;
        }

        public DataSourceType SourceFileType

        {
            get;
            set;
        }

        public int ProcessedEvents
        {
            get
            {
                return iProcessedEvents;
            }
        }

        public TimeSpan ProcessingTime
        {
            get
            {
                return swProcessingTime.Elapsed;
            }
        }

        public bool XELogsExist { get; set; }

        public List<InterestingEvent> AllInterestingEvents

        {
            get
            {
                return peGlobalProcessEvents.GetAllInterestingEvents();
            }
        }

        public int InterestingEventCount
        {
            get
            {
                return peGlobalProcessEvents.GetAllInterestingEvents().Count;
            }
        }

        int iProcessedEvents;
        Stopwatch swProcessingTime;
        IProcessEvents peGlobalProcessEvents;

        public XEReader(string strDirectory, DataSourceType XESourceFileCollectionType)
        {
            XEDirectory = strDirectory;
            SourceFileType = XESourceFileCollectionType;
            iProcessedEvents = 0;
            swProcessingTime = new Stopwatch();
            this.XELogsExist = false;
        }

        public bool ExecuteCheck()
        {
            swProcessingTime.Reset();

            try
            {
                string NameMask;
                if (!GetFileNameMask(out NameMask))
                {
                    throw new Exception("ExecuteCheck: Could Not Find A Suitable Name Mask.");
                }

                string XELFiles = XEDirectory + "\\" + NameMask;

                swProcessingTime.Start();

                QueryableXEventData QXED = new QueryableXEventData(XELFiles);

                //Changed so that this is dynamic and doesn't need to be touched
                peGlobalProcessEvents = GetNewEventProcessor();

                ProcessXEData(QXED, peGlobalProcessEvents);

                swProcessingTime.Stop();

                return true;
            }
            catch
            {
                throw;
            }
        }

        private bool GetFileNameMask(out string FileNameMask)
        {

            switch (SourceFileType)
            {
                case DataSourceType.AlwaysOnHealth:
                    FileNameMask = "alwayson_health*.xel";
                    break;
                case DataSourceType.AlwaysOnSpServerDiagnostics:
                    FileNameMask = "*sqldiag*.xel";
                    break;
                case DataSourceType.SystemHealth:
                    FileNameMask = "*system_health*.xel";
                    break;
                default:
                    FileNameMask = "Unkown";
                    return false;
            }

            return true;
        }

        private void ProcessXEData(QueryableXEventData QED, IProcessEvents PE)
        {
            foreach (PublishedEvent Event in QED)
            {
                PE.ProcessEventRow(Event);
                iProcessedEvents++;
            }

        }

        //Added for extensibility of datasourcetype processors
        private IProcessEvents GetNewEventProcessor()
        {
            switch (SourceFileType)
            {
                case DataSourceType.AlwaysOnHealth:
                    throw new NotImplementedException("AlwaysOnHealthProcessor Not Yet Implemented.");
                case DataSourceType.AlwaysOnSpServerDiagnostics:
                    return new SPServerDiagnosticsProcessor();
                case DataSourceType.SystemHealth:
                    return new SystemHealthProcessor();
            }

            throw new Exception("Invalid Processor Type!");
        }
    }

    interface IProcessEvents
    {
        void ProcessEventRow(PublishedEvent Event);
        List<InterestingEvent> GetAllInterestingEvents();

    }
}
