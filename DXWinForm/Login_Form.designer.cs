namespace DXWinForm
{
    partial class Login_Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login_Form));
            this.Login_labelControl = new DevExpress.XtraEditors.LabelControl();
            this.PassWord_labelControl = new DevExpress.XtraEditors.LabelControl();
            this.Login_simpleButton = new DevExpress.XtraEditors.SimpleButton();
            this.Exit_simpleButton = new DevExpress.XtraEditors.SimpleButton();
            this.Input_User = new DevExpress.XtraEditors.TextEdit();
            this.Input_password = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.Input_User.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Input_password.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // Login_labelControl
            // 
            this.Login_labelControl.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Login_labelControl.Appearance.Options.UseFont = true;
            this.Login_labelControl.Location = new System.Drawing.Point(257, 99);
            this.Login_labelControl.Name = "Login_labelControl";
            this.Login_labelControl.Size = new System.Drawing.Size(84, 25);
            this.Login_labelControl.TabIndex = 0;
            this.Login_labelControl.Text = "用户名：";
            // 
            // PassWord_labelControl
            // 
            this.PassWord_labelControl.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PassWord_labelControl.Appearance.Options.UseFont = true;
            this.PassWord_labelControl.Location = new System.Drawing.Point(257, 149);
            this.PassWord_labelControl.Name = "PassWord_labelControl";
            this.PassWord_labelControl.Size = new System.Drawing.Size(84, 25);
            this.PassWord_labelControl.TabIndex = 1;
            this.PassWord_labelControl.Text = "密   码：";
            // 
            // Login_simpleButton
            // 
            this.Login_simpleButton.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Login_simpleButton.Appearance.Options.UseFont = true;
            this.Login_simpleButton.Location = new System.Drawing.Point(257, 217);
            this.Login_simpleButton.LookAndFeel.SkinName = "Blue";
            this.Login_simpleButton.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Login_simpleButton.Name = "Login_simpleButton";
            this.Login_simpleButton.Size = new System.Drawing.Size(84, 32);
            this.Login_simpleButton.TabIndex = 2;
            this.Login_simpleButton.Text = "登录";
            this.Login_simpleButton.Click += new System.EventHandler(this.Login_simpleButton_Click);
            // 
            // Exit_simpleButton
            // 
            this.Exit_simpleButton.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Exit_simpleButton.Appearance.Options.UseFont = true;
            this.Exit_simpleButton.Location = new System.Drawing.Point(390, 217);
            this.Exit_simpleButton.LookAndFeel.SkinName = "Blue";
            this.Exit_simpleButton.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Exit_simpleButton.Name = "Exit_simpleButton";
            this.Exit_simpleButton.Size = new System.Drawing.Size(84, 32);
            this.Exit_simpleButton.TabIndex = 3;
            this.Exit_simpleButton.Text = "退出";
            this.Exit_simpleButton.Click += new System.EventHandler(this.Exit_simpleButton_Click);
            // 
            // Input_User
            // 
            this.Input_User.Location = new System.Drawing.Point(338, 99);
            this.Input_User.Name = "Input_User";
            this.Input_User.Properties.AutoHeight = false;
            this.Input_User.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Input_User.Size = new System.Drawing.Size(136, 27);
            this.Input_User.TabIndex = 0;
            // 
            // Input_password
            // 
            this.Input_password.Location = new System.Drawing.Point(338, 149);
            this.Input_password.Name = "Input_password";
            this.Input_password.Properties.AutoHeight = false;
            this.Input_password.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Input_password.Properties.PasswordChar = '*';
            this.Input_password.Size = new System.Drawing.Size(136, 27);
            this.Input_password.TabIndex = 1;
            // 
            // Login_Form
            // 
            this.AcceptButton = this.Login_simpleButton;
            this.Appearance.BackColor = System.Drawing.SystemColors.Window;
            this.Appearance.Options.UseBackColor = true;
            this.Appearance.Options.UseFont = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayoutStore = System.Windows.Forms.ImageLayout.Stretch;
            this.BackgroundImageStore = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImageStore")));
            this.ClientSize = new System.Drawing.Size(551, 348);
            this.Controls.Add(this.Input_password);
            this.Controls.Add(this.Input_User);
            this.Controls.Add(this.Exit_simpleButton);
            this.Controls.Add(this.Login_simpleButton);
            this.Controls.Add(this.PassWord_labelControl);
            this.Controls.Add(this.Login_labelControl);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IconOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("Login_Form.IconOptions.LargeImage")));
            this.InactiveGlowColor = System.Drawing.SystemColors.ActiveCaption;
            this.LookAndFeel.SkinName = "London Liquid Sky";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Login_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "登录";
            ((System.ComponentModel.ISupportInitialize)(this.Input_User.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Input_password.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl Login_labelControl;
        private DevExpress.XtraEditors.LabelControl PassWord_labelControl;
        private DevExpress.XtraEditors.SimpleButton Login_simpleButton;
        private DevExpress.XtraEditors.SimpleButton Exit_simpleButton;
        private DevExpress.XtraEditors.TextEdit Input_User;
        private DevExpress.XtraEditors.TextEdit Input_password;
    }
}

