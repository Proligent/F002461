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
    public partial class frmConfirmOK : Form
    {
        #region Variable

        private string m_str_Content;

        #endregion

        #region Propery

        public string Content
        {
            get
            {
                return m_str_Content;
            }
            set
            {
                m_str_Content = value;
            }
        }

        #endregion

        #region FrmLoad

        public frmConfirmOK()
        {
            InitializeComponent();
        }

        private void FrmConfirm_Load(object sender, EventArgs e)
        {
            this.ControlBox = false;

            this.textBox_Content.Text = m_str_Content;
        }

        #endregion

        #region Event

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        #endregion
    }
}
