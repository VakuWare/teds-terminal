using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using TTerm;
using TTerm.Terminal;
using Timer = System.Timers.Timer;

namespace TermTest
{
    public partial class MainWindow
    {
        class Infos
        {
            public object D1  { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            var size = new TerminalSize(80, 24);

            var profile = ExecutionProfile.CreateDefaultShell().ExpandVariables();

            this.DataContext = new Infos();

            terminalControl.Session = new TerminalSession(size, profile);
            // terminalControl.Session = new TerminalSession(size, new ExecutionProfile()
            // {
            //     Arguments = new []{ "-h" }, Command = @"c:\SN\tools\msys\usr\bin\pacman.exe", CurrentWorkingDirectory = @"c:\SN\tools\msys\usr\bin"
            // });

            // var client = new TcpClient();
            // client.Connect(IPAddress.Parse("192.168.1.15"), 5001 );
            // var stream = client.GetStream();
            // terminalControl.Session = new TerminalSession(size, stream, stream);
            // terminalControl.Session.ImplicitCrForLf = true;


            terminalControl.Session.Connect();

            terminalControl.Session.Finished += (sender, args) =>
            {
                terminalControl.Session.Dispose();
                terminalControl.Session = terminalControl.Session = new TerminalSession(size, profile);
                terminalControl.Session.Connect();
            };

            bool allowSlider = true;

            terminalControl.Session.ViewportChanged += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var buffer = terminalControl.Session.Buffer;
                    if (allowSlider)
                    {
                        allowSlider = false;

                        Slider.Minimum = -terminalControl.Buffer.CursorY;
                        Slider.Maximum = buffer.HistoryLength;
                        var value = -buffer.WindowTop;
                        if (value < Slider.Minimum) value = (int)Slider.Minimum;
                        if (value > Slider.Maximum) value = (int)Slider.Maximum;
                        Slider.Value = value;

                        allowSlider = true;
                    }
                   

                    
                    Debug.Text = buffer.WindowTop.ToString();
                    Debug1.Text = Slider.Minimum.ToString();
                    Debug2.Text = Slider.Maximum.ToString();
                });
            };

            Button1.Click += (sender, args) =>
            {
                terminalControl.Scroll(1);
            };

            Button2.Click += (sender, args) =>
            {
                terminalControl.Scroll(-1);
            };

            Slider.ValueChanged += (sender, args) =>
            {
                if (allowSlider)
                {
                    allowSlider = false;
                    terminalControl.ScrollTo(-(int)args.NewValue); 
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
