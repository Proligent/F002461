namespace F002461.Forms
{
    partial class frmConfirmYESNO
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
            this.textBox_Content = new System.Windows.Forms.TextBox();
            this.btnYES = new System.Windows.Forms.Button();
            this.btnNO = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox_Content
            // 
            this.textBox_Content.BackColor = System.Drawing.SystemColors.Control;
            this.textBox_Content.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_Content.Font = new System.Drawing.Font("Century Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Content.ForeColor = System.Drawing.SystemColors.Highlight;
            this.textBox_Content.Location = new System.Drawing.Point(12, 38);
            this.textBox_Content.Multiline = true;
            this.textBox_Content.Name = "textBox_Content";
            this.textBox_Content.Size = new System.Drawing.Size(260, 99);
            this.textBox_Content.TabIndex = 0;
            this.textBox_Content.Text = "Content";
            this.textBox_Content.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // btnYES
            // 
            this.btnYES.Location = new System.Drawing.Point(26, 179);
            this.btnYES.Name = "btnYES";
            this.btnYES.Size = new System.Drawing.Size(82, 40);
            this.btnYES.TabIndex = 1;
            this.btnYES.Text = "YES";
            this.btnYES.UseVisualStyleBackColor = true;
            // 
            // btnNO
            // 
            this.btnNO.Location = new System.Drawing.Point(175, 179);
            this.btnNO.Name = "btnNO";
            this.btnNO.Size = new System.Drawing.Size(82, 40);
            this.btnNO.TabIndex = 1;
            this.btnNO.Text = "NO";
            this.btnNO.UseVisualStyleBackColor = true;
            // 
            // frmConfirmYESNO
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 241);
            this.Controls.Add(this.btnNO);
            this.Controls.Add(this.btnYES);
            this.Controls.Add(this.textBox_Content);
            this.Name = "frmConfirmYESNO";
            this.Text = "Confirm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Content;
        private System.Windows.Forms.Button btnYES;
        private System.Windows.Forms.Button btnNO;
    }
}