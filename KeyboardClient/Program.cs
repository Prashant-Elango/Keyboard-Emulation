using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace KeyboardClient
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk,byte bScan,uint dwFlags,int dwExtraInfo);

        static void sendKey(byte bvk,short keyFlag)
        {
         
            if(keyFlag == 0x0100)
            {
                //Key Press
                keybd_event(bvk, 0, 0x0001, 0);
            }
            else
            {
                //Key Release
                keybd_event(bvk, 0, 0x0002, 0);

            }

        }
        static void Main(string[] args)
        {
            if(args.Length == 1)
            {
                if(args[0] == "\\h" || args[0] == "-h" || args[0] == "--h" || args[0] == "help")
                {
                    Console.WriteLine("Keyboard Client");
                    Console.WriteLine("         -c           sets keyboardServer Ip address");
                    Console.WriteLine("         -p           sets port");
                    Console.WriteLine("Usages:");
                    Console.WriteLine("     ex: keyboardClient.exe -c 192.168.1.10 -p 5555");
                    Console.WriteLine("     ex: keyboardClient.exe -c 192.168.1.10");
                }
                Environment.Exit(0);
            }
            var sock = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            var serverIp = "";
            var serverPort = 0;
            if(args.Length == 4)
            {
                if(args[0] == "-c")
                {
                    serverIp = args[1];
                }
                if(args[2] == "-p")
                {
                    try
                    {
                        serverPort = int.Parse(args[3]);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Invalid port");
                    }
                }
            }
            else if(args.Length == 2)
            {
                if(args[0] == "-c")
                {
                    serverIp = args[1];
                    serverPort = 9022;
                }
                else
                {
                    Console.WriteLine("Use -c to specify Server IP");
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
