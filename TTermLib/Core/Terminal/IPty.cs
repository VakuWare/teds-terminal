using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TTerm.Terminal;

namespace TTermLib.Terminal
{
    public interface IPty : IDisposable
    {
        Stream StandardInput { get; }
        Stream StandardOutput { get; }
        Stream StandardError { get; }
        TerminalSize Size { get; set; }
    }
}
