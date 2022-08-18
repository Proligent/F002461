using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using SmartFactory.ExternalDLL;
using F002461.Forms;
using F002461.Properties;

namespace F002461
{
    public partial class frmMain : Form
    {
        #region Enum

        #endregion

        #region Struct

        private struct OptionData
        {
            // Option.ini
            public string TestMode;
            public string DAQDevice;
            public string PLCIP;
            public string PLCPort;
            public int DB_Slot1_ReadDB;
            public int DB_Slot1_WriteDB;
            public int DB_Slot2_ReadDB;
            public int DB_Slot2_WriteDB;
            public int DB_Slot3_ReadDB;
            public int DB_Slot3_WriteDB;
            public int DB_Slot4_ReadDB;
            public int DB_Slot4_WriteDB;
            public string Area_Location;
            public string MES_Enable;
            public string SDCard_Enable;
            public string Reboot_WaitTime;

            // Setup.ini
            public string ADBDeviceName;
            public string QDLoaderPortName;
            public string DeviceAddress_Panel1;
            public string DeviceAddress_Panel2;
            public string DeviceAddress_Panel3;
            public string DeviceAddress_Panel4;      
        }

        private struct ModelOption
        {
            // Image
            public string ImageServerPath;
            public string ImageLocalPath;
            public string ImageCopyMode;

            // FlashMode
            public string FlashMode;

            // BITResult
            public string CheckManualBITResult_Enable;

            // Fastboot
            public string FASTBOOTEnable;
            public string FASTBOOTBatServerPath;
            public string FASTBOOTBatLocalPath;
            public string FASTBOOTBatFile;
            public int FASTBOOTTimeout;
            public string FASTBOOTSuccessFlag;

            // EDL
            public string EDLQFIL;
            public string EDLDeviceType;
            public string EDLFlashType;
            public string EDLELF;
            public string EDLPatch;
            public string EDLRawProgram;
            public string EDLReset;
            public int EDLTimeout;
            public string EDLSuccessFlag;

            // Keybox
            public string KeyboxEnable;
            public string KeyboxFilePath;
            public string KeyboxFile;
            public string KeyboxDevice;

            // SentienceKey
            public string SentienceKeyEnable;
            public string SentienceKeyHonEdgeProductName;
            public string SentienceKeyUploadEnable;
            public string SentienceKeyUploadServerPath;

            // Station
            public string StationName;

            // Check SKU
            public string SKUCheckEnable;

            // MDCS
            public string MDCSEnable;
            public string MDCSURL;
            public string MDCSDeviceName;
            public string MDCSPreStationResultCheck;
            public string MDCSPreStationDeviceName;
            public string MDCSPreStationVarName;
            public string MDCSPreStationVarValue;   
        }

        private struct MCFData
        {
            public string Model;
            public string SKU;
            public string OSPN;
            public string OSVersion;
        }

        private struct MESData
        {
            public string EID;
            public string WorkOrder;
        }

        private struct UnitDeviceInfo
        {
            public string Panel;  
            public string SN;
            public string SKU;
            public string Model;
            public string EID;
            public string WorkOrder;
            public string Status;
            public string PhysicalAddress;
        }

        #endregion

        #region Variable

        private bool m_bCollapse = true;
        private string m_str_Model = "";
        private string m_str_PdLine = "";
        private OptionData m_st_OptionData = new OptionData();
        private MCFData m_st_MCFData = new MCFData();
        private MESData m_st_MESData = new MESData();
        private Dictionary<string, UnitDeviceInfo> m_dic_UnitDevice = new Dictionary<string, UnitDeviceInfo>();
        private Dictionary<string, ModelOption> m_dic_ModelOption = new Dictionary<string, ModelOption>();
        private Dictionary<string, TestSaveData> m_dic_TestSaveData = new Dictionary<string, TestSaveData>();
        private Dictionary<string, bool> m_dic_TestStatus = new Dictionary<string, bool>();  // true:Running    false:Not Running

        private const string FASTBOOTMODE = "FASTBOOT";
        private const string EDLMODE = "EDL";

        private const string PANEL_1 = "1";
        private const string PANEL_2 = "2";
        private const string PANEL_3 = "3";
        private const string PANEL_4 = "4";
        private const string STATUS_CONNECTED = "Connected";
        private const string STATUS_DISCONNECTED = "Not Connected";
        private const string STATUS_FLASHING = "Ongoing";
        private const string STATUS_SUCCESSED = "PASS";
        private const string STATUS_FAILED = "FAIL";

        private bool m_b_Setting = false;
        private bool m_b_Running = false;
        private bool m_b_RunReslut = false;

        private clsNI6001 m_obj_Fixctrl = null;
        private CPLCDave m_obj_PLC = null;
        private System.Threading.Timer m_timer_WatchDog = null;
        private bool m_b_PLCRuning = false;
        private int m_i_WatchDog = 0;

        private static readonly object SaveLogLocker = new object();

        #endregion

        #region Form

        public frmMain()
        {
            InitializeComponent();
            CollapseMenu(true);

            // Form
            this.Text = string.Empty;
            this.ControlBox = false;
            this.DoubleBuffered = true;
            //this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
            this.MaximizedBounds = Screen.PrimaryScreen.WorkingArea;

        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev;

            if (InitRun() == false)
            {
                return;
            }

            if (m_st_OptionData.TestMode == "1")
            {
                //lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + " [Auto Test] " + m_st_MCFData.SKU + " " + m_st_MESData.EID + " " + m_st_MESData.WorkOrder;
                lblTitleBar.Text = "[Auto Test] " + Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev;
            }
            else
            {
                //lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + " [Manual Test] " + m_st_MCFData.SKU + " " + m_st_MESData.EID + " " + m_st_MESData.WorkOrder;
                lblTitleBar.Text = "[Manual Test] " + Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev;
            }

            return;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            bgFlashWorkerCancel();
            KillFastboot();
            KillAdb();

            if (m_st_OptionData.TestMode == "1")
            {
                if (m_str_Model.Contains("CT40"))
                {
                    string strErrorMessage = "";
                    USBPlugInit(ref strErrorMessage);
                    ReleaseNI6001();
                    PLCRelease();
                }
                else if (m_str_Model.Contains("EDA51") || m_str_Model.Contains("EDA52") || m_str_Model.Contains("EDA56") || m_str_Model.Contains("EDA5S"))
                {
                    PLCRelease();
                }
            }
        }


        #endregion

        #region Menu

        private void PortMapping_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_b_Setting = true;

            if (m_st_OptionData.TestMode == "1")
            {
                timerAutoTest.Enabled = false;
                timerKillProcess.Enabled = false;

                if (m_timer_WatchDog != null)
                {
                    m_timer_WatchDog.Dispose();
                    m_timer_WatchDog = null;
                }
            }
            else
            {
                timerMonitor.Enabled = false;
                timerDeviceConnect.Enabled = false;
                timerKillProcess.Enabled = false;
            }

            frmSetupUSB frm = new frmSetupUSB();
            frm.m_str_Model = m_str_Model;
            frm.ShowDialog();
        }

