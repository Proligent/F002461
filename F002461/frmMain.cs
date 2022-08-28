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
            public string Area_Location;
            public string MES_Enable;
            public string MES_Station;
            public string SDCard_Enable;
            public string Reboot_WaitTime;

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

            // Check SKU
            public string SKUCheckEnable;

            // ScanSheet
            public string ScanSheetStation;

            // MDCS
            public string MDCSEnable;
            public string MDCSURL;
            public string MDCSDeviceName;
            public string MDCSPreStationResultCheck;
            public string MDCSPreStationDeviceName;
            public string MDCSPreStationVarName;
            public string MDCSPreStationVarValue;   
        }

        #region Obsolete
        //private struct MCFData
        //{
        //    public string Model;
        //    public string SKU;
        //    public string OSPN;
        //    public string OSVersion;
        //}

        //private struct MESData
        //{
        //    public string EID;
        //    public string WorkOrder;
        //}
        #endregion

        private struct UnitDeviceInfo
        {
            public string Panel;  
            public string SN;
            public string SKU;
            public string Model;
            public string EID;
            public string WorkOrder;
            public string OSPN;
            public string OSVersion;
            public string Status;
            public string PhysicalAddress;
        }

        #endregion

        #region Variable

        private bool m_bCollapse = true;
        private string m_str_Model = "";
        private string m_str_PdLine = "";        
        //private MCFData m_st_MCFData = new MCFData();
        //private MESData m_st_MESData = new MESData();
        private OptionData m_st_OptionData = new OptionData();
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

        //private bool m_b_Setting = false;
        private bool m_b_Running = false;
        private bool m_b_RunReslut = false;

        private clsNI6001 m_obj_Fixctrl = null;
        private CPLCDave m_obj_PLC = null;
        private System.Threading.Timer m_timer_WatchDog = null;
        private bool m_b_PLCRuning = false;
        private int m_i_WatchDog = 0;

        private static readonly object SaveLogLocker = new object();
        private static readonly object ThreadLocker = new object();

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
                lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + "[Auto Test]";
            }
            else
            {
                //lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + " [Manual Test] " + m_st_MCFData.SKU + " " + m_st_MESData.EID + " " + m_st_MESData.WorkOrder;
                lblTitleBar.Text = Program.g_str_ToolNumber + " : " + Program.g_str_ToolRev + "[Manual Test]";
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
                if (m_str_PdLine.Contains("CT40"))
                {
                    string strErrorMessage = "";
                    USBPlugInit(ref strErrorMessage);
                    ReleaseNI6001();
                    PLCRelease();
                }
                else if (m_str_PdLine.Contains("EDA51") || m_str_PdLine.Contains("EDA52") || m_str_PdLine.Contains("EDA56") || m_str_PdLine.Contains("EDA5S"))
                {
                    PLCRelease();
                }
            }
        }

        #endregion

        #region Menu

        private void PortMapping_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //m_b_Setting = true;
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
            frm.m_str_Model = m_str_PdLine;
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

        //private void timerCopyImage_Tick(object sender, EventArgs e)
        //{
        //    if (m_b_Running == false)
        //    {
        //        if (m_b_RunReslut == true)
        //        {
        //            // Image copy finished
        //            timerCopyImage.Enabled = false;

        //            #region Copy Bat file from server to local

        //            if (m_st_OptionData.FlashMode == FASTBOOTMODE)
        //            {
        //                string strErrorMessage = "";
        //                DisplayMessage("Copy bat file from server to local.");
        //                if (CopyBatFileFromServerToLocal(ref strErrorMessage) == false)
        //                {
        //                    DisplayMessage("Failed to copy bat file from server." + strErrorMessage);
        //                    return;
        //                }
        //                DisplayMessage("Copy bat file from server to local successfully.");
        //            }

        //            #endregion

        //            #region Copy Bat file to image path

        //            if (m_st_OptionData.FlashMode == FASTBOOTMODE)
        //            {
        //                string strErrorMessage = "";
        //                DisplayMessage("Copy bat file to image path.");
        //                if (CopyBatFile(ref strErrorMessage) == false)
        //                {
        //                    DisplayMessage("Failed to copy bat file to image path." + strErrorMessage);
        //                    return;
        //                }
        //                DisplayMessage("Copy bat file to image path successfully.");
        //            }

        //            #endregion

        //            if (m_b_Setting == true)
        //            {
        //                DisplayMessage("Re-run the test tool after config port mapping ......");
        //                return;
        //            }

        //            if (m_st_OptionData.TestMode == "1")
        //            {
        //                // Auto Test
        //                #region Timer

        //                // 3s
        //                m_timer_WatchDog = new System.Threading.Timer(Thread_Timer_WatchDog, null, 1000, 3000);

        //                timerAutoTest.Interval = 5000;
        //                timerAutoTest.Enabled = true;
        //                timerAutoTest.Tick += new EventHandler(timerAutoTest_Tick);

        //                timerKillProcess.Interval = 20000;
        //                timerKillProcess.Enabled = true;
        //                timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

        //                DisplayMessage("Timer enabled sucessfully.");

        //                #endregion
        //            }
        //            else
        //            {
        //                // Manual Test
        //                #region Timer

        //                timerMonitor.Interval = 10000;
        //                timerMonitor.Enabled = true;
        //                timerMonitor.Tick += new EventHandler(timerMonitorRun_Tick);

        //                timerDeviceConnect.Interval = 15000;
        //                timerDeviceConnect.Enabled = true;
        //                timerDeviceConnect.Tick += new EventHandler(timerMonitorDeviceConnect_Tick);

        //                timerKillProcess.Interval = 20000;
        //                timerKillProcess.Enabled = true;
        //                timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

        //                DisplayMessage("Timer enabled sucessfully.");

        //                #endregion
        //            }
        //        }
        //        else
        //        {
        //            DisplayMessage("Failed to copy image.");
        //        }
        //    }
        //    else
        //    {
        //        DisplayMessage("Copying ......");
        //    }
        //}

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

        //private void timerMonitorDeviceConnect_Tick(object sender, EventArgs e)
        //{
        //    timerDeviceConnect.Enabled = false;

        //    MonitorDeviceConnected(PANEL_1);
        //    Dly(2);
        //    MonitorDeviceConnected(PANEL_2);
        //    Dly(2);
        //    MonitorDeviceConnected(PANEL_3);
        //    Dly(2);
        //    MonitorDeviceConnected(PANEL_4);
        //    Dly(2);

        //    timerDeviceConnect.Enabled = true;
        //}

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

                        // Init
                        UnitDeviceInfo stUnit = new UnitDeviceInfo();
                        stUnit.Panel = strPanel;
                        stUnit.PhysicalAddress = strPhysicalAddress;
                        stUnit.SN = strSN;
                        stUnit.SKU = "";
                        stUnit.Model = "";
                        stUnit.EID= "";
                        stUnit.WorkOrder= "";
                        stUnit.OSPN = "";
                        stUnit.OSVersion = "";
                        stUnit.Status = "1";
                        m_dic_UnitDevice[strPanel] = stUnit;

                        DisplayUnit(strPanel, strSN, Color.White);
                        DisplayUnitStatus(strPanel, STATUS_CONNECTED, Color.MediumSpringGreen);

                        #endregion

                        RunFlashWorker(strPanel); //Async 

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

        //private bool MonitorDeviceConnected(string strPanel)
        //{
        //    try
        //    {
        //        #region FASTBOOTMODE

        //        if (m_st_OptionData.FlashMode == FASTBOOTMODE)
        //        {
        //            if (MonitorADBConnected(strPanel) == false)
        //            {
        //                string strStatus = m_dic_UnitDevice[strPanel].Status;
        //                if (strStatus == "P" || strStatus == "F")
        //                {
        //                    // 测试完成设备会重启，不检测断开
        //                }
        //                else
        //                {
        //                    UnitDeviceInfo stUnit = new UnitDeviceInfo();     
        //                    stUnit.Panel = m_dic_UnitDevice[strPanel].Panel;
        //                    stUnit.PhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
        //                    stUnit.SN = m_dic_UnitDevice[strPanel].SN;
        //                    stUnit.SKU = "";
        //                    stUnit.Model = "";
        //                    stUnit.EID = "";
        //                    stUnit.WorkOrder = "";
        //                    stUnit.OSPN = "";
        //                    stUnit.OSVersion = "";
        //                    stUnit.Status = "0";
        //                    m_dic_UnitDevice[strPanel] = stUnit;

        //                    DisplayUnit(strPanel, m_dic_UnitDevice[strPanel].SN, Color.White);
        //                    DisplayUnitStatus(strPanel, STATUS_DISCONNECTED, Color.Orange);
        //                }
        //            }
        //            else
        //            {
        //            }
        //        }

        //        #endregion

        //        #region EDLMODE

        //        if (m_st_OptionData.FlashMode == EDLMODE)
        //        {
        //            if (MonitorADBConnected(strPanel) == false && MonitorPortConnected(strPanel) == false)
        //            {
        //                string strStatus = m_dic_UnitDevice[strPanel].Status;
        //                if (strStatus == "P" || strStatus == "F")
        //                {
        //                    // 测试完成设备会重启，不检测断开
        //                }
        //                else
        //                {
        //                    UnitDeviceInfo stUnit = new UnitDeviceInfo();
        //                    stUnit.Panel = m_dic_UnitDevice[strPanel].Panel;
        //                    stUnit.PhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
        //                    stUnit.SN = m_dic_UnitDevice[strPanel].SN;
        //                    stUnit.SKU = "";
        //                    stUnit.Model = "";
        //                    stUnit.EID = "";
        //                    stUnit.WorkOrder = "";
        //                    stUnit.OSPN = "";
        //                    stUnit.OSVersion = "";
        //                    stUnit.Status = "0";
        //                    m_dic_UnitDevice[strPanel] = stUnit;

        //                    DisplayUnit(strPanel, m_dic_UnitDevice[strPanel].SN, Color.White);
        //                    DisplayUnitStatus(strPanel, STATUS_DISCONNECTED, Color.Orange);
        //                }
        //            }
        //            else
        //            {
        //            }
        //        }

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        string strr = ex.Message;
        //        DisplayMessage("Monitor device connected Exception:" + strr);
        //        return false;
        //    }

        //    return true;
        //}

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
                strDeviceName = m_st_OptionData.ADBDeviceName;
                strPhysicalAddress = m_dic_UnitDevice[strPanel].PhysicalAddress;
                
                // Init
                UnitDeviceInfo stUnit1 = new UnitDeviceInfo();
                stUnit1.Panel = strPanel;
                stUnit1.PhysicalAddress = strPhysicalAddress;
                stUnit1.SN = "";
                stUnit1.SKU = "";
                stUnit1.Model = "";
                stUnit1.EID = "";
                stUnit1.WorkOrder = "";
                stUnit1.OSPN = "";
                stUnit1.OSVersion = "";
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

                        UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];     
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

                #region InitData

                InitMDCSData(strPanel);
                InitModelOptionData(strPanel);

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
                // Remark: WorkOrder sometimes maybe not written in property, get from scansheet.
                if (bRes == true)
                {
                    bRes = TestGetWorkOrder(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to get WorkOrder property." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Get EID Property

                if (bRes == true)
                {
                    bRes = TestGetEID(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to get EID property." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Read Model_Option.ini

                if (bRes == true)
                {
                    bRes = TestReadModelOption(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to read Model_Option.ini." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Check MES Data

                if (bRes == true)
                {
                    bRes = TestCheckMESData(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to check MES data." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Get ScanSheet

                if (bRes == true)
                {
                    bRes = TestGetScanSheet(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bUpdateMDCS = false;
                        bRes = false;
                        strErrorMessage = "Failed to get scansheet." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Test Copy Image

                if (bRes == true)
                {
                    lock(ThreadLocker)
                    {
                        bRes = TestCopyImage(strPanel, ref strErrorMessage);
                        if (bRes == false)
                        {
                            bUpdateMDCS = false;
                            bRes = false;
                            strErrorMessage = "Failed to copy image." + strErrorMessage;
                        }
                        else
                        {
                            bUpdateMDCS = true;
                            bRes = true;
                        }
                    }
                }

                #endregion

                #region Test Check Pre Station Result

                if (bRes == true)
                {
                    //bRes = TestCheckPreStationResult(strPanel, ref strErrorMessage);
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

                //if (bRes == true)
                //{
                //    bRes = TestCheckSKU(strPanel, ref strErrorMessage);
                //    if (bRes == false)
                //    {
                //        bUpdateMDCS = false;
                //        bRes = false;
                //        strErrorMessage = "Failed to check SKU." + strErrorMessage;
                //    }
                //    else
                //    {
                //        bUpdateMDCS = true;
                //        bRes = true;
                //    }
                //}

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
                        bUpdateMDCS = true;
                        bRes = true;
                    }
                }

                #endregion

                #region Test Keybox (Write or Check Keybox Whether Exist)

                if (bRes == true)
                {
                    bRes = TestKeybox(strPanel, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        strErrorMessage = "Failed to write or check Keybox." + strErrorMessage;
                    }
                    else
                    {
                        bUpdateMDCS = true;
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

                    string FlashMode = m_dic_ModelOption[strPanel].FlashMode;
                    if (FlashMode == FASTBOOTMODE)
                    {
                        //bRes = TestFastbootFlash(strPanel, ref strErrorMessage);
                    }
                    else if (FlashMode == EDLMODE)
                    {
                        //bRes = TestEDLFlash(strPanel, ref strErrorMessage);       
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
                objSaveData.TestRecord.Model = m_dic_UnitDevice[strPanel].Model;
                objSaveData.TestRecord.SKU = m_dic_UnitDevice[strPanel].SKU;
                objSaveData.TestRecord.OSVersion = m_dic_UnitDevice[strPanel].OSVersion;
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

                if (m_dic_ModelOption[strPanel].MDCSEnable == "1")
                {
                    if (bUpdateMDCS == true)
                    {
                        bool bSaveMDCS = false;
                        bSaveMDCS = SaveMDCS(strPanel, objSaveData);
                        if (bSaveMDCS == false)
                        {
                            bSaveMDCS = SaveMDCS(strPanel, objSaveData);
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
                        bool bPassFailFlag = false;
                        string strEID = m_dic_UnitDevice[strPanel].EID;               
                        string strStation = m_st_OptionData.MES_Station;
                        string strWorkOrder = m_dic_UnitDevice[strPanel].WorkOrder;
                        string strSN = m_dic_UnitDevice[strPanel].SN;

                        if (objSaveData.TestResult.TestPassed == true)
                        {
                            bPassFailFlag = true;
                        }
                        else
                        {
                            bPassFailFlag = false;
                        }

                        bUploadMES = MESUploadData(strEID, strStation, strWorkOrder, strSN, bPassFailFlag, ref strTempErrorMsg);

                        if (bUploadMES == false)
                        {
                            bUploadMES = MESUploadData(strEID, strStation, strWorkOrder, strSN, bPassFailFlag, ref strTempErrorMsg);
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
                    if (m_str_PdLine.Contains("CT40"))
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
                string MDCSPreStationResultCheck = m_dic_ModelOption[strPanel].MDCSPreStationResultCheck;
                string MDCSURL = m_dic_ModelOption[strPanel].MDCSURL;
                string MDCSPreStationDeviceName = m_dic_ModelOption[strPanel].MDCSPreStationDeviceName;
                string MDCSPreStationVarName = m_dic_ModelOption[strPanel].MDCSPreStationVarName;
                string MDCSPreStationVarValue = m_dic_ModelOption[strPanel].MDCSPreStationVarValue;

                if (MDCSPreStationResultCheck == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "\r\n"); });
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check Pre Station Result."); });
                    string str_SN = m_dic_UnitDevice[strPanel].SN;
                    if (str_SN == "")
                    {
                        strErrorMessage = "Invalid SN." + str_SN;
                        return false;
                    }
                    string str_ErrorMessage = "";
                    clsMDCS obj_SaveMDCS = new clsMDCS();
                    obj_SaveMDCS.ServerName = MDCSURL;
                    obj_SaveMDCS.DeviceName = MDCSPreStationDeviceName;
                    obj_SaveMDCS.UseModeProduction = true;

                    bool bRes = false;
                    string strValue = "";
                    for (int i = 0; i < 3; i++)
                    {
                        bRes = obj_SaveMDCS.GetMDCSVariable(MDCSPreStationDeviceName, MDCSPreStationVarName, str_SN, ref strValue, ref str_ErrorMessage);
                        if (bRes == false)
                        {
                            bRes = false;
                            strErrorMessage = "GetMDCSVariable fail.";
                            Dly(2);
                            continue;
                        }
                        else
                        {
                            if (strValue != MDCSPreStationVarValue)
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
                string strModel = "";
                if (GetSKUProperty(strPanel, ref strSKU, ref strErrorMessage) == false)
                {
                    return false;
                }

                int index = strSKU.IndexOf("-");
                strModel = strSKU.Substring(0, index);

                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.SKU = strSKU;
                stUnit.Model = strModel;
                m_dic_UnitDevice[strPanel] = stUnit;

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get SKU: " + strSKU + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetSKU Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestGetWorkOrder(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Get WorkOrder Property."); });

                string strWorkOrder = "";
                if (GetWorkOrderProperty(strPanel, ref strWorkOrder, ref strErrorMessage) == false)
                {
                    return false;
                }

                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.WorkOrder = strWorkOrder;          
                m_dic_UnitDevice[strPanel] = stUnit;

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get WorkOrder: " + strWorkOrder + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetWorkOrder Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestGetEID(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Get EID Property."); });

                string strEID = "";
                if (GetEIDProperty(strPanel, ref strEID, ref strErrorMessage) == false)
                {
                    return false;
                }

                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.EID = strEID;
                m_dic_UnitDevice[strPanel] = stUnit;

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get EID: " + strEID + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetEID Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestReadModelOption(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strOptionFileName = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Read ModelOption.ini file."); });

                #region SKUMatrix

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Parse SKU matrix."); });

                if (SKUMatrix(strPanel, ref strOptionFileName, ref strErrorMessage) == false)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Failed to execute SKUMatrix.bat."); });
                    return false;
                }
                string filename = Path.GetFileName(strOptionFileName);
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Model_Option:" + filename); });

                #endregion

                #region Model_Option.ini

                strErrorMessage = "";
                if (ReadModelOptionFile(strPanel, strOptionFileName, ref strErrorMessage) == false)
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Failed to read model option.ini."); });
                    return false;
                }

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Read ModelOption.ini file success." + "\r\n"); });

                #endregion
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetEID Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestCheckMESData(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                string EID = m_dic_UnitDevice[strPanel].EID;
                string StationName = m_st_OptionData.MES_Station;
                string WorkOrder = m_dic_UnitDevice[strPanel].WorkOrder;

                if (m_st_OptionData.MES_Enable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check MES Data."); });

                    if (MESCheckData(EID, StationName, WorkOrder, ref strErrorMessage) == false)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Failed to check MES data." + "\r\n"); });
                        return false;
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Check MES data successful." + "\r\n"); });
                    }
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skip to check MES data." + "\r\n"); });    
                }

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestCheckMESData Exception:" + ex.Message;
                return false;
            }
        }

        private bool TestGetScanSheet(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Get ScanSheet."); });

                string strSKU = m_dic_UnitDevice[strPanel].SKU;
                string strStation = m_dic_ModelOption[strPanel].ScanSheetStation;
                string strOSPN = "";
                string strOSVersion = "";

                bool bFlag = false;
                ApiResult result = ScanSheet.Get(strSKU);
                if (result.Status == 0)
                {
                    string Station = "";
                    string BarcodeValue = "";
                    string[] strArray;

                    string sJasonStr = JsonConvert.SerializeObject(result);
                    ScanSheetRes res = JsonConvert.DeserializeObject<ScanSheetRes>(sJasonStr);

                    foreach(var item in res.Data)
                    {
                        Station = item.Station.ToString();
                        if (Station.Contains(strStation))    // ScanSheet Station
                        {
                            bFlag = true;
                            BarcodeValue = item.BarCodeValue.ToString();
                            break;
                        }           
                    }

                    if (bFlag == false)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get Station fail." + "\r\n"); });
                        return false;
                    }

                    if (BarcodeValue != "")
                    {
                        strArray = BarcodeValue.Split(new char[] { '\r', '\n' });
                        strOSPN = strArray[1];
                        strOSVersion = strArray[2];
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get BarcodeValue fail." + "\r\n"); });
                        return false;
                    }         
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Get ScanSheet fail." + "\r\n"); });
                    return false;
                }
                
                UnitDeviceInfo stUnit = m_dic_UnitDevice[strPanel];
                stUnit.OSPN = strOSPN;
                stUnit.OSVersion = strOSVersion;
                m_dic_UnitDevice[strPanel] = stUnit;

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "OSPN: " + strOSPN); });
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "OSVersion: " + strOSVersion + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetScanSheet Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestCopyImage(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Copy Image."); });

                #region Check Image File Exist (OSVersion Folder)

                string strLocalPath = m_dic_ModelOption[strPanel].ImageLocalPath;       
                string CopyMode = m_dic_ModelOption[strPanel].ImageCopyMode;
                string FlashMode = m_dic_ModelOption[strPanel].FlashMode;
                string strOSVersion = m_dic_UnitDevice[strPanel].OSVersion;

                string strLocalImagePath = strLocalPath + "\\" + strOSVersion;

                if (FlashMode == FASTBOOTMODE)
                {
                    if (Directory.Exists(strLocalImagePath))
                    {
                        DirectoryInfo dir = new DirectoryInfo(strLocalImagePath);                 
                        FileInfo[] fil = dir.GetFiles(); 
                        DirectoryInfo[] dii = dir.GetDirectories();

                        if (fil.Length != 0 && dii.Length != 0)
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Already exist, Skip to copy." + "\r\n"); });
                            return true;
                        }                     
                    }
                }
                else if (FlashMode == EDLMODE)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(strLocalImagePath);
                    //FileInfo[] file = dirInfo.GetFiles();
                    DirectoryInfo[] dirArray = dirInfo.GetDirectories(); // Unzipped folder

                    if (dirArray.Length != 0)
                    {
                        bool bResult = false;
                        foreach (DirectoryInfo d in dirArray)
                        {
                            if (d.Name.Contains(strOSVersion))
                            {
                                bResult = true;
                                break;
                            }
                        }
                        if (bResult == true)
                        {
                            this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Already exist, Skip to copy." + "\r\n"); });
                            return true;
                        }
                    }
                }

                #endregion
      
                if (CopyMode == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Copy image."); });
          
                    // Start a thread to copy OS
                    Thread thread = new Thread(new ParameterizedThreadStart(this.CopyProcess));
                    thread.Start(strPanel);

                    m_b_Running = true;
                    m_b_RunReslut = false;

                    // Waiting copy finish (use timer)
                    while (m_b_Running == true)
                    {
                        this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Copying ......"); });
                        Dly(15); 
                    }

                    if (m_b_RunReslut == false)
                    {
                        DisplayMessage("Failed to copy image.");
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Copy image successful."); });

                    #region Copy Bat file from server to local

                    if (FlashMode == FASTBOOTMODE)
                    {
                        strErrorMessage = "";
                        DisplayMessage("Copy bat file from server to local.");
                        if (CopyBatFileFromServerToLocal(strPanel, ref strErrorMessage) == false)
                        {
                            DisplayMessage("Failed to copy bat file from server." + strErrorMessage);
                            return false;
                        }
                        DisplayMessage("Copy bat file from server to local successfully.");
                    }

                    #endregion

                    #region Copy Bat file to Image path

                    if (FlashMode == FASTBOOTMODE)
                    {
                        strErrorMessage = "";
                        DisplayMessage("Copy bat file to image path.");
                        if (CopyBatFile(strPanel, ref strErrorMessage) == false)
                        {
                            DisplayMessage("Failed to copy bat file to image path." + strErrorMessage);
                            return false;
                        }
                        DisplayMessage("Copy bat file to image path successfully.");
                    }

                    #endregion
                }
                else
                {

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Skiped to copy image, Load from local."); });
                    return true;
                }

                this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Copy Image Completed." + "\r\n"); });
            }
            catch (Exception ex)
            {
                strErrorMessage = "TestGetEID Exception:" + ex.Message;
                return false;
            }

            return true;
        }


        private bool TestCheckSKU(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_dic_ModelOption[strPanel].SKUCheckEnable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "\r\n"); });
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
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "\r\n"); });
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
                if (m_dic_ModelOption[strPanel].CheckManualBITResult_Enable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check ManualBITResult."); });

                    if (CheckManualBITResult(strPanel, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check ManualBITResult sucessfully." + "\r\n"); });
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
                if (m_dic_ModelOption[strPanel].KeyboxEnable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "\r\n"); });
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "Test Check Keybox."); });

                    if (CheckKeybox(strPanel, ref strErrorMessage) == false)
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
                strErrorMessage = "TestKeybox Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool TestSentienceKey(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                if (m_dic_ModelOption[strPanel].SentienceKeyEnable == "1")
                {
                    this.Invoke((MethodInvoker)delegate { DisplayUnitLog(strPanel, "\r\n"); });
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

        private bool CopyBatFile(string strPanel, ref string strErrorMessage)
        {
            string strSrcFile = "";
            string strDestFile = "";
            string FlashMode = m_dic_ModelOption[strPanel].FlashMode;
            string Model = m_dic_UnitDevice[strPanel].Model;
            string FastbootBatFile = m_dic_ModelOption[strPanel].FASTBOOTBatFile;
            string ImageLocalPath = m_dic_ModelOption[strPanel].ImageLocalPath;
            string strOSVersion = m_dic_UnitDevice[strPanel].OSVersion;

            try
            {
                if (FlashMode == FASTBOOTMODE)
                {
                    string strSrcDir = Application.StartupPath;

                    #region flash bat

                    strSrcFile = strSrcDir + "\\" + Model + "\\" + FastbootBatFile;
                    strDestFile = ImageLocalPath + "\\" + strOSVersion + "\\" + FastbootBatFile;
                    if (CopyFile(strSrcFile, strDestFile, true, ref strErrorMessage) == false)
                    {
                        return false;
                    }

                    #endregion

                    #region adb.exe

                    strSrcFile = strSrcDir + "\\" + "BAT" + "\\" + "adb.exe";
                    strDestFile = ImageLocalPath + "\\" + strOSVersion + "\\" + "adb.exe";
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
                    strDestFile = ImageLocalPath + "\\" + strOSVersion + "\\" + "AdbWinApi.dll";
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
                    strDestFile = ImageLocalPath + "\\" + strOSVersion + "\\" + "AdbWinUsbApi.dll";
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
                    strDestFile = ImageLocalPath + "\\" + strOSVersion + "\\" + "fastboot.exe";
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

        private bool CopyBatFileFromServerToLocal(string strPanel, ref string strErrorMessage)
        {
            try
            {
                string FlashMode = m_dic_ModelOption[strPanel].FlashMode;
                string FastbootEnable = m_dic_ModelOption[strPanel].FASTBOOTEnable;
                string FastbootBatServerPath = m_dic_ModelOption[strPanel].FASTBOOTBatServerPath;
                string FastbootBatFile = m_dic_ModelOption[strPanel].FASTBOOTBatFile;
                string Model = m_dic_UnitDevice[strPanel].Model;

                if (FlashMode == FASTBOOTMODE)
                {
                    if (FastbootEnable == "1")
                    {
                        string strServerFile = FastbootBatServerPath + "\\" + FastbootBatFile;
                        string strLocalFile = Application.StartupPath + "\\" + Model + "\\" + FastbootBatFile;
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

        //private bool SaveMDCS(string strPanel)
        //{
        //    try
        //    {
        //        if (m_dic_ModelOption[strPanel].MDCSEnable == "1")
        //        {
        //            string str_ErrorMessage = "";
        //            clsMDCS obj_SaveMDCS = new clsMDCS();
        //            obj_SaveMDCS.ServerName = m_dic_ModelOption[strPanel].MDCSURL;
        //            obj_SaveMDCS.DeviceName = m_dic_ModelOption[strPanel].MDCSDeviceName;
        //            obj_SaveMDCS.UseModeProduction = true;
        //            obj_SaveMDCS.p_TestData = m_st_TestSaveData;
        //            bool bRes = false;
        //            for (int i = 0; i < 5; i++)
        //            {
        //                bRes = obj_SaveMDCS.SendMDCSData(ref str_ErrorMessage);
        //                if (bRes == false)
        //                {
        //                    Dly(1);
        //                    continue;
        //                }
        //                bRes = true;
        //                break;
        //            }
        //            if (bRes == false)
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string strr = ex.Message;
        //        return false;
        //    }

        //    return true;
        //}

        private bool SaveMDCS(string strPanel, TestSaveData objSaveData)
        {
            try
            {
                if (m_dic_ModelOption[strPanel].MDCSEnable == "1")
                {
                    string str_ErrorMessage = "";
                    clsMDCS obj_SaveMDCS = new clsMDCS();
                    obj_SaveMDCS.ServerName = m_dic_ModelOption[strPanel].MDCSURL;
                    obj_SaveMDCS.DeviceName = m_dic_ModelOption[strPanel].MDCSDeviceName;
                    obj_SaveMDCS.UseModeProduction = true;
                    obj_SaveMDCS.p_TestData = objSaveData;

                    bool bRes = false;
                    for (int i = 0; i < 5; i++)
                    {
                        bRes = obj_SaveMDCS.SendMDCSData(ref str_ErrorMessage);
                        if (bRes == false)
                        {
                            DeleteMDCSSqueueXmlFile();
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

        private bool DeleteMDCSSqueueXmlFile()
        {
            try
            {
                string strXMLPath = System.IO.Path.GetTempPath() + "\\" + "mdcsqueue.xml";

                if (File.Exists(strXMLPath) == true)
                {
                    File.Delete(strXMLPath);
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

        private bool ExcuteBat(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, ref string strErrorMessage)
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

        private bool ExcuteBat(string strPanel, string strBatDir, string strBatFile, string strBatParameter, int iTimeOut, string strSearchResult, ref string strResult, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strResult = "";
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
        
                strResult = strOutput;

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
                string strSKU = m_dic_UnitDevice[strPanel].SKU;
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
                strBatParameter = strSN + " " + m_dic_UnitDevice[strPanel].SKU;
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
            strSKU = "";

            try
            {
                bool bRes = false;
                string strResult = "";
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
                strBatParameter = strSN;
                for (int i = 0; i < 5; i++)
                {
                    strResult = "";
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strResult, ref strErrorMessage);
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
                    strErrorMessage = "Execute bat to get SKU fail.";
                    return false;
                }

                // Truncate SKU      
                int index = strResult.IndexOf("SKU:");
                if (index != -1)
                {
                    strResult = strResult.Substring(index + 4);
                    index = strResult.IndexOf("*");
                    strSKU = strResult.Substring(0, index);
                }

                if (string.IsNullOrWhiteSpace(strSKU) || strSKU.IndexOf("-") == 0)
                {
                    strErrorMessage = "Get SKU format error !!!";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Get SKU exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool GetWorkOrderProperty(string strPanel, ref string strWorkOrder, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strWorkOrder = "";

            try
            {
                bool bRes = false;
                string strResult = "";
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "GetWorkOrder.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN;
                for (int i = 0; i < 5; i++)
                {
                    strResult = "";
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strResult, ref strErrorMessage);
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
                    strErrorMessage = "Execute bat to get WorkOrder fail.";
                    return false;
                }

                // Truncate WorkOrder   
                int index = strResult.IndexOf("WorkOrder:");
                if (index != -1)
                {
                    strResult = strResult.Substring(index + 10);
                    index = strResult.IndexOf("*");
                    strWorkOrder = strResult.Substring(0, index);
                }

                if (string.IsNullOrWhiteSpace(strWorkOrder))
                {
                    strErrorMessage = "Get WorkOrder is empty !!!";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Get WorkOrder exception:" + ex.Message;
                return false;
            }

            return true;
        }

        private bool GetEIDProperty(string strPanel, ref string strEID, ref string strErrorMessage)
        {
            strErrorMessage = "";
            strEID = "";

            try
            {
                bool bRes = false;
                string strResult = "";
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\" + "BAT";
                string strBatFile = strBatDir + "\\" + "GetEID.bat";
                int iTimeout = 15 * 1000;
                string strSearchResult = "SUCCESS";

                if (File.Exists(strBatFile) == false)
                {
                    strErrorMessage = "Check file exist fail." + strBatFile;
                    return false;
                }

                string strBatParameter = "";
                strBatParameter = strSN;
                for (int i = 0; i < 5; i++)
                {
                    strResult = "";
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, iTimeout, strSearchResult, ref strResult, ref strErrorMessage);
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
                    strErrorMessage = "Execute bat to get EID fail.";
                    return false;
                }

                int index = strResult.IndexOf("EID:");
                if (index != -1)
                {
                    strResult = strResult.Substring(index + 4);
                    index = strResult.IndexOf("*");
                    strEID = strResult.Substring(0, index);
                }

                if (string.IsNullOrWhiteSpace(strEID))
                {
                    strErrorMessage = "Get EID is empty !!!";
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "Get strEID exception:" + ex.Message;
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
                string strBatDir = m_dic_ModelOption[strPanel].ImageLocalPath;
                string strBatFile = strBatDir + "\\" + m_dic_ModelOption[strPanel].FASTBOOTBatFile;
                int iTimeout = m_dic_ModelOption[strPanel].FASTBOOTTimeout * 1000;
                string strSearchResult = m_dic_ModelOption[strPanel].FASTBOOTSuccessFlag;
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

            int EDLTimeout = m_dic_ModelOption[strPanel].EDLTimeout;
            string EDLSuccessFlag = m_dic_ModelOption[strPanel].EDLSuccessFlag;
            string EDLDeviceType = m_dic_ModelOption[strPanel].EDLDeviceType;
            string ImageLocalPath = m_dic_ModelOption[strPanel].ImageLocalPath;
            string strOSVersion = m_dic_UnitDevice[strPanel].OSVersion;
            string EDLELF = m_dic_ModelOption[strPanel].EDLELF;
            string EDLPatch = m_dic_ModelOption[strPanel].EDLPatch;
            string EDLRawProgram = m_dic_ModelOption[strPanel].EDLRawProgram;
            string EDLFlashType = m_dic_ModelOption[strPanel].EDLFlashType;

            ImageLocalPath = ImageLocalPath + "\\" + strOSVersion;

            try
            {
                bool bRes = false;
                string strSN = m_dic_UnitDevice[strPanel].SN;
                string strBatDir = Application.StartupPath + "\\QFIL";
                string strBatFile = "";
                int iTimeout = EDLTimeout * 1000;
                string strSearchResult = EDLSuccessFlag;
                string strCmd = "";
                string strQFIL = "";
                string strSearchPath = "";
                string strelf = "";
                string strPatch = "";
                string strRawProgram = "";
                string strReset = "";

                #region strCmd

                if (EDLDeviceType == "")
                {
                    strSearchPath = ImageLocalPath;
                    strelf = ImageLocalPath + "\\" + EDLELF;
                    strPatch = ImageLocalPath + "\\" + EDLPatch;
                    strRawProgram = ImageLocalPath + "\\" + EDLRawProgram;
                }
                else
                {
                    strSearchPath = ImageLocalPath + "\\" + EDLDeviceType;
                    strelf = ImageLocalPath + "\\" + EDLDeviceType + "\\" + EDLELF;
                    strPatch = ImageLocalPath + "\\" + EDLDeviceType + "\\" + EDLPatch;
                    strRawProgram = ImageLocalPath + "\\" + EDLDeviceType + "\\" + EDLRawProgram;
                }

                strQFIL = m_dic_ModelOption[strPanel].EDLQFIL;
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

                strReset = m_dic_ModelOption[strPanel].EDLReset;
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
                strCmd += " -RawProgram=" + EDLRawProgram;

                //Select Flash Type: emmc/ufs
                if (EDLFlashType != "")     
                {
                    strCmd += " -DEVICETYPE=" + EDLFlashType;
                }

                strCmd += " -";
                strCmd += " -patch=" + EDLPatch;
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

        private bool CheckKeybox(string strPanel, ref string strErrorMessage)
        {
            strErrorMessage = "";

            bool bRes = false;
            string strSN = m_dic_UnitDevice[strPanel].SN;
            string strBatDir = Application.StartupPath + "\\" + "BAT";
            string strBatFile = strBatDir + "\\" + "CheckKeybox.bat";
            int iTimeout = 15 * 1000;
            string strSearchResult = "SUCCESS";
            string strBatParameter = "";

            string strKeyboxDir = Application.StartupPath + "\\" + m_dic_UnitDevice[strPanel].Model;
            string strKeyboxFile = m_dic_ModelOption[strPanel].KeyboxFile;
            string strKeyboxDevice = m_dic_ModelOption[strPanel].KeyboxDevice;
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

                strDicName = m_dic_ModelOption[strPanel].SentienceKeyHonEdgeProductName + "_" + DateTime.Now.ToString("MM-dd-yyyy");

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

                if (m_dic_ModelOption[strPanel].SentienceKeyUploadEnable == "1")
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

                    string strServerFolderPath = m_dic_ModelOption[strPanel].SentienceKeyUploadServerPath + "\\" + strFolderName;
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

        //private bool UploadMES(TestSaveData objSaveData, ref string strErrorMessage)
        //{
        //    strErrorMessage = "";

        //    try
        //    {
        //        if (m_st_OptionData.MES_Enable == "1")
        //        {
        //            string strEID = "Automation";
        //            string strSN = "";
        //            string strWorkOrder = "";
        //            string strRouterCode = "G2H";
        //            string strSourceCode = "";
        //            string strTestResult = "";

        //            strSN = objSaveData.TestRecord.SN;
        //            if (strSN == "")
        //            {
        //                strErrorMessage = "Failed to upload MES,invalid SN.";
        //                return false;
        //            }

        //            if (objSaveData.TestResult.TestPassed == true)
        //            {
        //                strTestResult = "PASS";
        //            }
        //            else
        //            {
        //                strTestResult = "FAIL";
        //            }

        //            MESWebAPI.MoveResult mesResult = new MESWebAPI.MoveResult();
        //            mesResult = MESWebAPI.SNMove.CheckWorkOrder(strWorkOrder);
        //            if (mesResult.result.ToUpper() != "OK")
        //            {
        //                strWorkOrder = "2019091001"; //如果输入的工单无效,用默认的工单
        //            }

        //            mesResult = MESWebAPI.SNMove.MoveForAutoMation(strEID, strSourceCode, strSN, strWorkOrder, strRouterCode, strTestResult);
        //            if (mesResult.result.ToUpper() == "OK")
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                strErrorMessage = "Failed to upload MES." + mesResult.message.ToString();
        //                return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string strr = ex.Message;
        //        strErrorMessage = "UploadMES exception:" + ex.Message;
        //        return false;
        //    }

        //    return true;
        //}

        private bool InitMDCSData(string strPanel)
        {
            try
            {
                TestSaveData objSaveData = m_dic_TestSaveData[strPanel];

                objSaveData.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData.TestRecord.SN = "";
                objSaveData.TestRecord.Model = m_dic_UnitDevice[strPanel].Model;
                objSaveData.TestRecord.SKU = m_dic_UnitDevice[strPanel].SKU;
                objSaveData.TestRecord.OSVersion = m_dic_UnitDevice[strPanel].OSVersion;
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

        private bool InitModelOptionData(string strPanel)
        {
            try
            {
                ModelOption objModelOption = m_dic_ModelOption[strPanel];

                objModelOption.ImageServerPath = "";
                objModelOption.ImageLocalPath = "";
                objModelOption.ImageCopyMode = "";
                objModelOption.FlashMode = "";
                objModelOption.CheckManualBITResult_Enable = "";
                objModelOption.FASTBOOTEnable = "";
                objModelOption.FASTBOOTBatServerPath = "";
                objModelOption.FASTBOOTBatLocalPath = "";
                objModelOption.FASTBOOTBatFile = "";
                objModelOption.FASTBOOTTimeout = 600;
                objModelOption.FASTBOOTSuccessFlag = "";
                objModelOption.EDLQFIL = "";
                objModelOption.EDLDeviceType = "";
                objModelOption.EDLFlashType = "";
                objModelOption.EDLELF = "";
                objModelOption.EDLPatch = "";
                objModelOption.EDLRawProgram = "";
                objModelOption.EDLReset = "";
                objModelOption.EDLTimeout = 600;
                objModelOption.EDLSuccessFlag = "";

                m_dic_ModelOption[strPanel] = objModelOption;
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
                return false;
            }

            return true;
        }

        public static bool MESCheckData(string strEID, string strStation, string strWorkOrder, ref string strErrorMessage)
        {
            strErrorMessage = "";

            try
            {
                #region Check MES Data

                if (strEID == "")
                {
                    strErrorMessage = "Invalid EID.";
                    return false;
                }
                if (strStation == "")
                {
                    strErrorMessage = "Invalid StationName.";
                    return false;
                }
                if (strWorkOrder == "")
                {
                    strErrorMessage = "Invalid WorkOrder.";
                    return false;
                }

                #endregion

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
                    strErrorMessage = "FailCode: " + result.code.ToString() + ",  Message: " + result.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "MESCheckData Exception:" + ex.Message;
                return false;
            }
        }

        public static bool MESUploadData(string strEID, string strStation, string strWorkOrder, string strSN, bool bPassFailFlag, ref string strErrorMessage)
        {
            strErrorMessage = "";
            string strResult = "";

            try
            {
                #region Check MES Data

                if (strEID == "")
                {
                    strErrorMessage = "Invalid EID.";
                    return false;
                }
                if (strStation == "")
                {
                    strErrorMessage = "Invalid StationName.";
                    return false;
                }
                if (strWorkOrder == "")
                {
                    strErrorMessage = "Invalid WorkOrder.";
                    return false;
                }
                if (strSN == "")
                {
                    strErrorMessage = "Invalid SN.";
                    return false;
                }

                #endregion

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
                    strErrorMessage = "Fail: code=" + result.code.ToString() + ", Message: " + result.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                strErrorMessage = "MESUploadData Exception:" + ex.Message;
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

                    #region 

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

                        //m_dic_COMPort[strPanel] = "";   //Clear ComPort Record When Disconnect. (Undefined)

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

        //private bool CopyOSImage()
        //{
        //    Thread thread = new Thread(this.CopyProcess);
        //    thread.Start();
        //    m_b_Running = true;
        //    m_b_RunReslut = false;

        //    while (m_b_Running == true)
        //    {
        //        Application.DoEvents();
        //    }

        //    if (m_b_RunReslut == false)
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        private void CopyProcess(object obj)
        {
            string strPanel = obj.ToString();            
            DisplayMessage("Copy image.");
            
            bool bRes = false;
            string strErrorMessage = "";
            for (int i = 0; i < 3; i++)
            {
                if (CopyFromServerToLacal(strPanel, ref strErrorMessage) == false)
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

        private bool CopyFromServerToLacal(string strPanel, ref string strErrorMessage)
        {
            string FlashMode = m_dic_ModelOption[strPanel].FlashMode;

            if (FlashMode == FASTBOOTMODE)
            {
                if (CopyFromServerToLacal_FASTBOOT(strPanel, ref strErrorMessage) == false)
                {
                    return false;
                }
            }
            else if (FlashMode == EDLMODE)
            {
                if (CopyFromServerToLacal_EDL(strPanel, ref strErrorMessage) == false)
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

        private bool CopyFromServerToLacal_FASTBOOT(string strPanel, ref string strErrorMessage)
        {
            try
            {
                bool bRes = false;

                string strServerPath = m_dic_ModelOption[strPanel].ImageServerPath;
                string strLocalPath = m_dic_ModelOption[strPanel].ImageLocalPath;
                string strOSPN = m_dic_UnitDevice[strPanel].OSPN;
                string strOSPNPre = "";
                string strOSVersion = m_dic_UnitDevice[strPanel].OSVersion;

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
                    strErrorMessage = "Failed to get os pn pre." + ex.Message;
                    return false;
                }

                #endregion

                strServerPath = strServerPath + "\\" + strOSPNPre + "\\" + strOSPN + "\\" + strOSVersion + "\\";
                strLocalPath = strLocalPath + "\\" + strOSVersion + "\\";

                #region Check Server Path

                if (Directory.Exists(strServerPath) == false)
                {
                    strErrorMessage = "Failed to check server path exist." + strServerPath;
                    return false;
                }

                #endregion

                #region If Exist, Delete Local Directory

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

                    // Check Directory Empty
                    DirectoryInfo di = new System.IO.DirectoryInfo(strLocalPath);
                    if (di.GetFiles().Length + di.GetDirectories().Length != 0)
                    {
                        strErrorMessage = "Failed to check dir empty." + strLocalPath;
                        return false;
                    }
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

        private bool CopyFromServerToLacal_EDL(string strPanel, ref string strErrorMessage)
        {
            try
            {
                bool bRes = false;

                string strServerPath = m_dic_ModelOption[strPanel].ImageServerPath;
                string strLocalPath = m_dic_ModelOption[strPanel].ImageLocalPath;
                string strOSPN = m_dic_UnitDevice[strPanel].OSPN;
                string strOSVersion = m_dic_UnitDevice[strPanel].OSVersion;

                strServerPath = strServerPath + "\\" + strOSPN + "\\";
                strLocalPath = strLocalPath + "\\" + strOSVersion + "\\";

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
                    if (item.Name.Contains(strOSVersion))
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

                strLocalPath = strLocalPath + "\\" + strFileName.Substring(0, strFileName.Length - 4); // auto add os filename
                DisplayMessage("Local image path:" + strLocalPath);
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
                strErrorMessage = "Failed to exception." + ex.Message;
                return false;
            }

            return true;
        }

        private bool DeleteDir(string dirPath, ref string strErrorMessage)
        {
            try
            {
                //去除文件夹和子文件的只读属性
                //去除文件夹的只读属性
                DirectoryInfo fileInfo = new DirectoryInfo(dirPath);
                fileInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;

                //去除文件的只读属性
                File.SetAttributes(dirPath, System.IO.FileAttributes.Normal);

                //判断文件夹是否还存在
                if (Directory.Exists(dirPath))
                {
                    foreach (string f in Directory.GetFileSystemEntries(dirPath))
                    {
                        if (File.Exists(f))
                        {
                            //如果有子文件删除文件
                            File.Delete(f);
                            Console.WriteLine(f);
                        }
                        else
                        {
                            //循环递归删除子文件夹
                            DeleteDir(f, ref strErrorMessage);
                        }
                    }
                    //删除空文件夹
                    Directory.Delete(dirPath);
                }

                return true;
            }
            catch (Exception ex) // 异常处理
            {
                strErrorMessage = "Failed to exception." + ex.Message;
                return false;
            }
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

            #region ScanMES (Obsolete)

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

            #region SKUMatrix (Obsolete)

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

            #region Check MES Data (Obsolete)

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

            #region Copy OS Image (Obsolete)

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

            #region Timer (Obsolete)

            //timerCopyImage.Interval = 10000;
            //timerCopyImage.Enabled = true;
            //timerCopyImage.Tick += new EventHandler(timerCopyImage_Tick);

            #endregion

            #region Delete Local OS Folder

            DirectoryInfo appPath = new DirectoryInfo(Application.StartupPath);
            string parentPath = appPath.Parent.FullName;

            string CT40LocalOsPath = parentPath + "\\CT40\\";
            string EDA51LocalOsPath = parentPath + "\\EDA51\\";
            string EDA52LocalOsPath = parentPath + "\\EDA52\\";
            string EDA5SLocalOsPath = parentPath + "\\EDA5S\\";
            string EDA56LocalOsPath = parentPath + "\\EDA56\\";

            List<string> LocalOSPathList = new List<string>();
            LocalOSPathList.Add(CT40LocalOsPath);
            LocalOSPathList.Add(EDA51LocalOsPath);
            LocalOSPathList.Add(EDA52LocalOsPath);
            LocalOSPathList.Add(EDA5SLocalOsPath);
            LocalOSPathList.Add(EDA56LocalOsPath);

            bool bRes = false;
            foreach (string path in LocalOSPathList)
            {
                if (Directory.Exists(path))
                {   
                    for (int i = 0; i < 3; i++)
                    {
                        if (DeleteDir(path, ref strErrorMessage) == false)
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
                        strErrorMessage = "Failed to delete dir: " + path;
                        DisplayMessage( strErrorMessage);
                        return false;
                    }
                }
            }

            #endregion

            //if (m_b_Setting == true)
            //{
            //    DisplayMessage("Re run the test tool after config port mapping ......");
            //    return false;
            //}

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

                //timerDeviceConnect.Interval = 15000;
                //timerDeviceConnect.Enabled = true;
                //timerDeviceConnect.Tick += new EventHandler(timerMonitorDeviceConnect_Tick);

                timerKillProcess.Interval = 20000;
                timerKillProcess.Enabled = true;
                timerKillProcess.Tick += new EventHandler(timerKillProcess_Tick);

                #endregion
            }

            DisplayMessage("Timer enabled successfully.");

            #endregion

            return true;
        }

        //private bool ScanMCF()
        //{
        //    frmMCF frmMCF = new frmMCF();
        //    if (frmMCF.ShowDialog() != DialogResult.Yes)
        //    {
        //        return false;
        //    }
        //    m_st_MCFData.Model = frmMCF.Model;
        //    m_st_MCFData.SKU = frmMCF.SKU;
        //    m_st_MCFData.OSPN = frmMCF.OSPN;
        //    m_st_MCFData.OSVersion = frmMCF.OSVersion;

        //    m_str_Model = m_st_MCFData.Model;

        //    #region Get Model by SKU

        //    string strModel = "";
        //    if (GetModelBySKU(ref strModel) == false)
        //    {
        //        return false;
        //    }
        //    m_str_Model = strModel;

        //    #endregion

        //    return true;
        //}

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


        //private bool ScanMES()
        //{
        //    FrmMES frmMES = new FrmMES();
        //    if (frmMES.ShowDialog() != DialogResult.Yes)
        //    {
        //        return false;
        //    }
        //    m_st_MESData.EID = frmMES.EID;
        //    m_st_MESData.WorkOrder = frmMES.WorkOrder;

        //    return true;
        //}

        /// <summary>
        /// Parse sku to export model config file in SKUOption.txt
        /// </summary>
        /// <param name="strOptionFileName"></param>
        /// <param name="strErrorMessage"></param>
        /// <returns></returns>
        #region SKUMatrix_OLD
        //private bool SKUMatrix(ref string strOptionFileName, ref string strErrorMessage)
        //{
        //    try
        //    {
        //        strErrorMessage = "";
        //        strOptionFileName = "";

        //        string strFileName = "SKUOption.txt";
        //        string str_FilePath = "";
        //        str_FilePath = Application.StartupPath + "\\" + strFileName;
        //        clsIniFile objIniFile = new clsIniFile(str_FilePath);

        //        #region Delete File

        //        if (File.Exists(str_FilePath) == true)
        //        {
        //            File.Delete(str_FilePath);
        //            Dly(1);
        //            if (File.Exists(str_FilePath) == true)
        //            {
        //                strErrorMessage = "Delete file fail." + str_FilePath;
        //                return false;
        //            }
        //        }

        //        #endregion

        //        #region Generate File

        //        bool bRes = false;
        //        string strBatDir = Application.StartupPath;
        //        string strBatFile = strBatDir + "\\" + "SKUMatrix.bat";
        //        string strBatParameter = "";
        //        if (File.Exists(strBatFile) == false)
        //        {
        //            strErrorMessage = "Failed to file exist." + strBatFile;
        //            return false;
        //        }

        //        strBatParameter = m_str_Model + " " + m_st_MCFData.SKU;

        //        for (int i = 0; i < 3; i++)
        //        {
        //            bRes = ExcuteBat(strBatDir, strBatFile, strBatParameter, 3000, ref strErrorMessage);
        //            if (bRes == false)
        //            {
        //                bRes = false;
        //                Dly(1);
        //                continue;
        //            }

        //            bRes = true;
        //            break;
        //        }

        //        #endregion

        //        #region Exist File

        //        if (File.Exists(str_FilePath) == false)
        //        {
        //            strErrorMessage = "Check file exist fail." + str_FilePath;
        //            return false;
        //        }

        //        #endregion

        //        #region Read File

        //        StreamReader sr = null;
        //        try
        //        {
        //            sr = new StreamReader(str_FilePath, System.Text.Encoding.Default);
        //            string strContent = sr.ReadToEnd();
        //            strContent = strContent.Replace("\r\n", "");
        //            strContent = strContent.Trim();
        //            sr.Close();
        //            sr = null;

        //            if (m_str_Model == "UL")
        //            {
        //                m_str_Model = "EDA56";
        //            }

        //            strOptionFileName = Application.StartupPath + "\\" + m_str_Model + "\\" + strContent;
        //        }
        //        catch (Exception exx)
        //        {
        //            string strr = exx.Message;
        //            strErrorMessage = "Exception to read file." + str_FilePath;
        //            return false;
        //        }
        //        finally
        //        {
        //            if (sr != null)
        //            {
        //                sr.Close();
        //                sr = null;
        //            }
        //        }

        //        #endregion
        //    }
        //    catch (Exception ex)
        //    {
        //        string strr = ex.Message;
        //        return false;
        //    }

        //    return true;
        //}
        #endregion

        private bool SKUMatrix(string strPanel, ref string strOptionFileName, ref string strErrorMessage)
        {
            // maybe can use same file, use thread locker     
            try
            {
                strErrorMessage = "";
                strOptionFileName = "";
                string strFileName;

                switch (strPanel)
                {
                    case PANEL_1:
                        strFileName = "SKUOption1.txt";
                        break;

                    case PANEL_2:
                        strFileName = "SKUOption2.txt";
                        break;

                    case PANEL_3:
                        strFileName = "SKUOption3.txt";
                        break;

                    case PANEL_4:
                        strFileName = "SKUOption4.txt";
                        break;

                    default:
                        strFileName = "";
                        break;
                }

                string str_FilePath = "";
                str_FilePath = Application.StartupPath + "\\" + strFileName;
                //clsIniFile objIniFile = new clsIniFile(str_FilePath);

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

                string strModel = m_dic_UnitDevice[strPanel].Model;
                string strSKU = m_dic_UnitDevice[strPanel].SKU;
                strBatParameter = strModel + " " + strSKU + " " + strFileName;

                for (int i = 0; i < 3; i++)
                {
                    bRes = ExcuteBat(strPanel, strBatDir, strBatFile, strBatParameter, 3000, ref strErrorMessage);
                    if (bRes == false)
                    {
                        bRes = false;
                        Dly(3);
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

                    m_str_Model = m_dic_UnitDevice[strPanel].Model;
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

        private bool GetModelBySKU(string strSKU, ref string strModel)
        {
            try
            {
                strModel = "";
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

        private bool ReadModelOptionFile(string strPanel, string strOptionFileName, ref string strErrorMessage)
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

                ModelOption objModelOption = m_dic_ModelOption[strPanel];

                #region MDCS

                objModelOption.MDCSEnable = objIniFile.ReadString("MDCS", "Enable");
                if ((objModelOption.MDCSEnable != "0") && (objModelOption.MDCSEnable != "1"))
                {
                    strErrorMessage = "Invalid MDCS Enable:" + objModelOption.MDCSEnable;
                    return false;
                }

                objModelOption.MDCSURL = objIniFile.ReadString("MDCS", "URL");
                if (objModelOption.MDCSURL == "")
                {
                    strErrorMessage = "Invalid MDCS URL:" + objModelOption.MDCSURL;
                    return false;
                }

                objModelOption.MDCSDeviceName = objIniFile.ReadString("MDCS", "DeviceName");
                if (objModelOption.MDCSDeviceName == "")
                {
                    strErrorMessage = "Invalid MDCS DeviceName:" + objModelOption.MDCSDeviceName;
                    return false;
                }

                objModelOption.MDCSPreStationResultCheck = objIniFile.ReadString("MDCS", "PreStationResultCheck");
                if ((objModelOption.MDCSPreStationResultCheck != "0") && (objModelOption.MDCSPreStationResultCheck != "1"))
                {
                    strErrorMessage = "Invalid MDCS PreStationResultCheck:" + objModelOption.MDCSPreStationResultCheck;
                    return false;
                }

                objModelOption.MDCSPreStationDeviceName = objIniFile.ReadString("MDCS", "PreStationDeviceName");
                if (objModelOption.MDCSPreStationDeviceName == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationDeviceName:" + objModelOption.MDCSPreStationDeviceName;
                    return false;
                }
                objModelOption.MDCSPreStationVarName = objIniFile.ReadString("MDCS", "PreStationVarName");
                if (objModelOption.MDCSPreStationVarName == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationVarName:" + objModelOption.MDCSPreStationVarName;
                    return false;
                }

                objModelOption.MDCSPreStationVarValue = objIniFile.ReadString("MDCS", "PreStationVarValue");
                if (objModelOption.MDCSPreStationVarValue == "")
                {
                    strErrorMessage = "Invalid MDCS PreStationVarValue:" + objModelOption.MDCSPreStationVarValue;
                    return false;
                }

                #endregion

                #region Image

                objModelOption.ImageCopyMode = objIniFile.ReadString("Image", "CopyMode");
                if ((objModelOption.ImageCopyMode != "0") && (objModelOption.ImageCopyMode != "1"))
                {
                    strErrorMessage = "Invalid Image CopyMode:" + objModelOption.ImageCopyMode;
                    return false;
                }

                if (objModelOption.ImageCopyMode == "1")
                {
                    objModelOption.ImageServerPath = objIniFile.ReadString("Image", "ServerPath");
                    if (objModelOption.ImageServerPath.Substring(objModelOption.ImageServerPath.Length - 1, 1) == "\\")
                    {
                        objModelOption.ImageServerPath = objModelOption.ImageServerPath.Substring(0, objModelOption.ImageServerPath.Length - 1);
                    }
                    if (Directory.Exists(objModelOption.ImageServerPath) == false)
                    {
                        strErrorMessage = "Invalid Image ServerPath:" + objModelOption.ImageServerPath;
                        return false;
                    }
                }
                else
                {
                    objModelOption.ImageServerPath = "";
                }

                objModelOption.ImageLocalPath = objIniFile.ReadString("Image", "LocalPath");
                if (objModelOption.ImageLocalPath.Substring(objModelOption.ImageLocalPath.Length - 1, 1) == "\\")
                {
                    objModelOption.ImageLocalPath = objModelOption.ImageLocalPath.Substring(0, objModelOption.ImageLocalPath.Length - 1);
                }
                if (Directory.Exists(objModelOption.ImageLocalPath) == false)
                {
                    Directory.CreateDirectory(objModelOption.ImageLocalPath);

                    if (Directory.Exists(objModelOption.ImageLocalPath) == false)
                    {
                        strErrorMessage = "Invalid Image LocalPath:" + objModelOption.ImageLocalPath;
                        return false;
                    }
                }

                #endregion

                #region FlashMode

                objModelOption.FlashMode = objIniFile.ReadString("FlashMode", "Mode");
                objModelOption.FlashMode = objModelOption.FlashMode.ToUpper();
                if ((objModelOption.FlashMode != FASTBOOTMODE) && (objModelOption.FlashMode != EDLMODE))
                {
                    strErrorMessage = "Invalid FlashMode Mode:" + objModelOption.FlashMode;
                    return false;
                }

                #endregion

                #region FASTBOOT

                if (objModelOption.FlashMode == FASTBOOTMODE)
                {
                    objModelOption.FASTBOOTBatFile = objIniFile.ReadString("FASTBOOT", "BatFile");
                    if (objModelOption.FASTBOOTBatFile == "")
                    {
                        strErrorMessage = "Invalid FASTBOOT BatFile:" + objModelOption.FASTBOOTBatFile;
                        return false;
                    }

                    objModelOption.FASTBOOTTimeout = objIniFile.ReadInt("FASTBOOT", "Timeout");
                    if (objModelOption.FASTBOOTTimeout < 0)
                    {
                        strErrorMessage = "Invalid FASTBOOT Timeout:" + objModelOption.FASTBOOTTimeout;
                        return false;
                    }

                    objModelOption.FASTBOOTSuccessFlag = objIniFile.ReadString("FASTBOOT", "SuccessFlag");
                    if (objModelOption.FASTBOOTSuccessFlag == "")
                    {
                        strErrorMessage = "Invalid FASTBOOT SuccessFlag:" + objModelOption.FASTBOOTSuccessFlag;
                        return false;
                    }

                    objModelOption.FASTBOOTEnable = objIniFile.ReadString("FASTBOOT", "Enable");
                    if ((objModelOption.FASTBOOTEnable != "0") && (objModelOption.FASTBOOTEnable != "1"))
                    {
                        strErrorMessage = "Invalid FASTBOOT Enable:" + objModelOption.FASTBOOTEnable;
                        return false;
                    }

                    objModelOption.FASTBOOTBatServerPath = objIniFile.ReadString("FASTBOOT", "BatServerPath");
                    if (objModelOption.FASTBOOTBatServerPath == "")
                    {
                        strErrorMessage = "Invalid FASTBOOT BatServerPath:" + objModelOption.FASTBOOTBatServerPath;
                        return false;
                    }

                    objModelOption.FASTBOOTBatLocalPath = "";
                    //objModelOption.FASTBOOTBatLocalPath = objIniFile.ReadString("FASTBOOT", "BatLocalPath");
                    //if (objModelOption.FASTBOOTBatLocalPath == "")
                    //{
                    //    strErrorMessage = "Invalid FASTBOOT BatLocalPath:" + objModelOption.FASTBOOTBatLocalPath;
                    //    return false;
                    //}
                }

                #endregion

                #region EDL

                if (objModelOption.FlashMode == EDLMODE)
                {
                    objModelOption.EDLQFIL = objIniFile.ReadString("EDL", "QFIL");
                    if (objModelOption.EDLQFIL == "")
                    {
                        strErrorMessage = "Invalid EDL QFIL:" + objModelOption.EDLQFIL;
                        return false;
                    }
                    if (File.Exists(objModelOption.EDLQFIL) == false)
                    {
                        strErrorMessage = "File not exist:" + objModelOption.EDLQFIL;
                        return false;
                    }

                    objModelOption.EDLDeviceType = objIniFile.ReadString("EDL", "DeviceType");
                    //if (objModelOption.EDLDeviceType == "")
                    //{
                    //    strErrorMessage = "Invalid EDL DeviceType:" + objModelOption.EDLDeviceType;
                    //    return false;
                    //}

                    objModelOption.EDLFlashType = objIniFile.ReadString("EDL", "FlashType");
                    //if (objModelOption.EDLFlashType == "")
                    //{
                    //    strErrorMessage = "Invalid EDL FlashType:" + objModelOption.EDLFlashType;
                    //    return false;
                    //}

                    objModelOption.EDLELF = objIniFile.ReadString("EDL", "ELF");
                    if (objModelOption.EDLELF == "")
                    {
                        strErrorMessage = "Invalid EDL ELF:" + objModelOption.EDLELF;
                        return false;
                    }

                    objModelOption.EDLPatch = objIniFile.ReadString("EDL", "Patch");
                    if (objModelOption.EDLPatch == "")
                    {
                        strErrorMessage = "Invalid EDL Patch:" + objModelOption.EDLPatch;
                        return false;
                    }

                    objModelOption.EDLRawProgram = objIniFile.ReadString("EDL", "RawProgram");
                    if (objModelOption.EDLRawProgram == "")
                    {
                        strErrorMessage = "Invalid EDL RawProgram:" + objModelOption.EDLRawProgram;
                        return false;
                    }

                    objModelOption.EDLReset = objIniFile.ReadString("EDL", "Reset");
                    if ((objModelOption.EDLReset != "0") && (objModelOption.EDLReset != "1"))
                    {
                        strErrorMessage = "Invalid EDL Reset:" + objModelOption.EDLReset;
                        return false;
                    }

                    objModelOption.EDLTimeout = objIniFile.ReadInt("EDL", "Timeout");
                    if (objModelOption.EDLTimeout < 0)
                    {
                        strErrorMessage = "Invalid EDL Timeout:" + objModelOption.EDLTimeout;
                        return false;
                    }

                    objModelOption.EDLSuccessFlag = objIniFile.ReadString("EDL", "SuccessFlag");
                    if (objModelOption.EDLSuccessFlag == "")
                    {
                        strErrorMessage = "Invalid EDL SuccessFlag:" + objModelOption.EDLSuccessFlag;
                        return false;
                    }
                }

                #endregion

                #region Keybox

                objModelOption.KeyboxEnable = objIniFile.ReadString("Keybox", "Enable");
                if ((objModelOption.KeyboxEnable != "0") && (objModelOption.KeyboxEnable != "1"))
                {
                    strErrorMessage = "Invalid Keybox Enable:" + objModelOption.KeyboxEnable;
                    return false;
                }

                #region Temporary remove, when Enable == "1", just check keybox whether exist, not write keybox.

                objModelOption.KeyboxFilePath = "";
                objModelOption.KeyboxFile = "";
                objModelOption.KeyboxDevice = "";

                //if (objModelOption.KeyboxEnable == "1")
                //{
                //    objModelOption.KeyboxFilePath = objIniFile.ReadString("Keybox", "Path");
                //    if (Directory.Exists(objModelOption.KeyboxFilePath) == false)
                //    {
                //        strErrorMessage = "Invalid Keybox Path:" + objModelOption.KeyboxFilePath;
                //        return false;
                //    }

                //    objModelOption.KeyboxFile = objIniFile.ReadString("Keybox", "File");
                //    string strTemp = Application.StartupPath + "\\" + m_str_Model + "\\" + objModelOption.KeyboxFile;
                //    if (File.Exists(strTemp) == false)
                //    {
                //        strErrorMessage = "File not exist:" + objModelOption.KeyboxFile;
                //        return false;
                //    }

                //    objModelOption.KeyboxDevice = objIniFile.ReadString("Keybox", "Device");
                //    if (objModelOption.KeyboxDevice == "")
                //    {
                //        strErrorMessage = "Invalid Keybox Device:" + objModelOption.KeyboxDevice;
                //        return false;
                //    }
                //}

                #endregion

                #endregion

                #region SentienceKey

                objModelOption.SentienceKeyEnable = objIniFile.ReadString("SentienceKey", "Enable");
                if ((objModelOption.SentienceKeyEnable != "0") && (objModelOption.SentienceKeyEnable != "1"))
                {
                    strErrorMessage = "Invalid SentienceKey Enable:" + objModelOption.SentienceKeyEnable;
                    return false;
                }

                if (objModelOption.SentienceKeyEnable == "1")
                {
                    objModelOption.SentienceKeyHonEdgeProductName = objIniFile.ReadString("SentienceKey", "HonEdgeProductName");
                    if (objModelOption.SentienceKeyHonEdgeProductName == "")
                    {
                        strErrorMessage = "Invalid SentienceKey HonEdgeProductName:" + objModelOption.SentienceKeyHonEdgeProductName;
                        return false;
                    }
                }

                objModelOption.SentienceKeyUploadEnable = objIniFile.ReadString("SentienceKey", "UploadEnable");
                if ((objModelOption.SentienceKeyUploadEnable != "0") && (objModelOption.SentienceKeyUploadEnable != "1"))
                {
                    strErrorMessage = "Invalid SentienceKey UploadEnable:" + objModelOption.SentienceKeyUploadEnable;
                    return false;
                }

                if (objModelOption.SentienceKeyUploadEnable == "1")
                {
                    objModelOption.SentienceKeyUploadServerPath = objIniFile.ReadString("SentienceKey", "UploadServerPath");
                    if (Directory.Exists(objModelOption.SentienceKeyUploadServerPath) == false)
                    {
                        strErrorMessage = "Invalid SentienceKey UploadServerPath:" + objModelOption.SentienceKeyUploadServerPath;
                        return false;
                    }
                }

                #endregion

                #region SKU

                objModelOption.SKUCheckEnable = objIniFile.ReadString("SKU", "Enable");
                if ((objModelOption.SKUCheckEnable != "0") && (objModelOption.SKUCheckEnable != "1"))
                {
                    strErrorMessage = "Invalid SKU Enable Value:" + objModelOption.SKUCheckEnable;
                    return false;
                }

                #endregion

                #region ScanSheet

                objModelOption.ScanSheetStation = objIniFile.ReadString("ScanSheet", "Station");
                if (objModelOption.ScanSheetStation == "")
                {
                    strErrorMessage = "Invalid ScanSheet Station:" + objModelOption.ScanSheetStation;
                    return false;
                }

                #endregion


                #region Check ManualBITResult

                objModelOption.CheckManualBITResult_Enable = objIniFile.ReadString("CheckManualBITResult", "Enable");
                if ((objModelOption.CheckManualBITResult_Enable != "0") && (objModelOption.CheckManualBITResult_Enable != "1"))
                {
                    strErrorMessage = "Invalid CheckManualBITResult Enable Value: " + objModelOption.CheckManualBITResult_Enable;
                    return false;
                }

                #endregion

                m_dic_ModelOption[strPanel] = objModelOption;
            }
            catch (Exception ex)
            {
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

                m_st_OptionData.MES_Station = objIniFile.ReadString("MES", "Station");
                if (m_st_OptionData.MES_Station == "")
                {
                    strErrorMessage = "Invalid MES Station:" + m_st_OptionData.MES_Station;
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
                stUnit1.SKU = "";
                stUnit1.Model = "";
                stUnit1.EID = "";
                stUnit1.WorkOrder = "";
                stUnit1.OSPN = "";
                stUnit1.OSVersion = "";
                stUnit1.Status = "0";
                m_dic_UnitDevice.Add(PANEL_1, stUnit1);

                // Unit2
                UnitDeviceInfo stUnit2 = new UnitDeviceInfo();
                stUnit2.Panel = PANEL_2;
                stUnit2.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel2;
                stUnit2.SN = "";
                stUnit2.SKU = "";
                stUnit2.Model = "";
                stUnit2.EID = "";
                stUnit2.WorkOrder = "";
                stUnit2.OSPN = "";
                stUnit2.OSVersion = "";
                stUnit2.Status = "0";
                m_dic_UnitDevice.Add(PANEL_2, stUnit2);

                // Unit3
                UnitDeviceInfo stUnit3 = new UnitDeviceInfo();
                stUnit3.Panel = PANEL_3;
                stUnit3.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel3;
                stUnit3.SN = "";
                stUnit3.SKU = "";
                stUnit3.Model = "";
                stUnit3.EID = "";
                stUnit3.WorkOrder = "";
                stUnit3.OSPN = "";
                stUnit3.OSVersion = "";
                stUnit3.Status = "0";
                m_dic_UnitDevice.Add(PANEL_3, stUnit3);

                // Unit4
                UnitDeviceInfo stUnit4 = new UnitDeviceInfo();
                stUnit4.Panel = PANEL_4;
                stUnit4.PhysicalAddress = m_st_OptionData.DeviceAddress_Panel4;
                stUnit4.SN = "";
                stUnit4.SKU = "";
                stUnit4.Model = "";
                stUnit4.EID = "";
                stUnit4.WorkOrder = "";
                stUnit4.OSPN = "";
                stUnit4.OSVersion = "";
                stUnit4.Status = "0";
                m_dic_UnitDevice.Add(PANEL_4, stUnit4);

                #endregion

                #region m_dic_ModelOption

                m_dic_ModelOption.Clear();

                // Unit1
                ModelOption objModelOption1 = new ModelOption();
                objModelOption1.ImageServerPath = "";
                objModelOption1.ImageLocalPath = "";
                objModelOption1.ImageCopyMode = "";
                objModelOption1.FlashMode = "";
                objModelOption1.CheckManualBITResult_Enable = "";
                objModelOption1.FASTBOOTEnable = "";
                objModelOption1.FASTBOOTBatServerPath = "";
                objModelOption1.FASTBOOTBatLocalPath = "";
                objModelOption1.FASTBOOTBatFile = "";
                objModelOption1.FASTBOOTTimeout = 600;
                objModelOption1.FASTBOOTSuccessFlag = "";
                objModelOption1.EDLQFIL = "";
                objModelOption1.EDLDeviceType = "";
                objModelOption1.EDLFlashType = "";
                objModelOption1.EDLELF = "";
                objModelOption1.EDLPatch = "";
                objModelOption1.EDLRawProgram = "";
                objModelOption1.EDLReset = "";
                objModelOption1.EDLTimeout = 600;
                objModelOption1.EDLSuccessFlag = "";
                m_dic_ModelOption.Add(PANEL_1, objModelOption1);

                // Unit2
                ModelOption objModelOption2 = new ModelOption();
                objModelOption2.ImageServerPath = "";
                objModelOption2.ImageLocalPath = "";
                objModelOption2.ImageCopyMode = "";
                objModelOption2.FlashMode = "";
                objModelOption2.CheckManualBITResult_Enable = "";
                objModelOption2.FASTBOOTEnable = "";
                objModelOption2.FASTBOOTBatServerPath = "";
                objModelOption2.FASTBOOTBatLocalPath = "";
                objModelOption2.FASTBOOTBatFile = "";
                objModelOption2.FASTBOOTTimeout = 600;
                objModelOption2.FASTBOOTSuccessFlag = "";
                objModelOption2.EDLQFIL = "";
                objModelOption2.EDLDeviceType = "";
                objModelOption2.EDLFlashType = "";
                objModelOption2.EDLELF = "";
                objModelOption2.EDLPatch = "";
                objModelOption2.EDLRawProgram = "";
                objModelOption2.EDLReset = "";
                objModelOption2.EDLTimeout = 600;
                objModelOption2.EDLSuccessFlag = "";
                m_dic_ModelOption.Add(PANEL_2, objModelOption2);

                // Unit3
                ModelOption objModelOption3 = new ModelOption();
                objModelOption3.ImageServerPath = "";
                objModelOption3.ImageLocalPath = "";
                objModelOption3.ImageCopyMode = "";
                objModelOption3.FlashMode = "";
                objModelOption3.CheckManualBITResult_Enable = "";
                objModelOption3.FASTBOOTEnable = "";
                objModelOption3.FASTBOOTBatServerPath = "";
                objModelOption3.FASTBOOTBatLocalPath = "";
                objModelOption3.FASTBOOTBatFile = "";
                objModelOption3.FASTBOOTTimeout = 600;
                objModelOption3.FASTBOOTSuccessFlag = "";
                objModelOption3.EDLQFIL = "";
                objModelOption3.EDLDeviceType = "";
                objModelOption3.EDLFlashType = "";
                objModelOption3.EDLELF = "";
                objModelOption3.EDLPatch = "";
                objModelOption3.EDLRawProgram = "";
                objModelOption3.EDLReset = "";
                objModelOption3.EDLTimeout = 600;
                objModelOption3.EDLSuccessFlag = "";
                m_dic_ModelOption.Add(PANEL_3, objModelOption3);

                // Unit4
                ModelOption objModelOption4 = new ModelOption();
                objModelOption4.ImageServerPath = "";
                objModelOption4.ImageLocalPath = "";
                objModelOption4.ImageCopyMode = "";
                objModelOption4.FlashMode = "";
                objModelOption4.CheckManualBITResult_Enable = "";
                objModelOption4.FASTBOOTEnable = "";
                objModelOption4.FASTBOOTBatServerPath = "";
                objModelOption4.FASTBOOTBatLocalPath = "";
                objModelOption4.FASTBOOTBatFile = "";
                objModelOption4.FASTBOOTTimeout = 600;
                objModelOption4.FASTBOOTSuccessFlag = "";
                objModelOption4.EDLQFIL = "";
                objModelOption4.EDLDeviceType = "";
                objModelOption4.EDLFlashType = "";
                objModelOption4.EDLELF = "";
                objModelOption4.EDLPatch = "";
                objModelOption4.EDLRawProgram = "";
                objModelOption4.EDLReset = "";
                objModelOption4.EDLTimeout = 600;
                objModelOption4.EDLSuccessFlag = "";
                m_dic_ModelOption.Add(PANEL_4, objModelOption4);

                #endregion

                #region m_dic_TestSaveData

                m_dic_TestSaveData.Clear();

                // Unit1
                TestSaveData objSaveData1 = new TestSaveData();
                objSaveData1.TestRecord.ToolNumber = Program.g_str_ToolNumber;
                objSaveData1.TestRecord.ToolRev = Program.g_str_ToolRev;
                objSaveData1.TestRecord.SN = "";
                objSaveData1.TestRecord.Model = "";
                objSaveData1.TestRecord.SKU = "";
                objSaveData1.TestRecord.OSVersion = "";
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
                objSaveData2.TestRecord.Model = "";
                objSaveData2.TestRecord.SKU = "";
                objSaveData2.TestRecord.OSVersion = "";
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
                objSaveData3.TestRecord.Model = "";
                objSaveData3.TestRecord.SKU = "";
                objSaveData3.TestRecord.OSVersion = "";
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
                objSaveData4.TestRecord.Model = "";
                objSaveData4.TestRecord.SKU = "";
                objSaveData4.TestRecord.OSVersion = "";
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
