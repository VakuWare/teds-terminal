using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using TTerm.Terminal;
using TTermLib.Terminal;
using static TTerm.Native.Win32;
using static winpty.WinPty;

namespace TTermLibCmd
{

    internal static class ThrowHelper
    {
        public static int W32Throw(int code, string message = null)
        {
            if (code == 0)
                return 0;

            var msg = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            throw new InvalidOperationException(
                $"Win32 stuff failed with return code {code}: {msg} - {message}");
        }


        public static bool W32Throw(bool code)
        {
            if (code)
                return true;

            var msg = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            throw new InvalidOperationException(
                $"Win32 stuff failed with return code {code}: {msg}");
        }
    }


    internal class NativePty : IPty
    {
        private bool _isDisposed = false;
        private SafeFileHandle _writeHandle;
        private SafeFileHandle _readHandle;
        private IntPtr _ptyHandle;

        public NativePty(ExecutionProfile executionProfile, TerminalSize size)
        {
            SafeFileHandle stdin;
            SafeFileHandle stdout;

            CreatePipe(out stdin, out _writeHandle, IntPtr.Zero, 0);
            CreatePipe(out _readHandle, out stdout, IntPtr.Zero, 0);

            ThrowHelper.W32Throw(CreatePseudoConsole(new COORD {X = (short) size.Columns, Y = (short) size.Rows},
                stdin, stdout, 0, out this._ptyHandle));

            var allocSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref allocSize);
            if (allocSize == IntPtr.Zero)
                ThrowHelper.W32Throw(0, "allocation granularity whatever.");

            var startupInfo = new STARTUPINFOEX
            {
                StartupInfo = {cb = Marshal.SizeOf<STARTUPINFOEX>()},
                lpAttributeList = Marshal.AllocHGlobal(allocSize)
            };

            ThrowHelper.W32Throw(InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref allocSize));

            ThrowHelper.W32Throw(UpdateProcThreadAttribute(startupInfo.lpAttributeList, 0,
                (IntPtr) PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, _ptyHandle, (IntPtr) IntPtr.Size, IntPtr.Zero,
                IntPtr.Zero));

            var processSecurityAttr = new SECURITY_ATTRIBUTES();
            var threadSecurityAttr = new SECURITY_ATTRIBUTES();
            processSecurityAttr.nLength = threadSecurityAttr.nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>();

            var args = executionProfile.Arguments ?? Array.Empty<string>();
            var cmdline = executionProfile.EscapeArguments
                ? string.Join(" ", args.Select(x => $"\"{x}\""))
                : string.Join(" ", args);

            ThrowHelper.W32Throw(CreateProcess(executionProfile.Command, cmdline, ref processSecurityAttr, ref threadSecurityAttr,
                false, EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null, ref startupInfo, out var processInfo));

            DeleteProcThreadAttributeList(startupInfo.lpAttributeList);
            Marshal.FreeHGlobal(startupInfo.lpAttributeList);

            StandardOutput = new FileStream(_readHandle, FileAccess.Read);
            StandardInput = new FileStream(_writeHandle, FileAccess.Write);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _readHandle?.Dispose();
                _writeHandle?.Dispose();
                ClosePseudoConsole(_ptyHandle);
            }
        }

        public Stream StandardInput { get; }
        public Stream StandardOutput { get; }
        public Stream StandardError { get; }
        public TerminalSize Size { get; set; }
    }

    internal class WinPty : IPty
    {
        private bool _disposed;
        private IntPtr _handle;
        private TerminalSize _size;

        public Stream StandardInput { get; private set; }
        public Stream StandardOutput { get; private set; }
        public Stream StandardError { get; private set; }

        static WinPty()
        {
            if (!Environment.Is64BitProcess)
                throw new Exception("winpty.dll seems to be for a 64x process");
            string libPath = @"C:\Users\Malte\source\Repos\Terminal\TTermLib\Win\winpty.dll";
            IntPtr winptyHandle = LoadLibrary(libPath);
            if (winptyHandle == IntPtr.Zero)
            {
                throw new FileNotFoundException("Unable to find " + libPath);
            }
        }

        public WinPty(ExecutionProfile executionProfile, TerminalSize size, bool separateStdErr = false)
        {
            _size = size;

            IntPtr err = IntPtr.Zero;
            IntPtr cfg = IntPtr.Zero;
            IntPtr spawnCfg = IntPtr.Zero;
            try
            {
                ulong cfgFlags = WINPTY_FLAG_COLOR_ESCAPES;
                if (separateStdErr)
                {
                    cfgFlags |= WINPTY_FLAG_CONERR;
                }

                cfg = winpty_config_new(cfgFlags, out err);
                winpty_config_set_initial_size(cfg, size.Columns, size.Rows);

                _handle = winpty_open(cfg, out err);
                if (err != IntPtr.Zero)
                {
                    throw new WinPtrException(err);
                }

                string cmdline = null;
                string env = GetEnvironmentString(executionProfile.EnvironmentVariables);
                if (executionProfile.Arguments != null && executionProfile.Arguments.Length > 0)
                {
                    cmdline = executionProfile.EscapeArguments
                        ? string.Join(" ", executionProfile.Arguments.Select(x => $"\"{x}\""))
                        : string.Join(" ", executionProfile.Arguments);
                }

                cmdline = $"{executionProfile.Command} {cmdline}";
                spawnCfg = winpty_spawn_config_new(WINPTY_SPAWN_FLAG_AUTO_SHUTDOWN, executionProfile.Command, cmdline,
                    executionProfile.CurrentWorkingDirectory, env, out err);
                if (err != IntPtr.Zero)
                {
                    throw new WinPtrException(err);
                }

                StandardInput = CreatePipe(winpty_conin_name(_handle), PipeDirection.Out);
                StandardOutput = CreatePipe(winpty_conout_name(_handle), PipeDirection.In);
                if (separateStdErr)
                {
                    StandardError = CreatePipe(winpty_conerr_name(_handle), PipeDirection.In);
                }

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

        private string GetEnvironmentString(IDictionary<string, string> environmentVariables)
        {
            if (environmentVariables == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var kvp in environmentVariables)
            {
                sb.AppendFormat("{0}={1}\0", kvp.Key, kvp.Value);
            }

            sb.Append('\0');
            return sb.ToString();
        }

        public TerminalSize Size
        {
            get => _size;
            set
            {
                if (_size != value)
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