using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

/*********************************************************************************************************************************
 * 
 *The clusterlogevts class creates a class that queries the cluster log, locates specific cluster lines called clusterinterestinglines
 *and then analyzes them creating an array of information called analyzedclusterlines.
 *
 **********************************************************************************************************************************/

namespace agdiag
{
    /*
     * findmatchesinlog() is run against cluster log from primary and returns array of matching HADR health events that may be useful in the agdiag.
     * findmatchesinlog() is also run against remaining cluster node logs and returns all correlating results that may have occurred for each
     * matching HADR health event located in the primary cluster log.
     * 
     * These arrays are passed into analyzesqllines() in order to fill the interstingsqllines arrays called analyzedsqllines and analyzedsqlagrolelines.
     * 
     * clusterlogevts Structures:
     * 
     * interstingclusterlines: Contains 
     *      clusterevtdatetime datetime of event
     *      comment, rca and rcacomment for reporting
     *      clusterevtdatetime, clusterevtrangeend, localsrvevtrangebegin, localsrvevtrangeend, for correlating the event to other logs events
     *      evtpid, evtthread and evtpidthread for reporting the entire event in the Cluster log, the function is Utility.reportevt()
     * 
     * perfdata: Contains array of lease timeout performance data reported in Cluster log
     * 
     * methods: 
     * 
     * Utility.findmatchesinlog() is called to find HADR health events in primary Cluster log
     * Utility.returnlinesafterstrmatch() is called exclude analyzing everything prior to the [=== Cluster Logs ===] string.
     * processinterestinglines() is called to analyze the clustermatches array and populates the analyzedclusterlines[] array of interestingclusterlines
     * analyzeleaseperflines() is called to populate the analyzedclusterlines perfdata[] object
     * secondaryclusterlogs[] array is defined by calling returnexcludedmatch which returns array of only secondary cluster logs
     * analyzedclusterlines[] is updated by calling analyzefailedfailover() which correlates events in secondary cluster log to event in primary log when 
     * a failed failover event is detected.
     * We call renumberevts() to change the analyzedclusterlines evtid to renumber at 1 to n because we removed some events that didn't qualify in analyzefailedfailover()
     */

    public struct perfdata
    {
        public int evtid;
        public string evttime;
        public string cpu;
        public string memorybytes;
        public string avgreadssec;
        public string avgwritessec;
    }
    public struct interestingclusterline
    {
        public int lineid;
        public int evtid;
        public string evtmatch;
        public string srvname;
        public string primaryorsecondary;
        public DateTime clusterevtdatetime;
        public DateTime clusterevtrangebegin;
        public DateTime clusterevtrangeend;
        public DateTime localserverevtdatetime;
        public DateTime localsrvevtrangebegin;
        public DateTime localsrvevtrangeend;
        public string evttype;
        public string evtpid;
        public string evtthread;
        public string evtpidthread;
        public string comment;
        public string evtadditionalinfo;
        public string rca;
        public string rcacomment;
        public perfdata[] perfdata;
        public bool perfwarn;
        public bool sqldumpdetected;
        public int sqldumpline;
        public string[] perfwarnrpt;
    }

    public struct clusterevttype
    {
        public int evttypeid;
        public string evttype;
        public string evttypematch;
        public string evttypecomment;
    }

    class clusterlogevts
    {
        public string[] clusterlog { get; set; }
        public string[] secondarynodesclusterlogs { get; set; }
        public string clusterlogmask { get; set; }
        public string target { get; set; }
        public string primaryservername { get; set; }
        public bool isclusterlog { get; set; }
        public bool isclusterlogutcadjust { get; set; }
        public double clusterlogutcadjust {get; set;}
        public clusterevttype[] clusterevttypes { get; set; }
        public string[] primaryclusterlogtargets { get; set; }
        public string[] secondaryclusterlogtargets { get; set; }
        public string[] primaryclusterlogmatches { get; set; }
        public string[] secondaryclusterlogmatches { get; set; }
        public interestingclusterline[] analyzedclusterlines { get; set; }
        public interestingclusterline[] secondarynodesanalyzedclusterlines { get; set; }
        public perfdata[] perfdata { get; set; }
        public string[] perfwarnrpt { get; set; }

