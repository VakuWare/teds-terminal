using System;
using System.Collections.Generic;
using System.Text;
using TTerm;
using TTerm.Terminal;

namespace TTermLibCmd
{
    public class TerminalSessionCmd : TerminalSession
    {
        public TerminalSessionCmd(TerminalBuffer buffer, ExecutionProfile executionProfile) 
            : base(buffer, new WinPty(executionProfile, buffer.Size))
        {

        }
    }
}
