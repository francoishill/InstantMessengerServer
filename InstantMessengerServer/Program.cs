using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

/*
 * Code obtained from:
 * http://www.codeproject.com/Articles/429144/Simple-Instant-Messenger-with-SSL-Encryption-in-Cs
 */

namespace InstantMessengerServer
{
    public class Program
    {
		[STAThread]
        static void Main(string[] args)
        {
			SharedClasses.AutoUpdating.CheckForUpdates_ExceptionHandler();

			//try
			//{
				Program p = new Program();
				Console.WriteLine();
				Console.WriteLine("Press enter to close program.");
				Console.ReadLine();
			//}
			//catch (Exception exc)
			//{
			//    Console.WriteLine("Error: " + exc.Message + Environment.NewLine + "Stacktrace: " + exc.StackTrace);
			//    Console.ReadLine();
			//}
        }

        // Self-signed certificate for SSL encryption.
        // You can generate one using my generate_cert script in tools directory (OpenSSL is required).
        public X509Certificate2 cert = new X509Certificate2("server.pfx", "instant");

        // IP of this computer. If you are running all clients at the same computer you can use 127.0.0.1 (localhost). 
		public IPAddress ip = GetIPAddress();//IPAddress.Parse("127.0.0.1");
		public int port = 443;//9002;//80;//2000;
        public bool running = true;
        public TcpListener server;

        public Dictionary<string, UserInfo> users = new Dictionary<string, UserInfo>();  // Information about users + connections info.

        public Program()
        {
            Console.Title = "InstantMessenger Server";
            Console.WriteLine("----- InstantMessenger Server -----");
            LoadUsers();
            Console.WriteLine("[{0}] Starting server on {1}:{2}...", DateTime.Now, ip.ToString(), port);

            server = new TcpListener(ip, port);
            server.Start();
            Console.WriteLine("[{0}] Server is running properly!", DateTime.Now);
            
            Listen();
        }

        void Listen()  // Listen to incoming connections.
        {
            while (running)
            {
                TcpClient tcpClient = server.AcceptTcpClient();  // Accept incoming connection.
				Console.WriteLine("Accepted tcp client, trying to connect");
                Client client = new Client(this, tcpClient);     // Handle in another thread.
            }
        }

		public static IPAddress GetIPAddress()
		{
			return Dns.GetHostEntry(Environment.MachineName).AddressList.First(ad => ad.AddressFamily == AddressFamily.InterNetwork);
		}

		/*private delegate IPHostEntry GetHostEntryHandler(string ip);
		public static IPAddress GetIPAddressFromString(string ipAddressString, int timeout = 10000)
	{
		bool resolveDnsMode = false;
		foreach (char chr in ipAddressString)
			if (!char.IsNumber(chr) && chr != '.')
				resolveDnsMode = true;
		IPAddress returnIPAddress = null;

		if (!resolveDnsMode && !IPAddress.TryParse(ipAddressString, out returnIPAddress))
		{
			Console.WriteLine("Invalid IP address: " + (ipAddressString ?? ""));
			return null;
		}
		if (resolveDnsMode)
		{
			try
			{
				GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
				IAsyncResult result = callback.BeginInvoke(ipAddressString, null, null);
				if (result.AsyncWaitHandle.WaitOne(timeout, false))
				{
					IPHostEntry iphostEntry = callback.EndInvoke(result);
					if (iphostEntry == null || iphostEntry.AddressList.Length == 0)
					{
						Console.WriteLine("Could not resolve DNS from " + ipAddressString);
						return null;
					}
					else returnIPAddress = iphostEntry.AddressList[0];
				}
				else
				{
					Console.WriteLine("Timeout to resolve DNS from " + ipAddressString);
					return null;
				}
			}
			catch (Exception exc)
			{
				Console.WriteLine("Error occurred resolving DNS from " + ipAddressString + ": " + exc.Message);
				return null;
			}
		}
		return returnIPAddress;
	}*/

        string usersFileName = Environment.CurrentDirectory + "\\users.dat";
        public void SaveUsers()  // Save users data
        {
            try
            {
                Console.WriteLine("[{0}] Saving users...", DateTime.Now);
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = new FileStream(usersFileName, FileMode.Create, FileAccess.Write);
                bf.Serialize(file, users.Values.ToArray());  // Serialize UserInfo array
                file.Close();
                Console.WriteLine("[{0}] Users saved!", DateTime.Now);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void LoadUsers()  // Load users data
        {
            try
            {
                Console.WriteLine("[{0}] Loading users...", DateTime.Now);
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = new FileStream(usersFileName, FileMode.Open, FileAccess.Read);
                UserInfo[] infos = (UserInfo[])bf.Deserialize(file);      // Deserialize UserInfo array
                file.Close();
                users = infos.ToDictionary((u) => u.UserName, (u) => u);  // Convert UserInfo array to Dictionary
                Console.WriteLine("[{0}] Users loaded! ({1})", DateTime.Now, users.Count);
            }
            catch { }
        }
    }
}
