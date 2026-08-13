
namespace GCController
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            Sgry.Azuki.FontInfo fontInfo1 = new Sgry.Azuki.FontInfo();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.portsNameBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.updatePortsButton = new System.Windows.Forms.Button();
            this.connectButton = new System.Windows.Forms.Button();
            this.logBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.keyConStartButton = new System.Windows.Forms.Button();
            this.compileButton = new System.Windows.Forms.Button();
            this.runButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.keyConStopButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.新規作成ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.開くToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.名前を付けて保存ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.上書き保存ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.キーコンフィグToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.非アクティブキー入力有向ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loopCheckBox = new System.Windows.Forms.CheckBox();
            this.scriptBox = new Sgry.Azuki.WinForms.AzukiControl();
            this.loopBox = new System.Windows.Forms.NumericUpDown();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.シリアル通信設定ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rTS有効化ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dTR有効化ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loopBox)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // portsNameBox
            // 
            this.portsNameBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.portsNameBox.FormattingEnabled = true;
            this.portsNameBox.Location = new System.Drawing.Point(50, 27);
            this.portsNameBox.Name = "portsNameBox";
            this.portsNameBox.Size = new System.Drawing.Size(121, 20);
            this.portsNameBox.TabIndex = 0;
            this.portsNameBox.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "Ports";
            // 
            // updatePortsButton
            // 
            this.updatePortsButton.Location = new System.Drawing.Point(258, 25);
            this.updatePortsButton.Name = "updatePortsButton";
            this.updatePortsButton.Size = new System.Drawing.Size(75, 23);
            this.updatePortsButton.TabIndex = 2;
            this.updatePortsButton.TabStop = false;
            this.updatePortsButton.Text = "Update";
            this.updatePortsButton.UseVisualStyleBackColor = true;
            this.updatePortsButton.Click += new System.EventHandler(this.UpdatePortsButton_Click);
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(177, 25);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(75, 23);
            this.connectButton.TabIndex = 3;
            this.connectButton.TabStop = false;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.ConnectButton_Click);
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.Location = new System.Drawing.Point(50, 83);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logBox.Size = new System.Drawing.Size(283, 78);
            this.logBox.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 88);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(23, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "Log";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 59);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 12);
            this.label3.TabIndex = 6;
            this.label3.Text = "KeyboardController";
            // 
            // keyConStartButton
            // 
            this.keyConStartButton.Enabled = false;
            this.keyConStartButton.Location = new System.Drawing.Point(177, 54);
            this.keyConStartButton.Name = "keyConStartButton";
            this.keyConStartButton.Size = new System.Drawing.Size(75, 23);
            this.keyConStartButton.TabIndex = 8;
            this.keyConStartButton.TabStop = false;
            this.keyConStartButton.Text = "Start";
            this.keyConStartButton.UseVisualStyleBackColor = true;
            this.keyConStartButton.Click += new System.EventHandler(this.KeyboardControllerStartButton_Click);
            // 
            // compileButton
            // 
            this.compileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.compileButton.Location = new System.Drawing.Point(177, 356);
            this.compileButton.Name = "compileButton";
            this.compileButton.Size = new System.Drawing.Size(75, 23);
            this.compileButton.TabIndex = 10;
            this.compileButton.TabStop = false;
            this.compileButton.Text = "Compile";
            this.compileButton.UseVisualStyleBackColor = true;
            this.compileButton.Click += new System.EventHandler(this.CompileButton_Click);
            // 
            // runButton
            // 
            this.runButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.runButton.Enabled = false;
            this.runButton.Location = new System.Drawing.Point(258, 356);
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(75, 23);
            this.runButton.TabIndex = 11;
            this.runButton.TabStop = false;
            this.runButton.Text = "Run";
            this.runButton.UseVisualStyleBackColor = true;
            this.runButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 172);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 12);
            this.label4.TabIndex = 12;
            this.label4.Text = "Script";
            // 
            // keyConStopButton
            // 
            this.keyConStopButton.Enabled = false;
            this.keyConStopButton.Location = new System.Drawing.Point(258, 54);
            this.keyConStopButton.Name = "keyConStopButton";
            this.keyConStopButton.Size = new System.Drawing.Size(75, 23);
            this.keyConStopButton.TabIndex = 13;
            this.keyConStopButton.TabStop = false;
            this.keyConStopButton.Text = "Stop";
            this.keyConStopButton.UseVisualStyleBackColor = true;
            this.keyConStopButton.Click += new System.EventHandler(this.KeyboardControllerStopButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Enabled = false;
            this.cancelButton.Location = new System.Drawing.Point(258, 385);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 14;
            this.cancelButton.TabStop = false;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.settingToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(350, 24);
            this.menuStrip1.TabIndex = 17;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.新規作成ToolStripMenuItem,
            this.開くToolStripMenuItem,
            this.名前を付けて保存ToolStripMenuItem,
            this.上書き保存ToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // 新規作成ToolStripMenuItem
            // 
            this.新規作成ToolStripMenuItem.Name = "新規作成ToolStripMenuItem";
            this.新規作成ToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.新規作成ToolStripMenuItem.Text = "新規作成";
            this.新規作成ToolStripMenuItem.Click += new System.EventHandler(this.新規作成ToolStripMenuItem_Click);
            // 
            // 開くToolStripMenuItem
            // 
            this.開くToolStripMenuItem.Name = "開くToolStripMenuItem";
            this.開くToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.開くToolStripMenuItem.Text = "開く";
            this.開くToolStripMenuItem.Click += new System.EventHandler(this.開くToolStripMenuItem_Click);
            // 
            // 名前を付けて保存ToolStripMenuItem
            // 
            this.名前を付けて保存ToolStripMenuItem.Name = "名前を付けて保存ToolStripMenuItem";
            this.名前を付けて保存ToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.名前を付けて保存ToolStripMenuItem.Text = "名前を付けて保存";
            this.名前を付けて保存ToolStripMenuItem.Click += new System.EventHandler(this.名前を付けて保存ToolStripMenuItem_Click);
            // 
            // 上書き保存ToolStripMenuItem
            // 
            this.上書き保存ToolStripMenuItem.Name = "上書き保存ToolStripMenuItem";
            this.上書き保存ToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.上書き保存ToolStripMenuItem.Text = "上書き保存";
            this.上書き保存ToolStripMenuItem.Click += new System.EventHandler(this.上書き保存ToolStripMenuItem_Click);
            // 
            // settingToolStripMenuItem
            // 
            this.settingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.キーコンフィグToolStripMenuItem,
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem,
            this.非アクティブキー入力有向ToolStripMenuItem,
            this.シリアル通信設定ToolStripMenuItem});
            this.settingToolStripMenuItem.Name = "settingToolStripMenuItem";
            this.settingToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.settingToolStripMenuItem.Text = "Setting";
            // 
            // キーコンフィグToolStripMenuItem
            // 
            this.キーコンフィグToolStripMenuItem.Name = "キーコンフィグToolStripMenuItem";
            this.キーコンフィグToolStripMenuItem.Size = new System.Drawing.Size(249, 22);
            this.キーコンフィグToolStripMenuItem.Text = "キーコンフィグ";
            this.キーコンフィグToolStripMenuItem.Click += new System.EventHandler(this.キーコンフィグToolStripMenuItem_Click);
            // 
            // マクロ終了後にキーボードモードに移行ToolStripMenuItem
            // 
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem.Checked = true;
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem.Name = "マクロ終了後にキーボードモードに移行ToolStripMenuItem";
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem.Size = new System.Drawing.Size(249, 22);
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem.Text = "マクロ終了後にキーボードモードに移行";
            this.マクロ終了後にキーボードモードに移行ToolStripMenuItem.Click += new System.EventHandler(this.マクロ終了後にキーボードモードに移行ToolStripMenuItem_Click);
            // 
            // 非アクティブキー入力有向ToolStripMenuItem
            // 
            this.非アクティブキー入力有向ToolStripMenuItem.Checked = true;
            this.非アクティブキー入力有向ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.非アクティブキー入力有向ToolStripMenuItem.Name = "非アクティブキー入力有向ToolStripMenuItem";
            this.非アクティブキー入力有向ToolStripMenuItem.Size = new System.Drawing.Size(249, 22);
            this.非アクティブキー入力有向ToolStripMenuItem.Text = "非アクティブでもキー入力を受け付ける";
            this.非アクティブキー入力有向ToolStripMenuItem.Click += new System.EventHandler(this.非アクティブキー入力有向ToolStripMenuItem_Click);
            // 
            // loopCheckBox
            // 
            this.loopCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.loopCheckBox.AutoSize = true;
            this.loopCheckBox.Location = new System.Drawing.Point(50, 360);
            this.loopCheckBox.Name = "loopCheckBox";
            this.loopCheckBox.Size = new System.Drawing.Size(45, 16);
            this.loopCheckBox.TabIndex = 18;
            this.loopCheckBox.TabStop = false;
            this.loopCheckBox.Text = "loop";
            this.loopCheckBox.UseVisualStyleBackColor = true;
            // 
            // scriptBox
            // 
            this.scriptBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(250)))), ((int)(((byte)(240)))));
            this.scriptBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.scriptBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.scriptBox.DrawingOption = ((Sgry.Azuki.DrawingOption)(((((Sgry.Azuki.DrawingOption.DrawsFullWidthSpace | Sgry.Azuki.DrawingOption.DrawsTab) 
            | Sgry.Azuki.DrawingOption.HighlightCurrentLine) 
            | Sgry.Azuki.DrawingOption.ShowsLineNumber) 
            | Sgry.Azuki.DrawingOption.HighlightsMatchedBracket)));
            this.scriptBox.DrawsEolCode = false;
            this.scriptBox.FirstVisibleLine = 0;
            this.scriptBox.Font = new System.Drawing.Font("Consolas", 9F);
            fontInfo1.Name = "Consolas";
            fontInfo1.Size = 9;
            fontInfo1.Style = System.Drawing.FontStyle.Regular;
            this.scriptBox.FontInfo = fontInfo1;
            this.scriptBox.ForeColor = System.Drawing.Color.Black;
            this.scriptBox.Location = new System.Drawing.Point(50, 167);
            this.scriptBox.Name = "scriptBox";
            this.scriptBox.ScrollPos = new System.Drawing.Point(0, 0);
            this.scriptBox.ShowsDirtBar = false;
            this.scriptBox.Size = new System.Drawing.Size(283, 183);
            this.scriptBox.TabIndex = 19;
            this.scriptBox.TabStop = false;
            this.scriptBox.Text = "# ここにマクロを記述します";
            this.scriptBox.ViewWidth = 4133;
            // 
            // loopBox
            // 
            this.loopBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.loopBox.Location = new System.Drawing.Point(101, 360);
            this.loopBox.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.loopBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.loopBox.Name = "loopBox";
            this.loopBox.Size = new System.Drawing.Size(49, 19);
            this.loopBox.TabIndex = 20;
            this.loopBox.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 411);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(350, 22);
            this.statusStrip1.TabIndex = 21;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBox1.Location = new System.Drawing.Point(50, 385);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(100, 19);
            this.textBox1.TabIndex = 22;
            this.textBox1.Text = "=・ω・=";
            // 
            // シリアル通信設定ToolStripMenuItem
            // 
            this.シリアル通信設定ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rTS有効化ToolStripMenuItem,
            this.dTR有効化ToolStripMenuItem});
            this.シリアル通信設定ToolStripMenuItem.Name = "シリアル通信設定ToolStripMenuItem";
            this.シリアル通信設定ToolStripMenuItem.Size = new System.Drawing.Size(249, 22);
            this.シリアル通信設定ToolStripMenuItem.Text = "シリアル通信設定";
            // 
            // rTS有効化ToolStripMenuItem
            // 
            this.rTS有効化ToolStripMenuItem.Checked = true;
            this.rTS有効化ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.rTS有効化ToolStripMenuItem.Name = "rTS有効化ToolStripMenuItem";
            this.rTS有効化ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.rTS有効化ToolStripMenuItem.Text = "RTS有効化";
            this.rTS有効化ToolStripMenuItem.Click += new System.EventHandler(this.rTS有効化ToolStripMenuItem_Click);
            // 
            // dTR有効化ToolStripMenuItem
            // 
            this.dTR有効化ToolStripMenuItem.Checked = true;
            this.dTR有効化ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.dTR有効化ToolStripMenuItem.Name = "dTR有効化ToolStripMenuItem";
            this.dTR有効化ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.dTR有効化ToolStripMenuItem.Text = "DTR有効化";
            this.dTR有効化ToolStripMenuItem.Click += new System.EventHandler(this.dTR有効化ToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 433);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.loopBox);
            this.Controls.Add(this.scriptBox);
            this.Controls.Add(this.loopCheckBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.keyConStopButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.runButton);
            this.Controls.Add(this.compileButton);
            this.Controls.Add(this.keyConStartButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.connectButton);
            this.Controls.Add(this.updatePortsButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.portsNameBox);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(366, 445);
            this.Name = "Form1";
            this.Text = "Orca GC Controller";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loopBox)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox portsNameBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button updatePortsButton;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button keyConStartButton;
        private System.Windows.Forms.Button compileButton;
        private System.Windows.Forms.Button runButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button keyConStopButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem マクロ終了後にキーボードモードに移行ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 新規作成ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 開くToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 名前を付けて保存ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 上書き保存ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 非アクティブキー入力有向ToolStripMenuItem;
        private System.Windows.Forms.CheckBox loopCheckBox;
        private System.Windows.Forms.ToolStripMenuItem キーコンフィグToolStripMenuItem;
        private Sgry.Azuki.WinForms.AzukiControl scriptBox;
        private System.Windows.Forms.NumericUpDown loopBox;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ToolStripMenuItem シリアル通信設定ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rTS有効化ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dTR有効化ToolStripMenuItem;
    }
}

