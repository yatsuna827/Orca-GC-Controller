using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using ORCA.Core;
using Xunit;

namespace ORCA.Headless.Tests
{
    public class SessionTests
    {
        // マクロの完了通知が別スレッドから来るのでConcurrentQueueで受ける
        private readonly ConcurrentQueue<string> _output = new();
        private readonly RecordingPort _port = new();

        private Session NewSession() => new(_output.Enqueue, () => _port);

        private static string WriteMacro(params string[] lines)
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"macro_{Guid.NewGuid():N}.txt");
            File.WriteAllLines(path, lines);
            return path;
        }

        [Fact]
        public void 未接続の状態でのrunコマンドは拒否されること()
        {
            var session = NewSession();

            session.Execute("run");

            Assert.Contains("ポートが開放されていません", _output);
        }

        [Fact]
        public void マクロを読み込む前のrunコマンドは拒否されること()
        {
            var session = NewSession();

            session.Execute("connect COM_TEST");
            session.Execute("run");

            Assert.Contains("マクロが読み込まれていません", _output);
        }

        [Fact]
        public void マクロ実行中のrunコマンドは拒否されること()
        {
            var session = NewSession();

            session.Execute("connect COM_TEST");
            session.Execute($"load {WriteMacro("Press A -d=5000")}");
            session.Execute("run");
            session.Execute("run");

            Assert.Contains("すでに実行中です", _output);

            session.Execute("quit");
        }

        [Fact]
        public void マクロを実行中でないときのcancelコマンドは拒否されること()
        {
            var session = NewSession();

            session.Execute("cancel");

            Assert.Contains("実行中のマクロがありません", _output);
        }

        [Fact]
        public void cancelコマンドでマクロが止まったあと_全ボタンの解放が送信されること()
        {
            var session = NewSession();

            session.Execute("connect COM_TEST");
            session.Execute($"load {WriteMacro("Press A -d=5000")}");
            session.Execute("run");
            SpinWait.SpinUntil(() => _port.Entries.Length > 0, 2000);

            session.Execute("cancel");

            Assert.False(session.IsMacroRunning);
            Assert.Equal(["80 81 80", "80 80 80"], _port.HexLines);
        }

        [Fact]
        public void ポート開放に失敗したらエラーメッセージを出力すること()
        {
            var session = new Session(_output.Enqueue, () => new UnopenablePort());

            session.Execute("connect COM99");

            Assert.Contains(_output, _ => _.StartsWith("COM99 を開けませんでした: "));
            Assert.True(session.Running);
        }

        class UnopenablePort : IPort
        {
            public bool IsOpen => false;
            public void Open(string portName, bool rts, bool dtr)
                => throw new FileNotFoundException($"Could not find file '{portName}'.");
            public void Close() { }
            public void Write(byte[] buffer, int offset, int count) { }
        }

        [Fact]
        public void loadコマンドでマクロのコンパイルに失敗したらエラーメッセージが出力されること()
        {
            var session = NewSession();

            session.Execute($"load {WriteMacro("Press Q")}");

            Assert.Contains("エラー: [1行目] Pressコマンド 第1引数(ボタン指定)に不正な文字が含まれています", _output);
        }
        
        [Fact]
        public void loadコマンドでマクロのコンパイルに失敗してもセッション自体は停止しないこと()
        {
            var session = NewSession();

            session.Execute($"load {WriteMacro("Press Q")}");

            Assert.True(session.Running);
        }

        [Fact]
        public void loadコマンドでマクロファイルの読込に失敗したらエラーメッセージが出力されること()
        {
            var session = NewSession();

            session.Execute("load 存在しないファイル.txt");

            Assert.Contains(_output, _ => _.StartsWith("エラー: "));
            Assert.True(session.Running);
        }

        [Fact]
        public void 未知のコマンドは拒否されること()
        {
            var session = NewSession();

            session.Execute("foo");

            Assert.Contains("未知のコマンドです: foo", _output);
        }

        [Fact]
        public void statusコマンドで接続の有無が出力されること()
        {
            var session = NewSession();

            session.Execute("status");
            Assert.Contains("接続: なし", _output);

            session.Execute("connect COM_TEST");
            session.Execute("status");
            Assert.Contains("接続: あり", _output);
        }

        [Fact]
        public void quitコマンドでセッションが終了すること()
        {
            var session = NewSession();
            Assert.True(session.Running);

            session.Execute("quit");

            Assert.False(session.Running);
        }
    }
}