        //Constructor
        public clusterlogevts(double utcadjust, bool isutcadjust, string logpath, string primarysrvname)
        {
            this.isclusterlogutcadjust = isutcadjust;
            this.primaryservername = primarysrvname.ToLower();
            this.clusterlogutcadjust = utcadjust;
            this.isclusterlog = false;
            this.clusterlogmask = "*cluster.log";
            string testclog= logpath + "\\" + this.primaryservername + "_cluster.log";
            //Check for SDP cluster logs
            if (File.Exists(logpath + "\\" + this.primaryservername + "_cluster.log"))
                this.clusterlog = new string[] { (logpath + "\\" + this.primaryservername + "_cluster.log").ToLower() };
            //Check for pssdiag cluster logs
            else if (File.Exists(logpath + "\\ClusterLogs\\" + this.primaryservername + "_cluster.log"))
                this.clusterlog = Utility.findlogs(logpath, this.primaryservername, this.clusterlogmask);
            else if(Utility.isfullyqualifiedservername(this.primaryservername))
                this.clusterlog = Utility.findlogs(logpath, this.primaryservername.Substring(0, this.primaryservername.IndexOf(".")), this.clusterlogmask);
            else
                this.clusterlog = Utility.findlogs(logpath, this.primaryservername, this.clusterlogmask);

            //Check for just 'cluster.log'
            if (this.clusterlog.Length==0)
               if (File.Exists(logpath + "\\" + "cluster.log"))
                   this.clusterlog = new string[] { (logpath + "\\" + "cluster.log").ToLower() };


            //need to check for other variation of cluster log name
            if (this.clusterlog.Length==0)
            {
                this.clusterlog = Utility.findlogs(logpath, primarysrvname, this.clusterlogmask);
            }

            if (this.clusterlog.Length == 1)
            {

                this.isclusterlog = true;

                this.primaryclusterlogtargets = new string[] { "has come offline", "Lost quorum", "Quorum witness has better epoch than local node", "failed to update epoch after one of the nodes went down", "Shutting down", "Cluster service has terminated", "lost quorum", "ODBC Error", "Not failing over group", "Online for resource", "[=== Cluster Logs ===]", " ERR[QUORUM] ", " WARN[RHS] Cluster service has terminated", "INFO  [RES] SQL Server Availability Group", "WARN  [RES] SQL Server Availability Group", "ERR   [RES] SQL Server Availability Group", "[sqsrvres]" };
                this.secondaryclusterlogtargets = new string[] { "[=== Cluster Logs ===]", "ReadObject failed with GracefulClose", "NetftRemoteUnreachable", "NetftRemoteUnreachable", "has missed", "unreachable from", "is broken", "Not failing over group", "ODBC Error", "Online for resource", "Failed to connect to remote endpoint" };
                
                //Open Cluster log and create array with all lines that we want to process
                this.primaryclusterlogmatches = Utility.findmatchesinlog(this.clusterlog, primaryclusterlogtargets, "", "", 0);

                //Discard matches prior to [=== Cluster Logs ===] string
                this.primaryclusterlogmatches = Utility.returnlinesafterstrmatch(this.primaryclusterlogmatches, "[=== Cluster Logs ===]");

                //Process each interesting line, mining the datetime, pid/thread, event type, etc
                this.analyzedclusterlines= processinterestinglines(this.clusterlogutcadjust, this.primaryclusterlogmatches, "primary");

                this.perfdata = analyzeleaseperflines(this.analyzedclusterlines);

                //Open secondary Cluster nodes and search for additional important events.
                this.secondarynodesclusterlogs = Utility.returnexcludedmatch(Utility.findlogs(logpath, "", this.clusterlogmask), this.clusterlog[0]);

                this.secondaryclusterlogmatches = Utility.findmatchesinlog(this.secondarynodesclusterlogs, secondaryclusterlogtargets, "[=== Cluster Logs ===]", "", 0);

                this.secondarynodesanalyzedclusterlines = processinterestinglines(this.clusterlogutcadjust, this.secondaryclusterlogmatches, "secondary");
                
                //Handle failed failover, do all the analysis of secondary cluster logs for correlating events when failover fails.
                this.analyzedclusterlines = analyzefailedfailover(this);

                //When analyzing failed failover events, we may remove some events, need to renumber the events for reporting.
                this.analyzedclusterlines = renumberevts(this);
            }
        }

