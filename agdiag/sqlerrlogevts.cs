using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using System.Net.WebSockets;

namespace agdiag
{

    /*
     * findmatchesinlog() is run to open all SQL error logs and return a couple arrays of SQL Server messages that may be useful in the agdiag.
     * report: sqlmatches and sqlagrolematches.
     * 
     * These arrays are passed into analyzesqllines() in order to fill the interstingsqllines arrays called analyzedsqllines and analyzedsqlagrolelines.
     * 
     * sqlerrlogevts Structures:
     * 
     * interstingsqllines: Contains the SPID, date time, message, a comment for reporting, msgrangebegin and msgrangeend for correlating the message
     * to the Cluster log HADR health event. 
     * 
     * sqldumpevt: Contains details on a dump line that can be reported out in the report
     * 
     * method: analyzesqllines takes the sqlmatches and sqlagrolematches arrays and processes them, populating the analyzedsqllines and analyzedsqlagrolelines
     * arrays of interestingsqllines.
     */
    public struct sqldumpevt
    {
        public int lineid;
        public string line;
        public string spid;
        public string message;
    }
    public struct interestingsqlline
    {
        public int lineid;
        public string line;
        public string spid;
        public string message;
        public string comment;
        public DateTime msgdatetime;
        public DateTime msgrangebegin;
        public DateTime msgrangeend;
        public sqldumpevt[] sqldumpevt;
        public string msgadditionalinfo;
    }
    class sqlerrlogevts
    {
        public string[] sqlerrlogs { get; set; }
        public string sqlerrlogmask { get; set; }
        public string target { get; set; }
        public bool issqlerrlogs { get; set; }
        public bool issqllogutcadjust { get; set; }
        public double sqlutcadjust { get; set; }
        public string[] sqltargets { get; set; }
        public string[] sqlagroletargets { get; set; }
        public string[] sqlmatches { get; set; }
        public string[] sqlagrolematches { get; set; }
        public interestingsqlline[] analyzedsqllines { get; set; }
        public interestingsqlline[] analyzedsqlagrolelines { get; set; }
        public sqldumpevt[] sqldumpevt { get; set; }

        public sqlerrlogevts(string logpath, string srvname, bool issqlerrlogs)
        {
            this.issqlerrlogs = issqlerrlogs;
            this.sqlerrlogmask = "*errorlog.*";
            this.sqlerrlogs = Utility.findlogs(logpath, srvname, this.sqlerrlogmask);
            if (this.sqlerrlogs.Length==0 && Directory.GetFiles(logpath, "*ERRORLOG*").Length > 0)
                    this.sqlerrlogs = Directory.GetFiles(logpath, "*ERRORLOG*");

            if (this.sqlerrlogs.Length!=0)
            {
                issqllogutcadjust = false;

                this.issqlerrlogs = true;
                this.sqltargets = new string[] { "appears to be non-yielding on Scheduler", "The query processor ran out of internal resources", "write not complete", "Long Sync IO", "Non -yielding Scheduler", "     *", "StackDump", "BEGIN STACK DUMP", "Server was configured to produce dump", "EXCEPTION_ACCESS_VIOLATION", "Latch timeout", "Stalled Resource Monitor", "Stalled IOCP Listener", "Error: 19421", "did not receive a process event signal from the Windows Server Failover Cluster", "I/O requests taking longer", "time-out occurred while waiting for", "insufficient", "FlushCache", "Buffer Pool scan took", "Non-yielding IOCP Listener", "Error: 35217", "The thread pool for Always On Availability Groups was unable to start a new worker thread"};
                this.sqlagroletargets = new string[] {"'NOT_AVAILABLE' to 'RESOLVING_NORMAL'", "'RESOLVING_NORMAL' to 'NOT_AVAILABLE'", "'RESOLVING_NORMAL' to 'SECONDARY_NORMAL'", "'SECONDARY_NORMAL' to 'RESOLVING_NORMAL'", "'SECONDARY_NORMAL' to 'RESOLVING_PENDING_FAILOVER'", "'SECONDARY_NORMAL' to 'RESOLVING_PENDING_FAILOVER'", "'RESOLVING_NORMAL' to 'PRIMARY_PENDING'", "'PRIMARY_PENDING' to 'PRIMARY_NORMAL'", "'PRIMARY_NORMAL' to 'RESOLVING_NORMAL'",  };

                this.sqlmatches = Utility.findmatchesinlog(sqlerrlogs, sqltargets, "", "BEGIN STACK DUMP", 6);
                this.sqlagrolematches = Utility.findmatchesinlog(sqlerrlogs, sqlagroletargets, "", "", 0);

                //analyzedsqllines has array of SQL events with spid, datetime, string
                this.analyzedsqllines = analyzesqllines(sqlmatches);
                this.analyzedsqlagrolelines = analyzesqllines(sqlagrolematches);
            }
            else
            {
                Console.WriteLine("\r\nCould not locate SQL Error logs. Log path tested is " + logpath);
            }
        }


        //Clean up found SQL error log matches and create array of interesting SQL events.
        public interestingsqlline[] analyzesqllines(string[] sqlmatches)
        {
            DateTime sqldt;
            int index = 0;
            interestingsqlline[] arr = new interestingsqlline[sqlmatches.Length];

            foreach (string line in sqlmatches)
            {
                if(!line.Equals(""))
                {
                    //Check for valid datetime.  
                    bool validdt = DateTime.TryParse(line.Substring(0, 23), out sqldt);

                    arr[index].lineid = index;
                    arr[index].line = line;
                    //if(DateTime.TryParse(line.Substring(0, 23), out sqldt));
                    if(validdt)
                    {
                        arr[index].msgdatetime = sqldt;
                        arr[index].msgrangebegin = sqldt.AddMinutes(-1);
                        arr[index].msgrangeend = sqldt.AddMinutes(1);
                    }
                    //arr[index].msgdatetime = DateTime.Parse(line.Substring(0, 23));
                    //arr[index].msgrangebegin = arr[index].msgdatetime.AddMinutes(-1);
                    //arr[index].msgrangeend = arr[index].msgdatetime.AddMinutes(1);

                    string[] spid=line.Split(' ');
                    //Make sure the array is at least three long because I am accessing the third element
                    if (spid.Length > 2)
                    {
                        arr[index].spid = spid[2];
                        arr[index].message = line.Substring(line.IndexOf(arr[index].spid));
                        arr[index].message = arr[index].message.Substring(arr[index].message.IndexOf(" ")).Trim();
                    }
                    if (line.Contains("BEGIN STACK DUMP"))
                    {
                        arr[index].sqldumpevt = new sqldumpevt[6];
                        for (int i = 0; i < 6; i++)
                        {
                            arr[index].sqldumpevt[i].lineid = arr[index+i].lineid;
                            arr[index].sqldumpevt[i].line = arr[index + i].line;
                            arr[index].sqldumpevt[i].message = arr[index + i].message;
                            arr[index].sqldumpevt[i].spid = arr[index + i].spid;
                        }
                    }
                    index++;
                }
            }
            return arr;
        }
    }
}




