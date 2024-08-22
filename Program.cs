using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace agdiag
{
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]

        static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //Who: SGAL
            //What: Create a global exception handler to write to a local exception file,
            //      create a new email, then attach the file with whatever data, sending to
            //      the correct alias.

            //for now, just output to a file and let the user know.
            StreamWriter sw = new StreamWriter("ExceptionLog.txt");

            sw.Write(e.ExceptionObject.ToString());

            sw.Flush();
            sw.Close();

            Console.WriteLine("Whoops, looks like an issue has occurred. A log was written:");
            Console.WriteLine(System.AppDomain.CurrentDomain.BaseDirectory + "ExceptionLog.txt");

        }

        [STAThread]
        
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();

            //After we generate the report, update the Execution Count with user and datetime in UTC
            //int test = Utility.UpdateRunCount();

            if (args.Length > 1)
            {
                if (args[2] == "silent")
                {
                    //For SQLDragon internal use
                    maincode_block(args[1], true);
                }
                else
                { 
                    maincode_block(args[1], false);
                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }

        }

        public static void maincode_block(string logpath, bool silent)
        {
            double utcadjust = 0;
            bool isutcadjust = false;

            //install global handlers
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

                string primarysrvname = Utility.findserver(logpath);

                utcadjust = Utility.getutcadjustment(logpath, primarysrvname, out isutcadjust);

                var clusterlogevtlist = new clusterlogevts(utcadjust, isutcadjust, logpath, primarysrvname);

                if (clusterlogevtlist.isclusterlog)
                {
                    string path = "AGDiagReport.htm";

                    using (StreamWriter reportwriter = new StreamWriter(path))
                    {

                        if (primarysrvname == "")
                            Console.WriteLine("\r\nUnable to determine server name.");

                        Console.SetOut(reportwriter);

                        Console.WriteLine("<pre>");

                        var systeminfo = new systeminfo(logpath, primarysrvname);

                        var sqlerrloglist = new sqlerrlogevts(logpath, primarysrvname, false, false);

                        var sysevtloglist = new sysevtlogevts(logpath, primarysrvname, false);

                        new Report().agdiagsplash(utcadjust, primarysrvname);

                        new Report().reportlogsfound("aghealth", utcadjust, primarysrvname, logpath, clusterlogevtlist, sqlerrloglist, sysevtloglist, systeminfo);

                        agdiag.XEReader spsrvdiag = new agdiag.XEReader(logpath, DataSourceType.AlwaysOnSpServerDiagnostics);
                        spsrvdiag.ExecuteCheck();

                        agdiag.XEReader syshealth = new agdiag.XEReader(logpath, DataSourceType.SystemHealth);
                        syshealth.ExecuteCheck();

                    //new Report().connecttimeoutevtreport(sqlerrloglist, spsrvdiag, syshealth);

                    new Report().eventsummaryreport(clusterlogevtlist);

                        new Report().detailedreport(utcadjust, primarysrvname, clusterlogevtlist, sqlerrloglist, sysevtloglist, spsrvdiag, syshealth, systeminfo);
                        Console.WriteLine("</pre>");

                    //Close output file
                    reportwriter.Flush();
                        reportwriter.Close();

                        //Launch AGDiagReport.htm
                        if (!silent)
                            new Process { StartInfo = new ProcessStartInfo("AGDiagReport.htm") { UseShellExecute = true } }.Start();

                }
            }
                else
                    Console.WriteLine("\r\nCould not locate Cluster log, or more than one Cluster log detected. \r\nLog Path search is " + logpath);
        }
    }
}
