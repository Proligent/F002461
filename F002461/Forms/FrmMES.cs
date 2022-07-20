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
    public partial class FrmMES : Form
    {
        #region Variable

        private string m_str_EID = "";
        private string m_str_WorkOrder = "";

        #endregion

        #region Propery

        public string EID
        {
            get
            {
                return m_str_EID;
            }
        }

        public string WorkOrder
        {
            get
            {
                return m_str_WorkOrder;
            }
        }

        #endregion

        #region Load

        public FrmMES()
        {
            InitializeComponent();
        }

        private void FrmMES_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;
            textBox_EID.Focus();
        }

        #endregion

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;

            // EID
            m_str_EID = this.textBox_EID.Text.Trim();
            if (m_str_EID.Length != 7)
            {
                return;
            }

            // WorkOrder
            m_str_WorkOrder = this.textBox_WorkOrder.Text.Trim();
            if (m_str_WorkOrder.Length <= 0)
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

        private void textBox_EID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBox_EID.Text = "E123456";
                textBox_WorkOrder.Focus();
            }
            if (e.KeyValue == 13)
            {
                textBox_WorkOrder.Focus();
            }
        }

        private void textBox_WorkOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                textBox_WorkOrder.Text = "WO123456";
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
