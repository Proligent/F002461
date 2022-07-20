using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NationalInstruments;
using NationalInstruments.DAQmx;

namespace F002461
{
    public class clsDaqmx
    {
        #region Variables

        private DaqSystem m_DaqSystem = null;
        private string[] m_str_DeviceList;          // 电脑安装的NI板卡设备名列表
        private Device[] m_obj_DeviceList = null;   // 电脑安装的NI板卡设备对象列表
        private int m_i_DeviceNumber = 0;           // 电脑安装的NI板卡总个数

        private string m_str_DaqDevice = "";
        private string[] m_str_DaqDeviceList;
        private int m_i_DaqDeviceNumber = 0;

        private Task m_Task_MeasureLowFreq;
        private CounterReader m_CounterReader;
        private AsyncCallback m_AsyncCallback;
        private double m_d_MeasuredFrequency = 0;

        private bool m_b_Initialized = false;
        private string m_str_Error = "";

        #endregion

        #region Properties

        public string DaqDevice
        {
            get
            {
                return m_str_DaqDevice;
            }
            set
            {
                m_str_DaqDevice = value;
            }
        }

        public string DaqErrorMsg
        {
            get
            {
                return m_str_Error;
            }
            set
            {
                m_str_Error = value;
            }
        }

        public double MeasuredFrequency
        {
            get
            {
                return m_d_MeasuredFrequency;
            }
            set
            {
                m_d_MeasuredFrequency = value;
            }
        }

        #endregion

        #region Constructor

        public clsDaqmx()
        {
            m_DaqSystem = DaqSystem.Local;
            m_str_DeviceList = m_DaqSystem.Devices;
            m_i_DeviceNumber = m_str_DeviceList.Length;

            m_str_DaqDevice = "";
            m_b_Initialized = false;
            m_str_Error = "";
        }

