using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
//using XEReader;

namespace agdiag
{
    /*
     * Report class is composed of methods used to build and generate the report.
     * 
     * agdiagsplash() generates the initial agdiag splash output along with instructions on agdiag use
     * 
     * reportlogsfound() generates a summary report of all logs found or not found and a raw output of interesting events found in these logs.
     * 
     * eventsummaryreport() generates the summary report with a list of found HADR health events. 
     * 
     * detailedreport() is iterated through for each found event, reporting the event from the Cluster log using the threadpid, 
     * and then the events found in related logs and finally ROOT CAUSE ANALYSIS and RECOMMENDATIONS section. detailedreport() calls 
     * each of the following methods to output the details of the HADR health event reported on.
     * 
     *      reportevt() is called by the detailedreport() and produces the Cluster log output for the event using the threadpid.
     *      
     *      Each of the following methods is called to report on correlating log details.
     *      
     *          reportsysevtlogevts(), report on correlating system event log events.
     *          reportagroletransitions(), report on correlating SQL Server error log role transition events.
     *          reportsecondaryclusterevts(), report on correlating secondary cluster log events.
     *          reportsqlerrlogevts(), report on correlating SQL Server error log events.
     *          reportXEvts(), report on correlating system_health of cluster diagnostic log events.
     *      
     *      reportrootcauseanalysis() is called to report the ROOT CAUSE ANALYSIS and RECOMMENDATIONS section.
     */

    class Report
    {
        public void agdiagsplash(double utcadjust, string srvname)
        {
            //AGDiag Report Header
            Console.WriteLine("<br><br>");
            Console.WriteLine("<p style=font-family:calibri font-size:20px style='margin:1em auto' style='margin-right:auto' style='margin-left:auto'>");
            Console.WriteLine("<pre>");
            Console.WriteLine("<b>               * ");
            Console.WriteLine("              *** ");
            Console.WriteLine("             *****          **********");
            Console.WriteLine("            **    **        **");
            Console.WriteLine("           **********       **     ***");
            Console.WriteLine("          **        **      **       **");
            Console.WriteLine("         **          **     ***********");
            Console.WriteLine("        **            **    ");

            Console.WriteLine("<br>");
            Console.WriteLine("                 ***************");
            Console.WriteLine("                 **             **       *********         **        **********");
            Console.WriteLine("                 **              **         ***           *  *       **");
            Console.WriteLine("                 **               **        ***          ******      **     ***");
            Console.WriteLine("                 **              **         ***         **    **     **       **");
            Console.WriteLine("                 **             **       **********    **      **    ***********");
            Console.WriteLine("                 ***************</b>");

            Console.WriteLine("<br><br><br>");
            Console.WriteLine("         AGDiag diagnoses and reports failover and health events detected in the Cluster log of the primary replica.");
            Console.WriteLine("<br>         AGDIAG can be executed against logs collected by SDP, TSS, PSSDiag and SQL Log Scout (collect option Basic).");

            Console.WriteLine("<br>         AGDIAG will detect and report on these issues:");
            Console.WriteLine("<br>            * Detect and analyze Cluster or SQL health issues that cause availability group to fail over or go offline.");
            Console.WriteLine("            * Detect and analyze Cluster or SQL health issues that cause SQL Failover Cluster Instance to fail over or go offline.");
            Console.WriteLine("            * Detect and analyze why availability group failed to failover to failover partner during manual or automatic failover attempt.");
            Console.WriteLine("<br>         AGDiag works with just a Cluster log file but can report enhanced analysis when pssdiag or SQL TSS logs are provided from the primary");
            Console.WriteLine("         replica where the health issue occurred.");
            Console.WriteLine("<br>         <b>HOW TO USE</b>");
            Console.WriteLine("<br>         Diagnose Availability Group Failover: Launch AGDiag and specify the location folder of the logs that hosted");
            Console.WriteLine("         the primary replica when the health issue occurred.");

            Console.WriteLine("<br>         AGDIAG Version: {0}", Assembly.GetEntryAssembly().GetName().Version.ToString());

            Console.WriteLine("</p>");
            Console.WriteLine("</pre>");
        }

