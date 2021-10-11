using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Windows.Forms;
using System.Globalization;

namespace agdiag
{
    /*
     * Utility class contains methods used by all other classes.
     * 
     * processargs() processed arguments passed to agdiag on launch. They include
     *      path to primary replica log files
     *      sqldragon switch for running agdiag in sqldragon
     * 
     * getutcadjustment() determines UTC adjustment in hours from local server time. In this order, this function uses the following logs
     * to determine the UTC adjustment: Cluster log, MiscPssdiagInfo, SQL Server error logs, SDP MachineInfo.txt log.
     * 
     * getBetween() is a simple function for returning string between two strings passed into function
     * 
     * findlogs() is used to return an array of log files. Pass it the server name and / or a 'logmask' that identifies unique string 
     * identifier in file name for those types of logs.
     * 
     * unzipanyzipped() pass in the log path and this method unzips any found zip files.
     * 
     * findserver() pass in the log path and this function finds the server name by counting the files with the most matching server names.
     * 
     * returnexcludedmatch() pass in an array and a target string and this method returns that array minus the rows that matched the target string.
     * 
     * findmatchesinlog() pass in an array of logs and array of target strings, and this method opens each log and builds an array of matched lines 
     * for each target found.
     * 
     * returnlinesafterstrmatch() pass in log file and a target string and only return lines after you find the target string in the log file. 
     * This is used to begin searching the Cluster log after the string '[=== Cluster Logs ===]' is found.
     * 
     */


    class Utility
    {
        public string logpath
        {
            get;
            set;
        }

        public struct XMLAttributeValue
        {
            public string strAttributeQuery;
            public string strAttributeName;
            public string strAttributeValue;
        }

        static string AppRegyPath = "Software\\agdiag";
        static string rvn_ExecCount = "ExecCount";

        public static Microsoft.Win32.RegistryKey _appCuKey;
        public static Microsoft.Win32.RegistryKey AppCuKey
        {
            get
            {
                if (_appCuKey == null)
                {
                    _appCuKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AppRegyPath, true);
                    if (_appCuKey == null)
                        _appCuKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(AppRegyPath);
                }
                return _appCuKey;
            }
            set { _appCuKey = null; }
        }

        public static string processargs(string[] args, ref bool sqldragon)
        {
            string logpath = Directory.GetCurrentDirectory();

            if (args.Length > 0)
                for (int i = 0; i < args.Length; i++)
                {
                    //Console.WriteLine(args[i]);
                    if (args[i] == "sqldragon")
                    {
                        sqldragon = true;
                    }

                    if (args[i].Contains(":") && !Directory.Exists(args[i]))
                    {
                        Console.WriteLine("Specified path to log files does not exist: " + args[i]);
                        Console.WriteLine("\r\n***USE***\r\nEnter path to cluster log");
                        Console.WriteLine("Alternatively copy agdiag.exe to SDP folder and run there with no command line.");
                    }
                    if (Directory.Exists(args[i]))
                    {
                        logpath = args[i];
                        Console.WriteLine("\r\nSpecified logpath is {0}\r\n", logpath);
                        Console.WriteLine("\r\nAGDiagReport.htm will be generated in " + Directory.GetCurrentDirectory() + "\r\n", logpath);
                    }
                }
            if (args.Length == 0)
            {
                Console.WriteLine("\r\nNo log path was specified, AGDiag will search for Cluster and other supporting logs in the current directory.");
                Console.WriteLine("\r\nSpecified logpath is {0}\r\n", logpath);
                
            }
            return logpath;
        }

        public static int GetXMLAttributeValues(string strXMLDocument, ref XMLAttributeValue[] XMLAttributeValueArray)
        {
            int iSuccessfulAttributes = 0;
            XmlDocument xDoc = new XmlDocument();

            if (0 == XMLAttributeValueArray.Length)
            {
                return 0;
            }

            try
            {
                xDoc.LoadXml(strXMLDocument);

                for (int i = 0; i < XMLAttributeValueArray.Length; i++)
                {
                    XmlElement xValue = (XmlElement)xDoc.SelectSingleNode(XMLAttributeValueArray[i].strAttributeQuery);
                    XMLAttributeValueArray[i].strAttributeValue = xValue.GetAttribute(XMLAttributeValueArray[i].strAttributeName);
                    iSuccessfulAttributes++;
                }
            }
            catch
            {
                return iSuccessfulAttributes;
            }

            return iSuccessfulAttributes;
        }

