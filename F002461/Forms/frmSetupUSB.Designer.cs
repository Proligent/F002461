namespace F002461.Forms
{
    partial class frmSetupUSB
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
            this.lblNote = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Panel = new System.Windows.Forms.ComboBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.rtb_InfoList = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // lblNote
            // 
            this.lblNote.BackColor = System.Drawing.Color.White;
            this.lblNote.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblNote.Font = new System.Drawing.Font("Century Gothic", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNote.Location = new System.Drawing.Point(5, 10);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(488, 87);
            this.lblNote.TabIndex = 0;
            this.lblNote.Text = "Note:";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(79, 114);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 24);
            this.label1.TabIndex = 1;
            this.label1.Text = "Panel:";
            // 
            // comboBox_Panel
            // 
            this.comboBox_Panel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Panel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            this.comboBox_Panel.FormattingEnabled = true;
            this.comboBox_Panel.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4"});
            this.comboBox_Panel.Location = new System.Drawing.Point(154, 111);
            this.comboBox_Panel.Name = "comboBox_Panel";
            this.comboBox_Panel.Size = new System.Drawing.Size(211, 32);
            this.comboBox_Panel.TabIndex = 2;
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            this.btnOK.Location = new System.Drawing.Point(207, 160);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(88, 39);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // rtb_InfoList
            // 
            this.rtb_InfoList.BackColor = System.Drawing.SystemColors.Control;
            this.rtb_InfoList.Font = new System.Drawing.Font("Courier New", 12F);
            this.rtb_InfoList.ForeColor = System.Drawing.Color.Black;
            this.rtb_InfoList.Location = new System.Drawing.Point(10, 214);
            this.rtb_InfoList.Name = "rtb_InfoList";
            this.rtb_InfoList.ReadOnly = true;
            this.rtb_InfoList.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.rtb_InfoList.Size = new System.Drawing.Size(485, 140);
            this.rtb_InfoList.TabIndex = 4;
            this.rtb_InfoList.Text = "";
            // 
            // frmSetupUSB
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(503, 362);
            this.Controls.Add(this.rtb_InfoList);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.comboBox_Panel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblNote);
            this.MaximizeBox = false;
            this.Name = "frmSetupUSB";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "USB Address Map";
            this.Load += new System.EventHandler(this.frmSetupUSB_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_Panel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.RichTextBox rtb_InfoList;
    }
}