        public void eventsummaryreport(clusterlogevts clusterlogevtlist)
        {
            bool foundevtstoreport = false;
            int index = 0;
            //Report summary of found and reported events-

            Console.WriteLine("<table border=1 width=100%><caption><font size=5><br>AVAILABILITY GROUP HEALTH EVENT SUMMARY REPORT</font></caption>");
            Console.WriteLine("<thead> <tr style='background-color:Gainsboro'> <th colspan=1>");

            //Check for no Events to Report
            foreach (interestingclusterline line in clusterlogevtlist.analyzedclusterlines)
            {
                if (line.evtid > 0)
                {
                    foundevtstoreport = true;
                    break;
                }
            }

            if(foundevtstoreport)
            { 
                Console.WriteLine("<font size=4>Click an Event ID below to go to the detailed report for that event.</font>");

                Console.WriteLine("</th > </tr > </thead>");
                Console.WriteLine("<tbody> <tr> <td colspan=1>");

                Console.WriteLine("<table style='margin:1em auto' style='margin-right:auto' style='margin-left:auto' border=1 width=50%>");
                Console.WriteLine("<thead><tr style='background-color:Gainsboro'><th>Event</th> <th>Occurred At Local Server Time</th> <th>Event Type</th></tr></thead><tbody>");

                do
                {
                    //This is an actual event because it was assigned an event type.
                    if (clusterlogevtlist.analyzedclusterlines[index].evttype != "")
                    {
                        Console.WriteLine("<tr>");
                        Console.WriteLine("<td><a href='#" + clusterlogevtlist.analyzedclusterlines[index].evtid + "'>" + clusterlogevtlist.analyzedclusterlines[index].evtid + "</a></td><td>" + clusterlogevtlist.analyzedclusterlines[index].localserverevtdatetime + "</td><td>" + clusterlogevtlist.analyzedclusterlines[index].evttype + "</td>");
                        Console.WriteLine("</tr>");
                    }
                    index++;
                } while (index < clusterlogevtlist.analyzedclusterlines.Length - 1);
                Console.WriteLine("</tbody></table>");

                Console.WriteLine("</td> </tr> </tfoot> </table>");
            }
            else
                Console.WriteLine("<font size=4>No Events Found To Report in the Cluster Log.</font></td> </tr> </tfoot> </table>");
        }

