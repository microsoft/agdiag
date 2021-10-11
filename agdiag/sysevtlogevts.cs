using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;

public struct interestingsysevtline
{
    public int lineid;
    public string line;
    public DateTime evtdatetime;
    public DateTime evtrangebegin;
    public DateTime evtrangeend;
    public string evtid;
    public string sev;
    public string evtsource;
    public string message;
    public string comment;
    public string evtadditionalinfo;
}

namespace agdiag
{
    /*
     * 
     * This sysevtlogevts class declares array of interestingsysevtline, then the system event log is opened and searched for matches 
     * and the interestingsysevtline[] array is populated by calling analyzesysevtlines(). 
     * 
     */
    class sysevtlogevts
    {
        public string[] sysevtlog { get; set; }
        public string sysevtlogmask { get; set; }
        public string target { get; set; }
        public bool issysevtlog { get; set; }
        public string[] sysevtidtargets { get; set; }
        public string[] sysevtsevtargets { get; set; }
        public string[] sysevtidmatches { get; set; }
        public string[] sysevtidsevmatches { get; set; }
        public interestingsysevtline[] analyzedsysevts { get; set; }

        public sysevtlogevts(string logpath, string srvname, bool issysevtlog)
        {
            this.sysevtlog = sysevtlog;
            this.sysevtlogmask = "*_system.csv";
            this.issysevtlog = false;
            this.sysevtlog = Utility.findlogs(logpath, srvname, this.sysevtlogmask);
            if (sysevtlog.Length==0) 
                this.sysevtlog = Utility.findlogs(logpath, srvname, "*system_Shutdown*");

            if (this.sysevtlog.Length != 0)
            {
                this.issysevtlog = true;
                this.sysevtidtargets = new string[] { "Date,Time", "2004", ",153,", ",33,", ",2013,", ",1592,", ",1135,", ",1230,", ",1146,", ",1553,", ",7034,", ",1185,", ",1683,", ",9,", ",11,", ",15,", ",50,", ",51,", ",54,", ",55,", ",57,", ",129,", ",1066,", ",6008,", ",1450,", ",1014,", ",140,", ",1793,", ",1795,", ",1038,", ",1792," };
                this.sysevtsevtargets = new string[] {",Warning,", ",Error", ",Critical," };

                //Find event id targets
                this.sysevtidmatches = Utility.findmatchesinlog(sysevtlog, sysevtidtargets, "", "", 0);

                //Find those events that are Warning, Error and Critical
                this.sysevtidsevmatches = Utility.findmatchesinarray(this.sysevtidmatches, this.sysevtsevtargets);

                this.analyzedsysevts = analyzesysevtlines(this.sysevtidsevmatches);
            }
        }

        //Clean up found SQL error log matches and create array of interesting system event log events.
        public interestingsysevtline[] analyzesysevtlines(string[] sysevtmatches)
        {
            string logsource = "";
            int index = 0;
            
            var cultureInfo = new CultureInfo("en-US");

            interestingsysevtline[] arr = new interestingsysevtline[sysevtmatches.Length];

            if(sysevtidmatches.Length>0)
                { 
                //Check to see if there is a header in the text file, if there is this is tss, if not, its pssdiag, handle the columns accordingly.
                if (sysevtidmatches[0].Contains("Date,Time"))
                    logsource = "SDP";
                else if(sysevtidmatches[0].Contains("TimeGenerated"))
                    logsource = "LogScout";
                else
                    logsource = "pssdiag";

                //Need to split the line correctly if more , are detected.
                foreach (string line in sysevtmatches)
                {
                    string[] evtarray = line.Split(',');

                    arr[index].lineid = index + 1;
                    arr[index].line = line;

                    //The date and time are one columns in tss system event log, two columns in pssdiag event log
                    if(logsource == "pssdiag")
                        arr[index].evtdatetime = DateTime.Parse(evtarray[0], cultureInfo);
                    else if (logsource == "SDP")
                        arr[index].evtdatetime = DateTime.Parse(evtarray[0] + " " + evtarray[1], cultureInfo);

                    arr[index].evtrangebegin = arr[index].evtdatetime.AddMinutes(-1);
                    arr[index].evtrangeend =arr[index].evtdatetime.AddMinutes(1);
                    arr[index].sev = evtarray[2];
                    arr[index].evtid = evtarray[4];
                    arr[index].evtsource = evtarray[5];
                    index++;
                }
            }
            return arr;
        }

    }
}

