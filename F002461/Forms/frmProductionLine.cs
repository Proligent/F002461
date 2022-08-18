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
    public partial class frmProductionLine : Form
    {
        #region Variable

        private string m_sProductionLine = "";
  
        #endregion

        #region Propery

        public string ProductionLine
        {
            get
            {
                return m_sProductionLine;
            }
        }

        #endregion

        #region Load

        public frmProductionLine()
        {
            InitializeComponent();
        }

        private void frmProductionLine_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;
            this.comboxPdLine.SelectedItem = "1";
            this.comboxPdLine.Focus();
        }

        #endregion

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;

            m_sProductionLine = this.comboxPdLine.Text;
            if (string.IsNullOrWhiteSpace(m_sProductionLine))
            {
                return;     
            }

            this.DialogResult = DialogResult.Yes;
        }

        #endregion
    }
}
