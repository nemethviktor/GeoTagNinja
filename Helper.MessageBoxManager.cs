#pragma warning disable 0618

using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedVariable

[assembly: SecurityPermission(action: SecurityAction.RequestMinimum, UnmanagedCode = true)]

namespace System.Windows.Forms;

// via https://www.codeproject.com/Articles/18399/Localizing-System-MessageBox
public class MessageBoxManager
{
    private const int WH_CALLWNDPROCRET = 12;
    private const int WM_DESTROY = 0x0002;
    private const int WM_INITDIALOG = 0x0110;
    private const int WM_TIMER = 0x0113;
    private const int WM_USER = 0x400;
    private const int DM_GETDEFID = WM_USER + 0;

    private const int MBOK = 1;
    private const int MBCancel = 2;
    private const int MBAbort = 3;
    private const int MBRetry = 4;
    private const int MBIgnore = 5;
    private const int MBYes = 6;
    private const int MBNo = 7;

    private static readonly HookProc hookProc;
    private static readonly EnumChildProc enumProc;

    [ThreadStatic] private static IntPtr hHook;

    [ThreadStatic] private static int nButton;

    /// <summary>
    ///     OK text
    /// </summary>
    public static string OK = "&OK";

    /// <summary>
    ///     Cancel text
    /// </summary>
    public static string Cancel = "&Cancel";

    /// <summary>
    ///     Abort text
    /// </summary>
    public static string Abort = "&Abort";

    /// <summary>
    ///     Retry text
    /// </summary>
    public static string Retry = "&Retry";

    /// <summary>
    ///     Ignore text
    /// </summary>
    public static string Ignore = "&Ignore";

    /// <summary>
    ///     Yes text
    /// </summary>
    public static string Yes = "&Yes";

    /// <summary>
    ///     No text
    /// </summary>
    public static string No = "&No";

    static MessageBoxManager()
    {
        hookProc = MessageBoxHookProc;
        enumProc = MessageBoxEnumProc;
        hHook = IntPtr.Zero;
    }


    [DllImport(dllName: "user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd,
                                             int Msg,
                                             IntPtr wParam,
                                             IntPtr lParam);

    [DllImport(dllName: "user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook,
                                                  HookProc lpfn,
                                                  IntPtr hInstance,
                                                  int threadId);

    [DllImport(dllName: "user32.dll")]
    private static extern int UnhookWindowsHookEx(IntPtr idHook);

    [DllImport(dllName: "user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr idHook,
                                                int nCode,
                                                IntPtr wParam,
                                                IntPtr lParam);

    [DllImport(dllName: "user32.dll", EntryPoint = "GetWindowTextLengthW", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport(dllName: "user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd,
                                            StringBuilder text,
                                            int maxLength);

    [DllImport(dllName: "user32.dll")]
    private static extern int EndDialog(IntPtr hDlg,
                                        IntPtr nResult);

    [DllImport(dllName: "user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent,
                                                EnumChildProc lpEnumFunc,
                                                IntPtr lParam);

    [DllImport(dllName: "user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd,
                                           StringBuilder lpClassName,
                                           int nMaxCount);

    [DllImport(dllName: "user32.dll")]
    private static extern int GetDlgCtrlID(IntPtr hwndCtl);

    [DllImport(dllName: "user32.dll")]
    private static extern IntPtr GetDlgItem(IntPtr hDlg,
                                            int nIDDlgItem);

    [DllImport(dllName: "user32.dll", EntryPoint = "SetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern bool SetWindowText(IntPtr hWnd,
                                             string lpString);

    /// <summary>
    ///     Enables MessageBoxManager functionality
    /// </summary>
    /// <remarks>
    ///     MessageBoxManager functionality is enabled on current thread only.
    ///     Each thread that needs MessageBoxManager functionality has to call this method.
    /// </remarks>
    public static void Register()
    {
        if (hHook != IntPtr.Zero)
        {
            throw new NotSupportedException(message: "One hook per thread allowed.");
        }

        hHook = SetWindowsHookEx(idHook: WH_CALLWNDPROCRET, lpfn: hookProc, hInstance: IntPtr.Zero, threadId: AppDomain.GetCurrentThreadId());
    }

    /// <summary>
    ///     Disables MessageBoxManager functionality
    /// </summary>
    /// <remarks>
    ///     Disables MessageBoxManager functionality on current thread only.
    /// </remarks>
    public static void Unregister()
    {
        if (hHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(idHook: hHook);
            hHook = IntPtr.Zero;
        }
    }

    private static IntPtr MessageBoxHookProc(int nCode,
                                             IntPtr wParam,
                                             IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(idHook: hHook, nCode: nCode, wParam: wParam, lParam: lParam);
        }

        CWPRETSTRUCT msg = (CWPRETSTRUCT)Marshal.PtrToStructure(ptr: lParam, structureType: typeof(CWPRETSTRUCT));
        IntPtr hook = hHook;

        if (msg.message == WM_INITDIALOG)
        {
            int nLength = GetWindowTextLength(hWnd: msg.hwnd);
            StringBuilder className = new(capacity: 10);
            GetClassName(hWnd: msg.hwnd, lpClassName: className, nMaxCount: className.Capacity);
            if (className.ToString() == "#32770")
            {
                nButton = 0;
                EnumChildWindows(hWndParent: msg.hwnd, lpEnumFunc: enumProc, lParam: IntPtr.Zero);
                if (nButton == 1)
                {
                    IntPtr hButton = GetDlgItem(hDlg: msg.hwnd, nIDDlgItem: MBCancel);
                    if (hButton != IntPtr.Zero)
                    {
                        SetWindowText(hWnd: hButton, lpString: OK);
                    }
                }
            }
        }

        return CallNextHookEx(idHook: hook, nCode: nCode, wParam: wParam, lParam: lParam);
    }

    private static bool MessageBoxEnumProc(IntPtr hWnd,
                                           IntPtr lParam)
    {
        StringBuilder className = new(capacity: 10);
        GetClassName(hWnd: hWnd, lpClassName: className, nMaxCount: className.Capacity);
        if (className.ToString() == "Button")
        {
            int ctlId = GetDlgCtrlID(hwndCtl: hWnd);
            switch (ctlId)
            {
                case MBOK:
                    SetWindowText(hWnd: hWnd, lpString: OK);
                    break;
                case MBCancel:
                    SetWindowText(hWnd: hWnd, lpString: Cancel);
                    break;
                case MBAbort:
                    SetWindowText(hWnd: hWnd, lpString: Abort);
                    break;
                case MBRetry:
                    SetWindowText(hWnd: hWnd, lpString: Retry);
                    break;
                case MBIgnore:
                    SetWindowText(hWnd: hWnd, lpString: Ignore);
                    break;
                case MBYes:
                    SetWindowText(hWnd: hWnd, lpString: Yes);
                    break;
                case MBNo:
                    SetWindowText(hWnd: hWnd, lpString: No);
                    break;
            }

            nButton++;
        }

        return true;
    }

    private delegate IntPtr HookProc(int nCode,
                                     IntPtr wParam,
                                     IntPtr lParam);

    private delegate bool EnumChildProc(IntPtr hWnd,
                                        IntPtr lParam);


    [StructLayout(layoutKind: LayoutKind.Sequential)]
    public struct CWPRETSTRUCT
    {
        public IntPtr lResult;
        public IntPtr lParam;
        public IntPtr wParam;
        public uint message;
        public IntPtr hwnd;
    }
}