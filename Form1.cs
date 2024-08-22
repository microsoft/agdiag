using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace agdiag
{
    public partial class Form1 : Form
    {

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

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tboxVersion.Text = "AGDiag Version " + Assembly.GetEntryAssembly().GetName().Version.ToString();;
            this.textStatus.Text = "Select the folder containing primary replica Cluster log for analysis";
            this.textStatus.Refresh();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            string logpath = "";
            string logpathtimeoutreplica = "";
            string logpathpartnerreplica = "";
            double utcadjust = 0;
            double utcadjusttimeoutreplica = 0;
            double utcadjustpartnerreplica = 0;
            double replicatimedifference = 0;
            bool isutcadjust = false;

            this.textStatus.Text = "New Report Started";
            this.textStatus.Refresh();
            var openFolderDialog = new FolderBrowserDialog();
            
            openFolderDialog.SelectedPath = Utility.GetLastFolderOpened();

            if (!agfailover.Checked && !replicatimeout.Checked)
            {
                MessageBox.Show("Select 'Availability Group Failover' or 'Replica Connection Timeout' for analysis.");
                return;
            }
            if (agfailover.Checked && replicatimeout.Checked)
            {
                MessageBox.Show("Select 'Availability Group Failover' or 'Replica Connection Timeout' for analysis.");
                return;
            }

            if (agfailover.Checked)
            {
                if (openFolderDialog.ShowDialog() == DialogResult.OK)
                {
                    logpath = openFolderDialog.SelectedPath;
                    Utility.SetLastFolderOpened(logpath);

                    //install global handlers
                    AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

                    //Start waiting cursor for user
                    this.Cursor = Cursors.WaitCursor;

                    string primarysrvname = Utility.findserver(logpath);

                    utcadjust = Utility.getutcadjustment(logpath, primarysrvname, out isutcadjust);
                    
                    this.textStatus.Text = "Processing Cluster Log Events...";
                    this.Cursor = Cursors.WaitCursor;
                    this.textStatus.Refresh();

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

                            this.textStatus.Text = "Processing System Info";
                            this.textStatus.Refresh();
                            var systeminfo = new systeminfo(logpath, primarysrvname);

                            this.textStatus.Text = "Processing SQL Server Error Logs...";
                            this.textStatus.Refresh();
                            var sqlerrloglist = new sqlerrlogevts(logpath, primarysrvname, false, false);

                            this.textStatus.Text = "Processing System Event Log...";
                            this.textStatus.Refresh();
                            var sysevtloglist = new sysevtlogevts(logpath, primarysrvname, false);

                            new Report().agdiagsplash(utcadjust, primarysrvname);

                            this.textStatus.Text = "Generating Report - Logs Found...";
                            this.textStatus.Refresh();
                            new Report().reportlogsfound("aghealth", utcadjust, primarysrvname, logpath, clusterlogevtlist, sqlerrloglist, sysevtloglist, systeminfo);

                            this.textStatus.Text = "Processing sp_serverdiagnostics logs...";
                            this.textStatus.Refresh();
                            agdiag.XEReader spsrvdiag = new agdiag.XEReader(logpath, DataSourceType.AlwaysOnSpServerDiagnostics);
                            spsrvdiag.ExecuteCheck();

                            this.textStatus.Text = "Processing system_health logs...";
                            this.textStatus.Refresh();
                            agdiag.XEReader syshealth = new agdiag.XEReader(logpath, DataSourceType.SystemHealth);
                            syshealth.ExecuteCheck();

                            this.textStatus.Text = "Generating Report - Report Summary...";
                            this.textStatus.Refresh();
                            new Report().eventsummaryreport(clusterlogevtlist);

                            this.textStatus.Text = "Generating Report - Detailed Health Events...";
                            this.textStatus.Refresh();
                            new Report().detailedreport(utcadjust, primarysrvname, clusterlogevtlist, sqlerrloglist, sysevtloglist, spsrvdiag, syshealth, systeminfo);
                            Console.WriteLine("</pre>");

                            //Close output file
                            reportwriter.Flush();
                            reportwriter.Close();

                            this.textStatus.Text = "Generating Report - Launch in Browser...";
                            this.textStatus.Refresh();
                            //Launch AGDiagReport.htm
                            new Process { StartInfo = new ProcessStartInfo("AGDiagReport.htm") { UseShellExecute = true } }.Start();

                            this.textStatus.Text = "Report Complete for logs in " + logpath;
                            this.textStatus.Refresh();
                        }
                    }
                    else
                        MessageBox.Show("Could not locate Cluster log, or more than one Cluster log detected. \r\nLog Path search is " + logpath);
                }
            }
            this.Cursor = Cursors.Default;
            this.textStatus.Text = "Select the folder containing primary replica Cluster log for analysis";
            this.textStatus.Refresh();

            //*******************************************REPLICA TIMEOUT CODE**********************************
            //If we are reporting on replica timeout
            if (replicatimeout.Checked)
            {
                openFolderDialog.Description = "Specify folder with SQL Error logs reporting replica connection timeout";
                if (openFolderDialog.ShowDialog() == DialogResult.OK)
                {
                    openFolderDialog.Description = "Specify folder of partner replica";
                    logpathtimeoutreplica = openFolderDialog.SelectedPath;
                    Utility.SetLastFolderOpened(logpathtimeoutreplica);
                }
                else
                {
                    MessageBox.Show("Folder open canceled.");
                    return;
                }

                if (openFolderDialog.ShowDialog() == DialogResult.OK)
                {
                    logpathpartnerreplica = openFolderDialog.SelectedPath;
                    Utility.SetLastFolderOpened(logpathpartnerreplica);
                }
                else
                {
                    MessageBox.Show("Folder open canceled.");
                    return;
                }

                //install global handlers
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

                //Start waiting cursor for user
                this.Cursor = Cursors.WaitCursor;

                string srvnametimeoutreplica = Utility.findserver(logpathtimeoutreplica);
                string srvnamepartnerreplica = Utility.findserver(logpathpartnerreplica);

                utcadjusttimeoutreplica = Utility.getutcadjustment(logpathtimeoutreplica, srvnametimeoutreplica, out isutcadjust);
                utcadjustpartnerreplica = Utility.getutcadjustment(logpathtimeoutreplica, srvnametimeoutreplica, out isutcadjust);

                if (utcadjusttimeoutreplica > utcadjustpartnerreplica)
                    replicatimedifference = utcadjusttimeoutreplica - utcadjustpartnerreplica;
                else
                    replicatimedifference = -1*(utcadjustpartnerreplica - utcadjusttimeoutreplica);

                this.textStatus.Text = "Processing Cluster Log Events...";
                this.Cursor = Cursors.WaitCursor;
                this.textStatus.Refresh();

                var clusterlogevtlist = new clusterlogevts(utcadjusttimeoutreplica, isutcadjust, logpathpartnerreplica, srvnamepartnerreplica);

                string path = "AGDiagReport.htm";

                using (StreamWriter reportwriter = new StreamWriter(path))
                {
                    if (srvnametimeoutreplica == "")
                        Console.WriteLine("\r\nUnable to determine server name.");

                        Console.SetOut(reportwriter);

                        Console.WriteLine("<pre>");

                        this.textStatus.Text = "Processing System Info";
                        this.textStatus.Refresh();
                        var systeminfo = new systeminfo(logpathtimeoutreplica, srvnametimeoutreplica);

                        //Get SQL Error logs to find all connection timeout events
                        this.textStatus.Text = "Processing SQL Server Error Logs...";
                        this.textStatus.Refresh();
                        var sqlerrloglisttimeoutrep = new sqlerrlogevts(logpathtimeoutreplica, srvnametimeoutreplica, false, true);

                        if (!sqlerrloglisttimeoutrep.issqlerrlogs)
                        {
                            MessageBox.Show("Could not locate SQL Error logs. \r\nLog Path search is " + logpathtimeoutreplica);
                            return;
                        };

                        var sqlerrloglistpartnerrep = new sqlerrlogevts(logpathpartnerreplica, srvnamepartnerreplica, false, false);
                        if (!sqlerrloglisttimeoutrep.issqlerrlogs)
                        {
                            MessageBox.Show("Could not locate SQL Error logs. \r\nLog Path search is " + logpathpartnerreplica);
                            return;
                        };

                        this.textStatus.Text = "Processing System Event Log...";
                        this.textStatus.Refresh();
                        var sysevtloglist = new sysevtlogevts(logpathpartnerreplica, srvnamepartnerreplica, false);

                        this.textStatus.Text = "Starting report...";
                        this.textStatus.Refresh();
                        new Report().agdiagsplash(utcadjust, srvnametimeoutreplica);

                        new Report().reportlogsfound("connecttimeout", utcadjust, srvnamepartnerreplica, logpathpartnerreplica, clusterlogevtlist, sqlerrloglistpartnerrep, sysevtloglist, systeminfo);
                        this.textStatus.Text = "Generating Report - Logs Found...";
                        this.textStatus.Refresh();

                        this.textStatus.Text = "Processing sp_serverdiagnostics logs...";
                        this.textStatus.Refresh();
                        agdiag.XEReader spsrvdiag = new agdiag.XEReader(logpathpartnerreplica, DataSourceType.AlwaysOnSpServerDiagnostics);
                        spsrvdiag.ExecuteCheck();

                        this.textStatus.Text = "Processing system_health logs...";
                        this.textStatus.Refresh();
                        agdiag.XEReader syshealth = new agdiag.XEReader(logpathpartnerreplica, DataSourceType.SystemHealth);
                        syshealth.ExecuteCheck();

                        this.textStatus.Text = "Generating Report - Report Summary...";
                        this.textStatus.Refresh();
                        new Report().connectioneventsummaryreport(sqlerrloglisttimeoutrep, srvnametimeoutreplica, srvnamepartnerreplica);

                        this.textStatus.Text = "Generating Report - Detailed Health Events...";
                        this.textStatus.Refresh();
                        new Report().connecttimeoutdetailedreport(replicatimedifference, utcadjusttimeoutreplica, utcadjustpartnerreplica, srvnametimeoutreplica, srvnamepartnerreplica, sqlerrloglisttimeoutrep, sqlerrloglistpartnerrep, sysevtloglist, spsrvdiag, syshealth, systeminfo);
                        Console.WriteLine("</pre>");

                        //Close output file
                        reportwriter.Flush();
                        reportwriter.Close();

                        this.textStatus.Text = "Generating Report - Launch in Browser...";
                        this.textStatus.Refresh();

                        //Launch AGDiagReport.htm
                        new Process { StartInfo = new ProcessStartInfo("AGDiagReport.htm") { UseShellExecute = true } }.Start();

                        this.textStatus.Text = "Report Complete for logs in " + logpathpartnerreplica;
                        this.textStatus.Refresh();
                    }
                }

            this.Cursor = Cursors.Default;
            this.textStatus.Text = "Select the folder containing primary replica Cluster log for analysis";
            this.textStatus.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            
        }

        private void textBox1_TextChanged_2(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_3(object sender, EventArgs e)
        {

        }
    }
}
