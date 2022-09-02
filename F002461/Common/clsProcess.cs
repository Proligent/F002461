using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F002461.Common
{
    public class clsProcess
    {
        #region Variable

        private static string m_sErrorMessage = "";

        #endregion

        #region Property

        public string ErrorMessage
        {
            get
            {
                return m_sErrorMessage;
            }
        }

        #endregion

        #region Construct

        public clsProcess()
        {

        }

        #endregion

        #region Function

        public static bool ExcuteCmd(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                m_sErrorMessage = "Empty Command !";
                return false;
            }

            try
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();

                startInfo.FileName = @"C:\Windows\System32\cmd.exe";
                startInfo.Arguments = " /C " + command;     //“/C”Quit after finishing cmd
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;

                process.Start();
                process.WaitForExit();
                process.Close();
                process.Dispose();
            }
            catch (Exception ex)
            {
                m_sErrorMessage = "Excute cmd exception: " + ex.Message;
                return false;
            }

            return true;
        }


        #endregion












    }
}
