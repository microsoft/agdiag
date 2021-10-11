using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.XEvent.Linq;

namespace agdiag
{
    class SystemHealthProcessor : IProcessEvents
    {
        List<InterestingEvent> InterestingEventsList;

        List<InterestingEvent> IProcessEvents.GetAllInterestingEvents()
        {
            return InterestingEventsList;
        }

        void IProcessEvents.ProcessEventRow(PublishedEvent Event)
        {
            //We know Event is going to be generic, so classify it
            // and give it the right logic processor

            switch (Event.Name)
            {
                case "scheduler_monitor_system_health_ring_buffer_recorded":
                    CheckSchedulerMonitor(Event);
                    return;
                case "wait_info":
                    CheckWaitInfo(Event);
                    return;
                case "memory_node_oom_ring_buffer_recorded":
                    return;
                case "wait_info_external":
                    CheckWaitInfoExternal(Event);
                    return;
                case "scheduler_monitor_non_yielding_ring_buffer_recorded":
                    CheckNonYield(Event);
                    return;
                default: // do nothing, we don't care about the event
                    return;
            }
        }

        //constructor
        public SystemHealthProcessor()
        {
            InterestingEventsList = new List<InterestingEvent>();
        }

        void CheckSchedulerMonitor(PublishedEvent Event)
        {
            int ProcessUtilization = 0;
            int SystemIdle = 0;

            ProcessUtilization = Int32.Parse(Event.Fields["process_utilization"].Value.ToString());
            SystemIdle = Int32.Parse(Event.Fields["system_idle"].Value.ToString());

            //Is process utilization at or above 85%? Might indicate CPU pressure
            if (ProcessUtilization >= 85)
            {
                AddToInterestingEventList(CreateAndPopulateComponentCustomEvent(Event, "process_utilization", ProcessUtilization.ToString(), "none"));
                return;
            }

            //Is CPU Idle very low, the system is pretty busy
            if (SystemIdle <= 20)
            {
                AddToInterestingEventList(CreateAndPopulateComponentCustomEvent(Event, "system_idle", SystemIdle.ToString(), "none"));
                return;
            }

            //If cpu idle is very low and sql is low, call out other process(es)
            if ((SystemIdle + ProcessUtilization) <= 40)
            {
                AddToInterestingEventList(CreateAndPopulateComponentCustomEvent(Event, "OtherProcess(es)", (SystemIdle + ProcessUtilization).ToString(), "none"));
                return;
            }
        }

        void CheckNonYield(PublishedEvent Event)
        {
            int scheduler = 0;
            int process_utilization = 0;

            try
            {
                process_utilization = Int32.Parse(Event.Fields["process_utilization"].Value.ToString());
            }
            catch
            {
                scheduler = -1;
            }
            AddToInterestingEventList(CreateAndPopulateComponentCustomEvent(Event, "process_utilization", process_utilization.ToString(), "none"));
            //500ms = 0.5 seconds
            /*if (SignalDuration > 500)
            {
                string WaitType = Event.Fields["wait_type"].ToString();

            }
            */
        }

        void CheckWaitInfo(PublishedEvent Event)
        {
            int SignalDuration = 0;
            string WaitType = "";

            try
            {
                SignalDuration = Int32.Parse(Event.Fields["signal_duration"].Value.ToString());
                WaitType = Event.Fields["wait_type"].Value.ToString();
            }
            catch
            {
                SignalDuration = -1;
            }
            AddToInterestingEventList(CreateAndPopulateComponentCustomEvent(Event, "signal duration", SignalDuration.ToString(), WaitType));
            //500ms = 0.5 seconds
            /*if (SignalDuration > 500)
            {
                string WaitType = Event.Fields["wait_type"].ToString();

            }
            */
        }

        void CheckWaitInfoExternal(PublishedEvent Event)
        {
            int Duration = 0;
            string WaitType = "";
            try
            {
                Duration = Int32.Parse(Event.Fields["duration"].Value.ToString());
                WaitType = Event.Fields["wait_type"].Value.ToString();
            }
            catch
            {
                Duration = -1;
            }
            AddToInterestingEventList(CreateAndPopulateComponentCustomEvent(Event, "duration", Duration.ToString(), WaitType));
            //500ms = 0.5 seconds
            /*if (SignalDuration > 500)
            {
                string WaitType = Event.Fields["wait_type"].ToString();

            }
            */
        }

        private InterestingEvent CreateAndPopulateComponentCustomEvent(PublishedEvent Event, string Field, string Value, string AdditionalInfo)
        {
            InterestingEvent ie = new InterestingEvent();

            ie.strEventName = Event.Name;
            ie.strFieldName = Field;
            ie.strFieldValue = Value;
            //ie.strInstanceName = Event.Fields["instance_name"].Value.ToString();
            //ie.strNodeName = Event.Fields["node_name"].Value.ToString();
            try
            {
                ie.DateTimeOfEvent = DateTime.Parse(Event.Timestamp.ToString());
                ie.DateTimeOfEvent = TimeZone.CurrentTimeZone.ToUniversalTime(ie.DateTimeOfEvent);
            }
            catch
            {
            }
            ie.strAdditionalInformation = AdditionalInfo;
            //ie.strRawData = Event.Fields["data"].Value.ToString();

            return ie;
        }

        void AddToInterestingEventList(InterestingEvent ieEvent)
        {
            InterestingEventsList.Add(ieEvent);
        }

    }
}