        public void reportlogsfound(double utcadjust, string srvname, string logpath, clusterlogevts clusterlogevtlist, sqlerrlogevts sqlerrloglist, sysevtlogevts sysevtloglist, systeminfo systeminfo)
        {
            Console.WriteLine("<table border=1 width=100%><caption><b><font size=5>Initialize AGDIAG</font></b></caption>");
            Console.WriteLine("<tr style='background-color:Gainsboro'> <th colspan=2>");

            if (srvname == "")
                Console.WriteLine("<font size=5>Unable to Determine Server Name</font>");
            else
                Console.WriteLine("<font size=5>Server name is " + srvname + "</font>");

            if(utcadjust<0)
                Console.WriteLine("<br>Local server time is " + -utcadjust + " hours earlier than UTC time.</th></tr><tbody>");
            else if (utcadjust>0)
                Console.WriteLine("<br>Local server time is " + utcadjust + " hours later than UTC time.</th></tr><tbody>");

            /***********************************REPORT UNZIPPED LOGS*******************************/
            new Utility().unzipanyzipped(logpath);

            /***********************************REPORT CLUSTER LOG*******************************/
            Console.WriteLine("<tr><td>Searching " + logpath + " for Cluster log for analysis...</td>");
            foreach (string log in clusterlogevtlist.clusterlog)
            {
                Console.WriteLine("<td>Cluster log found! Cluster log used for analysis is " + log + "</td></tr>");
            }

            Console.WriteLine("<tr><td colspan=2><details><summary>Found Cluster Log Matches, drill into the triangle to see details.</summary>");
            foreach (interestingclusterline icl in clusterlogevtlist.analyzedclusterlines)
                Console.WriteLine("<br>"+icl.evtmatch);
            Console.WriteLine("</details></td></tr>");

            /***********************************REPORT SQL SERVER ERROR LOGS*******************************/
            Console.WriteLine("<tr><td>Searching " + logpath + " for SQL Server error logs for analysis...</td>");
            if (sqlerrloglist.issqlerrlogs)
            {

                Console.WriteLine("<td>SQL Server Error log(s) found!");
                Console.WriteLine("<details><summary>Found SQL Server Error log(s)<br></summary>");
                foreach (string log in sqlerrloglist.sqlerrlogs) Console.WriteLine(log + "<br>");
                Console.WriteLine("</details></td></tr>");

                if (sqlerrloglist.sqlerrlogs.Length != 0)
                {
                    Console.WriteLine("<tr><td colspan=2>SQL Server Error log(s) events found!");
                    Console.WriteLine("<details><summary>Found SQL Server Error log event matches, drill into the triangle to see details.<br></summary>");
                    foreach (interestingsqlline sqlline in sqlerrloglist.analyzedsqllines)
                        Console.WriteLine("<br>"+sqlline.line);
                }
                Console.WriteLine("</details></td></tr>");

                Console.WriteLine("<tr><td colspan=2>SQL Server Error log(s) availability group role transitions found!");
                Console.WriteLine("<details><summary>Found SQL Server Error log role transition matches, drill into the triangle to see details.<br></summary>");
                foreach (interestingsqlline sqlline in sqlerrloglist.analyzedsqlagrolelines)
                    Console.WriteLine("<br>"+sqlline.line);
                Console.WriteLine("</details></td></tr>");
            }
            else
                Console.WriteLine("<td>Could not locate SQL Error logs. Log path tested is " + logpath + "</td></tr>");

            /***********************************REPORT SQL SERVER CLUSTER DIAGNOSTIC LOGS*******************************/
            Console.WriteLine("<tr><td>Searching " + logpath + " for SQL Server Cluster Diagnostic logs (...SQLDIAG...xel) for analysis...</td>");
            //Process SQL Diagnostic Logs
            string[] clusterdiaglogfiles = Utility.findlogs(logpath, srvname, "*_SQLDIAG_*.xel");
            if (clusterdiaglogfiles.Length > 0)
            {
                agdiag.XEReader spsrv = new agdiag.XEReader(logpath, DataSourceType.AlwaysOnSpServerDiagnostics);
                spsrv.ExecuteCheck();

                Console.WriteLine("<td>SQL Server Cluster Diagnostic logs (...SQLDIAG...xel) found!");
                Console.WriteLine("<details><summary>Found SQL Server Cluster Diagnostic logs<br></summary>");
                foreach (string log in clusterdiaglogfiles) Console.WriteLine(log);
                Console.WriteLine("</details></td></tr>");

                if (spsrv.InterestingEventCount != 0)
                {
                    Console.WriteLine("<tr><td colspan=2>SQL Server Cluster Diagnostic logs events found!");
                    Console.WriteLine("<details><summary>Found SQL Server Cluster Diagnostic logs event matches, drill into the triangle to see details.<br></summary>");
                    Console.WriteLine("<table style='margin:1em auto' style='margin-right:auto' style='margin-left:auto' border=1 width=100%>");
                    Console.WriteLine("<thead><tr style='background-color:Gainsboro'><th>Event</th> <th>Occurred At</th></tr></thead><tbody>");
                    foreach (InterestingEvent ie in spsrv.AllInterestingEvents)
                    {
                        Console.WriteLine("<tr><td>" + ie.strEventName + "</td><td>" + ie.DateTimeOfEvent + "</td></tr>");
                    }
                    Console.WriteLine("</table>");
                }
                else
                    Console.WriteLine("<tr><td>No Interesting Cluster Diagnostic Log Events to Report</details></td></tr>");
            }
            else
                Console.WriteLine("<td>Could not locate SQL Server Cluster Diagnostic logs (...SQLDIAG...xel). Log path tested is " + logpath + "</td></tr>");


            /***********************************REPORT SYSTEM HEALTH LOGS*******************************/
            Console.WriteLine("<tr><td>Searching " + logpath + " for SQL Server System Health logs (...system_health...xel) for analysis...</td>");
            //Process SQL Diagnostic Logs
            string[] systemhealthlogfiles = Utility.findlogs(logpath, "", "*system_health*.xel");
            if (systemhealthlogfiles.Length > 0)
            {
                Console.WriteLine("<td>SQL Server System Health logs (...system_health...xel) found!");
                Console.WriteLine("<details><summary>Found SQL Server System Health logs<br></summary>");
                foreach (string log in systemhealthlogfiles) Console.WriteLine(log);
                Console.WriteLine("</details></td></tr>");

                agdiag.XEReader sh = new agdiag.XEReader(logpath, DataSourceType.SystemHealth);
                sh.ExecuteCheck();

                if (sh.InterestingEventCount != 0)
                {
                    Console.WriteLine("<tr><td colspan=2>SQL Server System Health log events found!");
                    Console.WriteLine("<details><summary>Found SQL Server System Health log event matches, drill into the triangle to see details.<br></summary>");
                    Console.WriteLine("<table style='margin:1em auto' style='margin-right:auto' style='margin-left:auto' border=1 width=100%>");
                    Console.WriteLine("<thead><tr style='background-color:Gainsboro'><th>Event</th> <th>Occurred At</th></tr></thead><tbody>");
                    foreach (InterestingEvent ie in sh.AllInterestingEvents)
                    {
                        Console.WriteLine("<tr><td>" + ie.strEventName + "</td><td>" + ie.DateTimeOfEvent + "</td></tr>");
                    }
                    Console.WriteLine("</table>");
                }
                else
                    Console.WriteLine("<tr><td>No Interesting System Health Log Events to Report</details></td></tr>");

            }
            else
                Console.WriteLine("<td>Could not locate SQL Server System Health logs (...system_health...xel). Log path tested is " + logpath + "</td></tr>");


            /***********************************REPORT SYSTEM EVENT LOG*******************************/
            Console.WriteLine("<tr><td>Searching " + logpath + " for System Event Log for analysis...</td>");
            if (sysevtloglist.issysevtlog)
            {
                foreach (string sysevtlog in sysevtloglist.sysevtlog)
                    Console.WriteLine("<td>System Event log found! System Event log used for analysis is " + sysevtlog + "</td></tr>");

                Console.WriteLine("<tr><td colspan=2><details><summary>Found System Event log event matches, drill into the triangle to see details.<br></summary>");

                foreach (interestingsysevtline evt in sysevtloglist.analyzedsysevts)
                    Console.WriteLine("<br>"+evt.line);
                Console.WriteLine("</details></td></tr>");
            }
            else
                Console.WriteLine("<td>Could not locate System Event Log. Log path tested is " + logpath + "</td></tr>");
        }

