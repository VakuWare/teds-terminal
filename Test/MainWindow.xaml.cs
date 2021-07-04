using MahApps.Metro.Controls;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using TTerm.Terminal;
using TTermLibCmd;
using Timer = System.Timers.Timer;

namespace Test
{
    public partial class MainWindow : MetroWindow
    {
        private TerminalBuffer Buffer { get; set; }
        private ExecutionProfile Profile { get; set; }
        private TerminalSize Size { get; } = new TerminalSize(80, 24);

        public MainWindow()
        {
            InitializeComponent();

            Profile = ExecutionProfile.CreateDefaultShell().ExpandVariables();
            Buffer = new TerminalBuffer(Size);

            terminalControl.AutoSizeTerminal = true;

            //CreateSshSession();
            CreateWinSession();
        }

        private void CreateTcpSession()
        {
            TcpClient client = new TcpClient();
            client.Connect(new IPEndPoint(
                new IPAddress(new byte[] { 192, 168, 178, 84 }), 22));
            Stream stream = client.GetStream();
            terminalControl.Session = new TerminalSession(Buffer, stream, stream);
            terminalControl.Session.ImplicitCrForLf = false;
            terminalControl.Session.Connect();
        }

        private void CreateSshSession()
        {
            AuthenticationMethod[] methods = new AuthenticationMethod[]
            {
                new PasswordAuthenticationMethod("root", "pwd")
            };
            var info = new ConnectionInfo("192.168.178.84", "root", methods);
            SshClient client = new SshClient(info);
            client.Connect();
            var terminalMode = new Dictionary<TerminalModes, uint>();
            terminalMode.Add(TerminalModes.VEOL, 1);
            var stream = client.CreateShellStream("vt102", 80, 32, 0, 0, 16, terminalMode);

            terminalControl.Session = new TerminalSession(Buffer, stream, stream);
            terminalControl.Session.ImplicitCrForLf = true;
            terminalControl.Session.Connect();
        }

        private void OnSessionOnFinished(object sender, EventArgs args)
        {
            if (terminalControl.Session != null)
            {
                terminalControl.Session.Finished -= OnSessionOnFinished;
                terminalControl.Session.Dispose();
            }
            CreateWinSession();
        }

        private void CreateWinSession()
        {
            terminalControl.Session = new TerminalSessionWin(Buffer, Profile);
            terminalControl.Session.Finished += OnSessionOnFinished;
            terminalControl.Session.Connect();
        }

        private void TerminalControl_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // throw new System.NotImplementedException();
        }
    }
}