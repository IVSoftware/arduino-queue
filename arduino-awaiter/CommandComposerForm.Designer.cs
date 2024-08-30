namespace arduino_queue
{
    partial class CommandComposerForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonQueueCommand = new Button();
            richTextBox = new RichTextBox();
            checkBoxHome = new CheckBox();
            textBoxX = new TextBox();
            textBoxY = new TextBox();
            buttonClearCommand = new Button();
            buttonClearLog = new Button();
            SuspendLayout();
            // 
            // buttonQueueCommand
            // 
            buttonQueueCommand.Enabled = false;
            buttonQueueCommand.Font = new Font("Segoe UI", 12F);
            buttonQueueCommand.Location = new Point(282, 31);
            buttonQueueCommand.Name = "buttonQueueCommand";
            buttonQueueCommand.Size = new Size(260, 50);
            buttonQueueCommand.TabIndex = 3;
            buttonQueueCommand.Text = "Queue Command";
            buttonQueueCommand.UseVisualStyleBackColor = true;
            // 
            // richTextBox
            // 
            richTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox.Location = new Point(12, 102);
            richTextBox.Name = "richTextBox";
            richTextBox.Size = new Size(754, 730);
            richTextBox.TabIndex = 4;
            richTextBox.Text = "";
            // 
            // checkBoxHome
            // 
            checkBoxHome.AutoSize = true;
            checkBoxHome.Font = new Font("Segoe UI", 12F);
            checkBoxHome.Location = new Point(20, 39);
            checkBoxHome.Name = "checkBoxHome";
            checkBoxHome.Size = new Size(105, 36);
            checkBoxHome.TabIndex = 0;
            checkBoxHome.Text = "Home";
            checkBoxHome.UseVisualStyleBackColor = true;
            // 
            // textBoxX
            // 
            textBoxX.Font = new Font("Segoe UI", 12F);
            textBoxX.Location = new Point(141, 37);
            textBoxX.Name = "textBoxX";
            textBoxX.PlaceholderText = "X";
            textBoxX.Size = new Size(56, 39);
            textBoxX.TabIndex = 1;
            textBoxX.TextAlign = HorizontalAlignment.Center;
            // 
            // textBoxY
            // 
            textBoxY.Font = new Font("Segoe UI", 12F);
            textBoxY.Location = new Point(203, 37);
            textBoxY.Name = "textBoxY";
            textBoxY.PlaceholderText = "Y";
            textBoxY.Size = new Size(56, 39);
            textBoxY.TabIndex = 2;
            textBoxY.TextAlign = HorizontalAlignment.Center;
            // 
            // buttonClearCommand
            // 
            buttonClearCommand.BackColor = Color.Firebrick;
            buttonClearCommand.Enabled = false;
            buttonClearCommand.Font = new Font("Segoe UI", 8F);
            buttonClearCommand.ForeColor = Color.White;
            buttonClearCommand.Location = new Point(561, 31);
            buttonClearCommand.Name = "buttonClearCommand";
            buttonClearCommand.Size = new Size(54, 50);
            buttonClearCommand.TabIndex = 5;
            buttonClearCommand.Text = "X";
            buttonClearCommand.UseVisualStyleBackColor = false;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClearLog.BackColor = Color.Firebrick;
            buttonClearLog.Font = new Font("Segoe UI", 8F);
            buttonClearLog.ForeColor = Color.White;
            buttonClearLog.Location = new Point(712, 113);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(31, 30);
            buttonClearLog.TabIndex = 5;
            buttonClearLog.Text = "X";
            buttonClearLog.UseVisualStyleBackColor = false;
            // 
            // CommandComposerForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(778, 844);
            Controls.Add(buttonClearLog);
            Controls.Add(buttonClearCommand);
            Controls.Add(textBoxY);
            Controls.Add(textBoxX);
            Controls.Add(checkBoxHome);
            Controls.Add(richTextBox);
            Controls.Add(buttonQueueCommand);
            Name = "CommandComposerForm";
            Text = "Command Composer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonQueueCommand;
        private RichTextBox richTextBox;
        private CheckBox checkBoxHome;
        private TextBox textBoxX;
        private TextBox textBoxY;
        private Button buttonClearCommand;
        private Button buttonClearLog;
    }
}
