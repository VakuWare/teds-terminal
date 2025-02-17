﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TTerm.Native;
using static TTerm.Native.Win32;

namespace TTermLibCmd
{
    internal static class UserUtility
    {
        public static IntPtr GetPrimaryToken()
        {
            return GetPrimaryToken(Process.GetCurrentProcess());
        }

        public static IntPtr GetPrimaryToken(Process process)
        {
            var token = IntPtr.Zero;
            var primaryToken = IntPtr.Zero;

            if (OpenProcessToken(process.Handle, TOKEN_DUPLICATE, ref token))
            {
                var sa = new Win32.SECURITY_ATTRIBUTES();
                sa.nLength = Marshal.SizeOf(sa);

                if (!DuplicateTokenEx(
                    token,
                    TOKEN_ALL_ACCESS,
                    ref sa,
                    (int)Win32.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    (int)Win32.TOKEN_TYPE.TokenPrimary,
                    ref primaryToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "DuplicateTokenEx failed");
                }

                CloseHandle(token);
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken failed");
            }

            return primaryToken;
        }
    }
}