        //Detailed report of each event, including correlating events from sql, system, system_health, cluster diag, and commentary on these correlating events.
        public void detailedreport(double utcadjust, string srvname, clusterlogevts clusterlogevtlist, sqlerrlogevts sqlerrloglist, sysevtlogevts sysevtloglist, agdiag.XEReader spsrvdiag, agdiag.XEReader syshealth, agdiag.systeminfo systeminfo)
        {
            int index = 0;
            Console.WriteLine("<table border=1 width=100%><caption><font size=5><br>DETAILED ANALYSIS OF DETECTED HEALTH EVENTS</font></caption>");
            Console.WriteLine("<thead> <tr style='background-color:Gainsboro' style='margin-left: 50%'> <th colspan=2>");

            Console.WriteLine("Cluster Log is UTC " + utcadjust);

            Console.WriteLine("<br><b>IMPORTANT</b> Events reported below are from Cluster log and likely reported in UTC.");
            Console.WriteLine("<br>Events reported in other logs are often local server time.");

            if (clusterlogevtlist.analyzedclusterlines.Length > 0)
            {
                //Close the table here and if there is no events later in the ELSE clause
                Console.WriteLine("</th> </tr > </thead></table>");
                do
                {
                    //This is an actual event because it was assigned an event type.
                    if (clusterlogevtlist.analyzedclusterlines[index].evttype != "")
                    {
                        reportevt(index, clusterlogevtlist.analyzedclusterlines[index].evtid, clusterlogevtlist);

                        //Handle each type of detailed report according to event type.
                        switch (clusterlogevtlist.analyzedclusterlines[index].evttype)
                        {
                            case "Lost quorum":
                                //Correlate certain logs to the event, looking for bread crumbs
                                reportsysevtlogevts(index, clusterlogevtlist, sysevtloglist);
                                reportagroletransitions(clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend, sqlerrloglist);
                                reportsecondaryclusterevts(index, clusterlogevtlist, systeminfo);
                                reportrootcauseanalysis(index, "quorumevt", clusterlogevtlist, sqlerrloglist, spsrvdiag, systeminfo);
                                break;
                            case "Unexpected SQL Server Service Shutdown":
                                //Correlate certain logs to the event, looking for bread crumbs
                                reportsqlerrlogevts(index, clusterlogevtlist, sqlerrloglist);
                                reportagroletransitions(clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend, sqlerrloglist);
                                reportXEvts("spsrvdiag", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, spsrvdiag);
                                reportXEvts("syshealth", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, syshealth);
                                reportsysevtlogevts(index, clusterlogevtlist, sysevtloglist);
                                reportrootcauseanalysis(index, "sqlserviceevt", clusterlogevtlist, sqlerrloglist, spsrvdiag, systeminfo);
                                break;
                            case "Lease Timeout":
                                //Correlate certain logs to the event, looking for bread crumbs
                                reportsqlerrlogevts(index, clusterlogevtlist, sqlerrloglist);
                                reportagroletransitions(clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend, sqlerrloglist);
                                reportXEvts("spsrvdiag", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, spsrvdiag);
                                reportXEvts("syshealth", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, syshealth);
                                reportsysevtlogevts(index, clusterlogevtlist, sysevtloglist);
                                reportrootcauseanalysis(index, "leasetimeoutevt", clusterlogevtlist, sqlerrloglist, spsrvdiag, systeminfo);
                                Console.WriteLine("</td> </tr> </tfoot> </table>");
                                break;
                            case "SQL Server Health Check Timeout":
                                //Correlate certain logs to the event, looking for bread crumbs
                                reportsqlerrlogevts(index, clusterlogevtlist, sqlerrloglist);
                                reportagroletransitions(clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend, sqlerrloglist);
                                reportXEvts("spsrvdiag", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, spsrvdiag);
                                reportXEvts("syshealth", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, syshealth);
                                reportsysevtlogevts(index, clusterlogevtlist, sysevtloglist);
                                reportrootcauseanalysis(index, "sqlhealthchecktimeoutevt", clusterlogevtlist, sqlerrloglist, spsrvdiag, systeminfo);
                                break;
                            case "SQL Server Internal Health Event":
                                //Correlate certain logs to the event, looking for bread crumbs
                                reportsqlerrlogevts(index, clusterlogevtlist, sqlerrloglist);
                                reportagroletransitions(clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend, sqlerrloglist);
                                reportXEvts("spsrvdiag", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, spsrvdiag);
                                reportXEvts("syshealth", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, syshealth);
                                reportsysevtlogevts(index, clusterlogevtlist, sysevtloglist);
                                reportrootcauseanalysis(index, "sqlinternalhealthevt", clusterlogevtlist, sqlerrloglist, spsrvdiag, systeminfo);
                                break;
                            case "SQL Server Failover Cluster Instance":
                                //Correlate certain logs to the event, looking for bread crumbs
                                reportsqlerrlogevts(index, clusterlogevtlist, sqlerrloglist);
                                reportagroletransitions(clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend, sqlerrloglist);
                                reportXEvts("spsrvdiag", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, spsrvdiag);
                                reportXEvts("syshealth", clusterlogevtlist.analyzedclusterlines[index].clusterevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].clusterevtrangeend, syshealth);
                                reportsysevtlogevts(index, clusterlogevtlist, sysevtloglist);
                                reportrootcauseanalysis(index, "sqlfci", clusterlogevtlist, sqlerrloglist, spsrvdiag, systeminfo);
                                break;
                            case "Availability Group Failed to Failover":
                                //Special case, need to call this for primary and secondary Cluster logs
                                reportsqlerrlogevts(index, clusterlogevtlist, sqlerrloglist);
                                reportagroletransitions(clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin, clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend, sqlerrloglist);
                                reportsysevtlogevts(index, clusterlogevtlist, sysevtloglist);
                                reportrootcauseanalysis(index, "agfailedfailover", clusterlogevtlist, sqlerrloglist, spsrvdiag, systeminfo);
                                break;
                            default:
                                Console.WriteLine("Default case");
                                break;
                        }
                    }
                    index++;
                } while (index < clusterlogevtlist.analyzedclusterlines.Length - 1);
            }
            else
                Console.WriteLine("<br><font size=4>No Events Found To Report in the Cluster Log.</font></td> </tr> </tfoot> </table>");
        }

