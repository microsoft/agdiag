using System;
using System.Collections.Generic;
using Microsoft.SqlServer.XEvent.Linq;

namespace agdiag
{
    class SPServerDiagnosticsProcessor:IProcessEvents
    {
        List<InterestingEvent> InterestingEventsList;
        int iMaxWorkers;
        int iMaxCreatedWorkers;

        public SPServerDiagnosticsProcessor()
        {
            InterestingEventsList = new List<InterestingEvent>();
            iMaxWorkers = 0;
            iMaxCreatedWorkers = 0;
        }

        void IProcessEvents.ProcessEventRow(PublishedEvent Event)
        {
            //main thing we're after, all of them should be this
            //but just in case, do a check

            switch (Event.Name)
            {
                case "component_health_result":
                    CheckComponentHealthResult(Event);
                    return;
                case "availability_group_is_alive_failure":
                    CheckIsAliveFailureResult(Event);
                    return;
                default: // do nothing, we don't care about the event
                    return;
            }

        }

        private void CheckComponentHealthResult(PublishedEvent Event)
        {
            //highest priority first
            if ("error" == Event.Fields["state_desc"].Value.ToString())
            {
                InterestingEvent ieTemp = CreateAndPopulateComponentInterestingEvent(Event, "state_desc");
                AttemptToFindBestReasonForComponentError(ref ieTemp, ref Event);
                ieTemp.strRawData = Event.Fields["data"].Value.ToString();
                InterestingEventsList.Add(ieTemp);
            }

            if ("query_processing" == Event.Fields["component"].Value.ToString())
            {
                UpdateMaxWorkers(Event);
                if (iMaxCreatedWorkers >= iMaxWorkers)
                {
                    InterestingEventsList.Add(CreateAndPopulateComponentCustomEvent(Event, "max_workers", iMaxCreatedWorkers.ToString(), "Created Workers >= Max Workers"));
                }
            }
        }

        private void CheckIsAliveFailureResult(PublishedEvent Event)
        {
            InterestingEventsList.Add(CreateAndPopulateIsAliveInterestingEvent(Event));
        }

        private void AttemptToFindBestReasonForComponentError(ref InterestingEvent ieEvent, ref PublishedEvent peEvent)
        {
            switch (peEvent.Fields["component"].Value.ToString())
            {
                case "system":
                    ieEvent.strAdditionalInformation = SystemErrorReason(peEvent.Fields["data"].Value.ToString());
                    break;
                case "resource":
                    ieEvent.strAdditionalInformation = ResourceErrorReason(peEvent.Fields["data"].Value.ToString());
                    break;
                case "query_processing":
                    ieEvent.strAdditionalInformation = QueryProcessingErrorReason(peEvent.Fields["data"].Value.ToString());
                    break;
            }
        }

        private string SystemErrorReason(string strXML)
        {

            Utility.XMLAttributeValue[] xavSystem = new Utility.XMLAttributeValue[3];

            xavSystem[0].strAttributeName = "totalDumpRequests";
            xavSystem[0].strAttributeQuery = "//system";

            xavSystem[1].strAttributeName = "intervalDumpRequests";
            xavSystem[1].strAttributeQuery = "//system";

            xavSystem[2].strAttributeName = "sickSpinlockTypeAfterAv";
            xavSystem[2].strAttributeQuery = "//system";

            int iTotal = Utility.GetXMLAttributeValues(strXML, ref xavSystem);

            if (iTotal < xavSystem.Length)
            {
                throw new Exception("SystemErrorReason: Couldn't get all results from XML parsing.");
            }

            int iTotalDumpCount = 0;
            int iIntervalDumpCount = 0;

            int.TryParse(xavSystem[0].strAttributeValue, out iTotalDumpCount);
            int.TryParse(xavSystem[1].strAttributeValue, out iIntervalDumpCount);

            if (iTotalDumpCount > 100 && iIntervalDumpCount > 0)
            {
                return string.Format("Error occurred because the number of dumps is {0}.", iTotalDumpCount);
            }

            if ("none" != xavSystem[2].strAttributeValue)
            {
                return "There was a sick spinlock after an access violation.";
            }


            return "Unknown";
        }

