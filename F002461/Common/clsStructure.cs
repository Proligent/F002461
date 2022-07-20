using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F002461
{
    public struct ComPortSetting
    {
        public Int32 PortNum;
        public Int32 BaudRate;
        public string Parity;
        public Int32 DataBits;
        public Int32 StopBits;
    }

    public struct TestResult
    {
        public bool TestPassed;
        public int TestFailCode;
        public string TestFailMessage;
        public string TestStatus;
    }

    public struct TestRecord
    {
        public string ToolNumber;
        public string ToolRev;
        public string SN;
        public string Model;
        public string SKU;
        public string OSVersion;

        public double TestTotalTime;
    }

    public struct TestSaveData
    {
        public TestResult TestResult;
        public TestRecord TestRecord;
    }

}
