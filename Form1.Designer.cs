
namespace agdiag
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.tboxVersion = new System.Windows.Forms.TextBox();
            this.textStatus = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.replicatimeout = new System.Windows.Forms.CheckBox();
            this.agfailover = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Dubai", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(7, 786);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(291, 44);
            this.button1.TabIndex = 2;
            this.button1.Text = "Select Log Folder";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Dubai", 15.75F);
            this.button2.Location = new System.Drawing.Point(304, 786);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(290, 44);
            this.button2.TabIndex = 4;
            this.button2.Text = "Exit";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.Control;
            this.textBox2.Location = new System.Drawing.Point(6, 455);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(587, 224);
            this.textBox2.TabIndex = 5;
            this.textBox2.Text = resources.GetString("textBox2.Text");
            this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // tboxVersion
            // 
            this.tboxVersion.BackColor = System.Drawing.SystemColors.Control;
            this.tboxVersion.Font = new System.Drawing.Font("Dubai", 12F);
            this.tboxVersion.Location = new System.Drawing.Point(164, 372);
            this.tboxVersion.Name = "tboxVersion";
            this.tboxVersion.Size = new System.Drawing.Size(256, 35);
            this.tboxVersion.TabIndex = 6;
            this.tboxVersion.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tboxVersion.TextChanged += new System.EventHandler(this.textBox1_TextChanged_1);
            // 
            // textStatus
            // 
            this.textStatus.BackColor = System.Drawing.SystemColors.Control;
            this.textStatus.Font = new System.Drawing.Font("Dubai", 12F);
            this.textStatus.Location = new System.Drawing.Point(7, 413);
            this.textStatus.Name = "textStatus";
            this.textStatus.Size = new System.Drawing.Size(586, 35);
            this.textStatus.TabIndex = 7;
            this.textStatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textStatus.TextChanged += new System.EventHandler(this.textBox1_TextChanged_2);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::agdiag.Properties.Resources.agdiagsplash;
            this.pictureBox1.Location = new System.Drawing.Point(7, 6);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(587, 360);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Control;
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F);
            this.textBox1.Location = new System.Drawing.Point(5, 684);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(589, 43);
            this.textBox1.TabIndex = 9;
            this.textBox1.Text = resources.GetString("textBox1.Text");
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged_3);
            // 
            // replicatimeout
            // 
            this.replicatimeout.AutoSize = true;
            this.replicatimeout.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.replicatimeout.Location = new System.Drawing.Point(290, 756);
            this.replicatimeout.Name = "replicatimeout";
            this.replicatimeout.Size = new System.Drawing.Size(237, 24);
            this.replicatimeout.TabIndex = 15;
            this.replicatimeout.Text = "Replica Connection TImeouts";
            this.replicatimeout.UseVisualStyleBackColor = true;
            // 
            // agfailover
            // 
            this.agfailover.AutoSize = true;
            this.agfailover.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.agfailover.Location = new System.Drawing.Point(65, 756);
            this.agfailover.Name = "agfailover";
            this.agfailover.Size = new System.Drawing.Size(208, 24);
            this.agfailover.TabIndex = 14;
            this.agfailover.Text = "Availability Group Failover";
            this.agfailover.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(83, 731);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 20);
            this.label1.TabIndex = 13;
            this.label1.Text = "Choose Analysis";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 835);
            this.Controls.Add(this.replicatimeout);
            this.Controls.Add(this.agfailover);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.textStatus);
            this.Controls.Add(this.tboxVersion);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AGDIAG ";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox tboxVersion;
        private System.Windows.Forms.TextBox textStatus;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.CheckBox replicatimeout;
        private System.Windows.Forms.CheckBox agfailover;
        private System.Windows.Forms.Label label1;
    }
}