        public interestingclusterline[] renumberevts(clusterlogevts clusterlogevtslist)
        {
            int evtidx = 1;
            int index = 0;

            foreach (interestingclusterline evtline in clusterlogevtslist.analyzedclusterlines)
            {
                if (!evtline.evtid.Equals(0) && !evtline.evttype.Equals(""))
                {
                    clusterlogevtslist.analyzedclusterlines[index].evtid = evtidx;
                    evtidx++;
                }
                index++;
            }
            return clusterlogevtslist.analyzedclusterlines;
        }

        public interestingclusterline[] analyzefailedfailover(clusterlogevts clusterlogevtslist)
        {

            for (int i = 0; i < clusterlogevtslist.analyzedclusterlines.Length - 1; i++)
            {
                if (clusterlogevtslist.analyzedclusterlines[i].evttype == "Availability Group Failed to Failover")
                {
                    //Special case, checking on primary
                    if (clusterlogevtslist.analyzedclusterlines[i].evtmatch.Contains("Not failing over group") && clusterlogevtslist.analyzedclusterlines[i].evtmatch.Contains("failoverCount"))
                    {
                        clusterlogevtslist.analyzedclusterlines[i].rca = "Number of Failovers Exceeds the Availability Group Role Maximum Failures In Specified Period setting";
                        clusterlogevtslist.analyzedclusterlines[i].rcacomment = "<br><b>Number of Failovers Exceeds the Availability Group Role Maximum Failures In Specified Period setting</b><br>The availability group failed to failover because the number of recent failovers exceeds the availability group role's 'Maximum Failures In Specified Period' setting. <br>The following message was found in the Cluster node of the primary replica, reporting a failed attempt to failover to a failover partner. <br><br>" + clusterlogevtslist.analyzedclusterlines[i].evtmatch + " <br><br><b>NEXT STEPS</b><br><br>Review the Case 'Maximum Failures in the Specified Period' value is exhausted' in article <a href='https://review.docs.microsoft.com/en-us/troubleshoot/sql/availability-groups/troubleshooting-automatic-failover-problems?branch=release-supportarticles-docs-pr'> Troubleshooting automatic failover problems in SQL Server 2012 AlwaysOn environments</a>, for more information on diagnosing the issue.<br>";
                    }
                    if (clusterlogevtslist.analyzedclusterlines[i].evtmatch.Contains("Resource") && clusterlogevtslist.analyzedclusterlines[i].evtmatch.Contains("has come offline"))
                    {
                        foreach (interestingclusterline secondaryline in clusterlogevtslist.secondarynodesanalyzedclusterlines)
                        {
                            if (secondaryline.clusterevtdatetime > clusterlogevtslist.analyzedclusterlines[i].clusterevtrangebegin && secondaryline.clusterevtdatetime < clusterlogevtslist.analyzedclusterlines[i].clusterevtrangeend)
                            {
                                if (secondaryline.evtmatch.Contains("Login failed for user") || secondaryline.evtmatch.Contains("The user does not have permission to perform") || secondaryline.evtmatch.Contains("Cannot alter the availability group"))
                                {
                                    clusterlogevtslist.analyzedclusterlines[i].rca = "Health Check Login User Permissions Issue During Failover Detected";
                                    clusterlogevtslist.analyzedclusterlines[i].rcacomment = "<br><b>Health Check Login User Permissions Issue During Failover Detected</b><br>On failover attempt SQL Server failed to connect or execute due to a Login or User permission error. SQL Server uses NT AUTHORITY\\SYSTEM to login and this account requires certain permissions. <br>The following message was found in the Cluster log of one of the secondary replicas, reporting a failed attempt to transition to the primary role at the same time that the availability group 'has come offline' above on this primary replica.<br><br>" + secondaryline.evtmatch + " <br><br><b>NEXT STEPS</b><br><br>Review the Case 'Insufficient NT Authority, SYSTEM account permissions' in article <a href='https://review.docs.microsoft.com/en-us/troubleshoot/sql/availability-groups/troubleshooting-automatic-failover-problems?branch=release-supportarticles-docs-pr'> Troubleshooting automatic failover problems in SQL Server 2012 AlwaysOn environments</a>, for more information on diagnosing the issue.<br>";
                                }
                                else if (secondaryline.evtmatch.Contains("One or more databases are not synchronized"))
                                {
                                    clusterlogevtslist.analyzedclusterlines[i].rca = "One or more availability group databases not synchronized.";
                                    clusterlogevtslist.analyzedclusterlines[i].rcacomment = "<br><b>One or more availability group databases not synchronized.</b><br>On failover attempt SQL Server failed to failover because one or more databases were not in a SYNCHRONIZED state. <br>The following message was found in the Cluster log of one of the secondary replicas, reporting a failed attempt to transition to the primary role at the same time that the availability group 'has come offline' above on this primary replica.<br><br>" + secondaryline.evtmatch + " <br><br><b>NEXT STEPS</b><br><br>Review the Case 'The availability databases are not in a SYNCHRONIZED state' in article <a href='https://review.docs.microsoft.com/en-us/troubleshoot/sql/availability-groups/troubleshooting-automatic-failover-problems?branch=release-supportarticles-docs-pr'> Troubleshooting automatic failover problems in SQL Server 2012 AlwaysOn environments</a>, for more information on diagnosing this issue.<br>";
                                }
                                else if (secondaryline.evtmatch.Contains("SSL Provider"))
                                {
                                    clusterlogevtslist.analyzedclusterlines[i].rca = "Health Check Login Failed Because Force Protocol Encryption is Enabled";
                                    clusterlogevtslist.analyzedclusterlines[i].rcacomment = "<br><b>Health Check Login Failed Because Force Protocol Encryption is Enabled</b><br>On failover attempt the local connection to SQL Server failed, possibly because the local Client Configuration is configured for 'Force Protocol Encryption.' <br>The following message was found in the Cluster log of one of the secondary replicas, reporting a failed attempt to transition to the primary role at the same time that the availability group 'has come offline' above on this primary replica.<br><br>" + secondaryline.evtmatch + " <br><br><b>NEXT STEPS</b><br><br>Review the Case ''Force Protocol Encryption' configuration has been selected for the client protocols' in article <a href='https://review.docs.microsoft.com/en-us/troubleshoot/sql/availability-groups/troubleshooting-automatic-failover-problems?branch=release-supportarticles-docs-pr'> Troubleshooting automatic failover problems in SQL Server 2012 AlwaysOn environments</a>, for more information on diagnosing the issue.<br>";
                                }
                                else if (secondaryline.evtmatch.Contains("Data source name not found and no default driver specified"))
                                {
                                    clusterlogevtslist.analyzedclusterlines[i].rca = "Health Check Login Failed Because ODBC Driver is Missing";
                                    clusterlogevtslist.analyzedclusterlines[i].rcacomment = "<br><b>Health Check Login Failed Because ODBC Driver is Missing</b><br>On failover attempt the local HADR health connection to the pending primary failed because the ODBC driver could not be loaded. <br>The following message was found in the Cluster log of one of the secondary replicas, reporting a failed attempt to transition to the primary role at the same time that the availability group 'has come offline' above on this primary replica.<br><br>" + secondaryline.evtmatch + " <br><br><b>NEXT STEPS</b><br><br>Launch ODBC Data Source Administrator, click the Drivers tab and confirm that the ODBC Driver for SQL Server is installed.<br><br>";
                                }
                                else if (secondaryline.evtmatch.Contains("Unable to complete login process due to delay in prelogin response"))
                                {
                                    clusterlogevtslist.analyzedclusterlines[i].rca = "Health Check Login Timeout During Failover Detected";
                                    clusterlogevtslist.analyzedclusterlines[i].rcacomment = "<br><b>Health Check Login Timeout During Failover Detected</b><br>On failover attempt the local HADR health connection failed after a login timeout. <br>The following message was found in the Cluster log of one of the secondary replicas, reporting a failed attempt to transition to the primary role at the same time that the availability group 'has come offline' above on this primary replica.<br><br>" + secondaryline.evtmatch + " <br><br><b>NEXT STEPS</b><br><br>This could be due to a sudden load on the SQL Server. Check for conditions like a low worker thread condition, low memory, high CPU, etc.<br>";
                                }
                                else if (secondaryline.evtmatch.Contains("SQL Server Network Interfaces: No client protocols are enabled and no protocol was specified") || secondaryline.evtmatch.Contains("Server doesn't support requested protocol"))
                                {
                                    clusterlogevtslist.analyzedclusterlines[i].rca = "Client protocol is not enabled on SQL Server";
                                    clusterlogevtslist.analyzedclusterlines[i].rcacomment = "<br><b>Client protocol is not enabled on SQL Server</b><br>On failover attempt the availability group failed to failover because the required client protocol is not enabled on the SQL Server. <br>The following message was found in the Cluster log of one of the secondary replicas, reporting a failed attempt to transition to the primary role at the same time that the availability group 'has come offline' above on this primary replica.<br><br>" + secondaryline.evtmatch + " <br><br><b>NEXT STEPS</b><br><br>Launch SQL Server Configuration Manager and confirm Shared Memory or TCPIP are enabled under the Client Protocols for the SQL Native Client Configuration.<br>";
                                }
                            }
                        }
                        if (clusterlogevtslist.analyzedclusterlines[i].rca.Equals(""))
                        {
                            clusterlogevtslist.analyzedclusterlines[i].evttype = "";
                            clusterlogevtslist.analyzedclusterlines[i].evtid = 0;
                        }
                    }
                }
            }
            return clusterlogevtslist.analyzedclusterlines;
        }
 