        public void reportevt(int index, int evtid, clusterlogevts clusterlogevtlist)
        {
            string evtpidthread = clusterlogevtlist.analyzedclusterlines[index].evtpidthread;
            DateTime evtdatetime = clusterlogevtlist.analyzedclusterlines[index].clusterevtdatetime;

            Console.WriteLine("<table border=1 width=100%><caption><font size=5>AlwaysOn Health Event </font></caption>");
            Console.WriteLine("<thead> <tr style='background-color:Gainsboro'> <th colspan=2>");
            Console.WriteLine("<font size=4>Event ID <a name='" + clusterlogevtlist.analyzedclusterlines[index].evtid + "'>" + clusterlogevtlist.analyzedclusterlines[index].evtid + "</a> " + clusterlogevtlist.analyzedclusterlines[index].evttype + "</font>");
            Console.WriteLine("</th> </tr > </thead >");
            Console.WriteLine("<tbody> <tr> <td colspan=2>");
            Console.WriteLine("<em>The following event is reported as found in the Cluster log of the primary replica, whose server name is <b>" + clusterlogevtlist.primaryservername + "</b>:</em><br>");
            Console.WriteLine("<em>The times reported here are in Cluster Log and may be different from local server time,");
            if (clusterlogevtlist.clusterlogutcadjust < 0)
                Console.WriteLine(" <b>Local server time is " + -clusterlogevtlist.clusterlogutcadjust + " hours earlier than UTC time.</b></em><br>");
            else if (clusterlogevtlist.clusterlogutcadjust > 0)
                Console.WriteLine(" <b>Local server time is " + clusterlogevtlist.clusterlogutcadjust + " hours later than UTC time.</b></em><br>");


            foreach (interestingclusterline line in clusterlogevtlist.analyzedclusterlines)
            {
                if (line.evtpidthread == evtpidthread && (evtdatetime > line.clusterevtrangebegin && evtdatetime < line.clusterevtrangeend))
                {
                    Console.WriteLine("<br>");
                    Console.WriteLine(line.evtmatch);
                }
            }
        }

