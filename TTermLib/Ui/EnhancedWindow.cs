using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Newtonsoft.Json;
using TTerm.Native;
using static TTerm.Native.Win32;

namespace TTerm.Ui
{
    public class EnhancedWindow : Window
    {
        private readonly WindowInteropHelper _interopHelper;

        protected bool IsResizing { get; private set; }
        protected IntPtr Hwnd => _interopHelper.Handle;
        protected DpiScale Dpi => VisualTreeHelper.GetDpi(this);

        public Size Size
        {
            get => RenderSize;
            set
            {
                if (value != RenderSize)
                {
                    var dpi = Dpi;
                    int width = (int)(value.Width * dpi.DpiScaleX);
                    int height = (int)(value.Height * dpi.DpiScaleY);
                    uint flags = SWP_NOMOVE | SWP_NOZORDER;
                    SetWindowPos(Hwnd, IntPtr.Zero, 0, 0, width, height, flags);
                }
            }
        }

        public EnhancedWindow()
        {
            _interopHelper = new WindowInteropHelper(this);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource.FromHwnd(Hwnd).AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_SIZING:
                    {
                        var bounds = Marshal.PtrToStructure<Win32.RECT>(lParam);
                        var newBounds = CorrectWindowBounds(bounds, wParam.ToInt32());
                        if (!newBounds.Equals(bounds))
                        {
                            IsResizing = true;
                            Marshal.StructureToPtr(newBounds, lParam, fDeleteOld: false);
                        }
                        break;
                    }
                case WM_EXITSIZEMOVE:
                    if (IsResizing)
                    {
                        OnResizeEnded();
                        IsResizing = false;
                    }
                    break;
                case WM_COPYDATA:
                    {
                        var data = Marshal.PtrToStructure<Win32.COPYDATASTRUCT>(lParam);
                        string forkDataJson = Marshal.PtrToStringUni(data.lpData);

                        // Defer the behaviour until after we have replied to the caller process
                        Dispatcher.InvokeAsync(() =>
                        {
                            var forkData = JsonConvert.DeserializeObject<ForkData>(forkDataJson);
                            OnForked(forkData);
                        });
                        return new IntPtr(1);
                    }
            }
            return IntPtr.Zero;
        }

        private Win32.RECT CorrectWindowBounds(Win32.RECT bounds, int grabDirection)
        {
            var result = bounds;
            var dpi = Dpi;
            var absoluteSize = new Size(bounds.right - bounds.left, bounds.bottom - bounds.top);
            var scaledSize = new Size(absoluteSize.Width / dpi.DpiScaleX, absoluteSize.Height / dpi.DpiScaleY);
            var newScaledSize = GetPreferedSize(scaledSize);
            if (newScaledSize != scaledSize)
            {
                var newAbsoluteSize = new Size(newScaledSize.Width * dpi.DpiScaleX, newScaledSize.Height * dpi.DpiScaleY);
                result = ChangeSizeInDirection(bounds, grabDirection, newAbsoluteSize);
            }
            return result;
        }

        private static Win32.RECT ChangeSizeInDirection(Win32.RECT bounds, int direction, Size size)
        {
            switch (direction)
            {
                case WMSZ_LEFT:
                case WMSZ_TOPLEFT:
                case WMSZ_BOTTOMLEFT:
                    bounds.left = bounds.right - (int)size.Width;
                    break;
                case WMSZ_RIGHT:
                case WMSZ_TOPRIGHT:
                case WMSZ_BOTTOMRIGHT:
                    bounds.right = bounds.left + (int)size.Width;
                    break;
            }
            switch (direction)
            {
                case WMSZ_TOP:
                case WMSZ_TOPLEFT:
                case WMSZ_TOPRIGHT:
                    bounds.top = bounds.bottom - (int)size.Height;
                    break;
                case WMSZ_BOTTOM:
                case WMSZ_BOTTOMLEFT:
                case WMSZ_BOTTOMRIGHT:
                    bounds.bottom = bounds.top + (int)size.Height;
                    break;
            }
            return bounds;
        }

        protected virtual Size GetPreferedSize(Size size)
        {
            return size;
        }

        protected virtual void OnResizeEnded()
        {
        }

        protected virtual void OnForked(ForkData data)
        {
        }
    }
}