        public perfdata[] analyzeleaseperflines(interestingclusterline[] clusterlines)
        {
            string pidthread = "";
            int index = 0;
            int perfwarnrptidx = 0;
            string[] splitperf;
            string perfdatastr = "";
            perfdata[] perfrecords = new perfdata[5];
            foreach (interestingclusterline line in clusterlines)
            {
                if (line.evttype== "Lease Timeout")
                {
                    pidthread = line.evtpidthread;
                    index = line.lineid;
                    do
                    {
                        //Evaluate perf data
                        if (clusterlines[index].evttype.Equals("") && clusterlines[index].evtmatch.Contains(".000000") && !(clusterlines[index].evtmatch.Contains("Processor time")))
                        {
                            perfdatastr = clusterlines[index].evtmatch.Substring(clusterlines[index].evtmatch.IndexOf("[hadrag] ") + 9, (clusterlines[index].evtmatch.Length) - (clusterlines[index].evtmatch.IndexOf("[hadrag] ") + 9));
                            splitperf = perfdatastr.Split(new char[] { ',' });
                            clusterlines[index].evtid = line.evtid;
                            clusterlines[index].evtadditionalinfo = "leaseperf";
                            clusterlines[index].perfdata = new perfdata[1];
                            clusterlines[index].perfdata[0].evttime = splitperf[0];
                            clusterlines[index].perfdata[0].cpu = splitperf[1].Trim(' ');
                            clusterlines[index].perfdata[0].memorybytes = splitperf[2].Trim(' ');
                            clusterlines[index].perfdata[0].avgreadssec = splitperf[3].Trim(' ');
                            clusterlines[index].perfdata[0].avgwritessec = splitperf[4].Trim(' ');
                            if (Convert.ToSingle(clusterlines[index].perfdata[0].cpu) > 90)
                            {
                                clusterlines[index].perfwarn = true;
                                clusterlines[index].perfwarnrpt[perfwarnrptidx] = "<br>CPU appears to be very high and could have contributed to lease timeout event. <br> CPU reported running at " + clusterlines[index].perfdata[0].cpu + " percent at " + clusterlines[index].perfdata[0].evttime + "<br><b>NEXT STEPS </b>Capture performance monitor Processor::% Processor Time to detect high CPU utilization on the system.<br>";
                                perfwarnrptidx++;
                            }
                            if (Convert.ToSingle(clusterlines[index].perfdata[0].memorybytes) < 200000000)
                            {
                                clusterlines[index].perfwarn = true;
                                clusterlines[index].perfwarnrpt[perfwarnrptidx] = "<br>Memory appears to be low and could have contributed to lease timeout event. <br> Available Memory reported at " + clusterlines[index].perfdata[0].memorybytes + " bytes at " + clusterlines[index].perfdata[0].evttime + "<br><b>NEXT STEPS </b>Capture performance monitor Memory::Available MBytes to detect low available memory on the system.<br>";
                                perfwarnrptidx++;
                            }
                            if (Convert.ToSingle(clusterlines[index].perfdata[0].avgreadssec) > .015)
                            {
                                clusterlines[index].perfwarn = true;
                                clusterlines[index].perfwarnrpt[perfwarnrptidx] = "<br>Average disk sec/read appears to be high and could have contributed to lease timeout event. <br> Average disk sec/read reported at " + clusterlines[index].perfdata[0].avgreadssec + " at " + clusterlines[index].perfdata[0].evttime + "<br><b>NEXT STEPS </b>Capture performance monitor Logical Disk::Avg Disk Sec/Read to monitor the performance of the disk that hosts the database log and data files.<br>";
                                perfwarnrptidx++;
                            }
                            if (Convert.ToSingle(clusterlines[index].perfdata[0].avgwritessec) > .015)
                            {
                                clusterlines[index].perfwarn = true;
                                clusterlines[index].perfwarnrpt[perfwarnrptidx] = "<br>Average disk sec/write appears to be high and could have contributed to lease timeout event. <br> Average disk sec/write reported at " + clusterlines[index].perfdata[0].avgwritessec + " at " + clusterlines[index].perfdata[0].evttime + "<br><b>NEXT STEPS </b>Capture performance monitor Logical Disk::Avg Disk Sec/Writes to monitor the performance of the disk that hosts the database log and data files.<br>";
                            }
                            perfwarnrptidx = 0;

                        }
                        index++;
                    } while (clusterlines[index].evtpidthread == pidthread && !(clusterlines[index].evtmatch.Contains("Stopping Health Worker Thread")));
                    index++;
                } 
            }
            return perfdata;
        }
        public interestingclusterline[] processinterestinglines(double utcadjust, string[] clusterlogmatches, string primaryorsecondary)
            {
            DateTime clusterdt;
            int index = 0;
            int evtid = 1;
            interestingclusterline[] arr = new interestingclusterline[clusterlogmatches.Length];
            foreach (string match in clusterlogmatches)
            {
                arr[index].lineid = index;
                arr[index].primaryorsecondary = primaryorsecondary;
                arr[index].evtmatch = clusterlogmatches[index];

                if (DateTime.TryParse(clusterlogmatches[index].Substring(clusterlogmatches[index].IndexOf("::") + 2, 23).Replace("-", " "), out clusterdt))
                { 
                    arr[index].clusterevtdatetime = DateTime.Parse(clusterlogmatches[index].Substring(clusterlogmatches[index].IndexOf("::") + 2, 23).Replace("-", " "));
                    arr[index].clusterevtrangebegin = arr[index].clusterevtdatetime.AddMinutes(-1);
                    arr[index].clusterevtrangeend = arr[index].clusterevtdatetime.AddMinutes(1);
                    arr[index].localserverevtdatetime = arr[index].clusterevtdatetime.AddHours(utcadjust);
                    arr[index].localsrvevtrangebegin = arr[index].localserverevtdatetime.AddMinutes(-1);
                    arr[index].localsrvevtrangeend = arr[index].localserverevtdatetime.AddMinutes(1); ;
                }
                arr[index].evttype = evaluateclusterevttype(index, clusterlogmatches[index], primaryorsecondary);
                arr[index].evtpidthread=clusterlogmatches[index].Substring(0, 17); 
                arr[index].evtpid = clusterlogmatches[index].Substring(clusterlogmatches[index].IndexOf(".") - 8, 8);
                arr[index].evtthread = clusterlogmatches[index].Substring(clusterlogmatches[index].IndexOf("::") - 8, 8);
                arr[index].rca = "";
                arr[index].rcacomment = "";
                arr[index].evtadditionalinfo ="";
                arr[index].perfwarn=false;
                //arr[index].perfwarnrpt[0]="";
                arr[index].perfwarnrpt = new string[4];
                if (arr[index].evttype == "SQL Server Internal Health Event")
                    arr[index].rca = Utility.getBetween(match, "Failure detected, the state of ", " component is error");

                if (!arr[index].evttype.Equals(""))
                {
                    arr[index].evtid = evtid;
                    evtid++;
                }
                index++;
            }
            return arr;
        }

