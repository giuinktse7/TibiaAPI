using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using OXGaming.TibiaAPI;
using OXGaming.TibiaAPI.Constants;
using OXGaming.TibiaAPI.Network.ClientPackets;
using OXGaming.TibiaAPI.Network.ServerPackets;

namespace DumpItems
{
    class Message
    {
        public byte[] Data { get; set; }

        public long Timestamp { get; set; }

        public PacketType Type { get; set; }
    }

    class Program
    {
        static Client _client;

        static string _tibiaDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/Tibia/packages/Tibia";

        public class GetObjectIdAndName : GetObjectInfo
        {

            public GetObjectIdAndName(Client client, ushort from, ushort to) : base(client)
            {
                Client = client;
                PacketType = ClientPacketType.GetObjectInfo;

                for (ushort i = from; i < to; ++i)
                {
                    Objects.Add((i, 0));
                }
            }
        }

        private static void SendItemNamesRequests()
        {

            const ushort MaxObjectsPerPacket = 255;

            /* Starts:
             * 100
             * 14670
             * 26947
             * 29942
             * 42049
             */
            ushort start = 100;
            ushort end = Convert.ToUInt16(_client.AppearanceStorage.LastObjectId + 1);

            ushort from = start;
            ushort to = Convert.ToUInt16(Math.Min(from + MaxObjectsPerPacket, end));

            Console.WriteLine("Last object ID: " + (end - 1));
            if (!File.Exists(@"./item_names.txt"))
            {
                File.WriteAllText(@"./item_names.txt", "");
            }


            _client.Connection.OnReceivedServerObjectInfoPacket += (packet) =>
            {
                StringBuilder sb = new StringBuilder();

                var objectInfoPacket = (ObjectInfo)packet;

                Console.WriteLine($"Received ids {from}-{to}");

                foreach (var (Id, Data, Name) in objectInfoPacket.Objects)
                {
                    if (Data != 0)
                    {
                        Console.WriteLine(Data);
                    }
                    if (!string.IsNullOrEmpty(Name))
                    {
                        sb.AppendLine($"[{Id}] {Name}");
                    }
                }

                System.IO.File.AppendAllText(@"./item_names.txt", sb.ToString());

                if (to != end)
                {
                    from = to;
                    to = Convert.ToUInt16(Math.Min(from + MaxObjectsPerPacket, end));

                    Console.WriteLine($"Sending request for {from}-{to}");
                    var nextPacket = new GetObjectIdAndName(_client, from, to);
                    _client.Connection.SendToServer(nextPacket);
                }

                return true;
            };

            var firstPacket = new GetObjectIdAndName(_client, from, to);
            _client.Connection.SendToServer(firstPacket);
        }


        public class GetCreatureIdAndName : LookAtCreature
        {
            public GetCreatureIdAndName(Client client, uint id) : base(client)
            {
                Client = client;
                PacketType = ClientPacketType.LookAtCreature;
                CreatureId = id;

            }
        }


        private static void SendCreatureLookRequests()
        {
            StringBuilder sb = new StringBuilder();

            ushort from = 0;
            ushort to = 1000;


            _client.Connection.OnReceivedServerMessagePacket += (packet) =>
            {
                var creatureLookPacket = (OXGaming.TibiaAPI.Network.ServerPackets.Message)packet;

                if (creatureLookPacket.MessageMode == MessageModeType.Look)
                {
                    Console.WriteLine($"Received message: {creatureLookPacket.Text}");
                }

                if (!string.IsNullOrEmpty(creatureLookPacket.Text))
                {
                    sb.AppendLine($"[{to}] {creatureLookPacket.Text}");
                }

                if (to != 1000)
                {
                    from = to;
                    to += 1;

                    var nextPacket = new GetCreatureIdAndName(_client, to);
                    _client.Connection.SendToServer(nextPacket);
                }
                else
                {
                    System.IO.File.WriteAllText(@"./creature_names.txt", sb.ToString());
                    Console.WriteLine("Done. Creature names have been written to ./creature_names.txt.");
                }

                return true;
            };


            var firstPacket = new GetCreatureIdAndName(_client, to);
            _client.Connection.SendToServer(firstPacket);
        }

        public static int IndexOfSequence(byte[] buffer, byte[] pattern, int startIndex)
        {
            int i = Array.IndexOf<byte>(buffer, pattern[0], startIndex);
            while (i >= 0 && i <= buffer.Length - pattern.Length)
            {
                byte[] segment = new byte[pattern.Length];
                Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);
                if (segment.SequenceEqual<byte>(pattern))
                {
                    return i;
                }

                i = Array.IndexOf<byte>(buffer, pattern[0], i + 1);
            }

            return -1;
        }

