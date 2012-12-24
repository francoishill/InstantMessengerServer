using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.IO;
using System.Threading;
using SharedClasses;

namespace InstantMessengerServer
{
    public class Client
    {
        public Client(Program p, TcpClient c)
        {
            prog = p;
            client = c;

            // Handle client in another thread.
            (new Thread(new ThreadStart(SetupConn))).Start();
        }

        Program prog;
        public TcpClient client;
        public NetworkStream netStream;  // Raw-data stream of connection.
        public SslStream ssl;            // Encrypts connection using SSL.
        public BinaryReader br;
        public BinaryWriter bw;

        UserInfo userInfo;  // Information about current user.
        
        void SetupConn()  // Setup connection and login or register.
        {
            try
            {
                Console.WriteLine("[{0}] New connection!", DateTime.Now);
                netStream = client.GetStream();
				ssl = new SslStream(netStream, false);
				ssl.AuthenticateAsServer(prog.cert, false, SslProtocols.Tls, true);
                Console.WriteLine("[{0}] Connection authenticated!", DateTime.Now);
                // Now we have encrypted connection.

                br = new BinaryReader(ssl/*netStream*/, Encoding.UTF8);
                bw = new BinaryWriter(ssl/*netStream*/, Encoding.UTF8);

                // Say "hello".
                bw.Write(InstantMessengerShared.IM_Hello);
                bw.Flush();
                int hello = br.ReadInt32();
				if (hello == InstantMessengerShared.IM_Hello)
				{
					// Hello packet is OK. Time to wait for login or register.
					byte logMode = br.ReadByte();
					string userName = br.ReadString();
					string password = br.ReadString();
					if (userName.Length <= 36) /*Guid length is 36*/ // Isn't username too long?
					{
						if (password.Length <= 36) /*Guid length is 36*/ // Isn't password too long?
						{
							if (logMode == InstantMessengerShared.IM_Register)  // Register mode
							{
								if (!prog.users.ContainsKey(userName))  // User already exists?
								{
									userInfo = new UserInfo(userName, password, this);
									prog.users.Add(userName, userInfo);  // Add new user
									bw.Write(InstantMessengerShared.IM_OK);
									bw.Flush();
									Console.WriteLine("[{0}] ({1}) Registered new user", DateTime.Now, userName);
									prog.SaveUsers();
									Receiver();  // Listen to client in loop.
								}
								else
									bw.Write(InstantMessengerShared.IM_Exists);
							}
							else if (logMode == InstantMessengerShared.IM_Login)  // Login mode
							{
								if (prog.users.TryGetValue(userName, out userInfo))  // User exists?
								{
									if (password == userInfo.Password)  // Is password OK?
									{
										// If user is logged in yet, disconnect him.
										if (userInfo.LoggedIn)
											userInfo.Connection.CloseConn();

										userInfo.Connection = this;
										bw.Write(InstantMessengerShared.IM_OK);
										bw.Flush();
										Receiver();  // Listen to client in loop.
									}
									else
										bw.Write(InstantMessengerShared.IM_WrongPass);
								}
								else
									bw.Write(InstantMessengerShared.IM_NoExists);
							}
						}
						else
							bw.Write(InstantMessengerShared.IM_TooPassword);
					}
					else
						bw.Write(InstantMessengerShared.IM_TooUsername);
				}
				else
				{
					int readnum = 0;
					byte[] buf = new byte[1];
					string responseheader = "";
					while ((readnum = ssl.Read(buf, 0, buf.Length)) > 0)
					{
						responseheader += Encoding.UTF8.GetString(buf);
						if (responseheader.EndsWith("\r\n\r\n"))
							break;
					}
					string r = responseheader;
					if (r != null)
					{
					}
				}
                CloseConn();
            }
            catch { CloseConn(); }
        }
        void CloseConn() // Close connection.
        {
            try
            {
                userInfo.LoggedIn = false;
                br.Close();
                bw.Close();
				ssl.Close();
                netStream.Close();
                client.Close();
                Console.WriteLine("[{0}] End of connection!", DateTime.Now);
            }
            catch { }
        }
        void Receiver()  // Receive all incoming packets.
        {
            Console.WriteLine("[{0}] ({1}) User logged in", DateTime.Now, userInfo.UserName);
            userInfo.LoggedIn = true;

            try
            {
                while (client.Client.Connected)  // While we are connected.
                {
                    byte type = br.ReadByte();  // Get incoming packet type.

                    if (type == InstantMessengerShared.IM_IsAvailable)
                    {
                        string who = br.ReadString();

                        bw.Write(InstantMessengerShared.IM_IsAvailable);
                        bw.Write(who);

                        UserInfo info;
                        if (prog.users.TryGetValue(who, out info))
                        {
                            if (info.LoggedIn)
                                bw.Write(true);   // Available
                            else
                                bw.Write(false);  // Unavailable
                        }
                        else
                            bw.Write(false);      // Unavailable
                        bw.Flush();
                    }
                    else if (type == InstantMessengerShared.IM_Send)
                    {
                        string to = br.ReadString();
                        string msg = br.ReadString();

                        UserInfo recipient;
                        if (prog.users.TryGetValue(to, out recipient))
                        {
                            // Is recipient logged in?
                            if (recipient.LoggedIn)
                            {
                                // Write received packet to recipient
                                recipient.Connection.bw.Write(InstantMessengerShared.IM_Received);
                                recipient.Connection.bw.Write(userInfo.UserName);  // From
                                recipient.Connection.bw.Write(msg);
                                recipient.Connection.bw.Flush();
                                Console.WriteLine("[{0}] ({1} -> {2}) Message sent!", DateTime.Now, userInfo.UserName, recipient.UserName);
                            }
                        }
                    }
					else if (type == InstantMessengerShared.IM_AskServer)
					{
						byte mes = br.ReadByte();
						if (mes == InstantMessengerShared.IM_GetLoggedInUsers)
						{
							bw.Write(InstantMessengerShared.IM_Received);
							bw.Write(InstantMessengerShared.IM_ServerUsername);
							bw.Write(
								InstantMessengerShared.IM_GetLoggedInUsers
								+ "|"
								+ string.Join("|", prog.users.Keys.Where(key => prog.users[key].LoggedIn)));
							bw.Flush();
						}
					}
                }
            }
            catch (IOException) { }

            userInfo.LoggedIn = false;
            Console.WriteLine("[{0}] ({1}) User logged out", DateTime.Now, userInfo.UserName);
        }
    }
}
