﻿using System;
using System.IO;
using System.IO.Pipes;
using tterm.Terminal;
using static tterm.Native.Win32;
using static tterm.Native.WinPty;

namespace tterm.Ansi
{
    internal class WinPty : IDisposable
    {
        private bool _disposed;
        private IntPtr _handle;
        private TerminalSize _size;

        public Stream StandardInput { get; private set; }
        public Stream StandardOutput { get; private set; }
        public Stream StandardError { get; private set; }

        static WinPty()
        {
            string platform = Environment.Is64BitProcess ? "x64" : "x86";
            string libPath = String.Format(@"winpty\{0}\winpty.dll", platform);
            IntPtr winptyHandle = LoadLibrary(libPath);
            if (winptyHandle == IntPtr.Zero)
            {
                throw new FileNotFoundException("Unable to find " + libPath);
            }
        }

        public WinPty(TerminalSize size)
        {
            _size = size;

            IntPtr err = IntPtr.Zero;
            IntPtr cfg = IntPtr.Zero;
            IntPtr spawnCfg = IntPtr.Zero;
            try
            {
                cfg = winpty_config_new(WINPTY_FLAG_CONERR | WINPTY_FLAG_COLOR_ESCAPES, out err);
                winpty_config_set_initial_size(cfg, size.Columns, size.Rows);

                _handle = winpty_open(cfg, out err);
                if (err != IntPtr.Zero)
                {
                    throw new WinPtrException(err);
                }

                string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                spawnCfg = winpty_spawn_config_new(WINPTY_SPAWN_FLAG_AUTO_SHUTDOWN, @"C:\Windows\System32\cmd.exe", null, homePath, null, out err);
                if (err != IntPtr.Zero)
                {
                    throw new WinPtrException(err);
                }

                StandardInput = CreatePipe(winpty_conin_name(_handle), PipeDirection.Out);
                StandardOutput = CreatePipe(winpty_conout_name(_handle), PipeDirection.In);
                StandardError = CreatePipe(winpty_conerr_name(_handle), PipeDirection.In);

                if (!winpty_spawn(_handle, spawnCfg, out IntPtr process, out IntPtr thread, out int procError, out err))
                {
                    throw new WinPtrException(err);
                }
            }
            finally
            {
                winpty_config_free(cfg);
                winpty_spawn_config_free(spawnCfg);
                winpty_error_free(err);
            }
        }

        ~WinPty()
        {
            if (!_disposed)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                StandardInput?.Dispose();
                StandardOutput?.Dispose();
                StandardError?.Dispose();
                winpty_free(_handle);

                StandardInput = null;
                StandardOutput = null;
                StandardError = null;
                _handle = IntPtr.Zero;
            }
        }

        private Stream CreatePipe(string pipeName, PipeDirection direction)
        {
            string serverName = ".";
            if (pipeName.StartsWith("\\"))
            {
                int slash3 = pipeName.IndexOf('\\', 2);
                if (slash3 != -1)
                {
                    serverName = pipeName.Substring(2, slash3 - 2);
                }
                int slash4 = pipeName.IndexOf('\\', slash3 + 1);
                if (slash4 != -1)
                {
                    pipeName = pipeName.Substring(slash4 + 1);
                }
            }

            var pipe = new NamedPipeClientStream(serverName, pipeName, direction);
            pipe.Connect();
            return pipe;
        }

        public TerminalSize Size
        {
            get => _size;
            set
            {
                IntPtr err = IntPtr.Zero;
                try
                {
                    winpty_set_size(_handle, value.Columns, value.Rows, out err);
                    if (err != IntPtr.Zero)
                    {
                        throw new WinPtrException(err);
                    }
                    _size = value;
                }
                finally
                {
                    winpty_error_free(err);
                }
            }
        }

        private class WinPtrException : Exception
        {
            public int Code { get; }

            public WinPtrException(IntPtr err)
                : base(winpty_error_msg(err))
            {
                Code = winpty_error_code(err);
            }
        }
    }
}
