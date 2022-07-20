namespace F002461.Forms
{
    partial class frmPass
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
            this.SuspendLayout();
            // 
            // lblTestResult
            // 
            this.lblTestResult.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblTestResult.Font = new System.Drawing.Font("Arial Black", 72F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.lblTestResult.ForeColor = System.Drawing.Color.Black;
            this.lblTestResult.Location = new System.Drawing.Point(39, 76);
            this.lblTestResult.Name = "lblTestResult";
            this.lblTestResult.Size = new System.Drawing.Size(505, 112);
            this.lblTestResult.TabIndex = 0;
            this.lblTestResult.Text = "PASSED";
            this.lblTestResult.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_Continue
            // 
            this.btn_Continue.BackColor = System.Drawing.SystemColors.Control;
            this.btn_Continue.Font = new System.Drawing.Font("Arial", 12F);
            this.btn_Continue.Location = new System.Drawing.Point(199, 292);
            this.btn_Continue.Name = "btn_Continue";
            this.btn_Continue.Size = new System.Drawing.Size(191, 48);
            this.btn_Continue.TabIndex = 1;
            this.btn_Continue.Text = "Continue";
            this.btn_Continue.UseVisualStyleBackColor = false;
            this.btn_Continue.Click += new System.EventHandler(this.btn_Continue_Click);
            // 
            // frmPass
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Green;
            this.ClientSize = new System.Drawing.Size(590, 363);
            this.Controls.Add(this.btn_Continue);
            this.Controls.Add(this.lblTestResult);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmPass";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TestResult";
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.Label lblTestResult;
        internal System.Windows.Forms.Button btn_Continue;
    }
}