        //In this function, we set the evt type for the first and key line reporting health event
        public string evaluateclusterevttype(int index, string clusterlogmatch, string primaryorsecondary)
        {
            //"Failing over group", "Starting clussvc as a service", "The logs were generated using", "HandleMonitorReply: FAILURENOTIFICATION", "[hadrag] Resource Alive result 0", "[RHS] Terminating resource", "ServiceStopReason::", "Lost quorum", "Cluster service has terminated.", "Closing all resources", "SQL server service is not alive", "QueryServiceStatusEx returned", "SQL service is either not inialized or no longer valid", "The lease is expired", "Lease renewal failed with error", "Lease renewal failed because the existing lease is no longer valid",  "Failed to retrieve", "did not receive healthinformation before", "Failure detected, diagnostics heartbeat is lost", "health state has been changed from 'warning' to 'error'", "SQL Server Availability Group: [hadrag] Failure detected, the state of", "Availability Group is not healthy with given HealthCheckTimeout and FailureConditionLevel", "Failed to retrieve data column", "Failed to retrieve creation_time column", "Failed to retrieve component_type column", "Failed to retrieve state column", "Invalid health state value", "Failed to retrieve health state_desc column", "bytes of memory to store diagnostics data column. Your system might run out of memory", "Rest of diagnostics data column is ignored since its length" };
            string evttype = "";
            if (clusterlogmatch.Contains("Lost quorum") || clusterlogmatch.Contains("Quorum witness has better epoch than local node"))
                evttype = "Lost quorum";
            if (clusterlogmatch.Contains("SQL server service is not alive"))
                evttype = "Unexpected SQL Server Service Shutdown";
            if (clusterlogmatch.Contains("[hadrag] Failure detected, diagnostics heartbeat is lost") || clusterlogmatch.Contains("did not receive healthinformation before HealthCheckTimeout"))
                evttype = "SQL Server Health Check Timeout";
            if (clusterlogmatch.Contains("[hadrag] Lease timeout detected"))
                evttype = "Lease Timeout";
            if (clusterlogmatch.Contains("Failure detected, the state of"))
                evttype = "SQL Server Internal Health Event";
            if (clusterlogmatch.Contains("[sqsrvres] SQLMoreResults") || clusterlogmatch.Contains("[sqsrvres] Lease timeout detected") || clusterlogmatch.Contains("[sqsrvres] Failure detected, the state of"))
                evttype = "SQL Server Failover Cluster Instance";
            if (primaryorsecondary == "secondary" && (clusterlogmatch.Contains("Failed to run diagnostics command") || clusterlogmatch.Contains("Failed to connect to SQL Server") || clusterlogmatch.Contains("Failed to run diagnostics command")))
                evttype = "Availability Group Failed to Failover";
            if (clusterlogmatch.Contains("Not failing over group") && clusterlogmatch.Contains("failoverCount"))
                evttype = "Availability Group Failed to Failover";
            if ((clusterlogmatch.Contains("has missed") && clusterlog.Contains("3343")) || clusterlogmatch.Contains("unreachable from") || clusterlogmatch.Contains("is broken"))
                evttype = "Lost Quorum";
            if ((clusterlogmatch.Contains("Resource") && clusterlogmatch.Contains("has come offline")))
                evttype = "Availability Group Failed to Failover";
            return evttype;
        }
    }
}
