using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace KeyboardClient
{
    class Program
    {
        static Dictionary<byte, byte> keyboardScanCode = new Dictionary<byte, byte>() { 
            (0x)
        };
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk,byte bScan,uint dwFlags,int dwExtraInfo);

        static void sendKey(byte bvk,short keyFlag)
        {
         
            if(keyFlag == 0x0100)
            {
                //Key Press
                keybd_event(bvk, 0, 0x0001|0x0000, 0);
            }
            else
            {
                //Key Release
                keybd_event(bvk, 0, 0x0001|0x0002, 0);

            }

        }
        static void Main(string[] args)
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var serverIp = "";
            var serverPort = 9022;
            if (args.Length > 0)
            {
                if (args[0] == "\\h" || args[0] == "-h" || args[0] == "--h" || args[0] == "help")
                {
                    Console.WriteLine("Keyboard Client");
                    Console.WriteLine("         -c           sets keyboardServer Ip address");
                    Console.WriteLine("         -p           sets port");
                    Console.WriteLine("      Default -p 9022");
                    Console.WriteLine("Usages:");
                    Console.WriteLine("     ex: keyboardClient.exe -c 192.168.1.10 -p 5555");
                    Console.WriteLine("     ex: keyboardClient.exe -c 192.168.1.10");
                    Environment.Exit(0);
                }

                if (args.Length % 2 == 0)
                {
                    var dictArgs = new Dictionary<string, string>();
                    for (int i = 0; i < (args.Length / 2); i++)
                    {
                        dictArgs.Add(args[i * 2], args[(i * 2) + 1]);
                    }
                    dictArgs.AsParallel().ForAll((KeyValuePair<string, string> pair) => {
                        if (pair.Key == "-c")
                        {
                            try
                            {
                                serverIp = pair.Value;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Invalid IpV4 server address");
                                Environment.Exit(0);
                            }
                        }
                        else if (pair.Key == "-p")
                        {
                            try
                            {
                                serverPort = int.Parse(pair.Value);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Invalid port");
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
            if (serverIp != "" && serverPort != 0)
            {
                try
                {
                    sock.Connect(serverIp, serverPort);
                    Console.WriteLine("connected");
                    while (sock.Connected)
                    {
                        var buffer = new byte[7];
                        var i = sock.Receive(buffer,7,SocketFlags.None);
                        var key = buffer[3];
                        var keyFlag = BitConverter.ToInt16(buffer,5);
                        sendKey(key, keyFlag);
                        Console.WriteLine(buffer[0] + " " + buffer[1] + " " + buffer[2] + " " + buffer[3] + " " + buffer[4] + " " + buffer[5] + " " + buffer[6] + " --> "+ keyFlag);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to connect to server");

                    Console.WriteLine(e.Message);
                }
                
            }

        }
    }
}
