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
            bool sqldragon = false;
            double utcadjust = 0;
            bool isutcadjust = false;

            this.textStatus.Text = "New Report Started";
            this.textStatus.Refresh();
            var openFolderDialog = new FolderBrowserDialog();
            openFolderDialog.Description = "Select the folder containing primary replica Cluster log for analysis";
            if (openFolderDialog.ShowDialog() == DialogResult.OK)
            {
                logpath = openFolderDialog.SelectedPath;

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
                        var systeminfo = new systeminfo(logpath, primarysrvname);

                        this.textStatus.Text = "Processing SQL Server Error Logs...";
                        this.textStatus.Refresh();
                        var sqlerrloglist = new sqlerrlogevts(logpath, primarysrvname, false);

                        this.textStatus.Text = "Processing System Event Log...";
                        this.textStatus.Refresh();

                        var sysevtloglist = new sysevtlogevts(logpath, primarysrvname, false);

                        new Report().agdiagsplash(utcadjust, primarysrvname);
                        
                        this.textStatus.Text = "Generating Report - Logs Found...";
                        this.textStatus.Refresh();

                        new Report().reportlogsfound(utcadjust, primarysrvname, logpath, clusterlogevtlist, sqlerrloglist, sysevtloglist, systeminfo);

                        agdiag.XEReader spsrvdiag = new agdiag.XEReader(logpath, DataSourceType.AlwaysOnSpServerDiagnostics);
                        spsrvdiag.ExecuteCheck();

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
                        if (!sqldragon)
                            new Process { StartInfo = new ProcessStartInfo("AGDiagReport.htm") { UseShellExecute = true } }.Start();

                        this.textStatus.Text = "Report Complete for logs in " + logpath;
                        this.textStatus.Refresh();
                    }
                }
                else
                    if (!sqldragon) 
                        MessageBox.Show("Could not locate Cluster log, or more than one Cluster log detected. \r\nLog Path search is " + logpath);
                    else
                        Console.WriteLine("\r\nCould not locate Cluster log, or more than one Cluster log detected. \r\nLog Path search is " + logpath);
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