        private string ResourceErrorReason(string strXML)
        {

            Utility.XMLAttributeValue[] xavResource = new Utility.XMLAttributeValue[1];

            xavResource[0].strAttributeName = "processOutOfMemoryPeriod";
            xavResource[0].strAttributeQuery = "//resource";

            int iTotal = Utility.GetXMLAttributeValues(strXML, ref xavResource);

            if (iTotal < xavResource.Length)
            {
                throw new Exception("ResourceErrorReason: Couldn't get all results from XML parsing.");
            }

            int iOOMTime = 0;

            int.TryParse(xavResource[0].strAttributeValue, out iOOMTime);

            if (iOOMTime > 120)
            {
                return String.Format("Error occurred because the amount of time SQL Server was in an Out Of Memory (OOM) state was longer than 2 Minutes. Current Total: {0}", iOOMTime.ToString());
            }

            return "Unknown";
        }
        private string QueryProcessingErrorReason(string strXML)
        {
            Utility.XMLAttributeValue[] xavQP = new Utility.XMLAttributeValue[2];

            xavQP[0].strAttributeName = "hasUnresolvableDeadlockOccurred";
            xavQP[0].strAttributeQuery = "//queryProcessing";

            xavQP[1].strAttributeName = "hasDeadlockedSchedulersOccurred";
            xavQP[1].strAttributeQuery = "//queryProcessing";

            int iTotal = Utility.GetXMLAttributeValues(strXML, ref xavQP);

            if (iTotal < xavQP.Length)
            {
                throw new Exception("QueryProcessingErrorReason: Couldn't get all results from XML parsing.");
            }

            int iUnresolveableDeadlock = -1;

            int.TryParse(xavQP[0].strAttributeValue, out iUnresolveableDeadlock);

            if (iUnresolveableDeadlock > 0)
            {
                return "Error occurred because of an unresolveable deadlock.";
            }

            int iDeadlockedSchedulers = -1;

            int.TryParse(xavQP[1].strAttributeValue, out iDeadlockedSchedulers);

            if (iDeadlockedSchedulers > 0)
            {
                return "Error occurred because of one or more deadlocked schedulers.";
            }

            return "Unknown";

        }

        private InterestingEvent CreateAndPopulateComponentInterestingEvent(PublishedEvent Event, string Field)
        {
            InterestingEvent ie = new InterestingEvent();

            ie.strEventName = Event.Name;
            ie.strFieldName = Field;
            ie.strFieldValue = Event.Fields[Field].Value.ToString();
            ie.strInstanceName = Event.Fields["instance_name"].Value.ToString();
            ie.strNodeName = Event.Fields["node_name"].Value.ToString();
            try
            {
                ie.DateTimeOfEvent = DateTime.Parse(Event.Timestamp.ToString());
                ie.DateTimeOfEvent = TimeZone.CurrentTimeZone.ToUniversalTime(ie.DateTimeOfEvent);
            }
            catch
            {
            }
            ie.strAdditionalInformation = Event.Fields["data"].Value.ToString();

            return ie;

        }

        private InterestingEvent CreateAndPopulateComponentCustomEvent(PublishedEvent Event, string Field, string Value, string AdditionalInfo)
        {
            InterestingEvent ie = new InterestingEvent();

            ie.strEventName = Event.Name;
            ie.strFieldName = Field;
            ie.strFieldValue = Value;
            ie.strInstanceName = Event.Fields["instance_name"].Value.ToString();
            ie.strNodeName = Event.Fields["node_name"].Value.ToString();
            try
            {
                ie.DateTimeOfEvent = DateTime.Parse(Event.Timestamp.ToString());
                ie.DateTimeOfEvent = TimeZone.CurrentTimeZone.ToUniversalTime(ie.DateTimeOfEvent);
            }
            catch
            {
            }
            ie.strAdditionalInformation = AdditionalInfo;
            ie.strRawData = Event.Fields["data"].Value.ToString();

            return ie;
        }

        private InterestingEvent CreateAndPopulateIsAliveInterestingEvent(PublishedEvent Event)
        {
            InterestingEvent ie = new InterestingEvent();

            ie.strInstanceName = Event.Fields["instance_name"].Value.ToString();
            ie.strFieldName = "reason";
            ie.strFieldValue = Event.Fields[ie.strFieldName].Value.ToString();
            ie.strNodeName = Event.Fields["server_name"].Value.ToString();
            ie.strEventName = Event.Name;
            try
            {
                ie.DateTimeOfEvent = DateTime.Parse(Event.Timestamp.ToString());
                ie.DateTimeOfEvent = TimeZone.CurrentTimeZone.ToUniversalTime(ie.DateTimeOfEvent);
            }
            catch
            {
            }

            ie.strAdditionalInformation = Event.Fields["availability_group_name"].Value.ToString();

            return ie;

        }

        void UpdateMaxWorkers(PublishedEvent Event)
        {
            Utility.XMLAttributeValue[] xavWorkersInfo = new Utility.XMLAttributeValue[2];

            xavWorkersInfo[0].strAttributeName = "maxWorkers";
            xavWorkersInfo[0].strAttributeQuery = "//queryProcessing";

            xavWorkersInfo[1].strAttributeName = "workersCreated";
            xavWorkersInfo[1].strAttributeQuery = "//queryProcessing";

            int iTotal = Utility.GetXMLAttributeValues(Event.Fields["data"].Value.ToString(), ref xavWorkersInfo);

            if (iTotal < xavWorkersInfo.Length)
            {
                throw new Exception("UpdateMaxWorkers: Couldn't get all results from XML parsing.");
            }

            int MaxWorkers = 0;
            int WorkersCreated = 0;

            if (int.TryParse(xavWorkersInfo[0].strAttributeValue, out MaxWorkers))
            {
                if (MaxWorkers > iMaxWorkers)
                {
                    iMaxWorkers = MaxWorkers;
                }
            }

            if (int.TryParse(xavWorkersInfo[1].strAttributeValue, out WorkersCreated))
            {
                if (WorkersCreated > iMaxCreatedWorkers)
                {
                    iMaxCreatedWorkers = WorkersCreated;
                }
            }
        }

        List<InterestingEvent> IProcessEvents.GetAllInterestingEvents()
        {
            return InterestingEventsList;
        }
    }
}
