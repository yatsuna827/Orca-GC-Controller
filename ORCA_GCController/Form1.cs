﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GCController.SerialCommunicationWrapper;
using Sgry.Azuki;
using Sgry.Azuki.Highlighter;

namespace GCController
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            var back = Color.White; //Color.FromArgb(0x202020);
            scriptBox.ColorScheme.BackColor = back;
            //scriptBox.ColorScheme.SetColor(CharClass.Normal, Color.White, back);
            scriptBox.ColorScheme.SetColor(CharClass.Keyword, Color.Blue, back);
            scriptBox.ColorScheme.SetColor(CharClass.Number, Color.YellowGreen, back);
            scriptBox.ColorScheme.SetColor(CharClass.DocComment, Color.Green, back);

            var keywordHighlighter = new KeywordHighlighter();
            keywordHighlighter.AddKeywordSet(new string[]
            {
                "Hit", "Press", "Start", "Wait"
            }, CharClass.Keyword);
            keywordHighlighter.AddKeywordSet(new string[]
            {
                "A", "B", "dD", "dL", "dR", "dU","L", "R", "Sl", "St", "tl", "Z"
            }, CharClass.Value);
            keywordHighlighter.AddRegex("-[a-z]=", CharClass.Keyword2);
            keywordHighlighter.AddRegex("-[0-9]", CharClass.Number);
            keywordHighlighter.AddLineHighlight("#", CharClass.DocComment);

            scriptBox.Highlighter = keywordHighlighter;

            UpdatePorts();
        }

        private SerialPort port;
        private MacroScript macroScript;
        private CancellationTokenSource macroCancellationTokenSource;
        private CancellationTokenSource controllerCancellationTokenSource;

        private void UpdatePorts()
        {
            portsNameBox.Items.Clear();
            portsNameBox.Items.AddRange(SerialPort.GetPortNames());
            if (portsNameBox.Enabled = portsNameBox.Items.Count > 0)
                portsNameBox.SelectedIndex = 0;
            else
                portsNameBox.Items.Add("接続可能なポートが見つかりませんでした");
        }

        private void UpdatePortsButton_Click(object sender, EventArgs e)
        {
            UpdatePorts();
        }

        private bool connecting;
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (!portsNameBox.Enabled) return;

            if(connecting)
            {
                controllerCancellationTokenSource?.Cancel();
                macroCancellationTokenSource?.Cancel();
                if (Terminate())
                {
                    logBox.AppendText("切断しました\r\n");
                    connectButton.Text = "Connect";
                    connecting = false;
                    keyConStartButton.Enabled = keyConStopButton.Enabled = runButton.Enabled = false;
                }
            }
            else
            {
                if (Connect())
                {
                    logBox.AppendText("接続に成功しました\r\n");
                    connectButton.Text = "Terminate";
                    connecting = true;
                    keyConStartButton.Enabled = true;
                }
            }
        }

        private bool Connect()
        {
            port = new SerialPort(portsNameBox.Text, 4800);
            try
            {
                port.Open();
                return true;
            }
            catch (Exception ex)
            {
                port = null;
                logBox.AppendText(ex.Message + Environment.NewLine);
                return false;
            }
        }
        private bool Terminate()
        {
            try
            {
                macroCancellationTokenSource?.Cancel();
                port.Close();
                return true;
            }
            catch (Exception ex)
            {
                logBox.AppendText(ex.Message + Environment.NewLine);
                return false;
            }
        }

        private void KeyboardControllerStartButton_Click(object sender, EventArgs e)
        {
            StartKeyboardController();
        }
        private async Task StartKeyboardController()
        {
            if (port == null || !port.IsOpen)
            {
                logBox.AppendText("ポートが開放されていません\r\n");
                return;
            }

            controllerCancellationTokenSource = new CancellationTokenSource();
            keyConStartButton.Enabled = false;
            keyConStopButton.Enabled = true;
            logBox.AppendText("コントローラモードを開始しました\r\n");
            await InputKeyAsync(controllerCancellationTokenSource.Token);
            logBox.AppendText("コントローラモードを終了しました\r\n");
            keyConStartButton.Enabled = true;
            keyConStopButton.Enabled = false;
        }

        private void KeyboardControllerStopButton_Click(object sender, EventArgs e)
        {
            controllerCancellationTokenSource?.Cancel();
        }

        private Task InputKeyAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                var prev = ControllerInput.KeysAllUp;
                var pressedMKey = false;
                var nextFrame = (double)Environment.TickCount;
                var frameRate = 1000 / 60.0;
                while (!token.IsCancellationRequested)
                {
                    if (Environment.TickCount >= nextFrame)
                    {
                        nextFrame += frameRate;

                        if (!非アクティブキー入力有向ToolStripMenuItem.Checked && ActiveForm != this) continue;

                        var keys = ControllerInput.KeysAllUp;
                        if (NativeMethods.IsKeyPress(Keys.Z)) keys |= ControllerInput.A;
                        if (NativeMethods.IsKeyPress(Keys.X)) keys |= ControllerInput.B;
                        if (NativeMethods.IsKeyPress(Keys.C)) keys |= ControllerInput.Z;
                        if (NativeMethods.IsKeyPress(Keys.Left)) keys |= ControllerInput.Left;
                        if (NativeMethods.IsKeyPress(Keys.Right)) keys |= ControllerInput.Right;
                        if (NativeMethods.IsKeyPress(Keys.Up)) keys |= ControllerInput.Up;
                        if (NativeMethods.IsKeyPress(Keys.Down)) keys |= ControllerInput.Down;
                        if (NativeMethods.IsKeyPress(Keys.Space)) keys |= ControllerInput.Start;
                        if (NativeMethods.IsKeyPress(Keys.Enter)) keys |= ControllerInput.Y;

                        // バックスペースが入力されたらキーボード入力モードを終了する.
                        if (NativeMethods.IsKeyPress(Keys.Back)) return;

                        // Mキーが入力されたらマクロを起動する.
                        var pressing = NativeMethods.IsKeyPress(Keys.M);
                        if (!pressedMKey && pressing)
                        {
                            if (macroScript != null)
                            {
                                Invoke((MethodInvoker)(() => RunMacro()));
                                return;
                            }

                            Invoke((MethodInvoker)(() => logBox.AppendText("マクロが検証されていません\r\n")));
                        }
                        pressedMKey = pressing;

                        if (keys != prev)
                            port.SetButtonState(keys);
                        prev = keys;
                    }
                }
            }, token);
        }

        private void CompileButton_Click(object sender, EventArgs e)
        {
            try
            {
                macroScript = MacroScript.Compile(scriptBox.Text.Replace("\r\n", "\n").Replace("\n\n", "\n").Split('\n'));
                runButton.Enabled = true;
                logBox.AppendText("コンパイルに成功しました\r\n");
            }
            catch(Exception ex)
            {
                logBox.AppendText("Compile Error " + ex.Message + Environment.NewLine);
            }
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            RunMacro();
        }

        private async Task RunMacro()
        {
            if (port == null || !port.IsOpen)
            {
                logBox.AppendText("ポートが開放されていません\r\n");
                return;
            }
            if (macroScript == null)
            {
                logBox.AppendText("マクロが検証されていません\r\n");
                return;
            }
            controllerCancellationTokenSource?.Cancel();
            macroCancellationTokenSource = new CancellationTokenSource();
            loopCheckBox.Enabled = keyConStartButton.Enabled = keyConStopButton.Enabled = compileButton.Enabled = runButton.Enabled = false;
            cancelButton.Enabled = true;

            frameList = macroScript.HitFrames;
            logBox.AppendText("マクロを実行します\r\n");
            timer1.Enabled = true;

            if (loopCheckBox.Checked)
                await macroScript?.RunLoopAsync(port, macroCancellationTokenSource.Token);
            else
                await macroScript?.RunOnceAsync(port, macroCancellationTokenSource.Token);
            
            timer1.Enabled = false;
            var canceled = macroCancellationTokenSource.IsCancellationRequested;
            logBox.AppendText(canceled ? "マクロが中断されました\r\n" : "マクロが終了しました\r\n");
            loopCheckBox.Enabled = keyConStartButton.Enabled = compileButton.Enabled = runButton.Enabled = true;
            cancelButton.Enabled = false;

            if (!canceled && マクロ終了後にキーボードモードに移行ToolStripMenuItem.Checked)
            {
                this.Activate();
                ActiveControl = textBox1;
                StartKeyboardController();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            macroCancellationTokenSource?.Cancel();
        }

        private (int,int)[] frameList;
        private void Timer1_Tick(object sender, EventArgs e)
        {
            var index = macroScript.CurrentHitIndex;
            if (index < 0 || index >= frameList.Length)
            {
                textBox1.Text = "";
            }
            else
            {
                var label = frameList[index].Item1;
                textBox1.Text = $"{(frameList[index].Item2 - macroScript.CurrentFrame(label)) / 60} [sec]";
            }
        }

        private void マクロ終了後にキーボードモードに移行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            マクロ終了後にキーボードモードに移行ToolStripMenuItem.Checked ^= true;
        }

        private void 非アクティブキー入力有向ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            非アクティブキー入力有向ToolStripMenuItem.Checked ^= true;
        }
        private string currentFilePath = "";
        private bool SetScriptText(string txt)
        {
            macroScript = null;
            scriptBox.Text = txt;
            return true;
        }
        private void 新規作成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetScriptText("");
        }

        private void 開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog() 
            {
                Filter = "テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*",
                Title = "開くファイルを選択してください"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var txt = File.ReadAllText(ofd.FileName);
                    if (SetScriptText(txt))
                        currentFilePath = ofd.FileName;
                }
            }
        }

        private void 名前を付けて保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog()
            {
                Filter = "テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*",
                Title = "名前を付けて保存"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = sfd.FileName;
                    File.WriteAllText(sfd.FileName, scriptBox.Text);
                }
            }
        }

        private void 上書き保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists(currentFilePath)) 
                名前を付けて保存ToolStripMenuItem_Click(sender, e);
            else
                File.WriteAllText(currentFilePath, scriptBox.Text);
        }

        private void キーコンフィグToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("実装はもうちょっと待っててね");
        }
    }
}