        public void reportsqlerrlogevts(int index, clusterlogevts clusterlogevtlist, sqlerrlogevts sqlerrloglist)
        {
            bool foundevents = false;
            Console.WriteLine("</td></tr><tr><td>");
            if (sqlerrloglist.issqlerrlogs)
            {
                Console.WriteLine("Searching for events from <b>SQL Server error log</b> found near this health event..<details><summary>Found event matches, drill into the triangle to see details.</summary>");
                foreach (interestingsqlline sqlerrline in sqlerrloglist.analyzedsqllines)
                {
                    if ((sqlerrline.msgdatetime > clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin) && (sqlerrline.msgdatetime < clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend))
                    {
                        Console.WriteLine(sqlerrline.line + "<br>");
                        foundevents = true;
                        if (sqlerrline.message.Contains("BEGIN STACK DUMP"))
                        {
                            clusterlogevtlist.analyzedclusterlines[index].sqldumpline = sqlerrline.lineid;
                            clusterlogevtlist.analyzedclusterlines[index].sqldumpdetected = true;
                        }
                    }
                }
                Console.WriteLine("</details>");
            }
            else
                Console.WriteLine("No SQL Server Error Logs Found.");
            if (!foundevents)
                Console.WriteLine("No events from the SQL Server error logs were found near this health event.");
        }
        public void reportXEvts(string rpttype, DateTime rangebegin, DateTime rangeend, XEReader xe)
        {
            bool foundevents = false;
            Console.WriteLine("</td></tr><tr><td>Searching for events from <b>" + xe.SourceFileType + "</b> logs found near this health event.");
            if (xe.InterestingEventCount != 0)
            {
                Console.WriteLine("<details><summary>Found event matches, drill into the triangle to see details.<br></summary>");
                Console.WriteLine("<table style='margin:1em auto' style='margin-right:auto' style='margin-left:auto' border=1 width=100%>");
                Console.WriteLine("<thead><tr style='background-color:Gainsboro'><th>Event</th> <th>Occurred At</th> <th>Event Field</th><th>Event Value</th><th>Additional Info</th><th>Raw Data</th></tr></thead>");
                foreach (InterestingEvent ie in xe.AllInterestingEvents)
                {
                    if (ie.DateTimeOfEvent > rangebegin && ie.DateTimeOfEvent < rangeend)
                    {
                        Console.WriteLine("<tr><td>" + ie.strEventName + "</td><td>" + ie.DateTimeOfEvent + "</td><td>" + ie.strFieldName + "</td><td>" + ie.strFieldValue + "</td><td>" + ie.strAdditionalInformation + "</td><td>" + ie.strRawData + "</td></tr>");
                        foundevents = true;
                    }
                }
                Console.WriteLine("</table></details>");
            }

            if (!foundevents || xe.InterestingEventCount == 0)
                Console.WriteLine("<br>No events from " + xe.SourceFileType + " logs found near this health event.");
        }

        public void reportsecondaryclusterevts(int index, clusterlogevts clusterlogevtlist, systeminfo systeminfo)
        {
            bool foundevents = false;
            Console.WriteLine("</td></tr><tr><td>");
            Console.WriteLine("<b>Searching for events in <b>secondary Cluster logs</b> found near this health event.</b><details><summary>Found event matches, drill into the triangle to see details.</summary>");
            foreach (interestingclusterline secondaryclusterline in clusterlogevtlist.secondarynodesanalyzedclusterlines)
            {
                if ((clusterlogevtlist.analyzedclusterlines[index].clusterevtdatetime > secondaryclusterline.clusterevtrangebegin) && (clusterlogevtlist.analyzedclusterlines[index].clusterevtdatetime < secondaryclusterline.clusterevtrangeend))
                {
                    Console.WriteLine("<br>" + secondaryclusterline.evtmatch);
                    foundevents = true;
                }
            }
            Console.WriteLine("</details>");
            if (!foundevents)
                    Console.WriteLine("No events from the secondary cluster log(s) were found near this health event.");
        }

