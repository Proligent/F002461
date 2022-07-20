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
    public partial class frmPass : Form
    {
        public frmPass()
        {
            InitializeComponent();
        }

        public string TestResult
        {
            get
            {
                return lblTestResult.Text;
            }
            set
            {
                lblTestResult.Text = value;
            }
        }

        private void FrmPass_Load(object sender, EventArgs e)
        {
            this.btn_Continue.Focus();
        }

        private void btn_Continue_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
