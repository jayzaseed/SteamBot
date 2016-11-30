using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Net;


using SteamKit2;
using SteamKit2.Internal;

using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.Unified;
using SteamKit2.Unified.Internal;


using SteamKit2.GC.CSGO;
using SteamKit2.GC.CSGO.Internal;

namespace Steam_Friend_Bot
{
    
    class Program
    {
        
        public ulong IDSteam { get; set; }
        static SteamClient steamClient;
        static CallbackManager manager;
        static SteamUser steamUser;
        static SteamFriends steamFriends;
        static SteamGameCoordinator gameCoordinator;


        static bool isRunning = false;

        static string user;
        static string pass;
        static string authcode;
        static string twofactor;

        static uint matchID;

        bool gotMatch;

        const int CSGOID = 730;

        public CDataGCCStrike15_v2_MatchInfo match { get; private set; }

        public Program(ulong steamid)
        {
            this.IDSteam = steamid;
        }

        static void Main(string[] args)
        {
            if(Internet() != true)
            {
                Console.WriteLine("[-] No Internet Connection.");
                Console.Read();
                return;
            }
            else
            {
                Console.WriteLine("[+] Internet connection stable.");
                
            }
            
            if (!File.Exists("chat.txt"))
            {
                File.Create("chat.txt").Close();
                File.WriteAllText("chat.txt", "penis | You dont have one!");

            }
            Console.Title = "Steam Bot";

            Console.Write("Username: ");
            user = Console.ReadLine();

            Console.Write("Password: ");
            pass = inputPass();
            Console.Clear();
            #region Ascii
           
            #endregion

            SteamLogin();
             

        }

        static string inputPass()
        {
            string pass = "";
            bool protectPass = true;
            while (protectPass)
            {
                char s = Console.ReadKey(true).KeyChar;
                if (s == '\r')
                {
                    protectPass = false;
                    Console.WriteLine();
                }
                else if (s == '\b' && pass.Length > 0)
                {
                    Console.CursorLeft -= 1;
                    Console.Write(' ');
                    Console.CursorLeft -= 1;
                    pass = pass.Substring(0, pass.Length - 1);
                }
                else
                {
                    pass = pass + s.ToString();
                    Console.Write("*");
                }

            }
            return pass;
        }

        static void SteamLogin()
        {

            matchID = matchID;
            steamClient = new SteamClient();

            manager = new CallbackManager(steamClient);

            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            gameCoordinator = steamClient.GetHandler<SteamGameCoordinator>();

            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            manager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
            manager.Subscribe<SteamFriends.FriendMsgCallback>(OnChatMessage);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            manager.Subscribe<SteamFriends.FriendAddedCallback>(OnFriendAdded);
            manager.Subscribe<SteamFriends.FriendsListCallback>(OnFriendsList);
            manager.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);
           

            isRunning = true;

            steamClient.Connect();

            while (isRunning)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            Console.ReadKey();
        }

