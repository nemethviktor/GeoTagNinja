using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using GeoTagNinja.Helpers;

namespace GeoTagNinja;

public partial class FrmSplashScreen : Form
{
    private Stopwatch stopWatch = new();

    public FrmSplashScreen()
    {
        InitializeComponent();
    }

    [DllImport(dllName: "User32.dll")]
    [return: MarshalAs(unmanagedType: UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int MIN_WAIT_TIME_BEFORE_CLOSE_MSEC = 1200;

    private void FrmSplashScreen_Load(object sender,
                                      EventArgs e)
    {
        HelperNonStatic helperNonStatic = new();
        helperNonStatic.CenterForm(frm: this);
        SetForegroundWindow(hWnd: Handle);
        stopWatch.Start();
    }

    /// <summary>
    ///     Updates the progress - just so that things don't look totally stupid there is a 1.2 sec wait before closing
    ///     assuming tasks have actually finished.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="close"></param>
    internal void UpdateProgress(int amount,
                                 bool close)
    {
        BeginInvoke(method: new MethodInvoker(delegate
        {
            pbr_Splash.Increment(value: amount);
            if (close)
            {
                while (stopWatch.Elapsed < TimeSpan.FromMilliseconds(value: MIN_WAIT_TIME_BEFORE_CLOSE_MSEC))
                {
                    Application.DoEvents();
                    Thread.Sleep(timeout: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 100));
                }

                Close();
            }
        }));
    }
}