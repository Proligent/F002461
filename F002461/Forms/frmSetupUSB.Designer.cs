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
            this.lblNote.Font = new System.Drawing.Font("Courier New", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNote.Location = new System.Drawing.Point(7, 13);
            this.lblNote.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(651, 116);
            this.lblNote.TabIndex = 0;
            this.lblNote.Text = "Note:";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(105, 152);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 32);
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
            this.comboBox_Panel.Location = new System.Drawing.Point(206, 148);
            this.comboBox_Panel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox_Panel.Name = "comboBox_Panel";
            this.comboBox_Panel.Size = new System.Drawing.Size(280, 37);
            this.comboBox_Panel.TabIndex = 2;
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold);
            this.btnOK.Location = new System.Drawing.Point(276, 214);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(117, 52);
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
            this.rtb_InfoList.Location = new System.Drawing.Point(13, 285);
            this.rtb_InfoList.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.rtb_InfoList.Name = "rtb_InfoList";
            this.rtb_InfoList.ReadOnly = true;
            this.rtb_InfoList.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.rtb_InfoList.Size = new System.Drawing.Size(645, 185);
            this.rtb_InfoList.TabIndex = 4;
            this.rtb_InfoList.Text = "";
            // 
            // frmSetupUSB
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(671, 482);
            this.Controls.Add(this.rtb_InfoList);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.comboBox_Panel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblNote);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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