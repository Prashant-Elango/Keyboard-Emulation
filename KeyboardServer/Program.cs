using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace KeyboardServer
{
    class Program
    {
        const int WH_Keyboard_LL = 13;

        static lowlevel_keyboardProc _proc = HookCallBack;
        static IntPtr _hookId = IntPtr.Zero;

        static Socket sSock;
        static Socket clientSock;
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "\\h" || args[0] == "-h" || args[0] == "--h" || args[0] == "help")
                {
                    Console.WriteLine("Keyboard Server");
                    Console.WriteLine("      -c           sets ipaddress to listen on");
                    Console.WriteLine("      -p           sets port");
                    Console.WriteLine("      -m           Mirror Mode, Were keyboard inputs are mirrored on both local and remote machine");
                    Console.WriteLine("     Default port is 9022");
                    Console.WriteLine("usage:");
                    Console.WriteLine("         keyboardServer.exe -c 192.168.1.10");
                    Console.WriteLine("         keyboardServerexe -c 192.168.1.10 -p 9022");
                }
                Environment.Exit(0);
            }

            sSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var internalIp = Dns.GetHostAddresses(Dns.GetHostName()).Where(t=> t.AddressFamily == AddressFamily.InterNetwork).ToList()[0];
                        int port = 9022;
            if(args.Length == 4)
            {
                if(args[0] == "-c")
                {
                    var _ip = args[1];
                    try
                    {
                        internalIp = Dns.GetHostAddresses(Dns.GetHostName()).Where(t => t.AddressFamily == AddressFamily.InterNetwork && t.ToString() == _ip).ToList()[0];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Invalid Ipv4");
                        Console.WriteLine(e.Message);
                    }
                }
                if(args[2] == "-p")
                {
                    var _port = args[3];
                    try
                    {
                        port = int.Parse(_port);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Invalid port");
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else if(args.Length == 2)
            {
                if (args[0] == "-p")
                {
                    var _port = args[1];
                    try
                    {
                        port = int.Parse(_port);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Invalid port");
                        Console.WriteLine(e.Message);
                    }
                }
            }
            Console.WriteLine($"Listening on {internalIp}:{port}");
            var ipEndPoint = new IPEndPoint(internalIp, port);
            sSock.Bind(ipEndPoint);

            var serverThread = new Thread(async () =>
            {
                while (true)
                {
                    sSock.Listen(1);
                    clientSock = await sSock.AcceptAsync();
                }
            });
            serverThread.Start();
            if (BlockInput(true))
            {
                Console.WriteLine("Keyboard input grabbed successfully");
            }
            else
            {
                Console.WriteLine("Keyboard input not grabbed");
                Console.WriteLine("Admin access is required to lock keyboard input");
            }
            _hookId = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookId);
        }

        delegate IntPtr lowlevel_keyboardProc(int ncode, IntPtr wParams, IntPtr lParam);
        static IntPtr HookCallBack(int ncode, IntPtr wParams, IntPtr lParam)
        {
            if (ncode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if(clientSock!=null)
                {
                    if(clientSock.Connected)
                    {
                        try
                        {
                            var keyFlag = BitConverter.GetBytes((short)wParams);
                            var buffer = new byte[] { 0, 0, 0, (byte)vkCode, 0, keyFlag[0], keyFlag[1] };
                            clientSock.Send(buffer);
                        }
                        catch (Exception)
                        {
                            clientSock = null;
                        }
                        

                    }
                }
            }
            return CallNextHookEx(_hookId, ncode, wParams, lParam);
        }
        static IntPtr SetHook(lowlevel_keyboardProc proc)
        {
            var currProcess = Process.GetCurrentProcess();
            var mainModule = currProcess.MainModule;
            var moduleHandle = GetModuleHandle(mainModule.ModuleName);
            return SetWindowsHookEx(WH_Keyboard_LL, proc, moduleHandle, 0);
        }

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool BlockInput(bool fBlockIt);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr GetModuleHandle(string moduleName);
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int hookId, lowlevel_keyboardProc lpFn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hookId);

    }
}