        //Get UTC adjustment for all analysis and reporting, need to be sure so we will check multiple logs, Cluster log, System_information, SQL Error, etc.
        public static double getutcadjustment(string logpath, string srvname, out bool isutcadjust)
        {
            string[] line=new string[] { "" };
            double utcadjust = 0;
            DateTime sqllocaldt;
            DateTime sqlutcdt;
            string machineinfologmask = "*_MachineInfo.xml";
            string clusterlogmask = "*_cluster.log";
            string pssdiaglogmask = "*MiscPssdiag*";
            string sqlerrlogmask = "*errorlog*";

            string[] msinfotarget = new string[] { "Time Zone" };
            string[] machineinfotarget = new string[] { "CurrentTimeZone=" };
            string[] msinforptdttarget = new string[] { "System Information report written at:" };
            string[] sqlerrlogtarget = new string[] { "UTC adjustment:" };
            string[] sqlerrloglocalutctargetlastreported = new string[] { "This instance of SQL Server last reported using a process ID" };
            string[] sqlerrloglocalutctargethasbeenusing = new string[] { "This instance of SQL Server has been using a process ID of" };
            string[] clusterlogtarget = new string[] { "the time zone offset of this machine"};
            string[] pssdiagtarget = new string[] { "UTCOffset_in_Hours" };
            
            isutcadjust = false;

            //Try the Cluster log from pssdiag
            string[] clusterlog = Utility.findlogs(logpath, srvname, clusterlogmask);
            if (clusterlog.Length == 1 && !isutcadjust)
            {
                line = Utility.findmatchesinlog(clusterlog, clusterlogtarget, "", "", 0);
                if (line.Length!=0 && !line[0].Equals(""))
                {
                    utcadjust = -(Convert.ToDouble(line[0].Substring(line[0].IndexOf(", or ") + 5, (line[0].IndexOf(" hours") - (line[0].IndexOf(", or ") + 5)))));
                    isutcadjust = true;
                }
            }

            //Try the MiscPssdiagInfo from pssdiag
            string[] pssdiaglog = Utility.findlogs(logpath, srvname, pssdiaglogmask);
            if (line.Length != 0 && !isutcadjust)
            {
                line = findmatchesinlog(pssdiaglog, pssdiagtarget, "", "", 0);
                if (line.Length != 0 && !line[0].Equals(""))
                {
                    utcadjust = Convert.ToDouble(Utility.getBetween(line[0], "UTCOffset_in_Hours", ".00"));
                    isutcadjust = true;
                }
            }

            //First check for SQL Error logs to calculate difference between local and UTC times
            string[] sqlerrlogs = Utility.findlogs(logpath, srvname, sqlerrlogmask);

            if (sqlerrlogs.Length >= 1 && !isutcadjust)
            {
                line = findmatchesinlog(sqlerrlogs, sqlerrloglocalutctargetlastreported, "", "", 0);
                if (line.Length!=0 && !isutcadjust)
                {
                    DateTime.TryParse(Utility.getBetween(line[0], "at ", " (local)"), out sqllocaldt);
                    DateTime.TryParse(Utility.getBetween(line[0], "(local) ", " (UTC)"), out sqlutcdt);
                    utcadjust = sqllocaldt.Hour - sqlutcdt.Hour;
                    isutcadjust = true;
                }
            }

            if (sqlerrlogs.Length >= 1 && !isutcadjust)
            {
                line = findmatchesinlog(sqlerrlogs, sqlerrloglocalutctargethasbeenusing, "", "", 0);
                if (line.Length != 0 && !isutcadjust)
                {
                    DateTime.TryParse(Utility.getBetween(line[0], "since ", " (local)"), out sqllocaldt);
                    DateTime.TryParse(Utility.getBetween(line[0], "(local) ", " (UTC)"), out sqlutcdt);
                    utcadjust = sqllocaldt.Hour - sqlutcdt.Hour;
                    isutcadjust = true;
                }
            }

            //Try the SQL Error logs from SDP or PSSDiag, find the string with 'UTC adjustment'
            if (sqlerrlogs.Length >= 1 && !isutcadjust)
            {
                line = findmatchesinlog(sqlerrlogs, sqlerrlogtarget, "", "", 0);
                if (line.Length != 0 && !line[0].Equals(""))
                {
                    utcadjust = Convert.ToDouble(Utility.getBetween(line[0], "UTC adjustment: ", ":00"));
                    isutcadjust = true;
                }
            }

            //Check for SDP MachineInfo.txt log
            string[] machineinfolog = Utility.findlogs(logpath, srvname, machineinfologmask);

            if (machineinfolog.Length == 1)
            {
                line = findmatchesinlog(machineinfolog, machineinfotarget, "", "", 0);
                if (!line[0].Equals(""))
                {
                    utcadjust = Convert.ToDouble(Utility.getBetween(line[0], "CurrentTimeZone=", " DaylightInEffect").Replace("\"", String.Empty)) / 60;
                    isutcadjust = true;
                }
            }

            /*
            //First check for msinfo log
            string[] msinfolog = Utility.findlogs(logpath, srvname, msinfologmask);

            if (msinfolog.Length == 1)
            {
                line = findmatchesinlog(msinfolog, msinfotarget, "");
                reportdateline = findmatchesinlog(msinfolog, msinforptdttarget, "");
                if (!line[0].Equals(""))
                {
                    utcadjuststr = line[0].Substring(10, line[0].Length - 10);
                    DateTime.TryParse(reportdateline[0].Substring(("System Information report written at:").Length, (reportdateline[0].Length - ("System Information report written at:").Length)), out msinforeportdate);
                    utcadjust = Convert.ToInt32(Utility.getBetween(line[0], "UTC", ":"));
                    isutcadjust = true;
                }
            }
            */
            return utcadjust;
        }

