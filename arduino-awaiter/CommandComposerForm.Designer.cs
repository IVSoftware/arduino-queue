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
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            loadToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            memoryToolStripMenuItem = new ToolStripMenuItem();
            clearToolStripMenuItem = new ToolStripMenuItem();
            runToolStripMenuItem = new ToolStripMenuItem();
            tableLayoutPanel1 = new TableLayoutPanel();
            buttonSaveCommand = new Button();
            listToolStripMenuItem = new ToolStripMenuItem();
            menuStrip.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // buttonQueueCommand
            // 
            buttonQueueCommand.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            buttonQueueCommand.Enabled = false;
            buttonQueueCommand.Font = new Font("Segoe UI", 11F);
            buttonQueueCommand.Location = new Point(287, 13);
            buttonQueueCommand.Margin = new Padding(10, 0, 10, 0);
            buttonQueueCommand.Name = "buttonQueueCommand";
            buttonQueueCommand.Size = new Size(240, 41);
            buttonQueueCommand.TabIndex = 3;
            buttonQueueCommand.Text = "Queue Command";
            buttonQueueCommand.UseVisualStyleBackColor = true;
            // 
            // richTextBox
            // 
            richTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox.Location = new Point(12, 135);
            richTextBox.Name = "richTextBox";
            richTextBox.ReadOnly = true;
            richTextBox.Size = new Size(854, 697);
            richTextBox.TabIndex = 4;
            richTextBox.Text = "";
            // 
            // checkBoxHome
            // 
            checkBoxHome.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            checkBoxHome.AutoSize = true;
            checkBoxHome.Font = new Font("Segoe UI", 12F);
            checkBoxHome.Location = new Point(10, 5);
            checkBoxHome.Margin = new Padding(10, 0, 10, 0);
            checkBoxHome.Name = "checkBoxHome";
            checkBoxHome.Size = new Size(105, 57);
            checkBoxHome.TabIndex = 0;
            checkBoxHome.Text = "Home";
            checkBoxHome.UseVisualStyleBackColor = true;
            // 
            // textBoxX
            // 
            textBoxX.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxX.Font = new Font("Segoe UI", 12F);
            textBoxX.Location = new Point(135, 14);
            textBoxX.Margin = new Padding(10, 0, 10, 0);
            textBoxX.Name = "textBoxX";
            textBoxX.PlaceholderText = "X";
            textBoxX.Size = new Size(56, 39);
            textBoxX.TabIndex = 1;
            textBoxX.TextAlign = HorizontalAlignment.Center;
            // 
            // textBoxY
            // 
            textBoxY.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxY.Font = new Font("Segoe UI", 12F);
            textBoxY.Location = new Point(211, 14);
            textBoxY.Margin = new Padding(10, 0, 10, 0);
            textBoxY.Name = "textBoxY";
            textBoxY.PlaceholderText = "Y";
            textBoxY.Size = new Size(56, 39);
            textBoxY.TabIndex = 2;
            textBoxY.TextAlign = HorizontalAlignment.Center;
            // 
            // buttonClearCommand
            // 
            buttonClearCommand.Anchor = AnchorStyles.None;
            buttonClearCommand.BackColor = Color.Firebrick;
            buttonClearCommand.Font = new Font("Segoe UI", 8F);
            buttonClearCommand.ForeColor = Color.White;
            buttonClearCommand.Location = new Point(800, 18);
            buttonClearCommand.Name = "buttonClearCommand";
            buttonClearCommand.Size = new Size(31, 30);
            buttonClearCommand.TabIndex = 5;
            buttonClearCommand.Text = "X";
            buttonClearCommand.UseVisualStyleBackColor = false;
            buttonClearCommand.Visible = false;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClearLog.BackColor = Color.Firebrick;
            buttonClearLog.Font = new Font("Segoe UI", 8F);
            buttonClearLog.ForeColor = Color.White;
            buttonClearLog.Location = new Point(812, 149);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(31, 30);
            buttonClearLog.TabIndex = 5;
            buttonClearLog.Text = "X";
            buttonClearLog.UseVisualStyleBackColor = false;
            buttonClearLog.Visible = false;
            // 
            // menuStrip
            // 
            menuStrip.BackColor = SystemColors.ControlLightLight;
            menuStrip.Font = new Font("Segoe UI", 12F);
            menuStrip.ImageScalingSize = new Size(24, 24);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, memoryToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(6, 2, 0, 5);
            menuStrip.Size = new Size(878, 43);
            menuStrip.TabIndex = 6;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { loadToolStripMenuItem, saveToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(67, 36);
            fileToolStripMenuItem.Text = "File";
            // 
            // loadToolStripMenuItem
            // 
            loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            loadToolStripMenuItem.Size = new Size(169, 40);
            loadToolStripMenuItem.Text = "Load";
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(169, 40);
            saveToolStripMenuItem.Text = "Save";
            // 
            // memoryToolStripMenuItem
            // 
            memoryToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { clearToolStripMenuItem, runToolStripMenuItem, listToolStripMenuItem });
            memoryToolStripMenuItem.Name = "memoryToolStripMenuItem";
            memoryToolStripMenuItem.Size = new Size(120, 36);
            memoryToolStripMenuItem.Text = "Memory";
            // 
            // clearToolStripMenuItem
            // 
            clearToolStripMenuItem.Enabled = false;
            clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            clearToolStripMenuItem.Size = new Size(270, 40);
            clearToolStripMenuItem.Text = "Clear";
            // 
            // runToolStripMenuItem
            // 
            runToolStripMenuItem.Enabled = false;
            runToolStripMenuItem.Name = "runToolStripMenuItem";
            runToolStripMenuItem.Size = new Size(270, 40);
            runToolStripMenuItem.Text = "Run";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 9;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(checkBoxHome, 1, 0);
            tableLayoutPanel1.Controls.Add(textBoxX, 2, 0);
            tableLayoutPanel1.Controls.Add(textBoxY, 3, 0);
            tableLayoutPanel1.Controls.Add(buttonQueueCommand, 4, 0);
            tableLayoutPanel1.Controls.Add(buttonSaveCommand, 5, 0);
            tableLayoutPanel1.Controls.Add(buttonClearCommand, 6, 0);
            tableLayoutPanel1.Location = new Point(12, 55);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(0, 5, 0, 0);
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(866, 62);
            tableLayoutPanel1.TabIndex = 7;
            // 
            // buttonSaveCommand
            // 
            buttonSaveCommand.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            buttonSaveCommand.Enabled = false;
            buttonSaveCommand.Font = new Font("Segoe UI", 11F);
            buttonSaveCommand.Location = new Point(547, 13);
            buttonSaveCommand.Margin = new Padding(10, 0, 10, 0);
            buttonSaveCommand.Name = "buttonSaveCommand";
            buttonSaveCommand.Size = new Size(240, 41);
            buttonSaveCommand.TabIndex = 3;
            buttonSaveCommand.Text = "Save Command";
            buttonSaveCommand.UseVisualStyleBackColor = true;
            // 
            // listToolStripMenuItem
            // 
            listToolStripMenuItem.Enabled = false;
            listToolStripMenuItem.Name = "listToolStripMenuItem";
            listToolStripMenuItem.Size = new Size(270, 40);
            listToolStripMenuItem.Text = "List";
            // 
            // CommandComposerForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(878, 844);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(buttonClearLog);
            Controls.Add(richTextBox);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Name = "CommandComposerForm";
            Text = "Command Composer";
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
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
        private MenuStrip menuStrip;
        private TableLayoutPanel tableLayoutPanel1;
        private Button buttonSaveCommand;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem loadToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem memoryToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        private ToolStripMenuItem runToolStripMenuItem;
        private ToolStripMenuItem listToolStripMenuItem;
    }
}
