using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F002461.Forms
{
    public partial class frmMCF : Form
    {
        #region Variable

        private string m_str_Model = "";
        private string m_str_SKU = "";
        private string m_str_OSPN = "";
        private string m_str_OSVersion = "";

        #endregion

        #region Propery

        public string Model
        {
            get
            {
                return m_str_Model;
            }
        }

        public string SKU
        {
            get
            {
                return m_str_SKU;
            }
        }

        public string OSPN
        {
            get
            {
                return m_str_OSPN;
            }
        }

        public string OSVersion
        {
            get
            {
                return m_str_OSVersion;
            }
        }

        #endregion

        #region Load

        public frmMCF()
        {
            InitializeComponent();
        }

        private void FrmMCF_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;

            //textBox_Model.Focus();

            lblModel.Visible = false;
            textBox_Model.Visible = false;
            textBox_SKU.Focus();
        }

        #endregion

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;

            //// Model
            //m_str_Model = this.textBox_Model.Text.Trim();
            //if (m_str_Model.Length <= 0)
            //{
            //    return;
            //}

            // SKU
            m_str_SKU = this.textBox_SKU.Text.Trim();
            if (m_str_SKU.Length <= 0)
            {
                return;
            }

            // OS PN
            m_str_OSPN = this.textBox_OSPN.Text.Trim();
            if (m_str_OSPN.Length <= 0)
            {
                return;
            }

            // OS Version
            m_str_OSVersion = this.textBox_OSVersion.Text.Trim();
            if (m_str_OSVersion.Length <= 0)
            {
                return;
            }

            this.DialogResult = DialogResult.Yes;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
        }

        #endregion

        #region KeyDown

        private void textBox_Model_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBox_Model.Text = "EDA61K";
                textBox_SKU.Focus();
            }
            if (e.KeyCode == Keys.F2)
            {
                textBox_Model.Text = "EDA71";
                textBox_SKU.Focus();
            }
            if (e.KeyCode == Keys.F3)
            {
                textBox_Model.Text = "CT40";
                textBox_SKU.Focus();
            }
            if (e.KeyCode == Keys.F4)
            {
                textBox_Model.Text = "CT40";
                textBox_SKU.Focus();
            }
            if (e.KeyCode == Keys.F5)
            {
                textBox_Model.Text = "EDA52";
                textBox_SKU.Focus();
            }
            if (e.KeyValue == 13)
            {
                textBox_OSPN.Focus();
            }
        }

        private void textBox_SKU_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBox_SKU.Text = "EDA61K-0-UB21PCC";
                textBox_OSPN.Focus();
            }
            if (e.KeyCode == Keys.F2)
            {
                textBox_SKU.Text = "EDA71-0-B731SOGR";
                textBox_OSPN.Focus();
            }
            if (e.KeyCode == Keys.F3)
            {
                textBox_SKU.Text = "CT40-L1N-17C210E";
                textBox_OSPN.Focus();
            }
            if (e.KeyCode == Keys.F4)
            {
                textBox_SKU.Text = "CT40-L1N-1NC11BE";
                textBox_OSPN.Focus();
            }
            if (e.KeyCode == Keys.F5)
            {
                textBox_SKU.Text = "EDA52-11AE7AN21R";
                textBox_OSPN.Focus();
            }
            if (e.KeyValue == 13)
            {
                textBox_OSPN.Focus();
            }
        }

        private void textBox_OSPN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBox_OSPN.Text = "50145760-004";
                textBox_OSVersion.Focus();
            }
            if (e.KeyCode == Keys.F2)
            {
                textBox_OSPN.Text = "50151151-001";
                textBox_OSVersion.Focus();
            }
            if (e.KeyCode == Keys.F3)
            {
                textBox_OSPN.Text = "50142920-001";
                textBox_OSVersion.Focus();
            }
            if (e.KeyCode == Keys.F4)
            {
                textBox_OSPN.Text = "50151151-001";
                textBox_OSVersion.Focus();
            }
            if (e.KeyValue == 13)
            {
                textBox_OSVersion.Focus();
            }
        }

        private void textBox_OSVersion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBox_OSVersion.Text = "214.02.00.0001E";
                btnOK.Focus();
            }
            if (e.KeyCode == Keys.F2)
            {
                textBox_OSVersion.Text = "213.01.00.0003";
                btnOK.Focus();
            }
            if (e.KeyCode == Keys.F3)
            {
                textBox_OSVersion.Text = "83.00.00FIMGG2H-(2082)";
                btnOK.Focus();
            }
            if (e.KeyCode == Keys.F4)
            {
                textBox_OSVersion.Text = "85.00.00-DEBUG-TESTG2H-(0604)";
                btnOK.Focus();
            }
            if (e.KeyCode == Keys.F5)
            {
                textBox_OSVersion.Text = "EDA52-smt-218.01.07.0024";
                btnOK.Focus();
            }
            if (e.KeyValue == 13)
            {
                btnOK.Focus();
            }
        }

        #endregion

     
    }
}