        private void DeleteCOMArbiterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DeleteCOMNameArbiterReg() == false)
            {
                MessageBox.Show("Delete COM Name Arbiter Reg Fail.");
            }
            else
            {
                MessageBox.Show("Delete COMName Arbiter Reg Successfully.");
            }
        }

        private void HWSerNumEmulationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (HWSerNumEmulationReg() == false)
            {
                MessageBox.Show("HWSerNumEmulationReg Fail.");
            }
            else
            {
                MessageBox.Show("HWSerNumEmulationReg Successfully.");
            }
        }

        #endregion

        #region Timer

        private void timerCopyImage_Tick(object sender, EventArgs e)
        {
            if (m_b_Running == false)
            {
                if (m_b_RunReslut == true)
                {
                    // Image copy finished
                    timerCopyImage.Enabled = false;
                  
                    #region Copy Bat file from server to local

                    if (m_st_OptionData.FlashMode == FASTBOOTMODE)
                    {
                        string strErrorMessage = "";
                        DisplayMessage("Copy bat file from server to local.");
                        if (CopyBatFileFromServerToLocal(ref strErrorMessage) == false)
                        {
                            DisplayMessage("Failed to copy bat file from server." + strErrorMessage);
                            return;
                        }
                        DisplayMessage("Copy bat file from server to local successfully.");
                    }

                    #endregion

                    #region Copy Bat file

                    if (m_st_OptionData.FlashMode == FASTBOOTMODE)
                    {
                        string strErrorMessage = "";
                        DisplayMessage("Copy bat file to image path.");
                        if (CopyBatFile(ref strErrorMessage) == false)
                        {
                            DisplayMessage("Failed to copy bat file to image path." + strErrorMessage);
                            return;
                        }
                        DisplayMessage("Copy bat file to image path successfully.");
                    }

                    #endregion

                    if (m_b_Setting == true)
                    {
                        DisplayMessage("Re-run the test tool after config port mapping ......");
                        return;
                    }

                    if (m_st_OptionData.TestMode == "1")
                    {
                        // Auto Test
                        #region Timer

                        // 3s
                        m_timer_WatchDog = new System.Threading.Timer(Thread_Timer_WatchDog, null, 1000, 3000);

                        timerAutoTest.Interval = 5000;
                        timerAutoTest.Enabled = true;
                        timerAutoTest.Tick += new EventHandler(timerAutoTest_Tick);

                        timerKillProcess.Interval = 20000;
                        timerKillProcess.Enabled = true;
                        timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

                        DisplayMessage("Timer enabled sucessfully.");

                        #endregion
                    }
                    else
                    {
                        // Manual Test
                        #region Timer

                        timerMonitor.Interval = 10000;
                        timerMonitor.Enabled = true;
                        timerMonitor.Tick += new EventHandler(timerMonitorRun_Tick);

                        timerDeviceConnect.Interval = 15000;
                        timerDeviceConnect.Enabled = true;
                        timerDeviceConnect.Tick += new EventHandler(timerMonitorDeviceConnect_Tick);

                        timerKillProcess.Interval = 20000;
                        timerKillProcess.Enabled = true;
                        timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

                        DisplayMessage("Timer enabled sucessfully.");

                        #endregion
                    }
                }
                else
                {
                    DisplayMessage("Failed to copy image.");
                }
            }
            else
            {
                DisplayMessage("Copying ......");
            }
        }

        private void timerMonitorRun_Tick(object sender, EventArgs e)
        {
            timerMonitor.Enabled = false;

            //DisplayMessage("StartTime:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            MonitorDeviceByPhysicalAddress(PANEL_1);
            Dly(2);
            MonitorDeviceByPhysicalAddress(PANEL_2);
            Dly(2);
            MonitorDeviceByPhysicalAddress(PANEL_3);
            Dly(2);
            MonitorDeviceByPhysicalAddress(PANEL_4);
            Dly(2);

            //DisplayMessage("EndTime:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            timerMonitor.Enabled = true;
        }

        private void timerMonitorDeviceConnect_Tick(object sender, EventArgs e)
        {
            timerDeviceConnect.Enabled = false;

            MonitorDeviceConnected(PANEL_1);
            Dly(2);
            MonitorDeviceConnected(PANEL_2);
            Dly(2);
            MonitorDeviceConnected(PANEL_3);
            Dly(2);
            MonitorDeviceConnected(PANEL_4);
            Dly(2);

            timerDeviceConnect.Enabled = true;
        }

        private void timerKillProcess_Tick(object sender, EventArgs e)
        {
            timerKillProcess.Enabled = false;

            if (bgFlashWorker1.IsBusy == false && bgFlashWorker2.IsBusy == false && bgFlashWorker3.IsBusy == false && bgFlashWorker4.IsBusy == false)
            {
                KillFastboot();
                KillAdb();
            }

            timerKillProcess.Enabled = true;
        }

        #endregion

        #region Function

        private bool MonitorDeviceByPhysicalAddress(string strPanel)
        {
            try
            {
                USBEnumerator usbEnum = new USBEnumerator();

                string strPhysicalAddress = "";
                List<string> m_List_SN = new List<string>();
                string strErrorMessage = "";
                string strDeviceName = "";

                m_List_SN.Clear();
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                strDeviceName = m_st_OptionData.ADBDeviceName;

                if (usbEnum.GetSerianNumberByUSBDevicePhysicalAddress(strPhysicalAddress, strDeviceName, ref m_List_SN, ref strErrorMessage) == false)
                {
                    DisplayMessage(strErrorMessage);
                    return false;
                }

                if (m_List_SN.Count == 1)
                {
                    string strSN = m_List_SN[0].Trim().ToString();
                    if (CheckSN(strSN) == false)
                    {
                        DisplayMessage("Panel " + strPanel + " invalid SN." + strSN);
                        return false;
                    }
                    if (m_dic_UnitDevice[strPanel].SN == strSN)
                    {
                        // 同一个产品
                        string strStatus = m_dic_UnitDevice[strPanel].Status;
                        if (strStatus == "0")
                        {
                            // 未连接
                        }
                        else if (strStatus == "1")
                        {
                            // 连接上，进行中
                        }
                        else if (strStatus == "P")
                        {
                            // 成功，不可以重新开始（只能更换USB口）
                        }
                        else if (strStatus == "F")
                        {
                            // 失败，不可以重新开始（只能更换USB口）
                            DisplayMessage("Panel " + strPanel + " change one unit to test.");
                        }
                    }
                    else
                    {
                        //DisplayMessage(m_dic_UnitDeviceInfo[strPanel].SN + "  >>  " + m_List_SN[0].Trim().ToString());

                        // 不同产品
                        #region STATUS_CONNECTED

                        UnitDeviceInfo stUnit = new UnitDeviceInfo();
                        stUnit.Panel = strPanel;
                        stUnit.PhysicalAddress = strPhysicalAddress;
                        stUnit.SN = strSN;
                        stUnit.Status = "1";
                        m_dic_UnitDevice[strPanel] = stUnit;

                        DisplayUnit(strPanel, strSN, Color.White);
                        DisplayUnitStatus(strPanel, STATUS_CONNECTED, Color.MediumSpringGreen);

                        #endregion

                        RunFlashWorker(strPanel);
                    }
                }
                else
                {
                    if (m_List_SN.Count > 1)
                    {
                        DisplayMessage("Panel " + strPanel + " mmonitor more unit:" + m_List_SN.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayMessage("Monitor device SN Exception:" + ex.Message);
                return false;
            }

            return true;
        }

        private bool MonitorDeviceConnected(string strPanel)
        {
            try
            {
                #region FASTBOOTMODE

                if (m_st_OptionData.FlashMode == FASTBOOTMODE)
                {
                    if (MonitorADBConnected(strPanel) == false)
                    {
                        string strStatus = m_dic_UnitDevice[strPanel].Status;
                        if (strStatus == "P" || strStatus == "F")
                        {
                            // 测试完成设备会重启，不检测断开
                        }
                        else
                        {
                            UnitDeviceInfo stUnit = new UnitDeviceInfo();
                            stUnit.Panel = m_dic_UnitDevice[strPanel].Panel;
                            stUnit.PhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                            stUnit.SN = m_dic_UnitDevice[strPanel].SN;
                            stUnit.Status = "0";
                            m_dic_UnitDevice[strPanel] = stUnit;

                            DisplayUnit(strPanel, m_dic_UnitDevice[strPanel].SN, Color.White);
                            DisplayUnitStatus(strPanel, STATUS_DISCONNECTED, Color.Orange);
                        }
                    }
                    else
                    {
                    }
                }

                #endregion

                #region EDLMODE

                if (m_st_OptionData.FlashMode == EDLMODE)
                {
                    if (MonitorADBConnected(strPanel) == false && MonitorPortConnected(strPanel) == false)
                    {
                        string strStatus = m_dic_UnitDevice[strPanel].Status;
                        if (strStatus == "P" || strStatus == "F")
                        {
                            // 测试完成设备会重启，不检测断开
                        }
                        else
                        {
                            UnitDeviceInfo stUnit = new UnitDeviceInfo();
                            stUnit.Panel = m_dic_UnitDevice[strPanel].Panel;
                            stUnit.PhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                            stUnit.SN = m_dic_UnitDevice[strPanel].SN;
                            stUnit.Status = "0";
                            m_dic_UnitDevice[strPanel] = stUnit;

                            DisplayUnit(strPanel, m_dic_UnitDevice[strPanel].SN, Color.White);
                            DisplayUnitStatus(strPanel, STATUS_DISCONNECTED, Color.Orange);
                        }
                    }
                    else
                    {
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Monitor device connected Exception:" + strr);
                return false;
            }

            return true;
        }

        private bool MonitorADBConnected(string strPanel)
        {
            try
            {
                USBEnumerator usbEnum = new USBEnumerator();
                string strPhysicalAddress = "";
                string strErrorMessage = "";
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                bool bRes = false;
                bRes = usbEnum.CheckADBConnectByUSBDevicePhysicalAddress(strPhysicalAddress, ref strErrorMessage);
                if (bRes == false)
                {
                    // 连接失败
                }
                else
                {
                    // 连接成功
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Monitor ADB connected Exception:" + strr);
                return false;
            }

            return true;
        }

        private bool MonitorPortConnected(string strPanel)
        {
            try
            {
                USBEnumerator usbEnum = new USBEnumerator();
                string strPhysicalAddress = "";
                string strErrorMessage = "";
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                bool bRes = false;
                string strPortName = m_st_OptionData.QDLoaderPortName;
                bRes = usbEnum.CheckPortConnectByUSBDevicePhysicalAddress(strPhysicalAddress, strPortName, ref strErrorMessage);
                if (bRes == false)
                {
                    // 连接失败
                }
                else
                {
                    // 连接成功
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Monitor QDLoader connected Exception:" + strr);
                return false;
            }

            return true;
        }

        private bool MonitorDeviceByPhysicalAddress_AutoTest(string strPanel, ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";

                USBEnumerator usbEnum = new USBEnumerator();
                string strPhysicalAddress = "";
                List<string> m_List_SN = new List<string>();
                string strDeviceName = "";

                m_List_SN.Clear();
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                strDeviceName = m_st_OptionData.ADBDeviceName;

                UnitDeviceInfo stUnit1 = new UnitDeviceInfo();
                stUnit1.Panel = strPanel;
                stUnit1.PhysicalAddress = strPhysicalAddress;
                stUnit1.SN = "";
                stUnit1.SKU = "";
                stUnit1.Model = "";
                stUnit1.WorkOrder = "";
                stUnit1.Status = "0";
                m_dic_UnitDevice[strPanel] = stUnit1;

                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = usbEnum.GetSerianNumberByUSBDevicePhysicalAddress(strPhysicalAddress, strDeviceName, ref m_List_SN, ref strErrorMessage);
                    if (bRes == false)
                    {
                        strErrorMessage = "Failed to monitor device get SN." + strErrorMessage;
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                    if (m_List_SN.Count == 0)
                    {
                        strErrorMessage = "Failed to monitor device.";
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                    else if (m_List_SN.Count == 1)
                    {
                        #region RunTest

                        string strSN = m_List_SN[0].Trim().ToString();
                        if (CheckSN(strSN) == false)
                        {
                            strErrorMessage = "Failed to monitor device check SN." + strSN;
                            bRes = false;
                            Dly(1);
                            continue;
                        }

                        UnitDeviceInfo stUnit = new UnitDeviceInfo();
                        stUnit.Panel = strPanel;
                        stUnit.PhysicalAddress = strPhysicalAddress;
                        stUnit.SN = strSN;
                        stUnit.Status = "1";
                        m_dic_UnitDevice[strPanel] = stUnit;

                        DisplayUnit(strPanel, strSN, Color.White);
                        DisplayUnitStatus(strPanel, STATUS_CONNECTED, Color.MediumSpringGreen);

                        RunFlashWorker(strPanel);   // Async RunTest

                        strErrorMessage = "";
                        bRes = true;
                        break;

                        #endregion
                    }
                    else
                    {
                        strErrorMessage = "Failed to monitor device SN count:" + m_List_SN.Count.ToString();
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                }
                if (bRes == false)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Failed to monitor device exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool RunFlashWorker(string strPanel)
        {
            try
            {
                if (strPanel == PANEL_1)
                {
                    if (!bgFlashWorker1.IsBusy)
                    {
                        bgFlashWorker1.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker1.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker1.IsBusy)
                        {
                            bgFlashWorker1.RunWorkerAsync();
                        }
                    }
                }
                else if (strPanel == PANEL_2)
                {
                    if (!bgFlashWorker2.IsBusy)
                    {
                        bgFlashWorker2.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker2.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker2.IsBusy)
                        {
                            bgFlashWorker2.RunWorkerAsync();
                        }
                    }
                }
                else if (strPanel == PANEL_3)
                {
                    if (!bgFlashWorker3.IsBusy)
                    {
                        bgFlashWorker3.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker3.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker3.IsBusy)
                        {
                            bgFlashWorker3.RunWorkerAsync();
                        }
                    }
                }
                else if (strPanel == PANEL_4)
                {
                    if (!bgFlashWorker4.IsBusy)
                    {
                        bgFlashWorker4.RunWorkerAsync();
                    }
                    else
                    {
                        bgFlashWorker4.CancelAsync();
                        Thread.Sleep(1000);
                        if (!bgFlashWorker4.IsBusy)
                        {
                            bgFlashWorker4.RunWorkerAsync();
                        }
                    }
                }
                else
                {
                    DisplayMessage("Failed to flash worker invalid pannel.");
                }
            }
            catch(Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Failed to exception." + strr);
                return false;
            }

            return true;
        }

        private bool CancelFlashWorker(string strPanel)
        {
            try
            {
                if (strPanel == PANEL_1)
                {
                    if (bgFlashWorker1.IsBusy)
                    {
                        bgFlashWorker1.CancelAsync();
                    }
                }
                else if (strPanel == PANEL_2)
                {
                    if (bgFlashWorker2.IsBusy)
                    {
                        bgFlashWorker2.CancelAsync();
                    }
                }
                else if (strPanel == PANEL_3)
                {
                    if (bgFlashWorker3.IsBusy)
                    {
                        bgFlashWorker3.CancelAsync();
                    }
                }
                else if (strPanel == PANEL_4)
                {
                    if (bgFlashWorker4.IsBusy)
                    {
                        bgFlashWorker4.CancelAsync();
                    }
                }
                else
                {
                    DisplayMessage("Failed to flash worker invalid pannel.");
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("Failed to exception." + strr);
                return false;
            }

            return true;
        }

        private bool RunTest(string strPanel)
        {
            bool bRes = true;
            bool bUpdateMDCS = true;

            try
            {
                string strErrorMessage = "";
                double dTotalTestTime = 0;
                long startTime = clsUtil.StartTimeInTicks();

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Start Test Time:" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); });

                #region STATUS_FLASHING

                this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_FLASHING, Color.YellowGreen); });
                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.Status = "1";
                m_dic_UnitDevice[strPanel] = stUnit;

                #endregion

                #region InitMDCSData

                InitMDCSData(strPanel);

                #endregion

                #region Clear Pannel

                this.Invoke((MethodInvoker)delegate { ClearUnitLog(strPanel); });

                #endregion

                #region Get SKU Property

                if (bRes == true)
                {
                    bRes = TestGetSKU(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to get SKU property." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                
                }

                #endregion

                #region Get WorkOrder Property



                #endregion

                #region Get EID Property



                #endregion




                #region Get ScanSheet


                #endregion



                #region Test Check Pre Station Result

                if (bRes == true)
                {
                    bRes = TestCheckPreStationResult(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to check pre station result." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Test Check ManualBITResult

                if (bRes == true)
                {
                    bRes = TestCheckManualBITResult(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to check ManualBITResult." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Test Check SKU

                if (bRes == true)
                {
                    bRes = TestCheckSKU(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to check SKU." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Test Check SDCard

                if (bRes == true)
                {
                    bRes = TestCheckSDCard(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        strErrorMessage = "Failed to check SDCard." + strErrorMessage;
                    }
                    else
                    {
                        bRes = true;
                    }
                }

                #endregion

                #region Test Keybox (write or check keybox whether exist)

                if (bRes == true)
                {
                    bRes = TestKeybox(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                    }
                    else
                    {
                        bRes = true;
                    }
                }

                #endregion

                #region Test SentienceKey (Write SentienceKey)

                if (bRes == true)
                {
                    bRes = TestSentienceKey(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                    }
                    else
                    {
                        bRes = true;
                    }
                }

                #endregion

                #region Test Flash

                if (bRes == true)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Flash OS."); });

                    if (m_st_OptionData.FlashMode == FASTBOOTMODE)
                    {
                        bRes = TestFastbootFlash(strPanel, ref strErrorMessage);
                    }
                    else if (m_st_OptionData.FlashMode == EDLMODE)
                    {
                        bRes = TestEDLFlash(strPanel, ref strErrorMessage);       
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Invalid Flash Mode."); });
                        bRes = false;
                    }

                    if (bRes == false)
                    {
                        strErrorMessage = "Failed to flash." + strErrorMessage;
                        bRes = false;
                    }
                    else
                    {
                        strErrorMessage = "";
                        bRes = true;
                    }
                }

                #endregion

                #region Wait Reboot

                if (bRes == true)
                {
                    int iWaitRebootTime = int.Parse(m_st_OptionData.Reboot_WaitTime);
                    if (iWaitRebootTime > 0)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Waiting to reboot:" + iWaitRebootTime.ToString() + "s."); });
                        Dly(iWaitRebootTime);
                    }
                }

                #endregion

                #region MDCS Data

                dTotalTestTime = clsUtil.ElapseTimeInSeconds(startTime);

                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];

                objSaveData.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData.TestRecord.SN = m_dic_UnitDevice[strPanel].SN;
                objSaveData.TestRecord.Model = m_str_Model;
                objSaveData.TestRecord.SKU = m_st_MCFData.SKU;
                objSaveData.TestRecord.OSVersion = m_st_MCFData.OSVersion;
                objSaveData.TestRecord.TestTotalTime = dTotalTestTime;

                if (bRes == true)
                {
                    objSaveData.TestResult.TestPassed = true;
                    objSaveData.TestResult.TestFailCode = 0;
                    objSaveData.TestResult.TestFailMessage = "";
                    objSaveData.TestResult.TestStatus = "";
                }
                else
                {
                    objSaveData.TestResult.TestPassed = false;
                    objSaveData.TestResult.TestFailCode = 2050;
                    objSaveData.TestResult.TestFailMessage = strErrorMessage;
                    objSaveData.TestResult.TestStatus = "";
                }

                m_dic_TestSaveData[strPanel] = objSaveData;

                #endregion

                #region Save MDCS

                if (m_st_OptionData.MDCSEnable == "1")
                {
                    if (bUpdateMDCS == true)
                    {
                        bool bSaveMDCS = false;
                        bSaveMDCS = SaveMDCS(objSaveData);
                        if (bSaveMDCS == false)
                        {
                            bSaveMDCS = SaveMDCS(objSaveData);
                        }
                        if (bSaveMDCS == false)
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Save MDCS Fail."); });
                            if (objSaveData.TestResult.TestPassed == true)
                            {
                                objSaveData.TestResult.TestPassed = false;
                                objSaveData.TestResult.TestFailCode = 2050;
                                objSaveData.TestResult.TestFailMessage = "Failed to save MDCS.";
                                objSaveData.TestResult.TestStatus = "";
                                m_dic_TestSaveData[strPanel] = objSaveData;
                            }
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Save MDCS Successfully."); });
                        }

                        if (bSaveMDCS == false)
                        {
                            bRes = false;
                        }
                    }
                }

                #endregion 

                #region Upload MES

                if (m_st_OptionData.MES_Enable == "1")
                {
                    if (bUpdateMDCS == true)
                    {
                        bool bUploadMES = false;
                        string strTempErrorMsg = "";
                        //public static bool MESUploadData(string strEID, string strStation, string strWorkOrder, string strSN, bool bPassFailFlag, ref string strErrorMessage)
                        //bUploadMES = MESUploadData(objSaveData, ref strTempErrorMsg);

                        if (bUploadMES == false)
                        {
                            //bUploadMES = MESUploadData(objSaveData, ref strTempErrorMsg);
                        }
                        if (bUploadMES == false)
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Upload MES Fail." + strTempErrorMsg); });

                            if (objSaveData.TestResult.TestPassed == true)
                            {
                                objSaveData.TestResult.TestPassed = false;
                                objSaveData.TestResult.TestFailCode = 2050;
                                objSaveData.TestResult.TestFailMessage = "Failed to upload MES.";
                                objSaveData.TestResult.TestStatus = "";
                                m_dic_TestSaveData[strPanel] = objSaveData;
                            }
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Upload MES Successfully."); });
                        }

                        if (bUploadMES == false)
                        {
                            bRes = false;
                        }
                    }
                }

                #endregion

                #region Save Test Report

                this.Invoke((MethodInvoker)delegate { SaveUnitTestReport(strPanel); });

                #endregion

                #region Test End

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Error Message:" + strErrorMessage); });
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "End Test Time:" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); });
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Total Test Time:" + dTotalTestTime.ToString() + " s."); });

                #endregion  
            }
            catch (Exception ex)
            {
                string strr = ex.Message;

                bRes = false;
                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];
                if (objSaveData.TestResult.TestPassed == true)
                {
                    objSaveData.TestResult.TestPassed = false;
                    objSaveData.TestResult.TestFailCode = 2050;
                    objSaveData.TestResult.TestFailMessage = "RunTest Exception." + strr;
                    objSaveData.TestResult.TestStatus = "";
                    m_dic_TestSaveData[strPanel] = objSaveData;
                }

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "RunTest Exception:" + strr); });
                DisplayMessage("RunTest Exception:" + strr);
                return false;
            }
            finally
            {
                #region STATUS_SUCCESSED / STATUS_FAILED

                if (bRes == true)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_SUCCESSED, Color.Green); });
                    UnitDeviceInfo stUnit1 = m_dic_UnitDevice[strPanel];
                    stUnit1.Status = "P";
                    m_dic_UnitDevice[strPanel] = stUnit1;
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_FAILED, Color.Red); });
                    UnitDeviceInfo stUnit2 = m_dic_UnitDevice[strPanel];
                    stUnit2.Status = "F";
                    m_dic_UnitDevice[strPanel] = stUnit2;
                }

                #endregion

                #region Auto Test

                if (m_st_OptionData.TestMode == "1")
                {
                    string strTempErrorMessage = "";
                    if (m_str_Model.Contains("CT40"))
                    {
                        if (UnitLocationCheckAction(strPanel, "0", ref strTempErrorMessage) == true)
                        {
                            if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.HAVEPRODUCT, ref strTempErrorMessage) == false)
                            {
                                SaveLogFile("Panel:" + strPanel + " FeedbackStatus HAVEPRODUCT fail." + strTempErrorMessage);
                            }
                        }
                        else
                        {
                            if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.ERROR, ref strTempErrorMessage) == false)
                            {
                                SaveLogFile("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strTempErrorMessage);
                            }
                        }

                        if (USBPlugAction(strPanel, "0", ref strTempErrorMessage) == false)
                        {
                            SaveLogFile("Panel:" + strPanel + " USB Plug Out fail.");
                            if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.ERROR, ref strTempErrorMessage) == false)
                            {
                                SaveLogFile("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strTempErrorMessage);
                            }
                        }
                        else
                        {
                        }
                    }

                    // Feedback Final RunResult CT40 and EDA51
                    if (FeedbackResult(strPanel, ref strTempErrorMessage) == false)
                    {
                        SaveLogFile("Panel:" + strPanel + " FeedbackResult fail." + strTempErrorMessage);
                        if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.ERROR, ref strTempErrorMessage) == false)
                        {
                            SaveLogFile("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strTempErrorMessage);
                        }
                    }

                    //m_dic_COMPort[strPanel] = "";   //Clear ComPort Record When Disconnect.

                    if (strTempErrorMessage != "")
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Run end error:" + strTempErrorMessage); });
                    }
                }

                #endregion

                m_dic_TestStatus[strPanel] = false;
            }

            return true;
        }

        private bool TestCheckPreStationResult(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_st_OptionData.MDCSPreStationResultCheck == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check Pre Station Result."); });

                    string str_SN = m_dic_UnitDevice[strPanel].SN;
                    if (str_SN == "")
                    {
                        strErrorMessage = "Invalid SN." + str_SN;
                        return false;
                    }
                    string str_ErrorMessage = "";
                    clsMDCS obj_SaveMDCS = new clsMDCS();
                    obj_SaveMDCS.ServerName = m_st_OptionData.MDCSURL;
                    obj_SaveMDCS.DeviceName = m_st_OptionData.MDCSPreStationDeviceName;
                    obj_SaveMDCS.UseModeProduction = true;

                    bool bRes = false;
                    string strValue = "";
                    for (int i = 0; i < 3; i++)
                    {
                        bRes = obj_SaveMDCS.GetMDCSVariable(m_st_OptionData.MDCSPreStationDeviceName, m_st_OptionData.MDCSPreStationVarName, str_SN, ref strValue, ref str_ErrorMessage);
                        if (bRes == false)
                        {
                            bRes = false;
                            strErrorMessage = "GetMDCSVariable fail.";
                            Dly(2);
                            continue;
                        }
                        else
                        {
                            if (strValue != m_st_OptionData.MDCSPreStationVarValue)
                            {
                                bRes = false;
                                strErrorMessage = "Compare value fail." + strValue;
                                Dly(1);
                                continue;
                            }
                            else
                            {
                                bRes = true;
                                strErrorMessage = "";
                                break;
                            } 
                        }
                    }
                    if (bRes == false)
                    {
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check Pre Station MDCS Test Result sucessfully."); });

                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to Check Pre Station MDCS Test Result."); });
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestCheckPreStationResult Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestGetSKU(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Get SKU Property."); });

                string strSKU = "";
                if (GetSKUProperty(strPanel, ref strSKU, ref strErrorMessage) == false)
                {
                    return false;
                }

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get SKU: " + strSKU); });
               
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestCheckManualBITResult Exception:" + strr;
                return false;
            }

            return true;
        }


        private bool TestCheckSKU(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_st_OptionData.SKUCheckEnable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check SKU."); });

                    if (CheckSKU(strPanel, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check SKU sucessfully."); });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to Test Check SKU."); });
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestCheckSKU Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestCheckSDCard(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_st_OptionData.SDCard_Enable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check SDCard."); });

                    if (CheckSDCard(strPanel, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check SDCard sucessfully."); });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to Test Check SDCard."); });
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestCheckSDCard Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestCheckManualBITResult(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_st_OptionData.CheckManualBITResult_Enable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check ManualBITResult."); });

                    if (CheckManualBITResult(strPanel, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check ManualBITResult sucessfully."); });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to Test Check ManualBITResult."); });
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestCheckManualBITResult Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestKeybox(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_st_OptionData.KeyboxEnable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Keybox."); });

                    if (WriteKeybox(strPanel, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Keybox successfully."); });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to Test Check Keybox."); });
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestKeybox Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestSentienceKey(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_st_OptionData.SentienceKeyEnable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test SentienceKey."); });

                    if (WriteSentienceKey(strPanel, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test SentienceKey successfully."); });
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to Test SentienceKey."); });

                    #region Check SentienceKey
                    //string strCmd = "adb shell ls /mnt/vendor/persist/data/";


                    #endregion

                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "TestSentienceKey Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestFastbootFlash(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                
                #region ADB Reboot Bootloader

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "ADB reboot bootloader."); });
                if (ADBRebootBootloader(strPanel, ref strErrorMessage) == false)
                {
                    return false;
                }

                Dly(3);

                #endregion

                #region Check ADB Connected

                //this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Check connected after reboot."); });

                //USBEnumerator usbEnum = new USBEnumerator();
                //bRes = false;
                //for (int i = 0; i < 10; i++)
                //{
                //    bRes = usbEnum.CheckADBConnectByUSBDevicePhysicalAddress(strPhysicalAddress, ref strErrorMessage);
                //    if (bRes == false)
                //    {
                //        Dly(3);
                //        bRes = false;
                //        continue;
                //    }

                //    bRes = true;
                //    break;
                //}
                //if (bRes == false)
                //{
                //    strErrorMessage = "Failed to enumerator ADB port after reboot.";
                //    return false;
                //}
                //Dly(1);

                #endregion

                #region Check Fastboot Device

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Check fastboot device."); });
                if (FastbootDevice(strPanel, ref strErrorMessage) == false)
                {
                    return false;
                }

                #endregion

                #region Flash OS

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Flash OS."); });
                string strTemp = "";
                for (int i = 0; i < 3; i++)
                {
                    bRes = FlashFastboot(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        if (FastbootDevice(strPanel, ref strTemp) == false)
                        {
                            strErrorMessage = strErrorMessage + strTemp;
                            bRes = false;
                            break;
                        }
                        else
                        {
                            Dly(1);
                            bRes = false;
                            continue;
                        }
                    }
                    else
                    {
                        strErrorMessage = "";
                        break;
                    }
                }
                if (bRes == false)
                {
                    return false;
                }
 
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Flash OS successfully."); });

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Fastboot Flash Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool TestEDLFlash(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                string strCOMPort = "";

                #region ADB Reboot EDL

                for (int i = 0; i < 5; i++)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "ADB Reboot EDL."); });
                    if (ADBRebootEDL(strPanel, ref strErrorMessage) == false)
                    {
                        Dly(2);
                        bRes = false;
                        break;
                    }
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "ADB Reboot EDL sucessfuly."); });
                    Dly(3);
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get COMPort after reboot."); });
                    if (GetCOMPort(strPanel, ref strCOMPort, ref strErrorMessage) == false)
                    {
                        bRes = false;
                        Dly(10);
                        continue;
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "COM Port:" + strCOMPort); });
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    return false;
                }
                Dly(10);

                #endregion

                #region QFIL

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Flash OS."); });
                if (FlashEDL(strPanel, strCOMPort, ref strErrorMessage) == false)
                {
                    strErrorMessage = "EDL fail." + strErrorMessage;
                    return false;
                }
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Flash OS successfully."); });

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "EDL Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool DeleteQFILConfigFile(string strCOMPORT, ref string strErrorMessage)
        {
            // C:\Users\ch3u-dcdgenl2\AppData\Roaming\Qualcomm\QFIL
            try
            {
                bool bRes = false;
                string strQFILDir = "";
                strQFILDir = "C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Qualcomm\\QFIL";

                #region Delete QFIL.config

                bRes = false;
                string strQFILConfigFile = strQFILDir + "\\" + "QFIL.config";
                for (int i = 0; i < 20; i++)
                {
                    if (File.Exists(strQFILConfigFile) == true)
                    {
                        File.Delete(strQFILConfigFile);
                        Dly(1);
                        if (File.Exists(strQFILConfigFile) == true)
                        {
                            bRes = false;
                            Dly(1);
                            continue;
                        }
                        else
                        {
                            bRes = true;
                            break;
                        }
                    }
                    else
                    {
                        bRes = true;
                        break;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "Delete QFIL.config." + strQFILConfigFile;
                    return false;
                }

                #endregion

                #region Delete COMPORT_XX

                bRes = false;
                string strCOMPORTDir = strQFILDir + "\\" + "COMPORT_" + strCOMPORT;
                for (int i = 0; i < 20; i++)
                {
                    if (Directory.Exists(strCOMPORTDir) == true)
                    {
                        if (DeleteDirectory(strCOMPORTDir, ref strErrorMessage) == false)
                        {
                            bRes = false;
                            Dly(1);
                            continue;
                        }

                        Dly(0.5);

                        Directory.Delete(strCOMPORTDir);

                        Dly(0.5);

                        if (Directory.Exists(strCOMPORTDir) == true)
                        {
                            bRes = false;
                            Dly(1);
                            continue;
                        }
                        else
                        {
                            bRes = true;
                            break;
                        }
                    }
                    else
                    {
                        bRes = true;
                        break;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "Delete COMPORT_ " + strCOMPORT + " " + strErrorMessage;
                    return false;
                }

                #endregion  
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private void OnDataReceived(object Sender, DataReceivedEventArgs e)
        {
            //if (e.Data != null)
            //{
            //}

            return;
        }

        private bool ExcuteCmd(string strCmd, int iTimeOut, ref string strResult, ref string strErrorMessage)
        {
            if (strCmd != null && !strCmd.Equals(""))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"C:\Windows\System32\cmd.exe";
                startInfo.Arguments = " /c " + strCmd;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;

                try
                {
                    if (process.Start())
                    {
                        string line = "";
                        StreamReader streamReader = process.StandardOutput;
                        StringBuilder strBuilder = new StringBuilder();
                        while (((line = streamReader.ReadLine()) != null))
                        {
                            if (line != "")
                            {
                                strBuilder.Append(line);
                            }
                        }
                        strResult = strBuilder.ToString();

                        if (iTimeOut == 0)
                        {
                            process.WaitForExit();
                        }
                        else
                        {
                            process.WaitForExit(iTimeOut);
                        }
                    }
                }
                catch (Exception ex)
                {
                    strErrorMessage = "Exception:" + ex.ToString();
                    return false;
                }
                finally
                {
                    if (process != null)
                    {
                        process.Close();
                    }
                }
            }

            return true;
        }

        private bool GenerateBatFile(string strBatDir, string strBatFile, string str_Content)
        {
            string str_FilePathName = strBatDir + "\\" + strBatFile;

            if (System.IO.Directory.Exists(strBatDir) == false)
            {
                System.IO.Directory.CreateDirectory(strBatDir);
            }
            if (System.IO.File.Exists(str_FilePathName))
            {
                try
                {
                    System.IO.File.Delete(str_FilePathName);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                    return false;
                }
                Dly(1);
            }

            #region Save File

            FileStream fs = null;
            try
            {
                fs = new FileStream(str_FilePathName, FileMode.Create);
                byte[] data = System.Text.Encoding.Default.GetBytes(str_Content);
                fs.Write(data, 0, data.Length);
                fs.Flush();
                fs.Close();
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }

            #endregion

            Dly(1);

            if (System.IO.File.Exists(str_FilePathName) == false)
            {
                return false;
            }
            return true;
        }

        private bool CopyBatFile(ref string strErrorMessage)
        {
            string strSrcFile = "";
            string strDestFile = "";

            try
            {
                if (m_st_OptionData.FlashMode == FASTBOOTMODE)
                {
                    string strSrcDir = Application.StartupPath;

                    #region flash bat

                    strSrcFile = strSrcDir + "\\" + m_str_Model + "\\" + m_st_OptionData.FASTBOOTBatFile;
                    strDestFile = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.FASTBOOTBatFile;
                    if (CopyFile(strSrcFile, strDestFile, true, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    #endregion

                    #region adb.exe

                    strSrcFile = strSrcDir + "\\" + "BAT" + "\\" + "adb.exe";
                    strDestFile = m_st_OptionData.ImageLocalPath + "\\" + "adb.exe";
                    if (File.Exists(strSrcFile) == false)
                    {
                        strErrorMessage = "Failed to file exist." + strSrcFile;
                        return false;
                    }
                    if (File.Exists(strDestFile) == false)
                    {
                        if (CopyFile(strSrcFile, strDestFile, true, ref strErrorMessage) == false)
                        {
                            return false;
                        }
                    }

                    #endregion

                    #region AdbWinApi.dll

                    strSrcFile = strSrcDir + "\\" + "BAT" + "\\" + "AdbWinApi.dll";
                    strDestFile = m_st_OptionData.ImageLocalPath + "\\" + "AdbWinApi.dll";
                    if (File.Exists(strSrcFile) == false)
                    {
                        strErrorMessage = "Failed to file exist." + strSrcFile;
                        return false;
                    }
                    if (File.Exists(strDestFile) == false)
                    {
                        if (CopyFile(strSrcFile, strDestFile, true, ref strErrorMessage) == false)
                        {
                            return false;
                        }
                    }

                    #endregion

                    #region AdbWinUsbApi.dll

                    strSrcFile = strSrcDir + "\\" + "BAT" + "\\" + "AdbWinUsbApi.dll";
                    strDestFile = m_st_OptionData.ImageLocalPath + "\\" + "AdbWinUsbApi.dll";
                    if (File.Exists(strSrcFile) == false)
                    {
                        strErrorMessage = "Failed to file exist." + strSrcFile;
                        return false;
                    }
                    if (File.Exists(strDestFile) == false)
                    {
                        if (CopyFile(strSrcFile, strDestFile, true, ref strErrorMessage) == false)
                        {
                            return false;
                        }
                    }

                    #endregion

                    #region fastboot.exe

                    strSrcFile = strSrcDir + "\\" + "BAT" + "\\" + "fastboot.exe";
                    strDestFile = m_st_OptionData.ImageLocalPath + "\\" + "fastboot.exe";
                    if (File.Exists(strSrcFile) == false)
                    {
                        strErrorMessage = "Failed to file exist." + strSrcFile;
                        return false;
                    }
                    if (File.Exists(strDestFile) == false)
                    {
                        if (CopyFile(strSrcFile, strDestFile, true, ref strErrorMessage) == false)
                        {
                            return false;
                        }
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Failed to exception." + strr;
                return false;
            }

            return true;
        }

        private bool CopyBatFileFromServerToLocal(ref string strErrorMessage)
        {
            try
            {
                if (m_st_OptionData.FlashMode == FASTBOOTMODE)
                {
                    if (m_st_OptionData.FASTBOOTEnable == "1")
                    {
                        string strServerFile = m_st_OptionData.FASTBOOTBatServerPath + "\\" + m_st_OptionData.FASTBOOTBatFile;
                        string strLocalFile = Application.StartupPath + "\\" + m_str_Model + "\\" + m_st_OptionData.FASTBOOTBatFile;
                        if (CopyFile(strServerFile, strLocalFile, true, ref strErrorMessage) == false)
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Failed to exception." + strr;
                return false;
            }

            return true;
        }

        private bool SaveMDCS()
        {
            try
            {
                if (m_st_OptionData.MDCSEnable == "1")
                {
                    //string str_ErrorMessage = "";
                    //clsMDCS obj_SaveMDCS = new clsMDCS();
                    //obj_SaveMDCS.ServerName = m_st_OptionData.MDCSURL;
                    //obj_SaveMDCS.DeviceName = m_st_OptionData.MDCSDeviceName;
                    //obj_SaveMDCS.UseModeProduction = true;
                    //obj_SaveMDCS.p_TestData = m_st_TestSaveData;
                    //bool bRes = false;
                    //for (int i = 0; i < 5; i++)
                    //{
                    //    bRes = obj_SaveMDCS.SendMDCSData(ref str_ErrorMessage);
                    //    if (bRes == false)
                    //    {
                    //        Dly(1);
                    //        continue;
                    //    }
                    //    bRes = true;
                    //    break;
                    //}
                    //if (bRes == false)
                    //{
                    //    return false;
                    //}
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool SaveMDCS(TestSaveData objSaveData)
        {
            try
            {
                if (m_st_OptionData.MDCSEnable == "1")
                {
                    string str_ErrorMessage = "";
                    clsMDCS obj_SaveMDCS = new clsMDCS();
                    obj_SaveMDCS.ServerName = m_st_OptionData.MDCSURL;
                    obj_SaveMDCS.DeviceName = m_st_OptionData.MDCSDeviceName;
                    obj_SaveMDCS.UseModeProduction = true;
                    obj_SaveMDCS.p_TestData = objSaveData;

                    bool bRes = false;
                    for (int i = 0; i < 5; i++)
                    {
                        bRes = obj_SaveMDCS.SendMDCSData(ref str_ErrorMessage);
                        if (bRes == false)
                        {
                            Dly(1);
                            continue;
                        }
                        bRes = true;
                        break;
                    }
                    if (bRes == false)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool KillFastboot()
        {
            try
            {
                clsExecProcess objprocess = new clsExecProcess();
                string strProcess = "fastboot";
                if (objprocess.KillProcess(strProcess) == false)
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

        private bool KillAdb()
        {
            try
            {
                clsExecProcess objprocess = new clsExecProcess();
                string strProcess = "adb";
                if (objprocess.KillProcess(strProcess) == false)
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

        private bool DeleteCOMNameArbiterReg()
        {
            try
            {
                //System.Diagnostics.Process.Start("regedit.exe", "/s reg.reg");
                string strReqName = Application.StartupPath + "\\" + "DeleteCOMNameArbiter.reg";
                if (File.Exists(strReqName) == false)
                {
                    return false;
                }
                Process.Start("regedit.exe", "/s " + strReqName);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool HWSerNumEmulationReg()
        {
            try
            {
                string strReqName = Application.StartupPath + "\\" + "HWSerNumEmulation.reg";
                if (File.Exists(strReqName) == false)
                {
                    return false;
                }
                Process.Start("regedit.exe", "/s " + strReqName);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckSN(string strSN)
        {
            if (strSN.Length != 10)
            {
                return false;
            }
            if (strSN == "0000000000")
            {
                return false;
            }

            return true;
        }

        private bool ExcuteBat(string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, ref string strErrorMessage)
        {
            strErrorMessage = "";

            Process process = null;
            ProcessStartInfo startInfo = null;

            if (strBatParameter != "")
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile, strBatParameter);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.Arguments = strBatParameter;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }
            else
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }

            try
            {
                if (process.Start())
                {
                    if (iTimeOut == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(iTimeOut);
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Bat Exception:" + strr;
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return true;
        }

        private bool ExcuteBat(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, string strSearchResult, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strOutput = "";

            Process process = null;
            ProcessStartInfo startInfo = null;

            if (strBatParameter != "")
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile, strBatParameter);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.Arguments = strBatParameter;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }
            else
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }

            try
            {
                if (process.Start())
                {
                    #region StandardOutput OutputDataReceived

                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        string strData = e.Data;
                        if (!String.IsNullOrEmpty(strData))
                        {
                            strOutput += strData;
                            this.Invoke((MethodInvoker)delegate
                            {
                                DisplayUnitLog(strPanel, strData);
                            });
                        }
                    });

                    process.BeginOutputReadLine();

                    if (iTimeOut == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(iTimeOut);
                    }

                    #endregion
                }

                #region Check Result

                // 输出有时会延迟
                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = strOutput.Contains(strSearchResult);
                    if (bRes == true)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(500);
                        bRes = false;
                        continue;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "Check bat result:" + strSearchResult;
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Bat Exception:" + strr;
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return true;
        }

        private bool ExcuteBat_Fastboot(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, string strSearchResult, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strOutput = "";

            Process process = null;
            ProcessStartInfo startInfo = null;

            if (strBatParameter != "")
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile, strBatParameter);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.Arguments = strBatParameter;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }
            else
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }

            try
            {
                if (process.Start())
                {
                    #region StandardOutput OutputDataReceived

                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        string strData = e.Data;
                        if (!String.IsNullOrEmpty(strData))
                        {
                            strOutput += strData;
                            strOutput += "\r\n";
                            this.Invoke((MethodInvoker)delegate
                            {
                                DisplayUnitLog(strPanel, strData);
                            });
                        }
                    });

                    process.BeginOutputReadLine();

                    if (iTimeOut == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(iTimeOut);
                    }

                    #endregion
                }

                if (SaveBatContentFile(strPanel, strOutput) == false)
                {
                    strErrorMessage = "Save bat file fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Bat Exception:" + strr;
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return true;
        }

        private bool ExcuteBat_QFIL(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strOutput = "";

            Process process = null;
            ProcessStartInfo startInfo = null;

            if (strBatParameter != "")
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile, strBatParameter);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.Arguments = strBatParameter;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }
            else
            {
                process = new Process();
                startInfo = new ProcessStartInfo(strBatFile);
                startInfo.WorkingDirectory = strBatDir;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                process.StartInfo = startInfo;
            }

            try
            {
                if (process.Start())
                {
                    #region StandardOutput OutputDataReceived

                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        string strData = e.Data;
                        if (!String.IsNullOrEmpty(strData))
                        {
                            strOutput += strData;
                            strOutput += "\r\n";
                        }
                    });

                    process.BeginOutputReadLine();

                    if (iTimeOut == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(iTimeOut);
                    }

                    #endregion
                }

                if (SaveBatContentFile(strPanel, strOutput) == false)
                {
                    strErrorMessage = "Save bat file fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Execute Bat Exception:" + strr;
                return false;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return true;
        }

        private bool ADBRebootBootloader(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "RebootBootloader.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                for (int i = 0; i < 6; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strSN, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(10);
                        //KillAdb();
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "ADBRebootBootloader fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "ADBRebootBootloader exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool ADBRebootEDL(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "RebootEDL.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                for (int i = 0; i < 6; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strSN, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(10);
                        //KillAdb();
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "ADBRebootEDL fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "ADBRebootEDL exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckSKU(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strSKU = m_st_MCFData.SKU;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "CheckSKU.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN + " " + strSKU;
                for (int i = 0; i < 6; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(10);
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "SKU not match scan sheet and unit.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Check SKU exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckSDCard(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "CheckSDCard.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN;
                for (int i = 0; i < 6; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(10);
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to check SDCard.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Check SDCard exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckManualBITResult(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "CheckManualBITResult.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN + " " + m_st_MCFData.SKU;
                for (int i = 0; i < 6; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(10);
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to check ManualBITResult.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Check ManualBITResult exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool GetSKUProperty(string strPanel, ref string strSKU, ref string strErrorMessage)
        {
            strErrorMessage = "";
  
            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "GetSKU.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN + " " + m_st_MCFData.SKU;
                for (int i = 0; i < 6; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(10);
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to check ManualBITResult.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Check ManualBITResult exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool GetCOMPort(string strPanel, ref string strCOMPort, ref string strErrorMessage)
        {
            try
            {
                strCOMPort = "";
                strErrorMessage = "";
                string strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;

                USBEnumerator usbEnum = new USBEnumerator();
                List<string> m_List_Port = new List<string>();
                bool bRes = false;
                for (int i = 0; i < 10; i++)
                {
                    m_List_Port.Clear();
                    bRes = usbEnum.GetPortByUSBDevicePhysicalAddress(strPhysicalAddress, ref m_List_Port, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(2);
                        continue;
                    }
                    if (m_List_Port.Count != 1)
                    {
                        bRes = false;
                        Dly(2);
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to COM port." + m_List_Port.Count.ToString();
                    return false;
                }
                strCOMPort = m_List_Port[0];

                #region Clear COM Port inuse status

                int iCOMPort = int.Parse(strCOMPort);
                if (iCOMPort > 200)
                {
                    if (DeleteCOMNameArbiterReg() == false)
                    {
                        strErrorMessage = "Failed to clear COM Port inuse status.";
                        return false;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "GetCOMPort Exception.";
                return false;
            }

            return true;
        }

        private bool FastbootDevice(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "FastbootDevice.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                for (int i = 0; i < 6; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strSN, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(10);
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "FastbootDevice fail.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "FastbootDevice exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool FlashFastboot(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = m_st_OptionData.ImageLocalPath;
                string strBatFile = strBatDir + "\\" + m_st_OptionData.FASTBOOTBatFile;
                int iTimeout = m_st_OptionData.FASTBOOTTimeout * 1000;
                string strSearchResult = m_st_OptionData.FASTBOOTSuccessFlag;
                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }
                for (int i = 0; i < 1; i++)
                {
                    //bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strSN, iTimeout, strSearchResult, ref strErrorMessage);
                    bRes = ExcuteBat_Fastboot(strPanel, strBatDir, strBatFile, strSN, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        strErrorMessage = "ExcuteBat_Fastboot fail.";
                        continue;
                    }
                    else
                    {
                        Dly(3);

                        #region Check Bat File

                        if (bRes == true)
                        {
                            string strLogFile = "";
                            strLogFile = "";
                            strLogFile = Application.StartupPath + "\\LOG" + "\\" + strSN + "_TestLog.txt";
                            bool bCheckLog = false;
                            for (int m = 0; m < 10; m++)
                            {
                                bCheckLog = CheckBatLogFile(strLogFile, strSearchResult);
                                if (bCheckLog == true)
                                {
                                    break;
                                }
                                else
                                {
                                    Dly(1);
                                    continue;
                                }
                            }
                            if (bCheckLog == false)
                            {
                                strErrorMessage = "Check flash result fail.";
                                bRes = false;
                            }
                            else
                            {
                                strErrorMessage = "";
                                bRes = true;
                            }
                        }

                        #endregion
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "Fastboot fail." + strErrorMessage;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Flash fastboot exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool FlashEDL(string strPanel, string strCOMPort, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\QFIL";
                string strBatFile = "";
                int iTimeout = m_st_OptionData.EDLTimeout * 1000;
                string strSearchResult = m_st_OptionData.EDLSuccessFlag;
                string strCmd = "";
                string strQFIL = "";
                string strSearchPath = "";
                string strelf = "";
                string strPatch = "";
                string strRawProgram = "";
                string strReset = "";

                #region strCmd

                if (m_st_OptionData.EDLDeviceType == "")
                {
                    strSearchPath = m_st_OptionData.ImageLocalPath;
                    strelf = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.EDLELF;
                    strPatch = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.EDLPatch;
                    strRawProgram = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.EDLRawProgram;
                }
                else
                {
                    strSearchPath = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.EDLDeviceType;
                    strelf = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.EDLDeviceType + "\\" + m_st_OptionData.EDLELF;
                    strPatch = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.EDLDeviceType + "\\" + m_st_OptionData.EDLPatch;
                    strRawProgram = m_st_OptionData.ImageLocalPath + "\\" + m_st_OptionData.EDLDeviceType + "\\" + m_st_OptionData.EDLRawProgram;
                }

                strQFIL = m_st_OptionData.EDLQFIL;
                if (File.Exists(strQFIL) == false)
                {
                    strErrorMessage = "Failed to check exist." + strQFIL;
                    return false;
                }
                
                if (Directory.Exists(strSearchPath) == false)
                {
                    strErrorMessage = "Failed to check exist." + strSearchPath;
                    return false;
                }

                if (File.Exists(strelf) == false)
                {
                    strErrorMessage = "Failed to check exist." + strelf;
                    return false;
                }

                if (File.Exists(strPatch) == false)
                {
                    strErrorMessage = "Failed to check exist." + strPatch;
                    return false;
                }

                if (File.Exists(strRawProgram) == false)
                {
                    strErrorMessage = "Failed to check exist." + strRawProgram;
                    return false;
                }

                strReset = m_st_OptionData.EDLReset;
                if (strReset == "1")
                {
                    strReset = "true";
                }
                else
                {
                    strReset = "false";
                }
                if (System.IO.Directory.Exists(strBatDir) == false)
                {
                    System.IO.Directory.CreateDirectory(strBatDir);
                    Dly(1);
                }
                /*Kristy,20210630,add YYMMDDHHMMSS in file name*/
                string strLogFilePathName = strBatDir + "\\" + strSN + "_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";

                strCmd = "";
                strCmd = "\"" + strQFIL + "\"";
                //strCmd = "QFIL.exe";
                strCmd += " -Mode=3";
                strCmd += " -COM=" + strCOMPort;
                strCmd += " -downloadflat";
                strCmd += " -Programmer=true;" + "\"" + strelf + "\"";
                strCmd += " -SEARCHPATH=" + "\"" + strSearchPath + "\"";
                strCmd += " -RawProgram=" + m_st_OptionData.EDLRawProgram;

                //Select Flash Type: emmc/ufs
                if (m_st_OptionData.EDLFlashType != "")     
                {
                    strCmd += " -DEVICETYPE=" + m_st_OptionData.EDLFlashType;
                }

                strCmd += " -";
                strCmd += " -patch=" + m_st_OptionData.EDLPatch;
                strCmd += " -RESETAFTERDOWNLOAD=" + strReset;
                strCmd += " -consolelog -LOGFILEPATH=" + "\"" + strLogFilePathName + "\"";

                #endregion

                #region Delete QFIL Log File

                //if (DeleteQFILLogFile(strLogFilePathName) == false)
                //{
                //    strErrorMessage = "Failed to delete QFIL Log." + strLogFilePathName;
                //    return false;
                //}

                #endregion

                #region Generate Bat File

                strBatFile = "QFIL_" + strCOMPort + ".bat";
                if (GenerateBatFile(strBatDir, strBatFile, strCmd) == false)
                {
                    strErrorMessage = "Failed to generate bat file." + strPatch;
                    return false;
                }
                Dly(1);
                strBatFile = strBatDir + "\\" + strBatFile;

                #endregion

                #region ExcuteBat

                bRes = false;
                for (int i = 0; i < 2; i++)
                {
                    #region DeleteQFILConfigFile

                    string strTempErrorMessage = "";
                    if (DeleteQFILConfigFile(strCOMPort, ref strTempErrorMessage) == false)
                    {
                        strErrorMessage = strTempErrorMessage;
                        bRes = false;
                        continue;
                    }

                    #endregion

                    bRes = ExcuteBat_QFIL(strPanel, strBatDir, strBatFile, "", iTimeout, ref strErrorMessage);

                    Dly(3);

                    #region Check QFIL File

                    if (bRes == true)
                    {
                        bool bCheckQFILLog = false;
                        /*20210630, check the lastest log file*/
                        /*string strQFILLogFile = "";
                        strQFILLogFile = "";
                        strQFILLogFile = strBatDir + "\\" + strSN + ".txt";*/
                        for (int m = 0; m < 20; m++)
                        {
                            //bCheckQFILLog = CheckBatLogFile(strQFILLogFile, strSearchResult);
                            bCheckQFILLog = CheckBatLogFile(strLogFilePathName, strSearchResult);
                            if (bCheckQFILLog == true)
                            {
                                break;
                            }
                            else
                            {
                                Dly(1);
                                continue;
                            }
                        }
                        if (bCheckQFILLog == false)
                        {
                            strErrorMessage = "Check flash result fail.";
                            bRes = false;
                        }
                        else
                        {
                            strErrorMessage = "";
                            bRes = true;
                        }
                    }

                    #endregion

                    #region DeleteQFILConfigFile

                    DeleteQFILConfigFile(strCOMPort, ref strTempErrorMessage);

                    #endregion

                    if (bRes == false)
                    {
                        // Retry still fail.
                        bRes = false;
                        break;
                    }
                    else
                    {
                        bRes = true;
                        break;
                    }
                }
                if (bRes == false)
                {
                    strErrorMessage = "Flash fail:" + strErrorMessage;
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "Flash EDL exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool CheckBatLogFile(string strFilePathName, string strSearchContent)
        {
            bool bRes = false;
            FileStream fs = null;
            StreamReader sr = null;

            try
            {
                if (File.Exists(strFilePathName) == false)
                {
                    System.Threading.Thread.Sleep(500);
                    return false;
                }

                fs = new FileStream(strFilePathName, FileMode.Open, FileAccess.Read);
                sr = new StreamReader(fs, System.Text.Encoding.Default);

                string strLine = "";
                while ((strLine = sr.ReadLine()) != null)
                {
                    if (strLine.Trim() == "")
                    {
                        continue;
                    }

                    if (strLine.Contains(strSearchContent) == true)
                    {
                        bRes = true;
                        break;
                    }
                    else
                    {
                        bRes = false;
                        continue;
                    }
                }

                if (sr != null)
                {
                    sr.Close();
                    sr = null;
                }
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }

                if (bRes == false)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (sr != null)
                {
                    sr.Close();
                    sr = null;
                }
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }

                string strr = ex.Message;
                return false;
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                    sr = null;
                }
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }

            return true;
        }

        private bool DeleteQFILLogFile(string strFilePathName)
        {
            try
            {
                if (File.Exists(strFilePathName) == true)
                {
                    File.Delete(strFilePathName);
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool WriteKeybox(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            bool bRes = false;
            string strSN = m_dic_UnitDevice[strPanel].SN;
            string strBatDir = Application.StartupPath + "\\" + "BAT";
            string strBatFile = strBatDir + "\\" + "Keybox.bat";
            int iTimeout = 15 * 1000;
            string strSearchResult = "SUCCESS";
            string strBatParameter = "";

            string strKeyboxDir = Application.StartupPath + "\\" + m_str_Model;
            string strKeyboxFile = m_st_OptionData.KeyboxFile;
            string strKeyboxDevice = m_st_OptionData.KeyboxDevice;
            strBatParameter = strSN + " " + strKeyboxDir + " " + strKeyboxFile + " " + strKeyboxDevice;
            if (File.Exists(strBatFile) == false)
            {
                strErrorMessage = "Failed to file exist." + strBatFile;
                return false;
            }

            for (int i = 0; i < 6; i++)
            {
                bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strErrorMessage);
                if (bRes == false)
                {
                    bRes = false;
                    Dly(10);
                    KillAdb();
                    continue;
                }

                bRes = true;
                break;
            }
            if (bRes == false)
            {
                //strErrorMessage = "Failed to " + "WriteKeybox.";
                strErrorMessage = "Failed to Check Keybox.";
                return false;
            }

            return true;
        }

        private bool WriteSentienceKey(string strPanel, ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";

                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "HonEdgeUtility";
                string strBatFile = strBatDir + "\\" + "HonEdgeUtility.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";
                string strBatParameter = "";
                string strHonEdgeLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\HONEdge\\";
                System.IO.DirectoryInfo ImgDictory = null;
                string strFolderName = "";
                string strDicName = "";

                #region Delete HonEdge Local Path (SN)

                strDicName = m_st_OptionData.SentienceKeyHonEdgeProductName + "_" + DateTime.Now.ToString("MM-dd-yyyy");

                if (Directory.Exists(strHonEdgeLocalPath))
                {
                    ImgDictory = new DirectoryInfo(strHonEdgeLocalPath);
                    System.IO.DirectoryInfo[] foldFiles = (System.IO.DirectoryInfo[])ImgDictory.GetDirectories().Where(x => x.Name.Contains(strDicName)).ToArray();
                    if (foldFiles.Length == 1)
                    {
                        strFolderName = foldFiles[0].Name;
                        var file_local = Directory.GetFiles(strHonEdgeLocalPath + strFolderName);
                        if (file_local.Count() != 0)
                        {
                            foreach (var item in file_local)
                            {
                                if (Path.GetFileName(item).Contains(strSN))
                                {
                                    File.Delete(Path.GetFullPath(item));
                                }
                            }
                        }
                    }
                }

                #endregion

                #region ExcuteBat

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Failed to file exist." + strBatFile;
                    return false;
                }

                strBatParameter = strSN;
                for (int i = 0; i < 3; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(1);
                        KillAdb();
                        continue;
                    }

                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to " + "WriteSentienceKey.";
                    return false;
                }

                #endregion

                #region Copy to server

                if (m_st_OptionData.SentienceKeyUploadEnable == "1")
                {
                    strFolderName = "";
                    if (Directory.Exists(strHonEdgeLocalPath))
                    {
                        ImgDictory = new DirectoryInfo(strHonEdgeLocalPath);
                        System.IO.DirectoryInfo[] foldFiles = (System.IO.DirectoryInfo[])ImgDictory.GetDirectories().Where(x => x.Name.Contains(strDicName)).ToArray();
                        if (foldFiles.Length == 1)
                        {
                            strFolderName = foldFiles[0].Name;
                        }
                        else
                        {
                            strErrorMessage = "Failed to Folder name is null." + strFolderName;
                            return false;
                        }
                    }
                    else
                    {
                        strErrorMessage = "Failed to Folder not exist." + strHonEdgeLocalPath;
                        return false;
                    }

                    string strServerFolderPath = m_st_OptionData.SentienceKeyUploadServerPath + "\\" + strFolderName;
                    if (!Directory.Exists(strServerFolderPath))
                    {
                        Directory.CreateDirectory(strServerFolderPath);
                        Dly(1);
                        if (!Directory.Exists(strServerFolderPath))
                        {
                            strErrorMessage = "Failed to Create directory fail." + strServerFolderPath;
                            return false;
                        }
                    }

                    var file_server = Directory.GetFiles(strServerFolderPath);
                    if (file_server.Count() != 0)
                    {
                        foreach (var item in file_server)
                        {
                            if (Path.GetFileName(item).Contains(strSN))
                            {
                                File.Delete(Path.GetFullPath(item));
                            }
                        }
                    }

                    var local = Directory.GetFiles(strHonEdgeLocalPath + strFolderName).Select(f => Path.GetFileName(f));
                    var server = Directory.GetFiles(strServerFolderPath).Select(f => Path.GetFileName(f));
                    var diff = local.Except(server).ToList();
                    if (diff.Count != 0)
                    {
                        foreach (var item in diff)
                        {
                            File.Copy(strHonEdgeLocalPath + strFolderName + "\\" + item, strServerFolderPath + "\\" + item, true);
                        }
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "WriteSentienceKey Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool UploadMES(TestSaveData objSaveData, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_st_OptionData.MES_Enable == "1")
                {
                    string strEID = "Automation";
                    string strSN = "";
                    string strWorkOrder = "";
                    string strRouterCode = "G2H";
                    string strSourceCode = "";
                    string strTestResult = "";

                    strSN = objSaveData.TestRecord.SN;
                    if (strSN == "")
                    {
                        strErrorMessage = "Failed to upload MES,invalid SN.";
                        return false;
                    }

                    if (objSaveData.TestResult.TestPassed == true)
                    {
                        strTestResult = "PASS";
                    }
                    else
                    {
                        strTestResult = "FAIL";
                    }

                    MESWebAPI.MoveResult mesResult = new MESWebAPI.MoveResult();
                    mesResult = MESWebAPI.SNMove.CheckWorkOrder(strWorkOrder);
                    if (mesResult.result.ToUpper() != "OK")
                    {
                        strWorkOrder = "2019091001"; //如果输入的工单无效,用默认的工单
                    }

                    mesResult = MESWebAPI.SNMove.MoveForAutoMation(strEID, strSourceCode, strSN, strWorkOrder, strRouterCode, strTestResult);
                    if (mesResult.result.ToUpper() == "OK")
                    {
                        return true;
                    }
                    else
                    {
                        strErrorMessage = "Failed to upload MES." + mesResult.message.ToString();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "UploadMES exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool InitMDCSData(string strPanel)
        {
            try
            {
                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];

                objSaveData.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData.TestRecord.SN = "";
                objSaveData.TestRecord.Model = m_str_Model;
                objSaveData.TestRecord.SKU = m_st_MCFData.SKU;
                objSaveData.TestRecord.OSVersion = m_st_MCFData.OSVersion;
                objSaveData.TestRecord.TestTotalTime = 0;

                objSaveData.TestResult.TestPassed = true;
                objSaveData.TestResult.TestFailCode = 0;
                objSaveData.TestResult.TestFailMessage = "";
                objSaveData.TestResult.TestStatus = "";

                m_dic_TestSaveData[strPanel] = objSaveData;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        #region MESCheckData_OLD

        //private bool MESCheckData_OLD(ref string strErrorMessage)
        //{
        //    strErrorMessage = "";

        //    try
        //    {
        //        if (m_st_OptionData.MES_Enable == "1")
        //        {
        //            UploadTestData.CheckData cd = new UploadTestData.CheckData();
        //            UploadTestData.Result result = new UploadTestData.Result();

        //            cd.EID = m_st_MESData.EID;
        //            cd.StationName = m_st_OptionData.StationName;
        //            cd.WorkOrder = m_st_MESData.WorkOrder;

        //            #region Check 

        //            if (m_st_MESData.EID == "")
        //            {
        //                strErrorMessage = "Invalid EID.";
        //                return false;
        //            }
        //            if (m_st_OptionData.StationName == "")
        //            {
        //                strErrorMessage = "Invalid StationName.";
        //                return false;
        //            }
        //            if (m_st_MESData.WorkOrder == "")
        //            {
        //                strErrorMessage = "Invalid WorkOrder.";
        //                return false;
        //            }

        //            #endregion

        //            result = UploadTestData.LineDashboard.CheckTestValid(cd);
        //            if (result.code == 0)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                strErrorMessage = "code:" + result.code.ToString() + ",message:" + result.message;
        //                return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string strr = ex.Message;
        //        strErrorMessage = "MESCheckData exception:" + ex.Message;
        //        return false;
        //    }

        //    return true;
        //}
        
        #endregion

        public static bool MESCheckData(string strEID, string strStation, string strWorkOrder, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (strEID == "" || strStation == "" || strWorkOrder == "")
                {
                    strErrorMessage = "Invalid MES data.";
                    return false;
                }

                UploadData data = new UploadData()
                {
                    EID = strEID,
                    StationName = strStation,
                    WorkOrder = strWorkOrder
                };

                Result result = LineDashboard.CheckTestValid(data);
                if (result.code == 0)
                {
                    return true;
                }
                else
                {
                    strErrorMessage = "Fail: code=" + result.code + ". Message: " + result.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                return false;
            }
        }

        #region MESUploadData_OLD
        //private bool MESUploadData_OLD(TestSaveData objSaveData, ref string strErrorMessage)
        //{
        //    strErrorMessage = "";

        //    try
        //    {
        //        if (m_st_OptionData.MES_Enable == "1")
        //        {
        //            UploadTestData.UploadData ud = new UploadTestData.UploadData();
        //            UploadTestData.Result result = new UploadTestData.Result();

        //            ud.EID = m_st_MESData.EID;
        //            ud.StationName = m_st_OptionData.StationName;
        //            ud.WorkOrder = m_st_MESData.WorkOrder;
        //            ud.SN = objSaveData.TestRecord.SN;
        //            ud.TestResult = "";

        //            if (ud.SN == "")
        //            {
        //                strErrorMessage = "Failed to upload MES,invalid SN.";
        //                return false;
        //            }
        //            if (objSaveData.TestResult.TestPassed == true)
        //            {
        //                ud.TestResult = "PASS";
        //            }
        //            else
        //            {
        //                ud.TestResult = "FAIL:" + objSaveData.TestResult.TestFailMessage;
        //            }

        //            result = UploadTestData.LineDashboard.UploadTestValue(ud);
        //            if (result.code == 0)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                strErrorMessage = "code:" + result.code.ToString() + ",message:" + result.message;
        //                return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string strr = ex.Message;
        //        strErrorMessage = "MESUploadData exception:" + ex.Message;
        //        return false;
        //    }

        //    return true;
        //}
        #endregion

        public static bool MESUploadData(string strEID, string strStation, string strWorkOrder, string strSN, bool bPassFailFlag, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strResult = "";

            try
            {
                if (strEID == "" || strStation == "" || strWorkOrder == "" || strSN == "")
                {
                    strErrorMessage = "Invalid MES data.";
                    return false;
                }
                if (bPassFailFlag == true)
                {
                    strResult = "PASS";
                }
                else
                {
                    strResult = "Failure";
                }

                UploadData data = new UploadData()
                {
                    EID = strEID,
                    StationName = strStation,
                    WorkOrder = strWorkOrder,
                    SN = strSN,
                    TestResult = strResult
                };

                Result result = LineDashboard.UploadTestValue(data);
                if (result.code == 0)
                {
                    return true;
                }
                else
                {
                    strErrorMessage = "Fail: code=" + result.code + ". Message: " + result.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                return false;
            }
        }

        #endregion

        #region AutoTest Function

        #region NI Card

        private bool InitNI6001()
        {
            try
            {
                m_obj_Fixctrl = null;
                m_obj_Fixctrl = new clsNI6001();
                if (m_obj_Fixctrl.Init(m_st_OptionData.DAQDevice) == false)
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

        private bool USBPlugInit(ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";

                if (USBPlugAction(PANEL_1, "0", ref strErrorMessage) == false)
                {
                    return false;
                }

                if (USBPlugAction(PANEL_2, "0", ref strErrorMessage) == false)
                {
                    return false;
                }

                if (USBPlugAction(PANEL_3, "0", ref strErrorMessage) == false)
                {
                    return false;
                }

                if (USBPlugAction(PANEL_4, "0", ref strErrorMessage) == false)
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

        private bool USBPlugAction(string strPanel, string strStatus, ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";

                bool bRes = false;
                for (int i = 0; i < 3; i++)
                {
                    bRes = USBPlug(strPanel, strStatus, ref strErrorMessage);

                    Dly(0.5);

                    if (bRes == true)
                    {
                        bRes = USBLocationCheck(strPanel, strStatus, ref strErrorMessage);
                        if (bRes == true)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                if (bRes == false)
                {
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

        private bool UnitLocationCheckAction(string strPanel, string strStatus, ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";

                bool bRes = false;
                for (int i = 0; i < 3; i++)
                {
                    bRes = UnitLocationCheck(strPanel, strStatus, ref strErrorMessage);

                    if (bRes == true)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (bRes == false)
                {
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

        // strStatus 1:工作位，0:初始位
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

                if (strPanel == PANEL_1)
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

                else if (strPanel == PANEL_2)
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

                else if (strPanel == PANEL_3)
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

                else if (strPanel == PANEL_4)
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

            Dly(0.1);

            return true;
        }

        // strStatus 1:工作位，0:初始位
        private bool USBLocationCheck(string strPanel, string strStatus, ref string strErrorMessage)
        {
            try
            {
                // strStatus 1:工作位，0:初始位
                if (strStatus != "1" && strStatus != "0")
                {
                    strErrorMessage = "Invalid status:" + strStatus;
                    return false;
                }

                double d_ltl = 0;
                double d_utl = 0;
                double d_value = 0;
                int i_AI = 0;

                if (strPanel == PANEL_1)
                {
                    i_AI = 0;
                }
                else if (strPanel == PANEL_2)
                {
                    i_AI = 1;
                }
                else if (strPanel == PANEL_3)
                {
                    i_AI = 2;
                }
                else if (strPanel == PANEL_4)
                {
                    i_AI = 3;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                m_obj_Fixctrl.GetAnalog(i_AI, 50, ref d_value, 0.1);

                if (strStatus == "1")
                {
                    d_ltl = 1.5;
                    d_utl = 10;  // 随机定义的最大值
                }
                else
                {
                    d_ltl = -0.1; // 随机定义的最小值
                    d_utl = 0.7;
                }

                if (CheckRange(d_ltl, d_utl, d_value) == false)
                {
                    strErrorMessage = "Failed to check USB Location: " + d_value.ToString("0.000");
                    return false;
                }

            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "USBLocationCheck Exception:" + strr;
                return false;
            }

            return true;
        }

        // strStatus 1:无，0:有
        private bool UnitLocationCheck(string strPanel, string strStatus, ref string strErrorMessage)
        {
            try
            {
                // strStatus 1:无，0:有
                if (strStatus != "1" && strStatus != "0")
                {
                    strErrorMessage = "Invalid status:" + strStatus;
                    return false;
                }

                double d_ltl = 0;
                double d_utl = 0;
                double d_value = 0;
                int i_AI = 0;

                if (strPanel == PANEL_1)
                {
                    i_AI = 4;
                }
                else if (strPanel == PANEL_2)
                {
                    i_AI = 5;
                }
                else if (strPanel == PANEL_3)
                {
                    i_AI = 6;
                }
                else if (strPanel == PANEL_4)
                {
                    i_AI = 7;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                m_obj_Fixctrl.GetAnalog(i_AI, 50, ref d_value, 0.1);

                if (strStatus == "1")
                {
                    d_ltl = 1.5;
                    d_utl = 10;  // 随机定义的最大值
                }
                else
                {
                    d_ltl = -0.1; // 随机定义的最小值
                    d_utl = 0.7;
                }

                if (CheckRange(d_ltl, d_utl, d_value) == false)
                {
                    strErrorMessage = "Failed to check unit Location: " + d_value.ToString("0.000");
                    return false;
                }

            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "UnitLocationCheck Exception:" + strr;
                return false;
            }

            return true;
        }

        private bool CheckRange(double d_ltl, double d_utl, double d_value)
        {
            //DisplayMessage("Range: " + d_ltl.ToString("##0.000") + " < " + d_value.ToString("##0.000") + " < " + d_utl.ToString("##0.000"));

            if (d_value < d_ltl)
            {
                return false;
            }
            else if (d_value > d_utl)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region PLC

        private bool PLCConnect()
        {
            try
            {
                m_obj_PLC = null;
                m_obj_PLC = new CPLCDave();

                string strPLCIP = m_st_OptionData.PLCIP;
                int iPLCPort = int.Parse(m_st_OptionData.PLCPort);
                string strErrorMessage = "";
                if (m_obj_PLC.Connect(strPLCIP, iPLCPort, ref strErrorMessage) == false)
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

        private bool PLCDisConnect()
        {
            try
            {
                if (m_obj_PLC != null)
                {
                    string strErrorMessage = "";
                    if (m_obj_PLC.DisConnect(ref strErrorMessage) == false)
                    {
                        return false;
                    }
                }
                m_obj_PLC = null;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private void PLCRelease()
        {
            m_b_PLCRuning = false;

            if (m_timer_WatchDog != null)
            {
                m_timer_WatchDog.Dispose();
                m_timer_WatchDog = null;
            }

            if (m_obj_PLC != null)
            {
                PLCDisConnect();
            }
        }

        private bool FeedbackResult(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_WriteDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_WriteDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_WriteDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_WriteDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];
                if (objSaveData.TestResult.TestPassed == true)
                {
                    #region FeedbackRunResult SUCCESS

                    bool bRes = false;
                    for (int i = 0; i < 20; i++)
                    {
                        bRes = m_obj_PLC.FeedbackRunResult(iDB, CPLCDave.FeedbackResult.SUCCESS, ref strErrorMessage);
                        if (bRes == false)
                        {
                            Dly(0.2);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (bRes == false)
                    {
                        return false;
                    }

                    #endregion
                }
                else
                {
                    #region FeedbackErrorCode

                    bool bRes = false;
                    string strErrorCode = "";
                    strErrorCode = "[G2H_" + m_st_OptionData.Area_Location + strPanel + "]:";
                    strErrorCode += "(" + objSaveData.TestRecord.SN + ")";
                    strErrorCode += objSaveData.TestResult.TestFailMessage;
                    for (int i = 0; i < 20; i++)
                    {
                        bRes = m_obj_PLC.FeedbackErrorCode(iDB, strErrorCode, ref strErrorMessage);
                        if (bRes == false)
                        {
                            Dly(0.2);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (bRes == false)
                    {
                        return false;
                    }

                    #endregion

                    #region FeedbackRunResult FAIL

                    bRes = false;
                    for (int i = 0; i < 20; i++)
                    {
                        bRes = m_obj_PLC.FeedbackRunResult(iDB, CPLCDave.FeedbackResult.FAIL, ref strErrorMessage);
                        if (bRes == false)
                        {
                            Dly(0.2);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (bRes == false)
                    {
                        return false;
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool FeedbackStatus(string strPanel, CPLCDave.FeedbackStatus enumStatus, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_WriteDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_WriteDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_WriteDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_WriteDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                #region FeedbackCurrentStatus

                bool bRes = false;
                for (int i = 0; i < 20; i++)
                {
                    bRes = m_obj_PLC.FeedbackCurrentStatus(iDB, enumStatus, ref strErrorMessage);
                    if (bRes == false)
                    {
                        Dly(0.2);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (bRes == false)
                {
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool WaitForTestSingal(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region DB

                int iDB = 0;

                if (strPanel == PANEL_1)
                {
                    iDB = m_st_OptionData.DB_Slot1_ReadDB;
                }
                else if (strPanel == PANEL_2)
                {
                    iDB = m_st_OptionData.DB_Slot2_ReadDB;
                }
                else if (strPanel == PANEL_3)
                {
                    iDB = m_st_OptionData.DB_Slot3_ReadDB;
                }
                else if (strPanel == PANEL_4)
                {
                    iDB = m_st_OptionData.DB_Slot4_ReadDB;
                }
                else
                {
                    strErrorMessage = "Invalid panel:" + strPanel;
                    return false;
                }

                #endregion

                #region ReadCommandStartTest

                bool bRes = false;
                for (int i = 0; i < 1; i++)
                {
                    bRes = m_obj_PLC.ReadCommandStartTest(iDB, ref strErrorMessage);
                    if (bRes == false)
                    {
                        Dly(0.2);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (bRes == false)
                {
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "Exception:" + ex.Message;
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        #endregion

        private void Thread_Timer_WatchDog(object obj)
        {
            try
            {
                if (m_b_PLCRuning == true)
                {
                    string strErrorMessage = "";

                    m_i_WatchDog %= 255;

                    //m_obj_PLC.WatchDog(m_st_OptionData.DB_Slot1_WriteDB, m_i_WatchDog.ToString(), ref strErrorMessage);
                    //m_obj_PLC.WatchDog(m_st_OptionData.DB_Slot2_WriteDB, m_i_WatchDog.ToString(), ref strErrorMessage);
                    //m_obj_PLC.WatchDog(m_st_OptionData.DB_Slot3_WriteDB, m_i_WatchDog.ToString(), ref strErrorMessage);
                    //m_obj_PLC.WatchDog(m_st_OptionData.DB_Slot4_WriteDB, m_i_WatchDog.ToString(), ref strErrorMessage);

                    if (m_obj_PLC.WatchDog(m_st_OptionData.DB_Slot1_WriteDB, m_i_WatchDog.ToString(), ref strErrorMessage) == false)
                    {
                        DisplayMessage("WatchDog Fail:" + strErrorMessage);
                        SaveLogFile("WatchDog Fail:" + strErrorMessage);
                    }

                    m_i_WatchDog++;
                }
            }
            catch (Exception exx)
            {
                string strr = exx.Message;
                return;
            }

            return;
        }

        private void timerAutoTest_Tick(object sender, EventArgs e)
        {
            timerAutoTest.Enabled = false;

            try
            {
                AutoTest(PANEL_1);
                Dly(1);
                AutoTest(PANEL_2);
                Dly(1);
                AutoTest(PANEL_3);
                Dly(1);
                AutoTest(PANEL_4);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("timerAutoTest Exception:" + strr);
                return;
            }
            finally
            {
                timerAutoTest.Enabled = true;
            }
        }

        private void AutoTest(string strPanel)
        {
            string strErrorMessage = "";

            try
            {
                if (m_dic_TestStatus[strPanel] == false)
                {
                    m_dic_TestStatus[strPanel] = true;

                    #region Check Product (Obsolete)

                    //if (m_str_Model.Contains("CT40"))   // CT40
                    //{
                    //    // 有产品检测, 0: have product, 1: Idle
                    //    if (UnitLocationCheckAction(strPanel, "0", ref strErrorMessage) == true)
                    //    {
                    //        // 有产品
                    //        if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.HAVEPRODUCT, ref strErrorMessage) == false)
                    //        {
                    //            DisplayMessage("Panel:" + strPanel + " start FeedbackStatus HAVEPRODUCT fail." + strErrorMessage);
                    //            SaveLogFile("Panel:" + strPanel + " start FeedbackStatus HAVEPRODUCT fail." + strErrorMessage);
                    //            m_dic_TestStatus[strPanel] = false;
                    //            return;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        // 无产品
                    //        if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.READY, ref strErrorMessage) == false)
                    //        {
                    //            DisplayMessage("Panel:" + strPanel + " FeedbackStatus READY fail." + strErrorMessage);
                    //            SaveLogFile("Panel:" + strPanel + " FeedbackStatus READY fail." + strErrorMessage);
                    //            m_dic_TestStatus[strPanel] = false;
                    //            return;
                    //        }
                    //    }
                    //}
                    //else    // EDA51            
                    //{
                    //    #region Give Ready Signal

                    //    DisplayMessage("Panel:" + strPanel + " Give Robot Ready Signal");
                    //    if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.READY, ref strErrorMessage) == false)
                    //    {
                    //        DisplayMessage("Panel:" + strPanel + " FeedbackStatus READY Fail:" + strErrorMessage);
                    //        SaveLogFile("Panel:" + strPanel + " FeedbackStatus READY Fail:" + strErrorMessage);
                    //        m_dic_TestStatus[strPanel] = false;
                    //        return;
                    //    }

                    //    #endregion
                    //}

                    #endregion

                    #region Check Product
      
                    #region Give Ready Signal

                    DisplayMessage("Panel:" + strPanel + " Give Robot Ready Signal");
                    if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.READY, ref strErrorMessage) == false)
                    {
                        DisplayMessage("Panel:" + strPanel + " FeedbackStatus READY Fail:" + strErrorMessage);
                        SaveLogFile("Panel:" + strPanel + " FeedbackStatus READY Fail:" + strErrorMessage);
                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }

                    #endregion
                  
                    #endregion

                    #region Wait Test

                    // Wait for start test
                    DisplayMessage("Panel:" + strPanel + " WaitForTestSingal");
                    if (WaitForTestSingal(strPanel, ref strErrorMessage) == false)
                    {
                        DisplayMessage("Panel:" + strPanel + " WaitForTestSingal, Not Ready! " + strErrorMessage);
                        SaveLogFile("Panel:" + strPanel + " WaitForTestSingal, Not Ready! " + strErrorMessage);
                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }
                    DisplayMessage("Panel:" + strPanel + " WaitForTestSingal Success.");

                    #endregion

                    #region Feedback Busy

                    // Busy
                    DisplayMessage("Panel:" + strPanel + " FeedbackStatus BUSY");
                    if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.BUSY, ref strErrorMessage) == false)
                    {
                        DisplayMessage("Panel:" + strPanel + " FeedbackStatus BUSY fail." + strErrorMessage);
                        SaveLogFile("Panel:" + strPanel + " FeedbackStatus BUSY fail." + strErrorMessage);
                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }
                    DisplayMessage("Panel:" + strPanel + " FeedbackStatus BUSY Success.");

                    #endregion

                    #region RunTest

                    bool bRunRes = true;

                    InitMDCSData(strPanel);

                    #region Plug USB

                    //if (m_str_Model.Contains("CT40"))
                    //{
                    //    // Plug USB
                    //    if (bRunRes == true)
                    //    {
                    //        bRunRes = USBPlugAction(strPanel, "1", ref strErrorMessage);
                    //        if (bRunRes == false)
                    //        {
                    //            DisplayMessage("Panel:" + strPanel + " USBPlug In fail." + strErrorMessage);
                    //            SaveLogFile("Panel:" + strPanel + " USBPlug In fail." + strErrorMessage);
                    //            m_dic_TestStatus[strPanel] = false;
                    //            bRunRes = false;
                    //            //return;
                    //        }
                    //    }
                    //}

                    #endregion

                    #region Monitor Device

                    if (bRunRes == true)
                    {
                        // Monitor Device
                        bRunRes = MonitorDeviceByPhysicalAddress_AutoTest(strPanel, ref strErrorMessage);
                        if (bRunRes == false)
                        {
                            DisplayMessage("Panel:" + strPanel + " MonitorDevice fail." + strErrorMessage);
                            SaveLogFile("Panel:" + strPanel + " MonitorDevice fail." + strErrorMessage);
                            m_dic_TestStatus[strPanel] = false;
                            bRunRes = false;
                            //return;
                        }
                    }

                    #endregion

                    #region Update Status (Failed to detect device)

                    if (bRunRes == false)
                    {
                        #region STATUS_FAILED

                        this.Invoke((MethodInvoker)delegate { DisplayUnitStatus(strPanel, STATUS_FAILED, Color.Red); });
                        UnitDeviceInfo stUnit2 = m_dic_UnitDevice[strPanel];
                        stUnit2.Status = "F";
                        m_dic_UnitDevice[strPanel] = stUnit2;

                        #endregion

                        #region MDCS Data

                        TestSaveData objSaveData = m_dic_TestSaveData[strPanel];
                        objSaveData.TestResult.TestPassed = false;
                        objSaveData.TestResult.TestFailCode = 2050;
                        objSaveData.TestResult.TestFailMessage = strErrorMessage;
                        objSaveData.TestResult.TestStatus = "";
                        m_dic_TestSaveData[strPanel] = objSaveData;

                        #endregion

                        //if (m_str_Model.Contains("CT40"))   // CT40
                        //{
                        //    if (UnitLocationCheckAction(strPanel, "0", ref strErrorMessage) == true)
                        //    {
                        //        // 有产品
                        //        if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.HAVEPRODUCT, ref strErrorMessage) == false)
                        //        {
                        //            DisplayMessage("Panel:" + strPanel + " run FeedbackStatus HAVEPRODUCT fail." + strErrorMessage);
                        //            SaveLogFile("Panel:" + strPanel + " run FeedbackStatus HAVEPRODUCT fail." + strErrorMessage);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        // 无产品
                        //        if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.ERROR, ref strErrorMessage) == false)
                        //        {
                        //            DisplayMessage("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strErrorMessage);
                        //            SaveLogFile("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strErrorMessage);
                        //        }
                        //    }

                        //    if (USBPlugAction(strPanel, "0", ref strErrorMessage) == false)
                        //    {
                        //        DisplayMessage("Panel:" + strPanel + " USB Plug Out fail." + strErrorMessage);
                        //        SaveLogFile("Panel:" + strPanel + " USB Plug Out fail." + strErrorMessage);
                        //        if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.ERROR, ref strErrorMessage) == false)
                        //        {
                        //            DisplayMessage("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strErrorMessage);
                        //            SaveLogFile("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strErrorMessage);
                        //        }
                        //    }
                        //    else
                        //    {
                        //        DisplayMessage("Panel:" + strPanel + " USB Plug Out success.");
                        //        SaveLogFile("Panel:" + strPanel + " USB Plug Out success.");
                        //    }
                        //}

                        // Feedback Test Result, CT40 and EDA51
                        if (FeedbackResult(strPanel, ref strErrorMessage) == false)
                        {
                            DisplayMessage("Panel:" + strPanel + " FeedbackResult fail." + strErrorMessage);
                            SaveLogFile("Panel:" + strPanel + " FeedbackResult fail." + strErrorMessage);
                            if (FeedbackStatus(strPanel, CPLCDave.FeedbackStatus.ERROR, ref strErrorMessage) == false)
                            {
                                DisplayMessage("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strErrorMessage);
                                SaveLogFile("Panel:" + strPanel + " FeedbackStatus ERROR fail." + strErrorMessage);
                            }
                        }
                        else
                        {
                        }

                        //m_dic_COMPort[strPanel] = "";   //Clear ComPort Record When Disconnect.

                        m_dic_TestStatus[strPanel] = false;
                        return;
                    }

                    #endregion

                    #endregion
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                DisplayMessage("AutoTest Exception:" + strr);
                SaveLogFile("Panel:" + strPanel + " AutoTest Exception:" + strr);
                return;
            }

            return;
        }

        #endregion

        #region BackgroundWorker

        #region BgFlashWorker1

        private BackgroundWorker bgFlashWorker1 = new BackgroundWorker();

        private void bgFlashWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_1);
        }

        private void bgFlashWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_1, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_1, "BackgroundWorker Error.");
            }
            else
            {
                DisplayUnitLog(PANEL_1, "BackgroundWorker Completed.");
            }
        }

        #endregion

        #region BgFlashWorker2

        private BackgroundWorker bgFlashWorker2 = new BackgroundWorker();

        private void bgFlashWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_2);
        }

        private void bgFlashWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_2, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_2, "BackgroundWorker Error.");
            }
            else
            {
                DisplayUnitLog(PANEL_2, "BackgroundWorker Completed.");
            }
        }

        #endregion

        #region BgFlashWorker3

        private BackgroundWorker bgFlashWorker3 = new BackgroundWorker();

        private void bgFlashWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_3);
        }

        private void bgFlashWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_3, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_3, "BackgroundWorker Error.");
            }
            else
            {
                DisplayUnitLog(PANEL_3, "BackgroundWorker Completed.");
            }
        }

        #endregion

        #region BgFlashWorker4

        private BackgroundWorker bgFlashWorker4 = new BackgroundWorker();

        private void bgFlashWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            RunTest(PANEL_4);
        }

        private void bgFlashWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                DisplayUnitLog(PANEL_4, "BackgroundWorker Cancelled.");
            }
            else if (e.Error != null)
            {
                DisplayUnitLog(PANEL_4, "BackgroundWorker Error.");
            }
            else
            {
                DisplayUnitLog(PANEL_4, "BackgroundWorker Completed.");
            }
        }

        #endregion

        private void InitBackgroundworker()
        {
            // Initialize backgroundWorker1
            bgFlashWorker1.DoWork += new DoWorkEventHandler(bgFlashWorker1_DoWork);
            bgFlashWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker1_RunWorkerCompleted);
            bgFlashWorker1.WorkerReportsProgress = false;
            bgFlashWorker1.WorkerSupportsCancellation = true;

            // Initialize backgroundWorker2
            bgFlashWorker2.DoWork += new DoWorkEventHandler(bgFlashWorker2_DoWork);
            bgFlashWorker2.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker2_RunWorkerCompleted);
            bgFlashWorker2.WorkerReportsProgress = false;
            bgFlashWorker2.WorkerSupportsCancellation = true;

            // Initialize backgroundWorker3
            bgFlashWorker3.DoWork += new DoWorkEventHandler(bgFlashWorker3_DoWork);
            bgFlashWorker3.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker3_RunWorkerCompleted);
            bgFlashWorker3.WorkerReportsProgress = false;
            bgFlashWorker3.WorkerSupportsCancellation = true;

            // Initialize backgroundWorker4
            bgFlashWorker4.DoWork += new DoWorkEventHandler(bgFlashWorker4_DoWork);
            bgFlashWorker4.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgFlashWorker4_RunWorkerCompleted);
            bgFlashWorker4.WorkerReportsProgress = false;
            bgFlashWorker4.WorkerSupportsCancellation = true;

            return;
        }

        private void bgFlashWorkerCancel()
        {
            try
            {
                if (bgFlashWorker1 != null)
                {
                    bgFlashWorker1.CancelAsync();
                }

                if (bgFlashWorker2 != null)
                {
                    bgFlashWorker2.CancelAsync();
                }

                if (bgFlashWorker3 != null)
                {
                    bgFlashWorker3.CancelAsync();
                }

                if (bgFlashWorker4 != null)
                {
                    bgFlashWorker4.CancelAsync();
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

        #region Image

        private bool CopyOSImage()
        {
            Thread thread = new Thread(this.CopyProcess);
            thread.Start();
            m_b_Running = true;
            m_b_RunReslut = false;

            while (m_b_Running == true)
            {
                Application.DoEvents();
            }

            if (m_b_RunReslut == false)
            {
                return false;
            }

            return true;
        }

        private void CopyProcess()
        {
            DisplayMessage("Copy image.");

            bool bRes = false;
            string strErrorMessage = "";
            for (int i = 0; i < 3; i++)
            {
                if (CopyFromServerToLacal(ref strErrorMessage) == false)
                {
                    DisplayMessage("Failed to copy image:" + strErrorMessage);
                    bRes = false;
                    continue;
                }
                bRes = true;
                break;
            }
            if (bRes == false)
            {
                m_b_Running = false;
                m_b_RunReslut = false;
                return;
            }

            m_b_Running = false;
            m_b_RunReslut = true;

            DisplayMessage("Copy image successfully.");

            return;
        }

        private bool CopyFromServerToLacal(ref string strErrorMessage)
        {
            if (m_st_OptionData.FlashMode == FASTBOOTMODE)
            {
                if (CopyFromServerToLacal_FASTBOOT(ref strErrorMessage) == false)
                {
                    return false;
                }
            }
            else if (m_st_OptionData.FlashMode == EDLMODE)
            {
                if (CopyFromServerToLacal_EDL(ref strErrorMessage) == false)
                {
                    return false;
                }
            }
            else
            {
                strErrorMessage = "Invalid flash mode.";
                return false;
            }

            return true;
        }

        private bool CopyFromServerToLacal_FASTBOOT(ref string strErrorMessage)
        {
            try
            {
                bool bRes = false;

                string strServerPath = m_st_OptionData.ImageServerPath;
                string strLocalPath = m_st_OptionData.ImageLocalPath;
                string strOSPN = m_st_MCFData.OSPN;
                string strOSPNPre = "";
                string strOSVersion = m_st_MCFData.OSVersion;

                #region OSPNPre

                try
                {
                    int iIndex = 0;
                    iIndex = strOSPN.IndexOf("-");
                    if (iIndex == -1)
                    {
                        strErrorMessage = "Invalid OS PN.";
                        return false;
                    }
                    strOSPNPre = strOSPN.Substring(0, iIndex);
                    if (strOSPNPre == "")
                    {
                        strErrorMessage = "Invalid OS PN.";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    string strr = ex.Message;
                    strErrorMessage = "Failed to get os pn pre.";
                    return false;
                }

                #endregion

                strServerPath = strServerPath + "\\" + strOSPNPre + "\\" + strOSPN + "\\" + strOSVersion + "\\";
                strLocalPath = strLocalPath + "\\";

                #region Check Server Path

                if (Directory.Exists(strServerPath) == false)
                {
                    strErrorMessage = "Failed to check server path exist." + strServerPath;
                    return false;
                }

                #endregion

                #region Delete Local Directory

                if (Directory.Exists(strLocalPath) == true)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (DeleteDirectory(strLocalPath, ref strErrorMessage) == false)
                        {
                            bRes = false;
                            Dly(1);
                            continue;
                        }
                        bRes = true;
                        break;
                    }
                    if (bRes == false)
                    {
                        strErrorMessage = "Failed to delete dir." + strLocalPath;
                        return false;
                    }

                    // Check directory empty
                    DirectoryInfo di = new System.IO.DirectoryInfo(strLocalPath);
                    if (di.GetFiles().Length + di.GetDirectories().Length != 0)
                    {
                        strErrorMessage = "Failed to check dir empty." + strLocalPath;
                        return false;
                    }
                }
                else
                {
                    strErrorMessage = "Failed to check local path exist." + strLocalPath;
                    return false;
                }

                #endregion

                #region Copy To Local

                if (Directory.Exists(strServerPath) == false)
                {
                    strErrorMessage = "Failed to check path exist." + strServerPath;
                    return false;
                }

                for (int i = 0; i < 3; i++)
                {
                    if (CopyDirectory(strServerPath, strLocalPath, ref strErrorMessage) == false)
                    {
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    strErrorMessage = "Failed to copy dir from " + strServerPath + " to " + strLocalPath;
                    return false;
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Failed to exception." + strr;
                return false;
            }

            return true;
        }

        private bool CopyFromServerToLacal_EDL(ref string strErrorMessage)
        {
            try
            {
                bool bRes = false;

                string strServerPath = m_st_OptionData.ImageServerPath; //
                string strLocalPath = m_st_OptionData.ImageLocalPath;

                strServerPath = strServerPath + "\\" + m_st_MCFData.OSPN + "\\";
                strLocalPath = strLocalPath + "\\";

                #region Delete Local Directory

                if (Directory.Exists(strLocalPath) == true)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (DeleteFiles(strLocalPath, ref strErrorMessage) == false)
                        {
                            bRes = false;
                            Dly(1);
                            continue;
                        }
                        bRes = true;
                        break;
                    }
                    if (bRes == false)
                    {
                        strErrorMessage = "Failed to delete files." + strLocalPath;
                        return false;
                    }
                }
                Dly(1);
                Directory.CreateDirectory(strLocalPath);
                if (Directory.Exists(strLocalPath) == false)
                {
                    strErrorMessage = "Failed to create directory." + strLocalPath;
                    return false;
                }

                #endregion

                #region Copy To Local

                if (Directory.Exists(strServerPath) == false)
                {
                    strErrorMessage = "Failed to check path exist." + strServerPath;
                    return false;
                }

                for (int i = 0; i < 3; i++)
                {
                    if (CopyDirectory(strServerPath, strLocalPath, ref strErrorMessage) == false)
                    {
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    return false;
                }

                #endregion

                #region Unzip

                string strFilePath = strLocalPath;
                System.IO.DirectoryInfo ImgDictory = new DirectoryInfo(strFilePath);
                System.IO.FileInfo[] ZipFiles = (System.IO.FileInfo[])ImgDictory.GetFiles();
                string strFileName = "";
                foreach (var item in ZipFiles)
                {
                    if (item.Name.Contains(m_st_MCFData.OSVersion))
                    {
                        strFileName = item.Name;
                        break;
                    }
                }
                if (strFileName == "")
                {
                    strErrorMessage = "Failed to check zip file exist." + strFileName;
                    return false;
                }

                for (int i = 0; i < 3; i++)
                {
                    if (UnZip(strFilePath + strFileName, strFilePath, null) == false)
                    {
                        strErrorMessage = "Failed to unzip." + strFileName;
                        bRes = false;
                        Dly(1);
                        continue;
                    }
                    bRes = true;
                    break;
                }
                if (bRes == false)
                {
                    return false;
                }

                #endregion

                m_st_OptionData.ImageLocalPath = m_st_OptionData.ImageLocalPath + "\\" + strFileName.Substring(0, strFileName.Length - 4); // auto add os filename
                DisplayMessage("Local image path:" + m_st_OptionData.ImageLocalPath);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Failed to exception." + strr;
                return false;
            }

            return true;
        }

        private bool DeleteDirectory(string strPath, ref string strErrorMessage)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(strPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo) // 判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true); // 删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName); // 删除指定文件
                    }
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Failed to exception." + strr;
                return false;
            }

            return true;
        }

        private bool DeleteFiles(string strPath, ref string strErrorMessage)
        {
            try
            {
                if (Directory.Exists(strPath) == false)
                {
                    strErrorMessage = "Path is not exist." + strPath;
                    return false;
                }

                DirectoryInfo dir = new DirectoryInfo(strPath);
                FileInfo[] files = dir.GetFiles();
                foreach (var item in files)
                {
                    File.Delete(item.FullName);
                }
                if (dir.GetDirectories().Length != 0)
                {
                    foreach (var item in dir.GetDirectories())
                    {
                        if (!item.ToString().Contains("$") && (!item.ToString().Contains("Boot")))
                        {
                            DeleteFiles(dir.ToString() + "\\" + item.ToString(), ref strErrorMessage);
                        }
                    }
                }

                Directory.Delete(strPath);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Failed to exception." + strr;
                return false;
            }

            return true;
        }

        private bool CopyDirectory(string strSourceDir, string strDestDir, ref string strErrorMessage)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(strSourceDir);

                #region Check

                if (!dir.Exists)
                {
                    strErrorMessage = "Source directory does not exist." + strSourceDir;
                    return false;
                }
                if (!Directory.Exists(strDestDir))
                {
                    Directory.CreateDirectory(strDestDir);
                    if (Directory.Exists(strDestDir) == false)
                    {
                        strErrorMessage = "Dest directory does not exist." + strDestDir;
                        return false;
                    }
                }

                #endregion

                #region Copy File

                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string strTempPath = Path.Combine(strDestDir, file.Name);
                    file.CopyTo(strTempPath, true);
                }

                #endregion

                #region Copy Directory

                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subdir in dirs)
                {
                    string strTempPath = Path.Combine(strDestDir, subdir.Name);
                    CopyDirectory(subdir.FullName, strTempPath, ref strErrorMessage);
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Failed to exception." + strr;
                return false;
            }

            return true;
        }

        private bool UnZip(string fileToUnZip, string zipedFolder, string password)
        {
            try
            {
                FastZip fastZip = new FastZip();
                fastZip.Password = password;
                fastZip.ExtractZip(fileToUnZip, zipedFolder, null);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool CopyFile(string strSrc, string strDest, bool bOverwrite, ref string strErrorMessage)
        {
            try
            {
                if (File.Exists(strSrc) == false)
                {
                    strErrorMessage = "Failed to check src file." + strSrc;
                    return false;
                }

                File.Copy(strSrc, strDest, bOverwrite);

                if (File.Exists(strDest) == false)
                {
                    strErrorMessage = "Failed to copy file." + strDest;
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

        #endregion

        #region Private

        private bool InitRun()
        {
            //string strOptionFileName = "";
            string strErrorMessage = "";

            rtbTestLog.Clear();
            InitBackgroundworker();

            #region Clear COM Port Inuse

            //DisplayMessage("Clear COM Port inuse status.");
            //if (DeleteCOMNameArbiterReg() == false)
            //{
            //    DisplayMessage("Clear COM Port inuse status fail.");
            //    return false;
            //}
            //DisplayMessage("Clear COM Port inuse status successfully.");
            //Dly(0.5);

            #endregion

            #region HWSerNumEmulationReg

            //DisplayMessage("HWSerNumEmulationReg.");
            //if (HWSerNumEmulationReg() == false)
            //{
            //    DisplayMessage("HWSerNumEmulationReg fail.");
            //    return false;
            //}
            //DisplayMessage("HWSerNumEmulationReg successfully.");
            //Dly(0.5);

            #endregion

            #region Option.ini

            DisplayMessage("Read Option.ini file.");
            if (ReadStartupFile(ref strErrorMessage) == false)
            {
                DisplayMessage("Failed to read Option.ini file." + strErrorMessage);
                return false;
            }

            #endregion

            #region Setup.ini

            DisplayMessage("Read Setup.ini file.");
            if (ReadSetupFile(ref strErrorMessage) == false)
            {
                DisplayMessage("Failed to read setup.ini file." + strErrorMessage);
                return false;
            }

            #endregion

            #region ScanMES

            //if (m_st_OptionData.MES_Enable == "1")
            //{
            //    DisplayMessage("MES input.");
            //    if (ScanMES() == false)
            //    {
            //        DisplayMessage("Failed to MES input.");
            //        return false;
            //    }
            //    DisplayMessage("EID:" + m_st_MESData.EID);
            //    DisplayMessage("WorkOrder:" + m_st_MESData.WorkOrder);
            //}

            #endregion

            #region ScanMCF (Obsolete)

            //DisplayMessage("Scan Sheet.");
            //if (ScanMCF() == false)
            //{
            //    DisplayMessage("Failed to Scan Sheet.");
            //    return false;
            //}
            //DisplayMessage("Model:" + m_str_Model);
            //DisplayMessage("SKU:" + m_st_MCFData.SKU);

            #endregion

            #region Select Production Line

            if (SelectLine() == false)
            {
                DisplayMessage("Failed to select production line.");
                return false;
            }
            DisplayMessage("Production Line: " + m_str_PdLine);
         
            #endregion


            #region SKUMatrix

            //DisplayMessage("Parse SKU matrix.");
            //if (SKUMatrix(ref strOptionFileName, ref strErrorMessage) == false)
            //{
            //    DisplayMessage("Failed to match SKU matrix.");
            //    return false;
            //}
            //DisplayMessage("Option:" + strOptionFileName);

            #endregion

            #region Model_Option.ini

            //DisplayMessage("Read model_option ini file." + strOptionFileName);
            //strErrorMessage = "";
            //if (ReadModelOptionFile(strOptionFileName, ref strErrorMessage) == false)
            //{
            //    DisplayMessage("Failed to read model_option.ini file." + strErrorMessage);
            //    return false;
            //}

            #endregion

            #region Check MES Data

            //if (m_st_OptionData.MES_Enable == "1")
            //{
            //    DisplayMessage("MES check data.");

            //    if (MESCheckData(ref strErrorMessage) == false)
            //    {
            //        DisplayMessage("Failed to MES check data." + strErrorMessage);
            //        return false;
            //    }
            //}

            #endregion

            #region Init Data

            DisplayMessage("Init data.");
            if (InitData() == false)
            {
                DisplayMessage("Failed to init data.");
                return false;
            }

            #endregion

            #region InitHW

            if (m_st_OptionData.TestMode == "1")
            {    
                if (m_str_PdLine.Contains("CT40"))
                {
                    if (InitNI6001() == false)
                    {
                        DisplayMessage("Failed to init NI Card.");
                        return false;
                    }

                    if (USBPlugInit(ref strErrorMessage) == false)
                    {
                        DisplayMessage("Failed to init USB Plug out init location." + strErrorMessage);
                        return false;
                    }
                }
                else if (m_str_PdLine.Contains("EDA51") || m_str_PdLine.Contains("EDA52"))
                {
                    if (PLCConnect() == false)
                    {
                        DisplayMessage("Failed to connect PLC......");
                        return false;
                    }
                }
                else
                {
                    DisplayMessage("Unknown Production Line !!!");
                    return false;
                }
               
                m_b_PLCRuning = true;
            }

            #endregion

            #region Copy OS Image

            //if (m_st_OptionData.ImageCopyMode == "1")
            //{
            //    DisplayMessage("Copy image.");

            //    Thread thread = new Thread(this.CopyProcess);
            //    thread.Start();
            //    m_b_Running = true;
            //    m_b_RunReslut = false;
            //}
            //else
            //{
            //    DisplayMessage("Skiped to copy image.");
            //    m_b_Running = false;
            //    m_b_RunReslut = true;
            //}

            #endregion

            #region Timer

            //timerCopyImage.Interval = 10000;
            //timerCopyImage.Enabled = true;
            //timerCopyImage.Tick += new EventHandler(timerCopyImage_Tick);

            #endregion

            #region Timer to Connect

            if (m_st_OptionData.TestMode == "1")
            {
                // Auto Test
                #region Timer

                // 3s
                m_timer_WatchDog = new System.Threading.Timer(Thread_Timer_WatchDog, null, 1000, 3000);

                timerAutoTest.Interval = 5000;
                timerAutoTest.Enabled = true;
                timerAutoTest.Tick += new EventHandler(timerAutoTest_Tick);

                timerKillProcess.Interval = 20000;
                timerKillProcess.Enabled = true;
                timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

                #endregion
            }
            else
            {
                // Manual Test
                #region Timer

                timerMonitor.Interval = 10000;
                timerMonitor.Enabled = true;
                timerMonitor.Tick += new EventHandler(timerMonitorRun_Tick);

                timerDeviceConnect.Interval = 15000;
                timerDeviceConnect.Enabled = true;
                timerDeviceConnect.Tick += new EventHandler(timerMonitorDeviceConnect_Tick);

                timerKillProcess.Interval = 20000;
                timerKillProcess.Enabled = true;
                timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

                #endregion
            }

            DisplayMessage("Timer enabled successfully.");

            #endregion


            return true;
        }

        private bool ScanMCF()
        {
            frmMCF frmMCF = new frmMCF();
            if (frmMCF.ShowDialog() != DialogResult.Yes)
            {
                return false;
            }
            m_st_MCFData.Model = frmMCF.Model;
            m_st_MCFData.SKU = frmMCF.SKU;
            m_st_MCFData.OSPN = frmMCF.OSPN;
            m_st_MCFData.OSVersion = frmMCF.OSVersion;

            m_str_Model = m_st_MCFData.Model;

            #region Get Model by SKU

            string strModel = "";
            if (GetModelBySKU(ref strModel) == false)
            {
                return false;
            }
            m_str_Model = strModel;

            #endregion

            return true;
        }

        private bool SelectLine()
        {
            frmProductionLine frmLine = new frmProductionLine();
            if (frmLine.ShowDialog() != DialogResult.Yes)
            {
                return false;
            }
            m_str_PdLine = frmLine.ProductionLine;

            return true;
        }


        private bool ScanMES()
        {
            FrmMES frmMES = new FrmMES();
            if (frmMES.ShowDialog() != DialogResult.Yes)
            {
                return false;
            }
            m_st_MESData.EID = frmMES.EID;
            m_st_MESData.WorkOrder = frmMES.WorkOrder;

            return true;
        }

        /// <summary>
        /// Parse sku to export model config file in SKUOption.txt
        /// </summary>
        /// <param name="strOptionFileName"></param>
        /// <param name="strErrorMessage"></param>
        /// <returns></returns>
        private bool SKUMatrix(ref string strOptionFileName, ref string strErrorMessage)
        {
            try
            {
                strErrorMessage = "";
                strOptionFileName = "";

                string strFileName = "SKUOption.txt";
                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                #region Delete File

                if (File.Exists(str_FilePath) == true)
                {
                    File.Delete(str_FilePath);
                    Dly(1);
                    if (File.Exists(str_FilePath) == true)
                    {
                        strErrorMessage = "Delete file fail." + str_FilePath;
                        return false;
                    }
                }

                #endregion

                #region Generate File

                bool bRes = false;
                string strBatDir = Application.StartupPath;
                string strBatFile = strBatDir + "\\" + "SKUMatrix.bat";
                string strBatParameter = "";
                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Failed to file exist." + strBatFile;
                    return false;
                }

                strBatParameter = m_str_Model + " " + m_st_MCFData.SKU;

                for (int i = 0; i < 3; i++)
                {
                    bRes = ExcuteBat(strBatDir, strBatFile, strBatParameter, 3000, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(1);
                        continue;
                    }

                    bRes = true;
                    break;
                }

                #endregion

                #region Exist File

                if (File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "Check file exist fail." + str_FilePath;
                    return false;
                }

                #endregion

                #region Read File

                StreamReader sr = null;
                try
                {
                    sr = new StreamReader(str_FilePath, System.Text.Encoding.Default);
                    string strContent = sr.ReadToEnd();
                    strContent = strContent.Replace("\r\n", "");
                    strContent = strContent.Trim();
                    sr.Close();
                    sr = null;

                    if (m_str_Model == "UL")
                    {
                        m_str_Model = "EDA56";
                    }

                    strOptionFileName = Application.StartupPath + "\\" + m_str_Model + "\\" + strContent;
                }
                catch (Exception exx)
                {
                    string strr = exx.Message;
                    strErrorMessage = "Exception to read file." + str_FilePath;
                    return false;
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Close();
                        sr = null;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool GetModelBySKU(ref string strModel)
        {
            try
            {
                strModel = "";
                string strSKU = m_st_MCFData.SKU;

                int iIndex = strSKU.IndexOf("-");

                strModel = strSKU.Substring(0, iIndex);
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool ReadModelOptionFile(string strOptionFileName, ref string strErrorMessage)
        {
            try
            {
                string str_FilePath = "";
                str_FilePath = strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                #region MDCS

                m_st_OptionData.MDCSEnable = objIniFile.ReadString("MDCS", "Enable");
                if ((m_st_OptionData.MDCSEnable != "0") && (m_st_OptionData.MDCSEnable != "1"))
                {
                    strErrorMessage = "Invalid MDCS Enable:" + m_st_OptionData.MDCSEnable;
                    return false;
                }

                m_st_OptionData.MDCSURL = objIniFile.ReadString("MDCS", "URL");
                if (m_st_OptionData.MDCSURL == "")
                {
                    strErrorMessage = "Invalid MDCS URL:" + m_st_OptionData.MDCSURL;
                    return false;
                }

                m_st_OptionData.MDCSDeviceName = objIniFile.ReadString("MDCS", "DeviceName");
                if (m_st_OptionData.MDCSDeviceName == "")
                {
                    strErrorMessage = "Invalid MDCS DeviceName:" + m_st_OptionData.MDCSDeviceName;
                    return false;
                }

                m_st_OptionData.MDCSPreStationResultCheck = objIniFile.ReadString("MDCS", "PreStationResultCheck");
                if ((m_st_OptionData.MDCSPreStationResultCheck != "0") && (m_st_OptionData.MDCSPreStationResultCheck != "1"))
                {
                    strErrorMessage = "Invalid MDCS PreStationResultCheck:" + m_st_OptionData.MDCSPreStationResultCheck;
                    return false;
                }

                m_st_OptionData.MDCSPreStationDeviceName = objIniFile.ReadString("MDCS", "PreStationDeviceName");
                if (m_st_OptionData.MDCSPreStationDeviceName == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationDeviceName:" + m_st_OptionData.MDCSPreStationDeviceName;
                    return false;
                }
                m_st_OptionData.MDCSPreStationVarName = objIniFile.ReadString("MDCS", "PreStationVarName");
                if (m_st_OptionData.MDCSPreStationVarName == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationVarName:" + m_st_OptionData.MDCSPreStationVarName;
                    return false;
                }

                m_st_OptionData.MDCSPreStationVarValue = objIniFile.ReadString("MDCS", "PreStationVarValue");
                if (m_st_OptionData.MDCSPreStationVarValue == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationVarValue:" + m_st_OptionData.MDCSPreStationVarValue;
                    return false;
                }

                #endregion

                #region Image

                m_st_OptionData.ImageCopyMode = objIniFile.ReadString("Image", "CopyMode");
                if ((m_st_OptionData.ImageCopyMode != "0") && (m_st_OptionData.ImageCopyMode != "1"))
                {
                    strErrorMessage = "Invalid Image CopyMode:" + m_st_OptionData.ImageCopyMode;
                    return false;
                }

                if (m_st_OptionData.ImageCopyMode == "1")
                {
                    m_st_OptionData.ImageServerPath = objIniFile.ReadString("Image", "ServerPath");
                    if (m_st_OptionData.ImageServerPath.Substring(m_st_OptionData.ImageServerPath.Length - 1, 1) == "\\")
                    {
                        m_st_OptionData.ImageServerPath = m_st_OptionData.ImageServerPath.Substring(0, m_st_OptionData.ImageServerPath.Length - 1);
                    }
                    if (Directory.Exists(m_st_OptionData.ImageServerPath) == false)
                    {
                        strErrorMessage = "Invalid Image ServerPath:" + m_st_OptionData.ImageServerPath;
                        return false;
                    }
                }
                else
                {
                    m_st_OptionData.ImageServerPath = "";
                }

                m_st_OptionData.ImageLocalPath = objIniFile.ReadString("Image", "LocalPath");
                if (m_st_OptionData.ImageLocalPath.Substring(m_st_OptionData.ImageLocalPath.Length - 1, 1) == "\\")
                {
                    m_st_OptionData.ImageLocalPath = m_st_OptionData.ImageLocalPath.Substring(0, m_st_OptionData.ImageLocalPath.Length - 1);
                }
                if (Directory.Exists(m_st_OptionData.ImageLocalPath) == false)
                {
                    Directory.CreateDirectory(m_st_OptionData.ImageLocalPath);

                    if (Directory.Exists(m_st_OptionData.ImageLocalPath) == false)
                    {
                        strErrorMessage = "Invalid Image LocalPath:" + m_st_OptionData.ImageLocalPath;
                        return false;
                    }
                }

                #endregion

                #region FlashMode

                m_st_OptionData.FlashMode = objIniFile.ReadString("FlashMode", "Mode");
                m_st_OptionData.FlashMode = m_st_OptionData.FlashMode.ToUpper();
                if ((m_st_OptionData.FlashMode != FASTBOOTMODE) && (m_st_OptionData.FlashMode != EDLMODE))
                {
                    strErrorMessage = "Invalid FlashMode Mode:" + m_st_OptionData.FlashMode;
                    return false;
                }

                #endregion

                #region FASTBOOT

                if (m_st_OptionData.FlashMode == FASTBOOTMODE)
                {
                    m_st_OptionData.FASTBOOTBatFile = objIniFile.ReadString("FASTBOOT", "BatFile");
                    if (m_st_OptionData.FASTBOOTBatFile == "")
                    {
                        strErrorMessage = "Invalid FASTBOOT BatFile:" + m_st_OptionData.FASTBOOTBatFile;
                        return false;
                    }

                    m_st_OptionData.FASTBOOTTimeout = objIniFile.ReadInt("FASTBOOT", "Timeout");
                    if (m_st_OptionData.FASTBOOTTimeout < 0)
                    {
                        strErrorMessage = "Invalid FASTBOOT Timeout:" + m_st_OptionData.FASTBOOTTimeout;
                        return false;
                    }

                    m_st_OptionData.FASTBOOTSuccessFlag = objIniFile.ReadString("FASTBOOT", "SuccessFlag");
                    if (m_st_OptionData.FASTBOOTSuccessFlag == "")
                    {
                        strErrorMessage = "Invalid FASTBOOT SuccessFlag:" + m_st_OptionData.FASTBOOTSuccessFlag;
                        return false;
                    }

                    m_st_OptionData.FASTBOOTEnable = objIniFile.ReadString("FASTBOOT", "Enable");
                    if ((m_st_OptionData.FASTBOOTEnable != "0") && (m_st_OptionData.FASTBOOTEnable != "1"))
                    {
                        strErrorMessage = "Invalid FASTBOOT Enable:" + m_st_OptionData.FASTBOOTEnable;
                        return false;
                    }

                    m_st_OptionData.FASTBOOTBatServerPath = objIniFile.ReadString("FASTBOOT", "BatServerPath");
                    if (m_st_OptionData.FASTBOOTBatServerPath == "")
                    {
                        strErrorMessage = "Invalid FASTBOOT BatServerPath:" + m_st_OptionData.FASTBOOTBatServerPath;
                        return false;
                    }

                    m_st_OptionData.FASTBOOTBatLocalPath = "";
                    //m_st_OptionData.FASTBOOTBatLocalPath = objIniFile.ReadString("FASTBOOT", "BatLocalPath");
                    //if (m_st_OptionData.FASTBOOTBatLocalPath == "")
                    //{
                    //    strErrorMessage = "Invalid FASTBOOT BatLocalPath:" + m_st_OptionData.FASTBOOTBatLocalPath;
                    //    return false;
                    //}
                }

                #endregion

                #region EDL

                if (m_st_OptionData.FlashMode == EDLMODE)
                {
                    m_st_OptionData.EDLQFIL = objIniFile.ReadString("EDL", "QFIL");
                    if (m_st_OptionData.EDLQFIL == "")
                    {
                        strErrorMessage = "Invalid EDL QFIL:" + m_st_OptionData.EDLQFIL;
                        return false;
                    }
                    if (File.Exists(m_st_OptionData.EDLQFIL) == false)
                    {
                        strErrorMessage = "File not exist:" + m_st_OptionData.EDLQFIL;
                        return false;
                    }

                    m_st_OptionData.EDLDeviceType = objIniFile.ReadString("EDL", "DeviceType");
                    //if (m_st_OptionData.EDLDeviceType == "")
                    //{
                    //    strErrorMessage = "Invalid EDL DeviceType:" + m_st_OptionData.EDLDeviceType;
                    //    return false;
                    //}

                    m_st_OptionData.EDLFlashType = objIniFile.ReadString("EDL", "FlashType");
                    //if (m_st_OptionData.EDLFlashType == "")
                    //{
                    //    strErrorMessage = "Invalid EDL FlashType:" + m_st_OptionData.EDLFlashType;
                    //    return false;
                    //}

                    m_st_OptionData.EDLELF = objIniFile.ReadString("EDL", "ELF");
                    if (m_st_OptionData.EDLELF == "")
                    {
                        strErrorMessage = "Invalid EDL ELF:" + m_st_OptionData.EDLELF;
                        return false;
                    }

                    m_st_OptionData.EDLPatch = objIniFile.ReadString("EDL", "Patch");
                    if (m_st_OptionData.EDLPatch == "")
                    {
                        strErrorMessage = "Invalid EDL Patch:" + m_st_OptionData.EDLPatch;
                        return false;
                    }

                    m_st_OptionData.EDLRawProgram = objIniFile.ReadString("EDL", "RawProgram");
                    if (m_st_OptionData.EDLRawProgram == "")
                    {
                        strErrorMessage = "Invalid EDL RawProgram:" + m_st_OptionData.EDLRawProgram;
                        return false;
                    }

                    m_st_OptionData.EDLReset = objIniFile.ReadString("EDL", "Reset");
                    if ((m_st_OptionData.EDLReset != "0") && (m_st_OptionData.EDLReset != "1"))
                    {
                        strErrorMessage = "Invalid EDL Reset:" + m_st_OptionData.EDLReset;
                        return false;
                    }

                    m_st_OptionData.EDLTimeout = objIniFile.ReadInt("EDL", "Timeout");
                    if (m_st_OptionData.EDLTimeout < 0)
                    {
                        strErrorMessage = "Invalid EDL Timeout:" + m_st_OptionData.EDLTimeout;
                        return false;
                    }

                    m_st_OptionData.EDLSuccessFlag = objIniFile.ReadString("EDL", "SuccessFlag");
                    if (m_st_OptionData.EDLSuccessFlag == "")
                    {
                        strErrorMessage = "Invalid EDL SuccessFlag:" + m_st_OptionData.EDLSuccessFlag;
                        return false;
                    }
                }

                #endregion

                #region Keybox

                m_st_OptionData.KeyboxEnable = objIniFile.ReadString("Keybox", "Enable");
                if ((m_st_OptionData.KeyboxEnable != "0") && (m_st_OptionData.KeyboxEnable != "1"))
                {
                    strErrorMessage = "Invalid Keybox Enable:" + m_st_OptionData.KeyboxEnable;
                    return false;
                }

                #region Temporary remove, when Enable == "1", just check keybox whether exist, not write keybox.

                m_st_OptionData.KeyboxFilePath = "";
                m_st_OptionData.KeyboxFile = "";
                m_st_OptionData.KeyboxDevice = "";

                //if (m_st_OptionData.KeyboxEnable == "1")
                //{
                //    m_st_OptionData.KeyboxFilePath = objIniFile.ReadString("Keybox", "Path");
                //    if (Directory.Exists(m_st_OptionData.KeyboxFilePath) == false)
                //    {
                //        strErrorMessage = "Invalid Keybox Path:" + m_st_OptionData.KeyboxFilePath;
                //        return false;
                //    }

                //    m_st_OptionData.KeyboxFile = objIniFile.ReadString("Keybox", "File");
                //    string strTemp = Application.StartupPath + "\\" + m_str_Model + "\\" + m_st_OptionData.KeyboxFile;
                //    if (File.Exists(strTemp) == false)
                //    {
                //        strErrorMessage = "File not exist:" + m_st_OptionData.KeyboxFile;
                //        return false;
                //    }

                //    m_st_OptionData.KeyboxDevice = objIniFile.ReadString("Keybox", "Device");
                //    if (m_st_OptionData.KeyboxDevice == "")
                //    {
                //        strErrorMessage = "Invalid Keybox Device:" + m_st_OptionData.KeyboxDevice;
                //        return false;
                //    }
                //}

                #endregion

                #endregion

                #region SentienceKey

                m_st_OptionData.SentienceKeyEnable = objIniFile.ReadString("SentienceKey", "Enable");
                if ((m_st_OptionData.SentienceKeyEnable != "0") && (m_st_OptionData.SentienceKeyEnable != "1"))
                {
                    strErrorMessage = "Invalid SentienceKey Enable:" + m_st_OptionData.SentienceKeyEnable;
                    return false;
                }

                if (m_st_OptionData.SentienceKeyEnable == "1")
                {
                    m_st_OptionData.SentienceKeyHonEdgeProductName = objIniFile.ReadString("SentienceKey", "HonEdgeProductName");
                    if (m_st_OptionData.SentienceKeyHonEdgeProductName == "")
                    {
                        strErrorMessage = "Invalid SentienceKey HonEdgeProductName:" + m_st_OptionData.SentienceKeyHonEdgeProductName;
                        return false;
                    }
                }

                m_st_OptionData.SentienceKeyUploadEnable = objIniFile.ReadString("SentienceKey", "UploadEnable");
                if ((m_st_OptionData.SentienceKeyUploadEnable != "0") && (m_st_OptionData.SentienceKeyUploadEnable != "1"))
                {
                    strErrorMessage = "Invalid SentienceKey UploadEnable:" + m_st_OptionData.SentienceKeyUploadEnable;
                    return false;
                }

                if (m_st_OptionData.SentienceKeyUploadEnable == "1")
                {
                    m_st_OptionData.SentienceKeyUploadServerPath = objIniFile.ReadString("SentienceKey", "UploadServerPath");
                    if (Directory.Exists(m_st_OptionData.SentienceKeyUploadServerPath) == false)
                    {
                        strErrorMessage = "Invalid SentienceKey UploadServerPath:" + m_st_OptionData.SentienceKeyUploadServerPath;
                        return false;
                    }
                }

                #endregion

                #region Station

                m_st_OptionData.StationName = objIniFile.ReadString("Station", "StationName");

                #endregion

                #region SKU

                m_st_OptionData.SKUCheckEnable = objIniFile.ReadString("SKU", "Enable");
                if ((m_st_OptionData.SKUCheckEnable != "0") && (m_st_OptionData.SKUCheckEnable != "1"))
                {
                    strErrorMessage = "Invalid SKU Enable Value:" + m_st_OptionData.SKUCheckEnable;
                    return false;
                }

                #endregion

                #region Check ManualBITResult

                m_st_OptionData.CheckManualBITResult_Enable = objIniFile.ReadString("CheckManualBITResult", "Enable");
                if ((m_st_OptionData.CheckManualBITResult_Enable != "0") && (m_st_OptionData.CheckManualBITResult_Enable != "1"))
                {
                    strErrorMessage = "Invalid CheckManualBITResult Enable Value: " + m_st_OptionData.CheckManualBITResult_Enable;
                    return false;
                }

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

        private bool ReadSetupFile(ref string strErrorMessage)
        {
            try
            {
                string strOptionFileName = "Setup.ini";

                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strOptionFileName;
                clsIniFile objIniFile = new clsIniFile(str_FilePath);

                // Check File Exist
                if (File.Exists(str_FilePath) == false)
                {
                    strErrorMessage = "File not exist." + str_FilePath;
                    return false;
                }

                // PortMapping
                m_st_OptionData.DeviceAddress_Panel1 = objIniFile.ReadString("PortMapping", "Panel_1");
                m_st_OptionData.DeviceAddress_Panel2 = objIniFile.ReadString("PortMapping", "Panel_2");
                m_st_OptionData.DeviceAddress_Panel3 = objIniFile.ReadString("PortMapping", "Panel_3");
                m_st_OptionData.DeviceAddress_Panel4 = objIniFile.ReadString("PortMapping", "Panel_4");
                if (m_st_OptionData.DeviceAddress_Panel1 == "" || m_st_OptionData.DeviceAddress_Panel2 == "" || m_st_OptionData.DeviceAddress_Panel3 == "" || m_st_OptionData.DeviceAddress_Panel4 == "")
                {
                    strErrorMessage = "Port Mapping not config." + strOptionFileName;
                    return false;
                }

                #region Check The Same Port

                if (m_st_OptionData.DeviceAddress_Panel1 == m_st_OptionData.DeviceAddress_Panel2)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel1 == m_st_OptionData.DeviceAddress_Panel3)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel1 == m_st_OptionData.DeviceAddress_Panel4)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel2 == m_st_OptionData.DeviceAddress_Panel3)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel2 == m_st_OptionData.DeviceAddress_Panel4)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }
                if (m_st_OptionData.DeviceAddress_Panel3 == m_st_OptionData.DeviceAddress_Panel4)
                {
                    strErrorMessage = "Setup.ini exist the same port.";
                    return false;
                }

                #endregion

                m_st_OptionData.QDLoaderPortName = objIniFile.ReadString("QDLoader", "PortName");
                if (m_st_OptionData.QDLoaderPortName == "")
                {
                    strErrorMessage = "QDLoader PortName not config." + strOptionFileName;
                    return false;
                }

                m_st_OptionData.ADBDeviceName = objIniFile.ReadString("PortDevice", "DeviceName");
                if (m_st_OptionData.ADBDeviceName == "")
                {
                    strErrorMessage = "PortDevice PortName not config." + strOptionFileName;
                    return false;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                strErrorMessage = "Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool ReadStartupFile(ref string strErrorMessage)
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

                m_st_OptionData.TestMode = objIniFile.ReadString("TestMode", "Mode");
                if ((m_st_OptionData.TestMode != "0") && (m_st_OptionData.TestMode != "1"))
                {
                    strErrorMessage = "Invalid TestMode Mode:" + m_st_OptionData.TestMode;
                    return false;
                }

                #endregion

                #region NI6001

                m_st_OptionData.DAQDevice = objIniFile.ReadString("NI6001", "Device");

                #endregion

                #region PLC

                m_st_OptionData.PLCIP = objIniFile.ReadString("PLC", "PLCIP");
                m_st_OptionData.PLCPort = objIniFile.ReadString("PLC", "PLCPort");

                #endregion

                #region DB Slot

                m_st_OptionData.DB_Slot1_ReadDB = objIniFile.ReadInt("DB_Slot1", "ReadDB");
                m_st_OptionData.DB_Slot1_WriteDB = objIniFile.ReadInt("DB_Slot1", "WriteDB");
                m_st_OptionData.DB_Slot2_ReadDB = objIniFile.ReadInt("DB_Slot2", "ReadDB");
                m_st_OptionData.DB_Slot2_WriteDB = objIniFile.ReadInt("DB_Slot2", "WriteDB");
                m_st_OptionData.DB_Slot3_ReadDB = objIniFile.ReadInt("DB_Slot3", "ReadDB");
                m_st_OptionData.DB_Slot3_WriteDB = objIniFile.ReadInt("DB_Slot3", "WriteDB");
                m_st_OptionData.DB_Slot4_ReadDB = objIniFile.ReadInt("DB_Slot4", "ReadDB");
                m_st_OptionData.DB_Slot4_WriteDB = objIniFile.ReadInt("DB_Slot4", "WriteDB");

                #endregion

                #region Area

                m_st_OptionData.Area_Location = objIniFile.ReadString("Area", "Location");

                #endregion

                #region MES

                m_st_OptionData.MES_Enable = objIniFile.ReadString("MES", "Enable");
                if ((m_st_OptionData.MES_Enable != "0") && (m_st_OptionData.MES_Enable != "1"))
                {
                    strErrorMessage = "Invalid MES Enable:" + m_st_OptionData.MES_Enable;
                    return false;
                }

                #endregion

                #region SDCard

                m_st_OptionData.SDCard_Enable = objIniFile.ReadString("SDCard", "Enable");
                if ((m_st_OptionData.SDCard_Enable != "0") && (m_st_OptionData.SDCard_Enable != "1"))
                {
                    m_st_OptionData.SDCard_Enable = "1";
                    //strErrorMessage = "Invalid SDCard Enable:" + m_st_OptionData.SDCard_Enable;
                    //return false;             
                }

                #endregion

                #region Reboot

                m_st_OptionData.Reboot_WaitTime = objIniFile.ReadString("Reboot", "WaitTime");
                if (m_st_OptionData.Reboot_WaitTime == "")
                {
                    strErrorMessage = "Invalid Reboot WaitTime:" + m_st_OptionData.Reboot_WaitTime;
                    return false;
                }

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

        private bool InitData()
        {
            try
            {
                #region m_dic_UnitDevice

                m_dic_UnitDevice.Clear();

                // Unit1
                UnitDeviceInfo stUnit1 = new UnitDeviceInfo();
                stUnit1.Panel = PANEL_1;
                stUnit1.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel1;
                stUnit1.SN = "";
                stUnit1.Status = "0";
                m_dic_UnitDevice.Add(PANEL_1, stUnit1);

                // Unit2
                UnitDeviceInfo stUnit2 = new UnitDeviceInfo();
                stUnit2.Panel = PANEL_2;
                stUnit2.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel2;
                stUnit2.SN = "";
                stUnit2.Status = "0";
                m_dic_UnitDevice.Add(PANEL_2, stUnit2);

                // Unit3
                UnitDeviceInfo stUnit3 = new UnitDeviceInfo();
                stUnit3.Panel = PANEL_3;
                stUnit3.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel3;
                stUnit3.SN = "";
                stUnit3.Status = "0";
                m_dic_UnitDevice.Add(PANEL_3, stUnit3);

                // Unit4
                UnitDeviceInfo stUnit4 = new UnitDeviceInfo();
                stUnit2.Panel = PANEL_4;
                stUnit4.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel4;
                stUnit4.SN = "";
                stUnit4.Status = "0";
                m_dic_UnitDevice.Add(PANEL_4, stUnit4);

                #endregion

                #region m_dic_TestSaveData

                m_dic_TestSaveData.Clear();

                // Unit1
                TestSaveData objSaveData1 = new TestSaveData();
                objSaveData1.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData1.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData1.TestRecord.SN = "";
                objSaveData1.TestRecord.Model = m_str_Model;
                objSaveData1.TestRecord.SKU = m_st_MCFData.SKU;
                objSaveData1.TestRecord.OSVersion = m_st_MCFData.OSVersion;
                objSaveData1.TestRecord.TestTotalTime = 0;
                objSaveData1.TestResult.TestPassed = false;
                objSaveData1.TestResult.TestFailCode = 0;
                objSaveData1.TestResult.TestFailMessage = "";
                objSaveData1.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_1, objSaveData1);

                // Unit2
                TestSaveData objSaveData2 = new TestSaveData();
                objSaveData2.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData2.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData2.TestRecord.SN = "";
                objSaveData2.TestRecord.Model = m_str_Model;
                objSaveData2.TestRecord.SKU = m_st_MCFData.SKU;
                objSaveData2.TestRecord.OSVersion = m_st_MCFData.OSVersion;
                objSaveData2.TestRecord.TestTotalTime = 0;
                objSaveData2.TestResult.TestPassed = false;
                objSaveData2.TestResult.TestFailCode = 0;
                objSaveData2.TestResult.TestFailMessage = "";
                objSaveData2.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_2, objSaveData2);

                // Unit3
                TestSaveData objSaveData3 = new TestSaveData();
                objSaveData3.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData3.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData3.TestRecord.SN = "";
                objSaveData3.TestRecord.Model = m_str_Model;
                objSaveData3.TestRecord.SKU = m_st_MCFData.SKU;
                objSaveData3.TestRecord.OSVersion = m_st_MCFData.OSVersion;
                objSaveData3.TestRecord.TestTotalTime = 0;
                objSaveData3.TestResult.TestPassed = false;
                objSaveData3.TestResult.TestFailCode = 0;
                objSaveData3.TestResult.TestFailMessage = "";
                objSaveData3.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_3, objSaveData3);

                // Unit4
                TestSaveData objSaveData4 = new TestSaveData();
                objSaveData4.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData4.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData4.TestRecord.SN = "";
                objSaveData4.TestRecord.Model = m_str_Model;
                objSaveData4.TestRecord.SKU = m_st_MCFData.SKU;
                objSaveData4.TestRecord.OSVersion = m_st_MCFData.OSVersion;
                objSaveData4.TestRecord.TestTotalTime = 0;
                objSaveData4.TestResult.TestPassed = false;
                objSaveData4.TestResult.TestFailCode = 0;
                objSaveData4.TestResult.TestFailMessage = "";
                objSaveData4.TestResult.TestStatus = "";
                m_dic_TestSaveData.Add(PANEL_4, objSaveData4);

                #endregion

                #region m_dic_TestStatus

                m_dic_TestStatus.Clear();

                // Unit1
                m_dic_TestStatus.Add(PANEL_1, false);

                // Unit2
                m_dic_TestStatus.Add(PANEL_2, false);

                // Unit3
                m_dic_TestStatus.Add(PANEL_3, false);

                // Unit4
                m_dic_TestStatus.Add(PANEL_4, false);

                #endregion
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
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
                lStartTime = DateTime.Now.Ticks;
                while ((DateTime.Now.Ticks - lStartTime) < lWaitTime)
                {
                    Thread.Sleep(50);
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                return;
            }

            return;
        }

        private void ConfirmDlg(string str_Content)
        {
            frmConfirmOK frm = new frmConfirmOK();
            frm.Content = str_Content;
            frm.ShowDialog();
        }

        private bool ConfirmYesNoDlg(string str_Content)
        {
            frmConfirmYESNO frm = new frmConfirmYESNO();
            frm.Content = str_Content;
            if (frm.ShowDialog() != DialogResult.Yes)
            {
                return false;
            }

            return true;
        }

        private void DisplayMessage(string str_Message)
        {
            try
            {
                if (rtbTestLog.Text.Length > 1000000)
                {
                    rtbTestLog.Clear();
                }

                str_Message = "[" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]:" + str_Message;

                rtbTestLog.AppendText(str_Message + Convert.ToChar(13) + Convert.ToChar(10));
                rtbTestLog.ScrollToCaret();
                rtbTestLog.Refresh();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void DisplayUnit(string str_Index, string str_Text, Color c_BackColor)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        lblUnit1.Text = str_Text;
                        lblUnit1.BackColor = c_BackColor;
                        break;
                    case 2:
                        lblUnit2.Text = str_Text;
                        lblUnit2.BackColor = c_BackColor;
                        break;
                    case 3:
                        lblUnit3.Text = str_Text;
                        lblUnit3.BackColor = c_BackColor;
                        break;
                    case 4:
                        lblUnit4.Text = str_Text;
                        lblUnit4.BackColor = c_BackColor;
                        break;
                    default:
                        break;
                }
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void DisplayUnitStatus(string str_Index, string str_Text, Color c_BackColor)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        lblUnit1Status.Text = str_Text;
                        lblUnit1Status.BackColor = c_BackColor;
                        break;
                    case 2:
                        lblUnit2Status.Text = str_Text;
                        lblUnit2Status.BackColor = c_BackColor;
                        break;
                    case 3:
                        lblUnit3Status.Text = str_Text;
                        lblUnit3Status.BackColor = c_BackColor;
                        break;
                    case 4:
                        lblUnit4Status.Text = str_Text;
                        lblUnit4Status.BackColor = c_BackColor;
                        break;
                    default:
                        break;
                }
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void DisplayUnitLog(string str_Index, string str_Text)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        rtbUnit1Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit1Log.ScrollToCaret();
                        rtbUnit1Log.Refresh();
                        break;
                    case 2:
                        rtbUnit2Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit2Log.ScrollToCaret();
                        rtbUnit2Log.Refresh();
                        break;
                    case 3:
                        rtbUnit3Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit3Log.ScrollToCaret();
                        rtbUnit3Log.Refresh();
                        break;
                    case 4:
                        rtbUnit4Log.AppendText(str_Text + Convert.ToChar(13) + Convert.ToChar(10));
                        rtbUnit4Log.ScrollToCaret();
                        rtbUnit4Log.Refresh();
                        break;
                    default:
                        break;
                }
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private void ClearUnitLog(string str_Index)
        {
            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        rtbUnit1Log.Clear();
                        break;
                    case 2:
                        rtbUnit2Log.Clear();
                        break;
                    case 3:
                        rtbUnit3Log.Clear();
                        break;
                    case 4:
                        rtbUnit4Log.Clear();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return;
            }

            return;
        }

        private bool SaveUnitTestReport(string str_Index)
        {
            string str_Path = "";
            string str_FileName = "";
            string str_SN = "";

            str_Path = Application.StartupPath + "\\Data";
            str_FileName = "TestReport.txt";
            str_SN = m_dic_UnitDevice[str_Index].SN;

            if (str_SN == "")
            {
                str_FileName = "";
                str_FileName = "TestReport.txt";
            }
            else
            {
                str_FileName = "";
                string str_DateTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                //str_FileName = str_SN + "_TestReport.txt";
                str_FileName = str_SN + "_" + str_DateTime + "_TestReport.txt";
            }

            if (System.IO.Directory.Exists(str_Path) == false)
            {
                System.IO.Directory.CreateDirectory(str_Path);
            }
            if (System.IO.File.Exists(str_Path + "\\" + str_FileName))
            {
                try
                {
                    System.IO.File.Delete(str_Path + "\\" + str_FileName);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                    return false;
                }
            }

            try
            {
                int i_Index = int.Parse(str_Index);
                switch (i_Index)
                {
                    case 1:
                        rtbUnit1Log.Refresh();
                        rtbUnit1Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    case 2:
                        rtbUnit2Log.Refresh();
                        rtbUnit2Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    case 3:
                        rtbUnit3Log.Refresh();
                        rtbUnit3Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    case 4:
                        rtbUnit4Log.Refresh();
                        rtbUnit4Log.SaveFile(str_Path + "\\" + str_FileName, RichTextBoxStreamType.PlainText);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message;
                return false;
            }

            return true;
        }

        private bool SaveUnitTestFile(string str_Index, string str_Content)
        {
            string str_Path = "";
            string str_FileName = "";
            string str_SN = "";

            str_Path = Application.StartupPath + "\\Data";
            str_FileName = "TestReport.txt";
            str_SN = m_dic_UnitDevice[str_Index].SN;

            if (str_SN == "")
            {
                str_FileName = "";
                str_FileName = "TestReport.txt";
            }
            else
            {
                str_FileName = "";
                str_FileName = str_SN + "_TestReport.txt";
            }

            if (System.IO.Directory.Exists(str_Path) == false)
            {
                System.IO.Directory.CreateDirectory(str_Path);
            }
            if (System.IO.File.Exists(str_Path + "\\" + str_FileName))
            {
                try
                {
                    System.IO.File.Delete(str_Path + "\\" + str_FileName);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                    return false;
                }
            }

            FileStream fs = null;
            try
            {
                fs = new FileStream(str_Path + "\\" + str_FileName, FileMode.Create);

                //获得字节数组
                byte[] data = System.Text.Encoding.Default.GetBytes(str_Content);

                //开始写入
                fs.Write(data, 0, data.Length);

                //清空缓冲区、关闭流
                fs.Flush();

                fs.Close();
                fs = null;
            }
            catch (Exception ex)
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }

                string str = ex.Message;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }

            return true;
        }

        private bool SaveBatContentFile(string str_Index, string str_Content)
        {
            string str_Path = "";
            string str_FileName = "";
            string str_SN = "";

            //str_Path = Application.StartupPath + "\\QFIL\\LOG";
            str_Path = Application.StartupPath + "\\LOG";
            str_FileName = "TestLog.txt";
            str_SN = m_dic_UnitDevice[str_Index].SN;

            if (str_SN == "")
            {
                str_FileName = "";
                str_FileName = "TestLog.txt";
            }
            else
            {
                str_FileName = "";
                str_FileName = str_SN + "_TestLog.txt";
            }

            if (System.IO.Directory.Exists(str_Path) == false)
            {
                System.IO.Directory.CreateDirectory(str_Path);
            }
            if (System.IO.File.Exists(str_Path + "\\" + str_FileName))
            {
                try
                {
                    System.IO.File.Delete(str_Path + "\\" + str_FileName);
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                    return false;
                }
            }

            FileStream fs = null;
            try
            {
                fs = new FileStream(str_Path + "\\" + str_FileName, FileMode.Create);

                //获得字节数组
                byte[] data = System.Text.Encoding.Default.GetBytes(str_Content);

                //开始写入
                fs.Write(data, 0, data.Length);

                //清空缓冲区、关闭流
                fs.Flush();

                fs.Close();
                fs = null;
            }
            catch (Exception ex)
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }

                string str = ex.Message;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }

            return true;
        }

        private bool WriteLogFile(string str_Content)
        {
            string str_Path = "";
            string str_FileName = "";
            string str_PathFileName = "";
            str_Path = Application.StartupPath + "\\LOG\\PLCLOG";
            str_FileName = "Test.log";
            str_PathFileName = str_Path + "\\" + str_FileName;

            try
            {
                if (Directory.Exists(str_Path) == false)
                {
                    Directory.CreateDirectory(str_Path);
                    Thread.Sleep(500);
                    if (Directory.Exists(str_Path) == false)
                    {
                        return false;
                    }
                }

                if (File.Exists(str_PathFileName) == false)
                {
                    StreamWriter sr = File.CreateText(str_PathFileName);
                    sr.Close();
                    Thread.Sleep(500);
                    if (File.Exists(str_PathFileName) == false)
                    {
                        return false;
                    }
                }

                // 大于2M
                FileInfo fileInfo = new System.IO.FileInfo(str_PathFileName);
                if (fileInfo.Length / (1024 * 1024) > 2)
                {
                    string str_NewFileName = str_Path + "\\" + "Test_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";
                    fileInfo.MoveTo(str_NewFileName);

                    StreamWriter sr = File.CreateText(str_PathFileName);
                    sr.WriteLine("[" + DateTime.Now.ToString() + "] " + str_Content);
                    sr.Close();
                }
                else
                {
                    StreamWriter sr = File.AppendText(str_PathFileName);
                    sr.WriteLine("[" + DateTime.Now.ToString() + "] " + str_Content);
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        private bool SaveLogFile(string str_Content)
        {
            lock (SaveLogLocker)
            {
                bool b_Res = false;
                for (int i = 0; i < 2; i++)
                {
                    b_Res = WriteLogFile(str_Content);
                    if (b_Res == true)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                }

                if (b_Res == false)
                {
                    return false;
                }

                return true;
            }
        }

        private void CollapseMenu(bool enable)
        {
            if (enable == true) //Collapse Menu
            {
                panelMenu.Width = 80;
                picBoxLogo.Visible = false;
                panelInfo.Visible = false;
                btnHome.Visible = true;
                btnHome.Dock = DockStyle.Top;

                //foreach (Button menuButton in panelMenu.Controls.OfType<Button>())
                //{
                //    menuButton.Text = "";
                //    menuButton.ImageAlign = ContentAlignment.MiddleCenter;
                //    menuButton.Padding = new Padding(0);
                //}
            }
            else  //Expand Menu
            {
                panelMenu.Width = 200;
                btnHome.Visible = false;
                //btnHome.Dock = DockStyle.None;
                picBoxLogo.Visible = true;
                picBoxLogo.Image = Resources.HoneywellLog_150;
                picBoxLogo.Dock = DockStyle.Fill;
                panelInfo.Visible = true;

                //foreach (Button menuButton in panelMenu.Controls.OfType<Button>())
                //{
                //    menuButton.Text = "       " + menuButton.Tag.ToString();    // Tag
                //    menuButton.ImageAlign = ContentAlignment.MiddleLeft;
                //    menuButton.Padding = new Padding(10, 0, 10, 0);
                //}
            }
        }

        #endregion

        #region Event

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                // Maximized
                this.WindowState = FormWindowState.Maximized;
                this.btnMaximize.Image = Resources.normal_16;
                if (m_bCollapse)
                {
                    this.panelMenu.Width = 80;
                }
                else
                {
                    this.panelMenu.Width = 250;
                }
                
                this.panelLog.Height = 160;
            }
            else
            {
                // Normal
                this.WindowState = FormWindowState.Normal;
                this.btnMaximize.Image = Resources.maximize_16;
                if (m_bCollapse)
                {
                    this.panelMenu.Width = 80;
                }
                else
                {
                    this.panelMenu.Width = 200;
                }

                this.panelLog.Height = 145;
            }
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void lblTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.panelMenu.Width > 200)
            {
                this.panelMenu.Width = 200;
            }
            this.panelLog.Height = 145;

            Win32.ReleaseCapture();
            Win32.SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private void lblTitleBar_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Only can use mouse right button, because left button event conflict with MouseDown event.
            if (e.Button == MouseButtons.Right)
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    this.WindowState = FormWindowState.Maximized;
                    this.btnMaximize.Image = Resources.normal_16;
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                    this.btnMaximize.Image = Resources.maximize_16;
                }
            }
        }

        private void picBoxLogo_Click(object sender, EventArgs e)
        {
            m_bCollapse = true;
            CollapseMenu(true);
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            m_bCollapse = false;
            CollapseMenu(false);
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            this.lblTitleBar.Width = this.Width - 105;

        }

        private void panelDesktop_Resize(object sender, EventArgs e)
        {
            int InnerHeight = this.panelUnits.Height - (this.panelUnits.Padding.Top + this.panelUnits.Padding.Bottom);
            int InnerWidth = this.panelUnits.Width - (this.panelUnits.Padding.Left + this.panelUnits.Padding.Right);

            this.panelUnitTop.Height = InnerHeight / 2;
            this.panelUnitTop.Width = InnerWidth / 2;
            this.panelUnitBottom.Height = InnerHeight / 2;
            this.panelUnitBottom.Width = InnerWidth / 2;

            this.panelUnit1.Width = InnerWidth / 2;
            this.panelUnit1.Height = InnerHeight / 2;

            this.panelUnit2.Width = InnerWidth / 2;
            this.panelUnit2.Height = InnerHeight / 2;

            this.panelUnit3.Width = InnerWidth / 2;
            this.panelUnit3.Height = InnerHeight / 2;

            this.panelUnit4.Width = InnerWidth / 2;
            this.panelUnit4.Height = InnerHeight / 2;

          
        }

        


        #endregion

        #region Override
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams paras = base.CreateParams;
                paras.ExStyle |= 0x02000000;
                return paras;
            }
        }

        #endregion

    }
}
