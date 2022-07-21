using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F002461
{
    /********************************************************************
     *  
     *  History Log:
     *  REV     AUTHOR      DATE            COMMENTS
     *  A       CalvinXie   2022/06/19      First Version.
     *  
     ********************************************************************/

    static class Program
    {
        public static string g_str_ToolNumber = "";
        public static string g_str_ToolRev = "";
        private static System.Threading.Mutex mutex;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            g_str_ToolNumber = "F002461";
            g_str_ToolRev = "A";

            mutex = new System.Threading.Mutex(false, "F002461 Flash Software");
            if (!mutex.WaitOne(0, false))
            {
                mutex.Close();
                mutex = null;
            }
            if (mutex != null)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("F002461 Already Exist !!!");
            }

        }
    }
}
