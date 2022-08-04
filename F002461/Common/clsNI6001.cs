using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F002461
{
    public class clsNI6001
    {
        #region Structs

        public struct Line_t
        {
            public string LineName;
            public int High;
            public int Low;
        }

        public struct PortLine_t
        {
            // NiDioPort0
            public Line_t[][] Port;

            // NiAnaInLine
            public string[] AnaInLine;

            // NiAnaOutLine
            public string[] AnaOutLine;
        }

        #endregion

        #region Variables

        private PortLine_t m_st_PortLine;
        private string m_str_DaqDevice = "";

        private clsDaqmx m_obj_Daqmx = null;
        private string m_str_Error = "";

        #endregion

        #region Properties

        public PortLine_t PortLine
        {
            get
            {
                return m_st_PortLine;
            }
        }

        public string DaqDevice
        {
            set
            {
                m_str_DaqDevice = value;
            }
        }

        #endregion

        #region Constructor

        public clsNI6001()
        {
            m_st_PortLine = new PortLine_t();

            m_st_PortLine.Port = new Line_t[3][];
            m_st_PortLine.Port[0] = new Line_t[8];
            m_st_PortLine.Port[1] = new Line_t[4];
            m_st_PortLine.Port[2] = new Line_t[1];

            m_st_PortLine.AnaInLine = new string[8];

            m_st_PortLine.AnaOutLine = new string[2];

            m_obj_Daqmx = new clsDaqmx();
        }

        ~clsNI6001()
        {
            if (m_obj_Daqmx != null)
            {
                m_obj_Daqmx = null;
            }
        }

        #endregion

        #region Function

        public bool Init(string str_DaqDevice)
        {
            try
            {
                m_str_DaqDevice = str_DaqDevice;
                m_obj_Daqmx.DaqDevice = m_str_DaqDevice;
                if (m_obj_Daqmx.InitDaq() == false)
                {
                    m_str_Error = m_obj_Daqmx.DaqErrorMsg;
                    return false;
                }

                Init_PortLine();
            }
            catch (Exception ex)
            {
                m_str_Error = "Init Exception." + ex.Message;
                return false;
            }

            return true;
        }

        public bool SetDigital(int i_Port, int i_Line, int i_Value, double d_Delay)
        {
            try
            {
                if (i_Value == 1)
                {
                    m_obj_Daqmx.WriteDigPort(m_st_PortLine.Port[i_Port][i_Line].LineName, "", m_st_PortLine.Port[i_Port][i_Line].High);
                }
                else if (i_Value == 0)
                {
                    m_obj_Daqmx.WriteDigPort(m_st_PortLine.Port[i_Port][i_Line].LineName, "", m_st_PortLine.Port[i_Port][i_Line].Low);
                }
                else
                {
                }

                Dly(d_Delay);
            }
            catch (Exception ex)
            {
                m_str_Error = "SetDigital Exception." + ex.Message;
                return false;
            }

            return true;
        }

        public bool GetDigital(int i_Port, int i_Line, ref int i_Value, double d_Delay)
        {
            try
            {
                i_Value = m_obj_Daqmx.ReadDigChannel(m_st_PortLine.Port[i_Port][i_Line].LineName, "");

                Dly(d_Delay);
            }
            catch (Exception ex)
            {
                m_str_Error = "GetDigital Exception." + ex.Message;
                return false;
            }

            return true;
        }

        public bool SetAnalog(int i_Port, double d_Value, double d_Delay)
        {
            try
            {
                m_obj_Daqmx.WriteVoltageAnalogChannel(m_st_PortLine.AnaOutLine[i_Port], "", d_Value, -10, 10);

                Dly(d_Delay);
            }
            catch (Exception ex)
            {
                m_str_Error = "SetAnalog Exception." + ex.Message;
                return false;
            }

            return true;
        }

        public bool GetAnalog(int i_Port, ref double d_Value, double d_Delay)
        {
            try
            {
                int res = 0;
                double[] value = null;
                res = m_obj_Daqmx.ReadAnalogChannel(m_st_PortLine.AnaInLine[i_Port], "Rse Analog Channel", NationalInstruments.DAQmx.AITerminalConfiguration.Rse, ref value, -10, 10);

                d_Value = value[0];

                Dly(d_Delay);
            }
            catch (Exception ex)
            {
                m_str_Error = "GetAnalog Exception." + ex.Message;
                return false;
            }

            return true;
        }

        public bool GetAnalog(int i_Port, int i_Cnt, ref double d_Value, double d_Delay)
        {
            try
            {
                int res = 0;
                double[] value = null;
                double anaout = 0;
                for (int i = 0; i < i_Cnt; i++)
                {
                    res = m_obj_Daqmx.ReadAnalogChannel(m_st_PortLine.AnaInLine[i_Port], "Rse Analog Channel", NationalInstruments.DAQmx.AITerminalConfiguration.Rse, ref value, -10, 10);
                    anaout += value[0];
                }
                anaout = anaout / i_Cnt;

                d_Value = anaout;

                Dly(d_Delay);
            }
            catch (Exception ex)
            {
                m_str_Error = "GetAnalog Exception." + ex.Message;
                return false;
            }

            return true;
        }

        private void Dly(double d_WaitTimeSecond)
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

            return;
        }

        private void Init_PortLine()
        {
            int i_PortIndex = 0;
            int i_LineIndex = 0;

            // NiDioPort0
            i_PortIndex = 0;
            for (i_LineIndex = 0; i_LineIndex < 8; i_LineIndex++)
            {
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].LineName = m_str_DaqDevice + "/port" + i_PortIndex.ToString() + "/line" + i_LineIndex.ToString();
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].High = 1 << i_LineIndex;
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].Low = 0;
            }

            i_PortIndex = 1;
            for (i_LineIndex = 0; i_LineIndex < 4; i_LineIndex++)
            {
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].LineName = m_str_DaqDevice + "/port" + i_PortIndex.ToString() + "/line" + i_LineIndex.ToString();
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].High = 1 << i_LineIndex;
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].Low = 0;
            }

            i_PortIndex = 2;
            for (i_LineIndex = 0; i_LineIndex < 1; i_LineIndex++)
            {
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].LineName = m_str_DaqDevice + "/port" + i_PortIndex.ToString() + "/line" + i_LineIndex.ToString();
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].High = 1 << i_LineIndex;
                m_st_PortLine.Port[i_PortIndex][i_LineIndex].Low = 0;
            }

            // NiAnaInLine
            for (i_LineIndex = 0; i_LineIndex < 8; i_LineIndex++)
            {
                m_st_PortLine.AnaInLine[i_LineIndex] = m_str_DaqDevice + "/ai" + i_LineIndex.ToString();
            }

            // NiAnaOutLine
            for (i_LineIndex = 0; i_LineIndex < 2; i_LineIndex++)
            {
                m_st_PortLine.AnaOutLine[i_LineIndex] = m_str_DaqDevice + "/ao" + i_LineIndex.ToString();
            }
        }

        #endregion
    
    }

}