        static void CreateTibiaApiClient(byte[] originalClientData, string originalClientPath, string tibiaApiClientPath)
        {
            const string TIBIA_RSA = "BC27F992A96B8E2A43F4DFBE1CEF8FD51CF43D2803EE34FBBD8634D8B4FA32F7D9D9E159978DD29156D62F4153E9C5914263FC4986797E12245C1A6C4531EFE48A6F7C2EFFFFF18F2C9E1C504031F3E4A2C788EE96618FFFCEC2C3E5BFAFAF743B3FC7A872EE60A52C29AA688BDAF8692305312882F1F66EE9D8AEB7F84B1949";
            const string OT_RSA = "9B646903B45B07AC956568D87353BD7165139DD7940703B03E6DD079399661B4A837AA60561D7CCB9452FA0080594909882AB5BCA58A1A1B35F8B1059B72B1212611C6152AD3DBB3CFBEE7ADC142A75D3D75971509C321C5C24A5BD51FD460F01B4E15BEB0DE1930528A5D3F15C1E3CBF5C401D6777E10ACAAB33DBE8D5B7FF5";

            const string tibiaLoginService = "loginWebService=https://www.tibia.com/clientservices/loginservice.php";
            const string tibiaAPILoginService = "loginWebService=http://127.0.0.1:7171/                               ";

            var tibiaRsaIndex = IndexOfSequence(originalClientData, Encoding.ASCII.GetBytes(TIBIA_RSA), 0);

            bool clientHasTibiaRsa = tibiaRsaIndex != -1;
            if (!clientHasTibiaRsa)
            {
                Console.WriteLine($"The client at {originalClientPath} was already configured to use a different RSA than the Tibia RSA.");
                return;
            }

            // TODO Can be made more efficient. Currently the whole client buffer is copied twice
            var exeWithOtRsa = Util.ReplaceBytes(originalClientData, Encoding.ASCII.GetBytes(TIBIA_RSA), Encoding.ASCII.GetBytes(OT_RSA));
            var exeWithLoginWebService = Util.ReplaceBytes(exeWithOtRsa, Encoding.ASCII.GetBytes(tibiaLoginService), Encoding.ASCII.GetBytes(tibiaAPILoginService));
            File.WriteAllBytes(tibiaApiClientPath, exeWithLoginWebService);
        }

        // tibiaDirectory: "<Tibia Location>/packages/Tibia"
        static void updateClient(string tibiaDirectory)
        {
            var originalClientPath = tibiaDirectory + "/bin/client.exe";
            var tibiaApiClientPath = tibiaDirectory + "/bin/client_tibiaapi.exe";

            var exe = File.ReadAllBytes(originalClientPath);
            if (File.Exists(tibiaApiClientPath))
            {
                var tibiaApiExe = File.ReadAllBytes(tibiaApiClientPath);
                bool updateTibiaApiClient = exe.Length != tibiaApiExe.Length;
                if (updateTibiaApiClient)
                {
                    Console.WriteLine($"Your TibiaAPI client at {tibiaApiClientPath} seems to be outdated. Do you want to update it? (The original Tibia client will be preserved as {originalClientPath})? y/N");
                    var response = Console.ReadLine();
                    if (response.ToLower() == "y" || response.ToLower() == "yes")
                    {
                        CreateTibiaApiClient(exe, originalClientPath, tibiaApiClientPath);
                        Console.WriteLine($"The client at {tibiaApiClientPath} has been updated to work with TibiaAPI.");
                    }
                }
                else
                {
                    Console.WriteLine($"Found valid TibiaAPI client at \"{tibiaApiClientPath}\".");
                }
            }
            else
            {
                Console.WriteLine($"Do you want to create a client configured for TibiaAPI at {tibiaApiClientPath}? (The original client will be preserved as {originalClientPath})? y/N");
                var response = Console.ReadLine();
                if (response.ToLower() == "y" || response.ToLower() == "yes")
                {
                    CreateTibiaApiClient(exe, originalClientPath, tibiaApiClientPath);
                    Console.WriteLine($"Created valid TibiaAPI client at {tibiaApiClientPath}");
                }

            }
        }


        static void Main(string[] args)
        {
            try
            {
                updateClient(_tibiaDirectory);

                var tibiaApiClientPath = _tibiaDirectory + "/bin/client_tibiaapi.exe";

                using (_client = new Client(_tibiaDirectory))
                {
                    _client.Connection.IsClientPacketParsingEnabled = true;
                    _client.Connection.IsServerPacketParsingEnabled = true;
                    _client.Connection.IsServerPacketModificationEnabled = false;

                    _client.serverMessageParseFilter.Add(ServerPacketType.ObjectInfo);
                    _client.StartConnection();

                    Console.WriteLine($@"Usage:
    1. Start this program (already done).
    2. Login to a Tibia server that is not protected by Battleye (Zuna/Zunera) using the client at ""{tibiaApiClientPath}"".
    3. Write 'send' or 'mon' in this terminal once your character is online.
                    ");

                    bool exit = false;
                    while (!exit)
                    {
                        var input = Console.ReadLine();
                        switch (input)
                        {
                            case "send":
                                SendItemNamesRequests();
                                break;
                            case "mon":
                                SendCreatureLookRequests();
                            case "quit":
                                exit = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Shutdown();
            }
        }


        private static void Shutdown()
        {
            if (_client != null)
            {
                _client.StopConnection();
            }
        }
    }
}
