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
    public partial class frmFail : Form
    {
        public frmFail()
        {
            InitializeComponent();
        }

        public string Message
        {
            get
            {
                return lblErrorMessage.Text;
            }
            set
            {
                lblErrorMessage.Text = value;
            }
        }

        private void btn_Continue_Click(object sender, EventArgs e)
        {
            this.Close();
        }


    }
}