        ~clsDaqmx()
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                string strr = ex.Message;
            }
        }

        #endregion

        #region Function

        public bool InitDaq()
        {
            try
            {
                if (!m_b_Initialized)
                {
                    bool b_FoundDevice = false;
                    int i = 0;

                    m_str_DaqDeviceList = FindDevices();
                    for (i = 0; i <= m_str_DaqDeviceList.GetUpperBound(0); i++)
                    {
                        if (m_str_DaqDeviceList[i] == m_str_DaqDevice)
                        {
                            b_FoundDevice = true;
                            break;
                        }
                    }
                    if (b_FoundDevice == false)
                    {
                        m_str_Error = "Error: Unable to find NiCard " + m_str_DaqDevice + ".";
                        return false;
                    }

                    m_str_DaqDevice = m_str_DaqDeviceList[i];
                    m_i_DaqDeviceNumber = i;
                }
            }
            catch (Exception ex)
            {
                m_str_Error = "Error: Unable to find NiCard." + ex.ToString();
                return false;
            }

            m_b_Initialized = true;

            return true;
        }

        private string[] FindDevices()
        {
            try
            {
                m_obj_DeviceList = new Device[m_i_DeviceNumber];
                string[] id = new string[m_i_DeviceNumber];

                for (int i = 0; i < m_i_DeviceNumber; i++)
                {
                    m_obj_DeviceList[i] = m_DaqSystem.LoadDevice(m_str_DeviceList[i]);
                    id[i] = m_obj_DeviceList[i].DeviceID;
                }

                return id;
            }
            catch (Exception ex)
            {
                m_str_Error = "Error:FindDevices Fail." + ex.ToString();
                return null;
            }
        }

        private void Close()
        {
            Reset(m_i_DaqDeviceNumber);
        }

        private bool Reset(int i_DaqDeviceNumber)
        {
            try
            {
                if (m_obj_DeviceList[i_DaqDeviceNumber] != null)
                {
                    m_obj_DeviceList[i_DaqDeviceNumber].Reset();
                    m_obj_DeviceList[i_DaqDeviceNumber] = null;
                }
            }
            catch (Exception ex)
            {
                m_str_Error = "Reset Exception:" + ex.Message;
                return false;
            }

            return true;
        }

        #region Digital

        /// <summary>
        /// WriteDigPort
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void WriteDigPort(string lines, string name, int val)
        {
            // Create a task such that it will be disposed after we are done using it.
            Task digitalWriteTask = new Task();
            DOChannel ch;

            try
            {
                // Create a Digital Output channel and name it.
                ch = digitalWriteTask.DOChannels.CreateChannel(lines, "line0", ChannelLineGrouping.OneChannelForAllLines);

                // Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                // of digital data on demand, so no timeout is necessary.
                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);

                DaqBuffer buf;
                buf = digitalWriteTask.Stream.Buffer;
                digitalWriteTask.Start();
                writer.WriteSingleSamplePort(false, val);
                digitalWriteTask.Stop();
                buf = digitalWriteTask.Stream.Buffer;
            }
            catch (System.Exception ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                digitalWriteTask.Dispose();
            }
        }

        public void WriteDigPortLine(string lines, string name, int val)
        {
            // Create a task such that it will be disposed after we are done using it.
            Task digitalWriteTask = new Task();
            DOChannel ch;

            try
            {
                // Create a Digital Output channel and name it.
                ch = digitalWriteTask.DOChannels.CreateChannel(lines, "line0", ChannelLineGrouping.OneChannelForAllLines);

                // Write digital port data. WriteDigitalSingChanSingSampPort writes a single sample
                // of digital data on demand, so no timeout is necessary.
                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);

                DaqBuffer buf;
                buf = digitalWriteTask.Stream.Buffer;
                digitalWriteTask.Start();
                writer.WriteSingleSamplePort(false, val);
                digitalWriteTask.Stop();
                buf = digitalWriteTask.Stream.Buffer;
            }
            catch (System.Exception ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                digitalWriteTask.Dispose();
            }
        }

        public void WriteDigitialPulse(string counter, COPulseIdleState iState, double frequency, double dutyCycle)
        {
            // This example uses the default source (or gate) terminal for 
            // the counter of your device.  To determine what the default 
            // counter pins for your device are or to set a different source 
            // (or gate) pin, refer to the Connecting Counter Signals topic
            // in the NI-DAQmx Help (search for "Connecting Counter Signals").

            Task myTask = new Task();
            COChannel CO;
            try
            {
                CO = myTask.COChannels.CreatePulseChannelFrequency(counter, "ContinuousPulseTrain", COPulseFrequencyUnits.Hertz, iState, 0.0, frequency, dutyCycle);
                myTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples, 1000);

                myTask.Start();
                myTask.Stop();
            }
            catch (System.Exception ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                myTask.Dispose();
            }
        }

        public int WriteDigChannel(string lines, string name, int val)
        {
            // Create a task such that it will be disposed after we are done using it.
            int res = 0;
            Task digitalWriteTask = new Task();
            DOChannel ch;

            try
            {
                // Create channel
                ch = digitalWriteTask.DOChannels.CreateChannel(lines, "port0", ChannelLineGrouping.OneChannelForEachLine);

                // Dim st As DOLineStatesStartState
                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalWriteTask.Stream);

                bool[] data;
                data = reader.ReadSingleSampleMultiLine();
                digitalWriteTask.Start();
                if (ch.Tristate == true)
                    ch.Tristate = false;
                writer.WriteSingleSamplePort(false, val);
                digitalWriteTask.Stop();
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
                res = -1;
            }
            finally
            {
                digitalWriteTask.Dispose();
            }
            return res;
        }

        public int WriteDigChannelGroup(string lines, string name, uint val)
        {
            // Create a task such that it will be disposed after we are done using it.
            int res = 0;
            Task digitalWriteTask = new Task();
            DOChannel ch;

            try
            {
                // Create channel
                ch = digitalWriteTask.DOChannels.CreateChannel(lines, "digwrite", ChannelLineGrouping.OneChannelForAllLines);

                // Dim st As DOLineStatesStartState
                DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalWriteTask.Stream);

                bool[] data;
                data = reader.ReadSingleSampleMultiLine();

                digitalWriteTask.Start();
                //st = ch.LineStatesStartState
                if (ch.Tristate == true)
                    ch.Tristate = false;
                writer.WriteSingleSamplePort(false, val);
                digitalWriteTask.Stop();
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
                res = -1;
            }
            finally
            {
                digitalWriteTask.Dispose();
            }
            return res;
        }

        /// <summary>
        /// ReadDigChannel
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public int ReadDigChannel(string lines, string name)
        {
            // Create a task such that it will be disposed after we are done using it.
            Task digitalReadTask = new Task();
            int val = 0;

            try
            {
                // Create channel
                digitalReadTask.DIChannels.CreateChannel(lines, "DigRead", ChannelLineGrouping.OneChannelForEachLine);

                bool[] data;
                DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                digitalReadTask.Start();
                data = reader.ReadSingleSampleMultiLine();
                digitalReadTask.Stop();

                for (int index = 0; index < data.Length; index++)
                {
                    if (data[index] == true)
                    {
                        // if bit is true add decimal value of bit
                        val += 1 << index;
                    }

                }
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                digitalReadTask.Dispose();
            }

            return val;
        }

        public uint ReadDigPort(string lines, string name)
        {
            // Create a task such that it will be disposed after we are done using it.
            Task digitalReadTask = new Task();
            uint data = 0;

            try
            {
                // Create channel
                digitalReadTask.DIChannels.CreateChannel(lines, "port0", ChannelLineGrouping.OneChannelForAllLines);

                DigitalSingleChannelReader reader = new DigitalSingleChannelReader(digitalReadTask.Stream);
                digitalReadTask.Start();
                data = reader.ReadSingleSamplePortUInt32();
                digitalReadTask.Stop();

                //Debug.Print(String.Format("0x{0:X}", data));
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                digitalReadTask.Dispose();
            }

            return data;
        }

        public bool ReadDigitalLowFrequency(string lines, string name, double min, double max, double time)
        {
            // This example uses the default source (or gate) terminal for 
            // the counter of your device.  To determine what the default 
            // counter pins for your device are or to set a different source 
            // (or gate) pin, refer to the Connecting Counter Signals topic
            // in the NI-DAQmx Help (search for "Connecting Counter Signals").
            // Uses default PFI 9/P2.1 as the pulse input.
            try
            {
                m_d_MeasuredFrequency = 0;

                m_Task_MeasureLowFreq = new Task();
                m_Task_MeasureLowFreq.CIChannels.CreateFrequencyChannel(lines, name, min, max, CIFrequencyStartingEdge.Rising,
                    CIFrequencyMeasurementMethod.LowFrequencyOneCounter, time, 4, CIFrequencyUnits.Hertz);
                m_CounterReader = new CounterReader(m_Task_MeasureLowFreq.Stream);

                // For .NET Framework 2.0 or later, use SynchronizeCallbacks to specify that the object 
                // marshals callbacks across threads appropriately.
                m_CounterReader.SynchronizeCallbacks = true;

                m_AsyncCallback = new AsyncCallback(CounterInCallback);
                m_CounterReader.BeginReadSingleSampleDouble(m_AsyncCallback, null);
            }
            catch (Exception ex)
            {
                m_str_Error = ex.Message;
                m_Task_MeasureLowFreq.Dispose();
                return false;
            }

            return true;
        }

        public double ReadDigitalPulseWidth(string counter, CIPulseWidthStartingEdge StartingEdge, double minPW_InSeconds, double maxPW_InSeconds)
        {
            // This example uses the default source (or gate) terminal for 
            // the counter of your device.  To determine what the default 
            // counter pins for your device are or to set a different source 
            // (or gate) pin, refer to the Connecting Counter Signals topic
            // in the NI-DAQmx Help (search for "Connecting Counter Signals").
            // Uses default PFI 9/P2.1 as the pulse input.
            // Returns PW in seconds.

            double pulseWidth = 0;
            Task myTask = null;
            NationalInstruments.DAQmx.CounterReader myCounterReader;

            try
            {
                myTask = new Task();
                myTask.CIChannels.CreatePulseWidthChannel(counter,
                    "Meas Pulse Width", minPW_InSeconds,
                    maxPW_InSeconds, StartingEdge,
                    CIPulseWidthUnits.Seconds);
                //RLC Wait up to 5 sec default was 10 sec
                myTask.Stream.Timeout = 5000;
                myCounterReader = new CounterReader(myTask.Stream);

                // For .NET Framework 2.0 or later, use SynchronizeCallbacks to specify that the object 
                // marshals callbacks across threads appropriately.
                myCounterReader.SynchronizeCallbacks = true;
                // For .NET Framework 1.1, set SynchronizingObject to the Windows Form to specify 
                // that the object marshals callbacks across threads appropriately.
                //myCounterReader.SynchronizingObject = this;


                //rlc AsyncCallback myCallback = new AsyncCallback(MeasurementCallback);
                //myCounterReader.BeginReadSingleSampleDouble(myCallback, null);
                IAsyncResult ar = null;
                ar = myCounterReader.BeginReadSingleSampleDouble(null, null);

                pulseWidth = myCounterReader.EndReadSingleSampleDouble(ar);
            }
            catch (Exception exception)
            {
                m_str_Error = exception.Message;
                pulseWidth = -1;
            }
            finally
            {
                if (myTask != null)
                    myTask.Dispose();
            }

            return pulseWidth;
        }

        #endregion

        #region Analog

        /// <summary>
        /// ReadAnalogChannel
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <param name="channelData"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public int ReadAnalogChannel(string lines, string name, AITerminalConfiguration config, ref double[] channelData, double min, double max)
        {
            // Create a task such that it will be disposed after we are done using it.
            Task analogReadTask = new Task();
            AIChannel ch = null;
            double[] channelData2 = null;

            if (max == 0)
            {
                max = 10;
            }

            try
            {
                // If min > ai.Minimum And max < ai.Maximum Then
                // Create a virtual channel
                ch = analogReadTask.AIChannels.CreateVoltageChannel(lines, "", config, min, max, AIVoltageUnits.Volts);

                // Verify the Task
                analogReadTask.Control(TaskAction.Verify);

                channelData = new double[analogReadTask.AIChannels.Count];
                channelData2 = new double[analogReadTask.AIChannels.Count];

                AnalogMultiChannelReader reader = new AnalogMultiChannelReader(analogReadTask.Stream);
                analogReadTask.Start();
                channelData2 = reader.ReadSingleSample();
                analogReadTask.Stop();

                for (int i = 0; i < analogReadTask.AIChannels.Count; i++)
                {
                    //Debug.Print(channelData2[0].ToString("#0.00"));
                }
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                //analogReadTask.Dispose();
                Array.Copy(channelData2, channelData, analogReadTask.AIChannels.Count);
                channelData = channelData2;
            }

            return 0;
        }

        public int ReadAnalogChannelConvertToDigital(string lines, string name, AITerminalConfiguration config, ref double[] channelData, double min, double max)
        {
            // Create a task such that it will be disposed after we are done using it.
            int val = 0;
            Task analogReadTask = new Task();
            AIChannel ch = null;

            if (max == 0)
            {
                max = 10;
            }

            try
            {
                // If min > ai.Minimum And max < ai.Maximum Then
                // Create a virtual channel
                ch = analogReadTask.AIChannels.CreateVoltageChannel(lines, "", config, min, max, AIVoltageUnits.Volts);

                // Verify the Task
                analogReadTask.Control(TaskAction.Verify);

                channelData = new double[analogReadTask.AIChannels.Count];
                //channelData2 = new double[analogReadTask.AIChannels.Count];
                AnalogMultiChannelReader reader = new AnalogMultiChannelReader(analogReadTask.Stream);
                analogReadTask.Start();
                channelData = reader.ReadSingleSample();
                analogReadTask.Stop();

                for (int i = 0; i < analogReadTask.AIChannels.Count; i++)
                {
                    //Debug.Print(channelData[0].ToString("#0.00"));
                }

                for (int i = 0; i < channelData.Length; i++)
                {
                    if (channelData[i] > 2.0)
                    {
                        // if bit is > 2.0V (logical high) add decimal value of bit
                        val += 1 << i;
                    }
                }
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                //analogReadTask.Dispose();
                //Array.Copy(channelData2, channelData, analogReadTask.AIChannels.Count);
                //channelData = channelData2;
            }

            return val;
        }

        public int ReadWaveformAnalogChannel(string lines, string name, AITerminalConfiguration config, ref AnalogWaveform<double>[] channelData, double min, double max, int nsamples)
        {
            // Create a task such that it will be disposed after we are done using it.
            Task analogReadTask = new Task();
            AIChannel ch = null;
            AnalogWaveform<double>[] channelData2 = new AnalogWaveform<double>[analogReadTask.AIChannels.Count];

            if (max == 0)
            {
                max = 10;
            }
            if (nsamples == 0)
            {
                nsamples = -1;
            }

            try
            {
                // If min > ai.Minimum And max < ai.Maximum Then
                // Create a virtual channel
                ch = analogReadTask.AIChannels.CreateVoltageChannel(lines, "", config, min, max, AIVoltageUnits.Volts);

                // Verify the Task
                analogReadTask.Control(TaskAction.Verify);

                AnalogMultiChannelReader reader = new AnalogMultiChannelReader(analogReadTask.Stream);
                analogReadTask.Start();
                channelData2 = reader.ReadWaveform(nsamples);
                analogReadTask.Stop();

                //Debug.Print(String.Format("0x{0:X}", channelData2));
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
            }
            finally
            {
                analogReadTask.Dispose();
                Array.Copy(channelData2, channelData, analogReadTask.AIChannels.Count);
                channelData = channelData2;
            }

            return 0;
        }

        public int WriteAnalogChannel(string lines, string name, int freq, int samples, int cycles, string sigtype, double ampl, double min, double max)
        {
            int res = 0;
            Task analogWriteTask = new Task();
            AOChannel ch;

            if (max == 0)
                max = 10;

            try
            {
                // Create the task and channel
                //myTask = New Task()
                ch = analogWriteTask.AOChannels.CreateVoltageChannel(lines, "", min, max, AOVoltageUnits.Volts);

                // Verify the task before doing the waveform calculations
                analogWriteTask.Control(TaskAction.Verify);

                // Calculate some waveform parameters and generate data
                //Dim fGen As New FunctionGenerator( _
                //    analogWriteTask.Timing, _
                //    freq.ToString, _
                //    samples.ToString, _
                //    cycles.ToString, _
                //    sigtype, _
                //    ampl.ToString)

                //Configure the sample clock with the calculated rate
                //analogWriteTask.Timing.ConfigureSampleClock( _
                //    "", _
                //    fGen.ResultingSampleClockRate, _
                //    SampleClockActiveEdge.Rising, _
                //    SampleQuantityMode.ContinuousSamples, 1000)

                // Write the data to the buffer
                AnalogSingleChannelWriter writer = new AnalogSingleChannelWriter(analogWriteTask.Stream);
                //writer.WriteMultiSample(False, fGen.Data)
                //Dim res As IAsyncResult
                //res = writer.BeginWriteMultiSample(False, fGen.Data, AddressOf _WriteAnalogChannelComplete, 0)
                analogWriteTask.Start();
                writer.WriteSingleSample(false, 2.5);

                //Start writing out data
                //analogWriteTask.Start()
                analogWriteTask.Stop();
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
                res = -1;
            }
            finally
            {
                analogWriteTask.Dispose();
            }
            return res;
        }

        public int WriteVoltageAnalogChannel(string lines, string name, double ampl, double min, double max)
        {
            int res = 0;
            Task analogWriteTask = new Task();
            AOChannel ch;

            if (max == 0)
                max = 10;

            try
            {
                // Create the task and channel myTask = New Task()
                ch = analogWriteTask.AOChannels.CreateVoltageChannel(lines, "", min, max, AOVoltageUnits.Volts);

                // Verify the task before doing the waveform calculations
                analogWriteTask.Control(TaskAction.Verify);

                // Write the data to the buffer
                AnalogSingleChannelWriter writer = new AnalogSingleChannelWriter(analogWriteTask.Stream);
                analogWriteTask.Start();
                writer.WriteSingleSample(false, ampl);
                analogWriteTask.Stop();
            }
            catch (DaqException ex)
            {
                m_str_Error = ex.Message;
                res = -1;
            }
            finally
            {
                analogWriteTask.Dispose();
            }
            return res;
        }

        #endregion

        #region Frequency

        public bool MeasureLowFrequency(string lines, string name, double min, double max, double time)
        {
            try
            {
                m_Task_MeasureLowFreq = new Task();
                m_Task_MeasureLowFreq.CIChannels.CreateFrequencyChannel(lines, name, min, max, CIFrequencyStartingEdge.Rising,
                    CIFrequencyMeasurementMethod.LowFrequencyOneCounter, time, 4, CIFrequencyUnits.Hertz);

                m_CounterReader = new CounterReader(m_Task_MeasureLowFreq.Stream);

                // For .NET Framework 2.0 or later, use SynchronizeCallbacks to specify that the object 
                // marshals callbacks across threads appropriately.
                m_CounterReader.SynchronizeCallbacks = true;

                m_AsyncCallback = new AsyncCallback(CounterInCallback);
                m_CounterReader.BeginReadSingleSampleDouble(m_AsyncCallback, null);
            }
            catch (Exception ex)
            {
                m_str_Error = ex.Message;
                m_Task_MeasureLowFreq.Dispose();
                return false;
            }

            return true;
        }

        public bool DisableMeasureLowFrequency()
        {
            try
            {
                m_CounterReader.SynchronizeCallbacks = false;
                m_Task_MeasureLowFreq.Stop();
                m_Task_MeasureLowFreq.Dispose();
                m_d_MeasuredFrequency = 0;
            }
            catch
            {
                m_Task_MeasureLowFreq.Dispose();
            }

            return true;
        }

        private void CounterInCallback(IAsyncResult ar)
        {
            // Read the measured value
            try
            {
                m_d_MeasuredFrequency = m_CounterReader.EndReadSingleSampleDouble(ar);
            }
            catch (DaqException exception)
            {
                m_str_Error = exception.Message;
            }
            finally
            {
                m_Task_MeasureLowFreq.Dispose();
            }
        }

        #endregion

        #endregion
    }
}
