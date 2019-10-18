using System;
using System.Collections.Generic;
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
        static Dictionary<int, short> activeKeys;
        static void Main(string[] args)
        {
            activeKeys = new Dictionary<int, short>();
            sSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var internalIp = Dns.GetHostAddresses(Dns.GetHostName()).Where(t => t.AddressFamily == AddressFamily.InterNetwork).ToList()[0];
            int port = 9022;
            bool mirror = false;
            int maxConnection = 1;
            if (args.Length > 0)
            {
                if (args[0] == "\\h" || args[0] == "-h" || args[0] == "--h" || args[0] == "help")
                {
                    Console.WriteLine("Keyboard Server");
                    Console.WriteLine("      -c x.x.x.x          sets ipaddress to listen on");
                    Console.WriteLine("      -p x                sets port");
                    Console.WriteLine("     Default port is 9022");
                    Console.WriteLine("      -m {true or false}          Mirror Mode, Were keyboard inputs are mirrored on both local and remote machine");
                    Console.WriteLine("     Default -m false");
                    //Console.WriteLine("      -maxConnection x            Maximum number of connection"); //Reserverd for future implementation
                    //Console.WriteLine("     Default -maxConnection 1");
                    Console.WriteLine("usage:");
                    Console.WriteLine("         keyboardServer.exe -c 192.168.1.10");
                    Console.WriteLine("         keyboardServerexe -c 192.168.1.10 -p 9022");
                    Environment.Exit(0);
                }
                
                if (args.Length%2==0)
                {
                    var dictArgs = new Dictionary<string,string>();
                    for (int i = 0; i < (args.Length/2); i++)
                    {
                        dictArgs.Add(args[i*2],args[(i*2)+1]);
                    }
                    dictArgs.AsParallel().ForAll((KeyValuePair<string,string> pair) => {
                        if(pair.Key == "-c")
                        {
                            try
                            {
                                internalIp = Dns.GetHostAddresses(Dns.GetHostName()).Where(t => t.AddressFamily == AddressFamily.InterNetwork && t.ToString() == pair.Value).ToList()[0];
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Invalid IpV4 address");
                                Environment.Exit(0);
                            }
                        }
                        else if(pair.Key == "-p")
                        {
                            try
                            {
                                port = int.Parse(pair.Value);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid port");
                                Environment.Exit(0);
                            }
                        }
                        else if(pair.Key == "-m")
                        {
                            try
                            {
                                mirror = bool.Parse(pair.Value);
                            }
                            catch (Exception)
                            {

                                Console.WriteLine("must be a bool");
                                Environment.Exit(0);

                            }
                        }
                    });
                }
                else
                {
                    Console.WriteLine("Ivalid commandline argument");
                    Environment.Exit(0);
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
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);
            serverThread.Start();
            if(!mirror)
            {
                if (BlockInput(true))
                {
                    Console.WriteLine("Keyboard input grabbed successfully");
                    Console.WriteLine("/n/nPress Ctrl-Alt-Del to break keyboard grab/n/nPress Ctrl-C to End program");
                }
                else
                {
                    Console.WriteLine("Keyboard input not grabbed");
                    Console.WriteLine("Admin access is required to lock keyboard input");
                }
            }
            else
            {
                Console.WriteLine("Mirror enabled");
            }

            _hookId = SetHook(_proc);
            Application.Run();
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            UnhookWindowsHookEx(_hookId);
        }

        delegate IntPtr lowlevel_keyboardProc(int ncode, IntPtr wParams, IntPtr lParam);
        static IntPtr HookCallBack(int ncode, IntPtr wParams, IntPtr lParam)
        {
            if (ncode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (clientSock != null)
                {
                    if (clientSock.Connected)
                    {
                        try
                        {
                            var keyFlag = BitConverter.GetBytes((short)wParams);
                            var buffer = new byte[] { 0, 0, 0, (byte)vkCode, 0, keyFlag[0], keyFlag[1] };
                            if (activeKeys.ContainsKey(vkCode))
                            {
                                if(activeKeys[vkCode] != (short)wParams)
                                {
                                    clientSock.Send(buffer);
                                    activeKeys.Remove(vkCode);
                                }

                            }
                            else
                            {
                                activeKeys.Add(vkCode, (short)wParams);
                                clientSock.Send(buffer);
                            }
                            Console.WriteLine("------------");
                            for (int i = 0; i < activeKeys.Count; i++)
                            {
                                var d = activeKeys.ElementAt(0);
                                Console.WriteLine($"key --> {d.Key} --> {d.Value}");
                            }
                            Console.WriteLine("end");
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
        enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_USER_PRESENT = 0x00000004 //Legacy. Don't use this one.
        }
        [DllImport("kernel32.dll")]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
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
