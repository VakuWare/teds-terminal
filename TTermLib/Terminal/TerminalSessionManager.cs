using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using TTerm.Extensions;

namespace TTerm.Terminal
{
    internal class TerminalSessionManager
    {
        private readonly List<TerminalSession> _sessions = new List<TerminalSession>();

        public IList<TerminalSession> Sessions
        {
            get => new ReadOnlyCollection<TerminalSession>(_sessions);
        }

        public TerminalSession CreateSession(TerminalSize size, ExecutionProfile executionProfile)
        {
            PrepareTTermEnvironment(executionProfile);

            var buffer = new TerminalBuffer(size);
            var session = new TerminalSession(buffer, executionProfile);
            session.Finished += OnSessionFinished;

            _sessions.Add(session);
            return session;
        }

        private void PrepareTTermEnvironment(ExecutionProfile executionProfile)
        {
            var env = executionProfile.EnvironmentVariables;

            // Force our own environment variable so shells know we are in a tterm session
            int pid = Process.GetCurrentProcess().Id;
            env[EnvironmentVariables.TTERM] = pid.ToString();

            // Add assembly directory to PATH so tterm can be launched from the shell
            // var app = Application.Current as App;
            // string path = env.GetValueOrDefault(EnvironmentVariables.PATH);
            // if (!string.IsNullOrEmpty(path))
            // {
            //     path = ";" + path;
            // }
            // env[EnvironmentVariables.PATH] = app.AssemblyDirectory + path;
        }

        private void OnSessionFinished(object sender, EventArgs e)
        {
            var session = sender as TerminalSession;
            _sessions.Remove(session);
        }
    }
}
