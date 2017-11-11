//Developed by Samkelo Ngubo 
//For More contact Samkelo at samke704@gmail.com
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PingAssessment
{
    class Program
    {
        static void Main(string[] args)
        {
          try
            {
              
                string address = string.Empty;

            if (args.Length == 0)
            {
                Console.WriteLine("Enter a host or IP address:");
                address = Console.ReadLine();
                Console.WriteLine();
            }
            else
            {
                address = args[0];
            }

            if (IsOffline())
            {
                Console.WriteLine("No internet connection detected.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
                return;
            }

            IPAddress ip = null;
            try
            {
                ip = Dns.GetHostEntry(address).AddressList[0];
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine("DNS Error: {0}", ex.Message);
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Pinging {0} [{1}] with 32 bytes of data:", address, ip.ToString());
            Console.WriteLine();
            Thread pingThread = new Thread(new ParameterizedThreadStart(AsyncStartPing));
            pingThread.Start(ip);
            pingThread.Join();
                Thread.Sleep(5000);
           
            }
            catch (Exception e)
            {
                // Will not catch it here!
                Console.WriteLine(e);
            }
       }

        [Flags]
        enum ConnectionState : int
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

        [DllImport("wininet", CharSet = CharSet.Auto)]
        static extern bool InternetGetConnectedState(ref ConnectionState lpdwFlags, int dwReserved);

        static bool IsOffline()
        {
            ConnectionState state = 0;
            InternetGetConnectedState(ref state, 0);
            if (((int)ConnectionState.INTERNET_CONNECTION_OFFLINE & (int)state) != 0)
            {
                return true;
            }

            return false;
        }


       public  static async void AsyncStartPing(object argument)
        {
            IPAddress ip = (IPAddress)argument;

           
            //set options ttl=128 and no fragmentation
            PingOptions options = new PingOptions(128, true);

            //create a Ping object
            Ping ping = new Ping();

            //32 empty bytes buffer
            byte[] data = new byte[32];

            int received = 0;
            List<long> responseTimes = new List<long>();
           
            //ping 4 times
            for (int i = 0; i < 4; i++)
            {
                PingReply reply = ping.Send(ip, 1000, data, options);

                if (reply != null)
                {
                    switch (reply.Status)
                    {
                        case IPStatus.Success:
                            Console.WriteLine("Reply from {0}: bytes={1} time={2}ms TTL={3}",
                                reply.Address, reply.Buffer.Length, reply.RoundtripTime, reply.Options.Ttl);
                            received++;
                            responseTimes.Add(reply.RoundtripTime);
                            break;
                        case IPStatus.TimedOut:
                            Console.WriteLine("Request timed out.");
                            break;
                        default:
                            Console.WriteLine("Ping failed {0}", reply.Status.ToString());
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Ping failed for an unknown reason");
                }
            }

            //statistics calculations
            long averageTime = -1;
            long minimumTime = 0;
            long maximumTime = 0;

            for (int i = 0; i < responseTimes.Count; i++)
            {
                if (i == 0)
                {
                    minimumTime = responseTimes[i];
                    maximumTime = responseTimes[i];
                }
                else
                {
                    if (responseTimes[i] > maximumTime)
                    {
                        maximumTime = responseTimes[i];
                    }
                    if (responseTimes[i] < minimumTime)
                    {
                        minimumTime = responseTimes[i];
                    }
                }
                averageTime += responseTimes[i];
            }

            StringBuilder statistics = new StringBuilder();
            statistics.AppendFormat("Ping statistics for {0}:", ip.ToString());
            statistics.AppendLine();
            statistics.AppendFormat("   Packets: Sent = 4, Received = {0}, Lost = {1} <{2}% loss>,",
                received, 4 - received, Convert.ToInt32(((4 - received) * 100) / 4));
            statistics.AppendLine();
            statistics.Append("Approximate round trip times in milli-seconds:");
            statistics.AppendLine();

            //show only if loss is not 100%
            if (averageTime != -1)
            {
                statistics.AppendFormat("    Minimum = {0}ms, Maximum = {1}ms, Average = {2}ms",
                    minimumTime, maximumTime, (long)(averageTime / received));
            }

            Console.WriteLine();
            Console.WriteLine(statistics.ToString());
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        
        }
        

    }
}
