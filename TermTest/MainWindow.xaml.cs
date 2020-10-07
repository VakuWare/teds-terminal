using System.Windows.Input;
using TTerm;
using TTerm.Terminal;

namespace TermTest
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var size = new TerminalSize(80, 24);

            var profile = Profile.CreateDefaultProfile().ExpandVariables();


            terminalControl.Session = new TerminalSession(size, profile);
        }

        private void TerminalControl_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // throw new System.NotImplementedException();
        }
    }
}
