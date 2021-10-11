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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());


        }

    }
}