            public static bool agdneedsupgrade()
        {
            return true;
        }

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            return "";
        }

        public static string[] findlogs(string logpath, string srvname, string logmask)
        {
            int LogFileCount = Directory.GetFiles(logpath).Count();
            string logfilestr="";
            string[] logfiles = new string[LogFileCount];
            int index = 0;

            if (Directory.Exists(logpath))
            {
                foreach (string logfile in Directory.GetFiles(logpath, logmask))
                {
                    //Check for these variations: servername.DC.EDU_cluster.log, servername_cluster.log, 
                    logfilestr = logfile.Substring(logfile.LastIndexOf("\\")+1, logfile.Length-logfile.LastIndexOf("\\")-1);
                    if (logfilestr.ToLower().Contains(srvname.ToLower()))
                    {
                        logfiles[index] = logfile.ToLower();
                        index++;
                    }
                }
            }
            if (Directory.Exists(logpath + "\\ClusterLogs\\") && logmask.Contains("cluster"))
            {
                foreach (string logfile in Directory.GetFiles(logpath + "\\ClusterLogs\\", logmask))
                {
                    if (logfile.ToLower().Contains(srvname.ToLower()))
                    {
                        logfiles[index] = logfile.ToLower();
                        index++;
                    }
                }
            }
            Array.Resize(ref logfiles, index);
            return logfiles;
        }

        public void unzipanyzipped(string logpath)
        {
            Console.WriteLine("<tr><td>Locating and unzipping logs...</td>");
            string entryforexception="";
            string[] ZipFiles = Directory.GetFiles(logpath, "*.zip");
            if (ZipFiles.Length >= 1)
            {
                try { 
                Console.WriteLine("<td>Zipped files found! Extracting...");
                Console.WriteLine("<details>");
                Console.WriteLine("<summary>Unzipped files</summary>");
                Console.WriteLine("<p>");
                for (int i = 0; i < ZipFiles.Length; i++)
                {
                    using (ZipArchive archive = ZipFile.Open(ZipFiles[i], ZipArchiveMode.Read))
                    {
                        // ... Loop over entries.
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            entryforexception = entry.Name;
                            Console.WriteLine("Name: " + entry.Name + ", Size: " + entry.CompressedLength + "<br>");
                            entry.ExtractToFile(logpath + "\\" + entry.Name, true);
                        }
                    }
                }
                Console.WriteLine("</p></details></td></tr>");
                }
                catch (SystemException se)
                {
                    if (se.Message.Contains("being used by another process"))
                    {
                        MessageBox.Show("File being used by another process, aborting unzip. Report maybe incomplete. \r\nZip File is " + entryforexception);
                        return;
                    }

                    else
                        MessageBox.Show(se.Message);
                }
            }
            else
            {
                Console.WriteLine("<td>No zip files found to unzip.</td>");
            }
        }

        public static bool isfullyqualifiedservername(string srvname)
        { 
            bool fullyqualified = false;

            if (srvname.Contains("."))
                fullyqualified = true;
            return fullyqualified;
        }

        public static string findserver(string logpath)
        {
            int count = 0;
            int mostmatches = 0;
            string srvname = "";
            string[] files = Directory.GetFiles(logpath).Select(file => Path.GetFileName(file)).ToArray();
            string[] srvnames = files.ToArray();

            for (int i = 0; i < files.Length - 1; i++)
            {
                if (files[i].Contains("_"))
                {
                    srvnames[i] = files[i].Substring(0, files[i].IndexOf("_"));
                }
                count = srvnames.Count(s => s == srvnames[i]);
                if (count > mostmatches)
                {
                    mostmatches = count;
                    srvname = srvnames[i];
                }
            }
            return srvname;
        }

        public static string[] returnexcludedmatch(string[] targetarr, string target)
        {
            int i = 0;
            int j = 0;
            string[] arr = new string[targetarr.Length - 1];

                foreach(string log in targetarr)
                {
                    if (log != target)
                    {
                        arr[i] = log;
                        i++;
                    }
                    j++;
                }
            return arr;
        }

        public static string[] findmatchesinlog(string[] logs, string[] targets, string startafterstr, string getlinesaftermatch, int numberlinesaftermatch)
        {
            int index = 0;
            string line = "";
            String[] matches = new String[1000000];

            foreach (string log in logs)
            {
                using (StreamReader logfile = new StreamReader(log))
                {
                    while (!logfile.EndOfStream)
                    { 
                        //Make sure we get past startafterstr before we start reading Cluster log.
                        line = logfile.ReadLine();
                        if (line.Contains(startafterstr) && !startafterstr.Equals(""))
                        {
                            line = logfile.ReadLine();
                            index = 0;
                        }
                        foreach (string target in targets)
                        {
                            if (line.Contains(target))
                            {
                                if (!getlinesaftermatch.Equals(""))
                                {
                                    if (target == getlinesaftermatch)
                                    {
                                        for (int i = 0; i < numberlinesaftermatch; i++)
                                        {
                                            matches[i + index] = line;
                                            line = logfile.ReadLine();
                                        }
                                        index = index + numberlinesaftermatch;
                                    }
                                    else
                                    { 
                                        matches[index] = line;
                                        index++;
                                    }
                                }
                                else
                                {
                                    matches[index] = line;
                                    index++;
                                }
                            }
                        }
                    }
                }
            }
            Array.Resize(ref matches, index);
            return matches;
        }

        public static string[] returnlinesafterstrmatch(string[] clusterlogmatches, string targetstr)
        {
            if (!targetstr.Equals(""))
            {
                int index = Array.IndexOf(clusterlogmatches, targetstr);
                string[] arr = new string[clusterlogmatches.GetUpperBound(0) - index];
                Array.Copy(clusterlogmatches, index + 1, arr, 0, clusterlogmatches.GetUpperBound(0) - index);
                return arr;
            }
            else
                return clusterlogmatches;
        }

        public static string[] findmatchesinarray(string[] evtmatches, string[] evttargets)
        {
            int index = 0;
            String[] matches = new String[evtmatches.Length];
            foreach (string match in evtmatches)
            {
                foreach (string sevstr in evttargets)
                {
                    if (match.Contains(sevstr))
                    {
                        matches[index] = match;
                        index++;
                    }
                }
            }
            Array.Resize(ref matches, index);
            return matches;
        }
    }
}