        public void reportsysevtlogevts(int index, clusterlogevts clusterlogevtlist, sysevtlogevts sysevtloglist)
        {
            bool foundevents = false;
            Console.WriteLine("</td></tr><tr><td>");
            if (sysevtloglist.issysevtlog)
            {
                Console.WriteLine("<b>Searching for events <b>System Event log</b> found near this health event.</b><details><summary>Found event matches, drill into the triangle to see details.</summary>");
                foreach (interestingsysevtline sysevtline in sysevtloglist.analyzedsysevts)
                {
                    if ((sysevtline.evtdatetime > clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangebegin) && (sysevtline.evtdatetime < clusterlogevtlist.analyzedclusterlines[index].localsrvevtrangeend))
                    {
                        Console.WriteLine("<br>" + sysevtline.line);
                        foundevents = true;
                    }
                }
                Console.WriteLine("</details>");
                if (!foundevents)
                    Console.WriteLine("No events from the system event log were found near this health event.");
            }
        }
        public void reportagroletransitions(DateTime rangebegin, DateTime rangeend, sqlerrlogevts sqlerrloglist)
        {
            bool foundevents = false;
            Console.WriteLine("</td></tr><tr><td>");
            if (sqlerrloglist.issqlerrlogs)
            {
                Console.WriteLine("Searching for <b>availability group role transition in SQL Server error log</b> found near this health event.");

                Console.WriteLine("<details><summary>Found event matches, drill into the triangle to see details.</summary>");
                foreach (interestingsqlline sqlroleline in sqlerrloglist.analyzedsqlagrolelines)
                {
                    if ((sqlroleline.msgdatetime > rangebegin) && (sqlroleline.msgdatetime < rangeend))
                    {
                        Console.WriteLine("<br>" + sqlroleline.line);
                        foundevents = true;
                    }
                }
                Console.WriteLine("</details>");
                if (!foundevents)
                    Console.WriteLine("No role transition events from the SQL Server error logs were found near this health event.");

            }
        }
        public void reportrootcauseanalysis(int index, string rcatype, clusterlogevts clusterlogevtlist, sqlerrlogevts sqlerrloglist, XEReader spsrv, systeminfo systeminfo)
        {
            string matchingipaddress = "";
            bool ipaddressmatch = false;
            bool RCAmessage = false;
            string pidthread = clusterlogevtlist.analyzedclusterlines[index].evtpidthread;

            Console.WriteLine("</td> </tr> </tbody> <tfoot> <tr> <td colspan=2>" + clusterlogevtlist.analyzedclusterlines[index].evttype + " <b>ROOT CAUSE ANALYSIS and RECOMMENDATIONS</b><br>");

            switch (rcatype)
            {
                case "sqlfci":
                    Console.WriteLine("<br>SQL FCI RCA</br>");
                    break;
                case "quorumevt":
                    Console.WriteLine("<br><b>CAUSE FOR Lost Quorum</b> <br> Most commonly, the other Cluster nodes were unable to communicate with this Cluster node.");

                    if(!systeminfo.ipaddresses.Equals(null))
                        foreach (string ipaddress in systeminfo.ipaddresses)
                        {
                            foreach (interestingclusterline secondaryclusterline in clusterlogevtlist.secondarynodesanalyzedclusterlines)
                            {
                                if ((clusterlogevtlist.analyzedclusterlines[index].clusterevtdatetime > secondaryclusterline.clusterevtrangebegin) && (clusterlogevtlist.analyzedclusterlines[index].clusterevtdatetime < secondaryclusterline.clusterevtrangeend) && secondaryclusterline.evtmatch.Contains(ipaddress))
                                {
                                    ipaddressmatch = true;
                                    matchingipaddress = ipaddress;

                                }
                            }
                        }
                    if (ipaddressmatch)
                    {
                        Console.WriteLine("<br><br><b>Quorum Loss DIAGNOSIS</b> ");
                        Console.WriteLine("<br>Secondary Cluster nodes have reported communication events with the active server whose IP address is " + matchingipaddress + ".<br><br>Review the results in the <b>Searching for events in secondary Cluster logs</b> findings above for details of communciations problems matching this server's IP address.");
                    }

                    Console.WriteLine("<br><br><b>NEXT STEPS<br><br></b>Review the <b>Searching for events System Event log found near this health event</b> section above which reports if the Cluster node may have been removed from the Cluster.");
                    Console.WriteLine("<br><br>Review the <b>Searching for events in secondary Cluster logs found near this health event</b> section above which report communication problems detected by other Cluster nodes that caused the node to be removed from the Cluster.");
                    Console.WriteLine("<br><br>If there are messages reporting missed heartbeats, this indicates that the Cluster node was removed from the Cluster because other nodes were unable to consistently communicate with it..");
                    Console.WriteLine("<br><br>IMPORTANT This is a Windows Cluster issue, collaborate with the Windows High Availability Support team to resolve this issue.");
                    break;
                case "sqlserviceevt":
                    Console.WriteLine("<br><b>CAUSE FOR Unexpected SQL Server Service Shutdown</b> <br> The SQL Server process terminated unexpectedly. This usually indicates some kind of fatal exception in the SQL Server process.");
                    Console.WriteLine("<br><br>If this is a SQL Server Failover Cluster Instance, it could mean that a health event triggered the failover of the SQL FCI role. To confirm, review the AGDiag summary report for SQL Server Failover Cluster Instance events reported around the same time as this event.");
                    Console.WriteLine("<br><br><b>NEXT STEPS</b> <br>Review the <b>SQL Server error logs</b> and <b>system event logs</b> sections above which report events that occurred around the time of this unexpected SQL Server service shutdown event.");
                    Console.WriteLine("<br><br>If there are SQL Server dumps that were generated on this event, upload the dumps into the <a href='https://sqldumpviewer/'> SQLdumpviewer </a> for analysis.");
                    break;
                case "leasetimeoutevt":
                    Console.WriteLine("<br><b>CAUSE FOR Lease Timeout</b> <br> Usually lease timeouts are a result of a <b>(1) SQL Server process produces a dump diagnostic (and becomes unresponsive during dump process)</b> or <b>(2) system wide performance event for which performance monitor counters may help with diagnosis.</b>");
                    Console.WriteLine("<br><br><b>Lease Timeout DIAGNOSIS</b> ");
                    Console.WriteLine("<br><br><b>(1) Checking for SQL Server Dump diagnostics around the time of this Lease Timeout event...</b>");

                    if (clusterlogevtlist.analyzedclusterlines[index].sqldumpdetected)
                    {
                        Console.WriteLine("<br>SQL Server dump diagnostic detected at the time of the Lease Timeout Event!! This likely caused the Lease Timeout event.");
                        Console.WriteLine("<br><br>Review the SQL Server error log for the following event:<br>");

                        for (int i = 0; i < 6; i++)
                        {
                            Console.WriteLine("<br>" + sqlerrloglist.analyzedsqllines[clusterlogevtlist.analyzedclusterlines[index].sqldumpline + i].line);
                        }
                        Console.WriteLine("<br><br><b>NEXT STEPS </b>Upload the dump into the <a href='https://sqldumpviewer/'> SQLdumpviewer </a> for analysis.");
                    }
                    else
                        Console.WriteLine("<br>No SQL Server dump diagnostic detected at the time of the Lease Timeout Event.");

                    //Analyze, report on perf data
                    Console.WriteLine("<br><br><b>(2) Analyzing performance data, for possible resource impact to the system</b>");
                    do
                    {
                        if ((clusterlogevtlist.analyzedclusterlines[index].perfwarn))
                        {
                            for(int i=0;i< clusterlogevtlist.analyzedclusterlines[index].perfwarnrpt.Length;i++)
                                Console.WriteLine(clusterlogevtlist.analyzedclusterlines[index].perfwarnrpt[i]);
                            RCAmessage = true;
                        }
                        index++;
                    } while (clusterlogevtlist.analyzedclusterlines[index].evtpidthread == pidthread);

                    if (!RCAmessage)
                        Console.WriteLine("<br>Performance Monitor data looks healthy.");
                    break;
                case "sqlhealthchecktimeoutevt":
                    Console.WriteLine("<br><b>CAUSE FOR HEALTH CHECK TIMEOUT</b> <br> SQL Server did not respond to the ODBC connection from the local HADR health monitoring process within the availability group HEALTH_CHECK_TIMEOUT time (default is 30 seconds).");
                    Console.WriteLine("<br><br><b>NEXT STEPS</b> <br>Review the <b>AlwaysOnSpServerDiagnostics, SQL Server error logs, SystemHealth and system event logs</b> sections above which report events that occurred around the time of this SQL Server HEALTH_CHECK_TIMEOUT event.");
                    Console.WriteLine("<br><br>For more information on HEALTH_CHECK_TIMEOUT see " + "<a href='https://docs.microsoft.com/en-us/sql/database-engine/availability-groups/windows/configure-flexible-automatic-failover-policy?view=sql-server-ver15#HCtimeout'> Configure a flexible automatic failover policy for an Always On availability group - Health-Check Timeout Threshold </a>");
                    break;
                case "sqlinternalhealthevt":
                    Console.WriteLine("<br><b>CAUSE FOR SQL Server INTERNAL HEALTH EVENT</b> <br> SQL Server reported severity ERROR for one of several components generated by the SP_SERVER_DIAGNOSTICS routine and reported it to the local HADR health monitoring process.");
                    Console.WriteLine("<br>SQL Server reported an internal health issue from the <b>" + clusterlogevtlist.analyzedclusterlines[index].rca.ToUpper() + " </b>component.<br><br><b>NEXT STEPS</b> Review the section above, 'Searching for events from <b>AlwaysOnSpServerDiagnostics</b> logs found near this health event.'<br> for events of type 'component_health_result' which should provide the internal health issue. <br><br>Also, review the <b>SQL Server error logs, SystemHealth and system event logs</b> sections above which report events that occurred around the time of this SQL Server internal health event.");
                    Console.WriteLine("b>IMPORTANT</b> If no events are found in above section, 'Searching for events from <b>AlwaysOnSpServerDiagnostics</b> it may be because these logs were not collected quickly enough after the event, usually these logs will hold 24 hours of trace data, so data collection must be expedited when internal health event occurs.");
                    Console.WriteLine("<br><br>For more information on the SQL Server internal health see " + "<a href='https://docs.microsoft.com/en-us/sql/database-engine/availability-groups/windows/configure-flexible-automatic-failover-policy?view=sql-server-ver15#FClevel'> Configure a flexible automatic failover policy for an Always On availability group - Failure-Condition Level </a>");
                    break;
                case "agfailedfailover":
                    Console.WriteLine("<br><b>Failed to Failover DIAGNOSIS</b><br> ");
                    for (int i = 0; i < clusterlogevtlist.analyzedclusterlines.Length - 1; i++)
                    {
                        if ((!clusterlogevtlist.analyzedclusterlines[i].rcacomment.Equals("")) && clusterlogevtlist.analyzedclusterlines[i].evtpidthread == pidthread && (clusterlogevtlist.analyzedclusterlines[index].clusterevtdatetime> clusterlogevtlist.analyzedclusterlines[i].clusterevtrangebegin && clusterlogevtlist.analyzedclusterlines[index].clusterevtdatetime< clusterlogevtlist.analyzedclusterlines[i].clusterevtrangeend))
                        {
                            Console.WriteLine(clusterlogevtlist.analyzedclusterlines[i].rcacomment);
                            RCAmessage = true;
                        }
                    }
                    if (!RCAmessage)
                        Console.WriteLine("<br>Searched the secondary cluster logs for corresponding events to the event reported above in the Cluster log from the primary replica. <br>No root cause analysis available for the failed failover.");
                    break;
                default:
                    Console.WriteLine("Default case");
                    break;
            }
            Console.WriteLine("</td> </tr> </tfoot> </table>");
        }

    }
}
