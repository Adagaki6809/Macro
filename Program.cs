using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Macro;

static class Program
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MOUSEMOVE = 0x0200;

    private static LowLevelProc mouseProc = MouseHookCallback, keyboardProc = KeyboardHookCallback;
    private static IntPtr mouseHookID = IntPtr.Zero;
    private static IntPtr keyboardHookID = IntPtr.Zero;
    private static Stopwatch sw = new Stopwatch();
    private static int j = 1;
    private static object locker = new object();
    private static Thread myThread = new Thread(Click);
    private static bool Activated = false, leftUp, lastLeftUp, leftDown;
    private static string appName = "Macro.exe";

    [STAThread]
    static void Main()
    {
        mouseHookID = SetHook(mouseProc, WH_MOUSE_LL);
        keyboardHookID = SetHook(keyboardProc, WH_KEYBOARD_LL);
        Application.Run();
        UnhookWindowsHookEx(mouseHookID);
        UnhookWindowsHookEx(keyboardHookID);
    }

    private static IntPtr SetHook(LowLevelProc proc, int hookType)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule? curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(hookType, proc,
                GetModuleHandle(curModule?.ModuleName ?? appName), 0);
        }
    }
    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if ((nCode >= 0) && (wParam == (IntPtr)WM_KEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            switch(vkCode)
            {
                case 109: // pressed -
                    if (Activated)
                    {
                        Activated = false;
                        Console.WriteLine("Deactivated");
                    }
                    else  
                    {
                        Activated = true;
                        Console.WriteLine("Activated");
                    }
                    break;
                case 111: // pressed /
                    for (int i = 1; i < 1214; i++) // bc2: 1214 0.75
                    {
                        mouse_event(MouseFlags.Move, 1, 0, 0, UIntPtr.Zero);
                        if (i%10 == 0)
                            Thread.Sleep(10);
                    }
                    break;
                default:
                    break;
            }
            
        }
        return CallNextHookEx(keyboardHookID, nCode, wParam, lParam);
    }
    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (Activated && nCode >= 0)
        {
            switch ((int)wParam)
            {
                case WM_LBUTTONDOWN:
                    leftUp = false;
                    lastLeftUp = false;
                    sw.Start();
                    if (myThread.ThreadState == System.Threading.ThreadState.Unstarted)
                    {
                        myThread.Start();
                    }
                    if (leftDown)
                    {
                        return new IntPtr(1);
                    }
                    leftDown = true;
                    break;

                case WM_LBUTTONUP:
                    leftDown = false;
                    if (leftUp)
                    {
                        sw.Stop();
                        Console.WriteLine($"Time: {sw.ElapsedMilliseconds}");
                        sw.Reset();
                        j = 1;
                        lastLeftUp = true;
                        return new IntPtr(1);
                    }
                    leftUp = true;
                    break;

                default:
                    break;
            }
        }
        return CallNextHookEx(mouseHookID, nCode, wParam, lParam);
    }

    static void Click()
    {
        lock (locker)
        {    
            while (true)
            {
                if (!lastLeftUp && Activated)
                {
                    var t1 = DateTime.Now;
                    mouse_event(MouseFlags.Absolute | MouseFlags.LeftDown, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(37);
                    mouse_event(MouseFlags.Absolute | MouseFlags.LeftUp, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(37);
                    var t2 = DateTime.Now;
                    Console.WriteLine("{0,2} {1}",j++, (t2-t1).Milliseconds);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }
    }
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    static extern void mouse_event(MouseFlags dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);

    [Flags]
    enum MouseFlags
    {
        Move = 0x0001, LeftDown = 0x0002, LeftUp = 0x0004, RightDown = 0x0008,
        RightUp = 0x0010, Absolute = 0x8000
    };    
}
