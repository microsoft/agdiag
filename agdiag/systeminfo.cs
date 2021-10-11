using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

public struct interestingsysinfoline
{
    public int lineid;
    public string line;
    public string utcadjust;
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
    class systeminfo
    {
        public string sysinfologmask { get; set; }
        public string[] sysinfolog { get; set; }
        public bool issysinfolog { get; set; }
        public bool issysinfoutcadjust { get; set; }
        public string sysinfoutcadjust { get; set; }
        public string[] processors { get; set; }
        public string systemmodel { get; set; }
        public string systemmanufacturer { get; set; }
        public string osversion { get; set; }
        public string osname { get; set; }
        public string biosversion { get; set; }
        public string totalmemory { get; set; }
        public string virtualmemoryinuse { get; set; }
        public string virtualmemorymaxsize { get; set; }
        public string virtualmemoryavailable { get; set; }
        public string domain { get; set; }
        public bool isclusterlog { get; set; }
        public bool isutcadjust { get; set; }
        public string[] ipaddresses { get; set; }
        public string[] sysinfotargets { get; set; }
        public string[] sysinfomatches { get; set; }
        public string[] sysevtidsevmatches { get; set; }
        public interestingsysinfoline[] analyzedsysinfo { get; set; }

        public systeminfo(string logpath, string srvname)
        {
            int ipaddindex = 0;
            this.issysinfolog = false;
            this.sysinfologmask = "*_msinfo32.txt";
            //need to check for other variation of cluster log name
            this.sysinfolog = Utility.findlogs(logpath, srvname, this.sysinfologmask);

            if (this.sysinfolog.Length == 1)
            {
                this.issysinfolog = true;
                this.sysinfotargets = new string[] { "IP Address\t" };
                //Find sysinfo targets
                this.sysinfomatches = Utility.findmatchesinlog(this.sysinfolog, this.sysinfotargets, "", "", 0);
                string[] addresses = new string[100];

                //Need to split the line correctly if more , are detected.
                foreach (string line in this.sysinfomatches)
                {
                    if (line.Contains("."))
                    {
                        string addressline = line.Substring(11, line.Length - 11);
                        if (addressline.Contains(","))
                        {
                            string[] splitaddresses = addressline.Split(',');
                            for (int i = 0; i < splitaddresses.Length; i++)
                            {
                                addresses[ipaddindex] = splitaddresses[i].Trim();
                                ipaddindex = ipaddindex + 1;
                            }
                        }
                        else
                        {
                            addresses[ipaddindex] = addressline.Trim();
                            ipaddindex = ipaddindex + 1;
                        }
                    }
                }
                Array.Resize(ref addresses, ipaddindex);
                this.ipaddresses = addresses;
            }
            else
                this.ipaddresses = new string[0];
        }
    }
}
