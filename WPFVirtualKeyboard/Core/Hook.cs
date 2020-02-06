
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace WPFVirtualKeyboard.Core
{
    public class Hook
    {
        #region Variable

        private static IntPtr _handle = IntPtr.Zero;
        private static IntPtr _hModule = IntPtr.Zero;
        private static IntPtr _keyboardId = IntPtr.Zero;
        private static IntPtr _mouseId = IntPtr.Zero;

        private static Win32Api.HookProc _keyboardProc = new Win32Api.HookProc(KeyboardProc);
        private static Win32Api.HookProc _mouseProc = new Win32Api.HookProc(MouseProc);

        private static Win32Api.MOUSEHOOKSTRUCT _mouseParam;

        private static IntPtr _prevWindow = IntPtr.Zero;
        private static IntPtr _prevFocus = IntPtr.Zero;

        public delegate void MouseClickEventHandler(Win32Api.POINT point, Win32Api.MouseMessages msg);
        public static event MouseClickEventHandler MouseClickEvent;

        public delegate void KeyClickEventHandler(uint keyCode);
        public static event KeyClickEventHandler KeyClickEvent;

        #endregion

        #region Property

        public static bool IsRun { get; private set; }
        public static bool UseGlobal { get; set; }
        public static Rect HookArea { get; set; }
        public static UIElement HookElement { get; set; }

        #endregion

        #region Constructor

        public Hook()
        {
        }

        #endregion

        #region Public Method

        public static void Start()
        {
            if (!IsRun)
            {
                var threadId = Win32Api.GetCurrentThreadId();

                using (Process process = Process.GetCurrentProcess())
                {
                    using (ProcessModule module = process.MainModule)
                    {
                        _handle = process.MainWindowHandle;

                        _hModule = Win32Api.GetModuleHandle(module.ModuleName);

                        _keyboardId = Win32Api.SetWindowsHookEx((int)Win32Api.HookType.WH_KEYBOARD_LL, _keyboardProc, _hModule, 0);
                        _mouseId = Win32Api.SetWindowsHookEx((int)Win32Api.HookType.WH_MOUSE_LL, _mouseProc, _hModule, 0);

                        IsRun = true;
                    }
                }
            }
        }

        public static void Stop()
        {
            if (IsRun)
            {
                Win32Api.UnhookWindowsHookEx(_keyboardId);
                Win32Api.UnhookWindowsHookEx(_mouseId);

                IsRun = false;
            }
        }

        #endregion

        #region Private Method
        private static IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == Win32Api.HC_ACTION)
            {
                var wParamValue = (uint)wParam;
                var lParamValue = (long)lParam;

                if (wParamValue == 256)
                {
                    var keyboardParam = (Win32Api.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32Api.KBDLLHOOKSTRUCT));

                    KeyClickEvent?.Invoke(keyboardParam.vkCode);
                }

                // 229 ( 0xE5 ) : VK_PROCESSKEY ( IME PROCESS key )
                if ((wParamValue == 229 && lParamValue == -2147483647) || (wParamValue == 229 && lParamValue == -2147483648))
                {
                    if (IsHookingArea())
                    {
                        return (IntPtr)1;
                    }
                }
            }

            return Win32Api.CallNextHookEx(_keyboardId, nCode, wParam, lParam);
        }

        private static IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                _mouseParam = (Win32Api.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32Api.MOUSEHOOKSTRUCT));
                var mouseMessage = (Win32Api.MouseMessages)wParam;

                if (UseGlobal)
                {
                    if (mouseMessage == Win32Api.MouseMessages.WM_LBUTTONDOWN || mouseMessage == Win32Api.MouseMessages.WM_LBUTTONUP)
                    {
                        MouseClickEvent?.Invoke(_mouseParam.pt, mouseMessage);

                        if (mouseMessage == Win32Api.MouseMessages.WM_LBUTTONDOWN && IsHookingArea())
                        {
                            return (IntPtr)1;
                        }
                    }
                }
            }

            return Win32Api.CallNextHookEx(_mouseId, nCode, wParam, lParam);
        }

        private static bool IsHookingArea()
        {
            if (HookElement != null && !HookArea.IsEmpty)
            {
                var point = HookElement.PointFromScreen(new Point(_mouseParam.pt.x, _mouseParam.pt.y));
                var contains = HookArea.Contains(point);

                return contains;
            }

            return true;
        }
        #endregion
    }
}
