using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using TTerm;
using TTerm.Terminal;
using TTermLib.Cmd;
using Timer = System.Timers.Timer;

namespace TermTest
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var size = new TerminalSize(80, 24);

            var profile = ExecutionProfile.CreateDefaultShell().ExpandVariables();
            // var profile = new ExecutionProfile()
            // {
            //     Arguments = new[] {"-Sy"}, Command = @"c:\SN\tools\msys\usr\bin\pacman.exe",
            //     CurrentWorkingDirectory = @"c:\SN\tools\msys\usr\bin"
            // };
            var b = new TerminalBuffer(size);

            terminalControl.AutoSizeTerminal = true;

            // terminalControl.Session = new TerminalSession(b, profile);
            // terminalControl.Session = new TerminalSession(size, new ExecutionProfile()
            // {
            //     Arguments = new []{ "-h" }, Command = @"c:\SN\tools\msys\usr\bin\pacman.exe", CurrentWorkingDirectory = @"c:\SN\tools\msys\usr\bin"
            // });

            // var client = new TcpClient();
            // client.Connect(IPAddress.Parse("192.168.1.15"), 5001 );
            // var stream = client.GetStream();
            // terminalControl.Session = new TerminalSession(size, stream, stream);
            // terminalControl.Session.ImplicitCrForLf = true;
            // terminalControl.Session.Connect();

            int x = 5;

            void OnSessionOnFinished(object sender, EventArgs args)
            {
                if (terminalControl.Session != null)
                {
                    terminalControl.Session.Finished -= OnSessionOnFinished;
                    terminalControl.Session.Dispose();
                }

                if (x-- > 0)
                    CreatePermaSession();
            }

            void CreatePermaSession()
            {
                terminalControl.Session = new TerminalSessionCmd(b, profile);
                terminalControl.Session.Finished += OnSessionOnFinished;
                terminalControl.Session.Connect();
            }

            CreatePermaSession();
            bool allowSlider = true;

            terminalControl.Buffer.ViewportChanged += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var buffer = terminalControl.Buffer;
                    if (allowSlider)
                    {
                        allowSlider = false;

                        Slider.Minimum = -terminalControl.Buffer.CursorY;
                        Slider.Maximum = buffer.HistoryLength;
                        var value = -buffer.WindowTop;
                        if (value < Slider.Minimum) value = (int) Slider.Minimum;
                        if (value > Slider.Maximum) value = (int) Slider.Maximum;
                        Slider.Value = value;

                        allowSlider = true;
                    }


                    Debug.Text = buffer.WindowTop.ToString();
                    Debug1.Text = Slider.Minimum.ToString();
                    Debug2.Text = Slider.Maximum.ToString();
                });
            };

            Button1.Click += (sender, args) => { terminalControl.Scroll(1); };

            Button2.Click += (sender, args) => { terminalControl.Scroll(-1); };

            Slider.ValueChanged += (sender, args) =>
            {
                if (allowSlider)
                {
                    allowSlider = false;
                    terminalControl.ScrollTo(-(int) args.NewValue);
                    allowSlider = true;
                }
            };


            var t = new Timer(100);
            t.Start();
            t.Elapsed += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Slider.Maximum = terminalControl.Session.Buffer.Size.Rows + terminalControl.Buffer.HistoryLength;
                    // var value = terminalControl.Buffer.HistoryLength + terminalControl.Buffer.WindowTop;
                    // if (value < 0) value = 0;
                    // if (value > Slider.Maximum) value = (int) Slider.Maximum;
                    // Slider.Value = value;


                    // Debug1.Text = Slider.Maximum.ToString();
                    // Debug2.Text = terminalControl.Buffer.WindowTop.ToString();
                });
            };
        }

        private void TerminalControl_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // throw new System.NotImplementedException();
        }
    }
}