        static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("[-] Unable to connect to steam => {0}", callback.Result);
                isRunning = false;
                return;
            }
            Console.WriteLine("\n[i] Connected to Steam!\n[i]Logging in with account {0}", user);

            byte[] sentryHash = null;

            if (File.Exists("sentry.bin"))
            {
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");

                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = user,
                Password = pass,
                AuthCode = authcode,
                TwoFactorCode = twofactor,
                SentryFileHash = sentryHash,
            });
        }

        static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
           

            if (callback.Result == EResult.AccountLogonDenied)
            {
                Console.WriteLine("\n[-] The account {0} is Steam Guard protected", user);

                Console.Write("[i] Please enter your Steam Guard Key sent to an email at {0}: ", callback.EmailDomain);

                authcode = Console.ReadLine();

                return;
            }

            if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                Console.WriteLine("\n[-] The account {0} uses a TwoFactor Auth Code", user);
                Console.Write("[i] Please enter your TwoFactor Auth Code: ");

                twofactor = Console.ReadLine();
                return;
            }

            if (callback.Result == EResult.TwoFactorCodeMismatch)
            {
                Console.WriteLine("\n[-] Wrong TwoFactor Code.");
                isRunning = false;
                return;
            }
            if (callback.Result == EResult.InvalidPassword)
            {
                Console.WriteLine("[-] Wrong password.");
                isRunning = false;
                return;
            }

            
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("[-] Unable to connect to Steam => {0}", callback.Result);
                isRunning = false;
                return;
            }
            Console.WriteLine("[+] Logged in!");
            Console.WriteLine("\n[i] Logged in with localIP => " + steamClient.LocalIP);
            Console.WriteLine("[i] Current SessionID => " + steamClient.SessionID);
            Console.WriteLine("[i] Current Session Token => " + steamClient.SessionToken);
            Console.WriteLine("[i] SteamID: " + steamClient.SteamID + "\n");

        }

        static void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("[i] Updating Sentry File...");

            byte[] sentryHash = CryptoHelper.SHAHash(callback.Data);

            File.WriteAllBytes("sentry.bin", callback.Data);

            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,
                FileName = callback.FileName,
                BytesWritten = callback.BytesToWrite,
                FileSize = callback.Data.Length,
                Offset = callback.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = callback.OneTimePassword,
                SentryFileHash = sentryHash,
                
            });
            Console.WriteLine("> Done!");
        }

        static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("\n[i] {0} is reconnecting to Steam!\n[i] This is normal!", user);
            Thread.Sleep(TimeSpan.FromSeconds(5));

            steamClient.Connect();

        }

        static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            steamFriends.SetPersonaState(EPersonaState.Online); 

        }

        static void OnChatMessage(SteamFriends.FriendMsgCallback callback)
        {
            string[] args;

            if(callback.EntryType == EChatEntryType.ChatMsg)
            {
                if (callback.Message.Length > 1) 
                {
                    if(callback.Message.Remove(1) == "!")
                    {
                        string command = callback.Message;
                        if(callback.Message.Contains(" "))
                        {
                            command = callback.Message.Remove(callback.Message.IndexOf(' '));
                            
                        }

                        switch(command)
                        {
                            case "!coder":
                                args = seperate(0, ' ',callback.Message );
                                Console.WriteLine("[i] Your Steamfriend {0} has used the command !coder", steamFriends.GetFriendPersonaName(callback.Sender));
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Coder: Logan.");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Coded in: C#");
                                
                            break;
                            #region Clear
                            case "!clear":
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, " ");
                                break;
                            #endregion
                            case "!send":
                                args = seperate(2, ' ', callback.Message);
                               
                                if(args[0] == "-1")
                                {
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Correct Syntax: !send [Friend] [message]");
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Please write the name in lower cases!");
                                    return;
                                }
                                Console.WriteLine("[i] Your Steamfriend {0} has orderd you to send " + args[1] + " " + args[2] + " !", steamFriends.GetFriendPersonaName(callback.Sender));
                                for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                                {
                                    SteamID friend = steamFriends.GetFriendByIndex(i);
                                    if(steamFriends.GetFriendPersonaName(friend).ToLower().Contains(args[1]))
                                    {
                                        steamFriends.SendChatMessage(friend, EChatEntryType.ChatMsg, args[2]);
                                    }
                                }
                            break;

                            case "!help":
                                Console.WriteLine("[i] Your Steamfriend {0} has used the Command !help", steamFriends.GetFriendPersonaName(callback.Sender));
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Welcome friend. So you need help? Type !ger for german help or !eng for english help.");
                            break;

                            case "!ger":
                                Console.WriteLine("[i] Your Steamfriend {0} has used the Command !ger", steamFriends.GetFriendPersonaName(callback.Sender));
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Hallo, Du hast die ehre mit dem wundervollen Steambot von mir zu schreiben! Achtung: Ich bin KEIN Tradebot. Ich mach diesen BOT an wenn ich den Account gerade nicht benutze / AFK bin. Also kannst du dich mit ihm unterhalten :) Benutze !commands für eine volle commands liste. Viel Spaß :)");
                            break;

                            case "!eng":
                                Console.WriteLine("[i] Your Steamfriend {0} has used the Command !eng", steamFriends.GetFriendPersonaName(callback.Sender));
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Nothing to see here. Wait for an Update! :)");
                            break;

                            case "!steamID":
                                Console.WriteLine("[i] Your Steamfriend {0} has used the Command !steamID", steamFriends.GetFriendPersonaName(callback.Sender));
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Your SteamID is: " + callback.Sender);

                            break;
                            case "!friends":
                                Console.WriteLine("[i] Your Steamfriend {0} has used !friends", steamFriends.GetFriendPersonaName(callback.Sender));
                                for(int i = 0; i < steamFriends.GetFriendCount(); i++)
                                {
                                    SteamID friend = steamFriends.GetFriendByIndex(i);
                                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Friend: " + steamFriends.GetFriendPersonaName(friend) + " State: " + steamFriends.GetFriendPersonaState(friend));
                                   
                                }
                            break;
                            case "!music":
                                Console.WriteLine("[i] Your Steamfriend {0} has used !music", steamFriends.GetFriendPersonaName(callback.Sender));
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "The music I often listen to:");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "https://www.youtube.com/watch?v=GSWJ21L4SfQ");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "https://www.youtube.com/watch?v=O81P0dWo6yg");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "https://www.youtube.com/watch?v=cEU1NV3efDo");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "https://www.youtube.com/watch?v=09wdQP1FFR0");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "https://www.youtube.com/watch?v=_y8p6uQDH4s");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "https://www.youtube.com/watch?v=pVLmZMjxfjw");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Stay tuned for updates :)");
                                
                            break;
                            case "!teamspeak":
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "XXXXXXXXX");
                            break;
                            case "!imp":
                                args = seperate(1, ' ', callback.Message);
                                for (int i = 0; i < steamFriends.GetFriendCount(); i++) 
                                {
                                    
                                    SteamID friend = steamFriends.GetFriendByIndex(i); 
                                    if (args[0] == "-1")
                                    {
                                        steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Correct Syntax: !imp [message]"); 
                                        return;
                                    }
                                    if (steamFriends.GetFriendPersonaName(friend).Contains("Logan"))
                                    {

                                        
                                        steamFriends.SendChatMessage(friend, EChatEntryType.ChatMsg, "Important message from: " + steamFriends.GetFriendPersonaName(callback.Sender) + ": " +  args[1]);
                                        Console.Beep();
                                       
                                        
                                    }
                                    

                                }
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Message sent succesfully!");
                                Console.WriteLine("\n\n==============================================================");
                                Console.WriteLine("IMPORTANT: You have recieved an IMPORTANT message from {0}", steamFriends.GetFriendPersonaName(callback.Sender));
                                Console.WriteLine("{0}: " + args[1], steamFriends.GetFriendPersonaName(callback.Sender));
                                Console.WriteLine("[i] End of message.");
                                Console.WriteLine("==============================================================\n");
                                break;
                            case "!commands":
                                Console.WriteLine("[i] I listed some commands to {0}", steamFriends.GetFriendPersonaName(callback.Sender));
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Commandlist:");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!help - For more help");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!ger - Help in german");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!eng - Help in english");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!steamID - Get your SteamID");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!friends - Get my Friendslist");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!music - Get my fav. music atm");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!coder - Get the coder of this bot");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!send - Send any friend of mine a message");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!teamspeak - Get my teamspeak IP");
                                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "!commands - Get the full command list");
                            break;
                            case "!rename":
                                args = seperate(1, ' ', callback.Message);

                                steamFriends.SetPersonaName(args[1]);
                                break;
                            case "!news":
                                News();

                                break;
                            case "!dev":
                                ClientMsgProtobuf<CMsgClientGamesPlayed> msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
                                CMsgClientGamesPlayed.GamePlayed item = new CMsgClientGamesPlayed.GamePlayed
                                {
                                    game_id = (ulong)new GameID(730)
                                };
                                msg.Body.games_played.Add(item);
                                steamClient.Send(msg);
                                Thread.Sleep(5000);
                                ClientGCMsgProtobuf<CMsgClientHello> Servus = new ClientGCMsgProtobuf<CMsgClientHello>(4006, 64);
                                gameCoordinator.Send(Servus, 730);
                                break;
                        }
                    }

                    string rLine;
                    string trimmed = callback.Message;
                    char[] trim = { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '_', '=', '+', '[', ']', '{', '}', '\\', '|', ';', ':', '"', '\'', ',', '<', '.', '>', '/', '?' };
                    for(int i = 0; i < 30; i++)
                    {
                        trimmed = trimmed.Replace(trim[i].ToString(), "");
                    }
                    StreamReader sReader = new StreamReader("chat.txt");
                    while((rLine = sReader.ReadLine()) != null)
                    {
                        string text = rLine.Remove(rLine.IndexOf('|') - 1);
                        string response = rLine.Remove(0, rLine.IndexOf('|') + 2);

                        if(callback.Message.Contains(text))
                        {
                            steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, response);
                            sReader.Close();
                            return;
                        }
                    }
                }


            }
            
        }

        public static string[] seperate(int number,char seperator, string thestring)
        {
            string[] returned = new string[5];

            int i = 0;

            int error = 0;

            int lenght = thestring.Length;

            foreach (char c in thestring)
            {
                if (i != number)
                {
                    if (error > lenght || number > 5)
                    {
                        returned[0] = "-1";
                        return returned;
                    }
                    else if (c == seperator)
                    {

                        returned[i] = thestring.Remove(thestring.IndexOf(c));
                        thestring = thestring.Remove(0, thestring.IndexOf(c) + 1);
                        i++;
                    }
                    error++;
                    if (error == lenght && i != number)
                    {
                        returned[0] = "-1";
                        return returned;

                    }
                }
                else
                {
                    returned[i] = thestring;
                }


            }
        return returned;
        }

        static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        { 
           Console.WriteLine( "WARNING: Logged of of Steam: {0}", callback.Result ); 
        }

        static void OnFriendsList(SteamFriends.FriendsListCallback callback)
        {
            
            Thread.Sleep(TimeSpan.FromSeconds(5));
            if (steamFriends.GetPersonaState() == EPersonaState.Online)
            {
                for (int i = 0; i < steamFriends.GetFriendCount(); i++)
                {
                    SteamID friend = steamFriends.GetFriendByIndex(i);
                    Console.WriteLine("Friend: " + steamFriends.GetFriendPersonaName(friend) + " State: " + steamFriends.GetFriendPersonaState(friend));
                    
                    
                }
            }


            Thread.Sleep(TimeSpan.FromSeconds(2));
            Console.WriteLine("\n");





        }

        static void OnFriendAdded(SteamFriends.FriendAddedCallback callback)
        {
            
            Console.WriteLine("[i] {0} is now a friend", callback.PersonaName);
        }

        static void News()
        {
            using (dynamic steamNews = WebAPI.GetInterface("ISteamNews"))
            {
                KeyValue kvNews = steamNews.GetNewsForApp(appid: 730);

                foreach (KeyValue news in kvNews["newsitems"]["newsitem"].Children)
                {
                    Console.WriteLine("News: {0}", news["title"].AsString());
                    
                }

            }
        }

        public static bool Internet()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://www.google.com"))
                    {
                        
                        return true;
                    }
                }
            }
            catch
            {

                return false;
            }
        }



      

       static void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            Console.WriteLine("EMSG: " + callback.EMsg.ToString());

            switch(callback.EMsg)
            {
                case 4004: // GC Welcome
                    new ClientGCMsgProtobuf<CMsgClientWelcome>(callback.Message);
                    Console.WriteLine("> GC sagt hallo.");

                    JoiningTheLobby();

                    break;
                case 6604:
                    new ClientGCMsgProtobuf<CMsgClientMMSJoinLobby>(callback.Message);
                    Console.WriteLine("> Hello world.");
                    break;
                case 9110:
                    
                    Console.WriteLine($"EMSG Response{callback.Message}");
                    break;

            }


        }

        static void JoiningTheLobby()
        {
            ClientMsgProtobuf<CMsgClientMMSJoinLobby> join = new ClientMsgProtobuf<CMsgClientMMSJoinLobby>(EMsg.ClientMMSJoinLobby);

            join.ProtoHeader.routing_appid = 730;
            join.Body.app_id = 730;
            join.Body.persona_name = "AMK";
            join.Body.steam_id_lobby = (ulong)109775243754032135;
            steamClient.Send(join);
            Thread.Sleep(5000);

        }

    }
}