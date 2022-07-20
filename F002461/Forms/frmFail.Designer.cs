namespace F002461.Forms
{
    partial class frmFail
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTestResult = new System.Windows.Forms.Label();
            this.btn_Continue = new System.Windows.Forms.Button();
            this.lblErrorMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTestResult
            // 
            this.lblTestResult.Font = new System.Drawing.Font("Arial Black", 72F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.lblTestResult.ForeColor = System.Drawing.Color.Black;
            this.lblTestResult.Location = new System.Drawing.Point(57, 84);
            this.lblTestResult.Name = "lblTestResult";
            this.lblTestResult.Size = new System.Drawing.Size(493, 131);
            this.lblTestResult.TabIndex = 0;
            this.lblTestResult.Text = "FAIL";
            this.lblTestResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Continue
            // 
            this.btn_Continue.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Continue.Font = new System.Drawing.Font("Arial", 12F);
            this.btn_Continue.Location = new System.Drawing.Point(210, 291);
            this.btn_Continue.Name = "btn_Continue";
            this.btn_Continue.Size = new System.Drawing.Size(192, 48);
            this.btn_Continue.TabIndex = 1;
            this.btn_Continue.Text = "Continue";
            this.btn_Continue.UseVisualStyleBackColor = false;
            this.btn_Continue.Click += new System.EventHandler(this.btn_Continue_Click);
            // 
            // lblErrorMessage
            // 
            this.lblErrorMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblErrorMessage.Location = new System.Drawing.Point(80, 238);
            this.lblErrorMessage.Name = "lblErrorMessage";
            this.lblErrorMessage.Size = new System.Drawing.Size(470, 38);
            this.lblErrorMessage.TabIndex = 2;
            this.lblErrorMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // frmFail
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Red;
            this.ClientSize = new System.Drawing.Size(590, 363);
            this.Controls.Add(this.lblErrorMessage);
            this.Controls.Add(this.btn_Continue);
            this.Controls.Add(this.lblTestResult);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmFail";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TestResult";
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.Label lblTestResult;
        internal System.Windows.Forms.Button btn_Continue;
        private System.Windows.Forms.Label lblErrorMessage;
    }
}