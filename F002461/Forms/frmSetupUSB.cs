using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F002461.Forms
{
    public partial class frmSetupUSB : Form
    {
        #region Variable

        private string m_str_DeviceName = "";
        private clsNI6001 m_obj_Fixctrl = null;
        private string m_str_TestMode = "";
        private string m_str_DAQDevice = "";
        public string m_str_Model = "";     // Device Model

        #endregion

        #region Load

        public frmSetupUSB()
        {
            InitializeComponent();
        }

        private void frmSetupUSB_Load(object sender, EventArgs e)
        {
            string strNote = "";
            strNote =  "Step1: Connect the device to one USB port." + "\r\n";
            strNote += "Step2: Select one pannel, then click OK." + "\r\n";
            strNote += "Step3: Select four pannels and four USB port in turn." + "\r\n";
            strNote += "Step4: Restart the test tool after configuration.";
            lblNote.Text = strNote;

            comboBox_Panel.SelectedItem = "1";

            string strErrorMessage = "";
            if (ReadSetupFile(ref strErrorMessage) == false)
            {
                MessageBox.Show("Failed to read setup ini file." + strErrorMessage);
                return;
            }

            if (ReadOptionFile(ref strErrorMessage) == false)
            {
                MessageBox.Show("Failed to read option ini file." + strErrorMessage);
                return;
            }

            if (m_str_TestMode == "1" && m_str_Model.Contains("CT40"))
            {
                if (InitNI6001() == false)
                {
                    MessageBox.Show("Failed to init NI6001.");
                    return;
                }
            }
            return;
        }

        private void frmSetupUSB_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_str_TestMode == "1" && m_str_Model.Contains("CT40"))
            {
                if (ReleaseNI6001() == false)
                {
                    MessageBox.Show("Failed to Release NI6001.");
                    return;
                }
            }
            return;
        }

        #endregion

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            btnOK.Enabled = false;

            SetupUSB();

            btnOK.Enabled = true;

            return;
        }

        private void SetupUSB()
        {
            bool bRes = false;
            string strPanel = comboBox_Panel.Text.Trim();

            if (strPanel == "")
            {
                DisplayMessage("Please select panel.");
            }
            else
            {
                if (m_str_TestMode == "1" && m_str_Model.Contains("CT40"))
                {
                    string strErrorMessage = "";
                    if (USBPlug(strPanel, "1", ref strErrorMessage) == false)
                    {
                        DisplayMessage("USBPlug IN failed.");
                        return;
                    }

                    Dly(1);
                }

                for (int i = 0; i < 5; i++)
                {
                    bRes = SetupUSBDevicePhysicalAddress();
                    if (bRes == true)
                    {
                        break;
                    }
                    if (bRes == false)
                    {
                        Dly(1);
                        continue;
                    }
                }
                if (bRes == false)
                {
                    DisplayMessage("Setup failed.");
                }
                else
                {
                    DisplayMessage("Setup successfully.");
                }

                if (m_str_TestMode == "1" && m_str_Model.Contains("CT40"))
                {
                    string strErrorMessage = "";
                    if (USBPlug(strPanel, "0", ref strErrorMessage) == false)
                    {
                        DisplayMessage("USBPlug OUT failed.");
                        return;
                    }
                }
            }

            return;
        }

        #endregion

        #region Function

        private bool SetupUSBDevicePhysicalAddress()
        {
            string strErrorMessage = "";

            if (m_str_DeviceName == "")
            {
                DisplayMessage("Faild to get device name from ini file.");
                return false;
            }

            USBEnumerator usbEnum = new USBEnumerator();
            string strDeviceName = m_str_DeviceName;
            List<string> m_List_PhysicalAddress = new List<string>();
            if (usbEnum.GetUSBDevicePhysicalAddressByDeviceName(strDeviceName, ref m_List_PhysicalAddress, ref strErrorMessage) == false)
            {
                DisplayMessage(strErrorMessage);
                return false;
            }
            if (m_List_PhysicalAddress.Count != 1)
            {
                strErrorMessage = "Failed to enume USB device." + m_List_PhysicalAddress.Count.ToString();
                DisplayMessage(strErrorMessage);
                return false;
            }

            string strPanel = "Panel_" + comboBox_Panel.Text.Trim();
            if (WriteOptionFile(strPanel, m_List_PhysicalAddress[0].ToString(), ref strErrorMessage) == false)
            {
                DisplayMessage(strErrorMessage);
                return false;
            }

            DisplayMessage("Panel:" + strPanel);
            DisplayMessage("USBDevicePhysicalAddress:" + m_List_PhysicalAddress[0].ToString());

            return true;
        }

        #endregion

        #region NI Card

        private bool ReadOptionFile(ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Option.ini";
                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                #region TestMode

                m_str_TestMode = objIniFile.ReadString("TestMode", "Mode");
                if ((m_str_TestMode != "0") && (m_str_TestMode != "1"))
                {
                    strErrorMessage = "Invalid TestMode Mode:" + m_str_TestMode;
                    return false;
                }

                #endregion

                #region NI6001

                m_str_DAQDevice = objIniFile.ReadString("NI6001", "Device");

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool InitNI6001()
        {
            try
            {
                m_obj_Fixctrl = null;
                m_obj_Fixctrl = new clsNI6001();
                if (m_obj_Fixctrl.Init(m_str_DAQDevice) == false)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool ReleaseNI6001()
        {
            try
            {
                if (m_obj_Fixctrl != null)
                {
                    m_obj_Fixctrl = null;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool USBPlug(string strPanel, string strStatus, ref string strErrorMessage)
        {
            try
            {
                // strStatus 1:工作位，0:初始位
                if (strStatus != "1" && strStatus != "0")
                {
                    strErrorMessage = "Invalid status:" + strStatus;
                    return false;
                }

                if (strPanel == "1")
                {
                    if (strStatus == "1")
                    {
                        m_obj_Fixctrl.SetDigital(0, 0, 1, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 1, 0, 0.1);
                    }
                    else
                    {
                        m_obj_Fixctrl.SetDigital(0, 0, 0, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 1, 1, 0.1);
                    }
                }

                else if (strPanel == "2")
                {
                    if (strStatus == "1")
                    {
                        m_obj_Fixctrl.SetDigital(0, 2, 1, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 3, 0, 0.1);
                    }
                    else
                    {
                        m_obj_Fixctrl.SetDigital(0, 2, 0, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 3, 1, 0.1);
                    }
                }

                else if (strPanel == "3")
                {
                    if (strStatus == "1")
                    {
                        m_obj_Fixctrl.SetDigital(0, 4, 1, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 5, 0, 0.1);
                    }
                    else
                    {
                        m_obj_Fixctrl.SetDigital(0, 4, 0, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 5, 1, 0.1);
                    }
                }

                else if (strPanel == "4")
                {
                    if (strStatus == "1")
                    {
                        m_obj_Fixctrl.SetDigital(0, 6, 1, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 7, 0, 0.1);
                    }
                    else
                    {
                        m_obj_Fixctrl.SetDigital(0, 6, 0, 0.1);
                        m_obj_Fixctrl.SetDigital(0, 7, 1, 0.1);
                    }
                }

                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "USBPlug Exception:" + strr;
                return false;
            }

            return true;
        }

        #endregion

        #region Private

        private bool ReadSetupFile(ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Setup.ini";

                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (System.IO.File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                m_str_DeviceName = objIniFile.ReadString("PortDevice", "DeviceName");
                if (m_str_DeviceName == "")
                {
                    strErrorMessage = "Read ini key fail.";
                    return false;
                }

            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool WriteOptionFile(string strKey, string strValue, ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Setup.ini";

                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (System.IO.File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                objIniFile.WriteIniFile("PortMapping", strKey, strValue);

                string strReadValue = objIniFile.ReadString("PortMapping", strKey);
                if (strReadValue != strValue)
                {
                    strErrorMessage = "Write ini key value fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + strr;
                return false;
            }

            return true;
        }

        private void DisplayMessage(string str_Message)
        {
            try
            {
                rtb_InfoList.AppendText(str_Message + Convert.ToChar(13) + Convert.ToChar(10));
                rtb_InfoList.ScrollToCaret();
                rtb_InfoList.Refresh();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                rtb_InfoList.Clear();
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void Dly(double d_WaitTimeSecond)
        {
            try
            {
                long lWaitTime = 0;
                long lStartTime = 0;

                if (d_WaitTimeSecond <= 0)
                {
                    return;
                }

                lWaitTime = Convert.ToInt64(d_WaitTimeSecond * TimeSpan.TicksPerSecond);
                lStartTime = System.DateTime.Now.Ticks;
                while ((System.DateTime.Now.Ticks - lStartTime) < lWaitTime)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        #endregion

    }
}
