namespace F002461.Forms
{
    partial class frmMCF
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
            this.lblModel = new System.Windows.Forms.Label();
            this.textBox_Model = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_SKU = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_OSPN = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_OSVersion = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblModel
            // 
            this.lblModel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblModel.Location = new System.Drawing.Point(37, 31);
            this.lblModel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(91, 32);
            this.lblModel.TabIndex = 0;
            this.lblModel.Text = "Model:";
            this.lblModel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBox_Model
            // 
            this.textBox_Model.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Model.Location = new System.Drawing.Point(160, 27);
            this.textBox_Model.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_Model.Name = "textBox_Model";
            this.textBox_Model.Size = new System.Drawing.Size(277, 34);
            this.textBox_Model.TabIndex = 1;
            this.textBox_Model.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_Model_KeyDown);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(37, 89);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 32);
            this.label1.TabIndex = 0;
            this.label1.Text = "SKU:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_SKU
            // 
            this.textBox_SKU.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_SKU.Location = new System.Drawing.Point(160, 85);
            this.textBox_SKU.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_SKU.Name = "textBox_SKU";
            this.textBox_SKU.Size = new System.Drawing.Size(277, 34);
            this.textBox_SKU.TabIndex = 1;
            this.textBox_SKU.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_SKU_KeyDown);
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(37, 152);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(113, 32);
            this.label2.TabIndex = 0;
            this.label2.Text = "OS PN:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_OSPN
            // 
            this.textBox_OSPN.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_OSPN.Location = new System.Drawing.Point(160, 148);
            this.textBox_OSPN.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_OSPN.Name = "textBox_OSPN";
            this.textBox_OSPN.Size = new System.Drawing.Size(277, 34);
            this.textBox_OSPN.TabIndex = 1;
            this.textBox_OSPN.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_OSPN_KeyDown);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(37, 213);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 32);
            this.label3.TabIndex = 0;
            this.label3.Text = "OS Ver:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBox_OSVersion
            // 
            this.textBox_OSVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_OSVersion.Location = new System.Drawing.Point(160, 209);
            this.textBox_OSVersion.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_OSVersion.Name = "textBox_OSVersion";
            this.textBox_OSVersion.Size = new System.Drawing.Size(277, 34);
            this.textBox_OSVersion.TabIndex = 1;
            this.textBox_OSVersion.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_OSVersion_KeyDown);
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.Location = new System.Drawing.Point(160, 301);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(107, 60);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.Location = new System.Drawing.Point(332, 301);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(107, 60);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // frmMCF
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 371);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.textBox_OSVersion);
            this.Controls.Add(this.textBox_OSPN);
            this.Controls.Add(this.textBox_SKU);
            this.Controls.Add(this.textBox_Model);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblModel);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "frmMCF";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Scan Sheet";
            this.Load += new System.EventHandler(this.FrmMCF_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.TextBox textBox_Model;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_SKU;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_OSPN;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_OSVersion;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}