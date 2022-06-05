using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        static string _tibiaDirectory = "D:/Programs/TibiaLatest/packages/Tibia";

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
            StringBuilder sb = new StringBuilder();

            const ushort MaxObjectsPerPacket = 255;

            ushort start = 100;
            ushort end = Convert.ToUInt16(_client.AppearanceStorage.LastObjectId + 1);

            ushort from = start;
            ushort to = Convert.ToUInt16(Math.Min(from + MaxObjectsPerPacket, end));

            Console.WriteLine("Last object ID: " + (end - 1));

            _client.Connection.OnReceivedServerObjectInfoPacket += (packet) =>
            {
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

                if (to != end)
                {
                    from = to;
                    to = Convert.ToUInt16(Math.Min(from + MaxObjectsPerPacket, end));

                    var nextPacket = new GetObjectIdAndName(_client, from, to);
                    _client.Connection.SendToServer(nextPacket);
                }
                else
                {
                    System.IO.File.WriteAllText(@"./item_names.txt", sb.ToString());
                    Console.WriteLine("Done. Item names have been written to ./item_names.txt.");
                }

                return true;
            };

            var firstPacket = new GetObjectIdAndName(_client, from, to);
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
                    return i;
                i = Array.IndexOf<byte>(buffer, pattern[0], i + 1);
            }
            return -1;

            
        }

        static void Main(string[] args)
        {
            try
            {
                /*
                 * TODO Simplify the process of changing the .exe using below code:
                var exeDirectory = _tibiaDirectory + "/bin/client.exe";
                var exe = File.ReadAllBytes(exeDirectory);
                string TIBIA_RSA = "BC27F992A96B8E2A43F4DFBE1CEF8FD51CF43D2803EE34FBBD8634D8B4FA32F7D9D9E159978DD29156D62F4153E9C5914263FC4986797E12245C1A6C4531EFE48A6F7C2EFFFFF18F2C9E1C504031F3E4A2C788EE96618FFFCEC2C3E5BFAFAF743B3FC7A872EE60A52C29AA688BDAF8692305312882F1F66EE9D8AEB7F84B1949";
                string OT_RSA = "9B646903B45B07AC956568D87353BD7165139DD7940703B03E6DD079399661B4A837AA60561D7CCB9452FA0080594909882AB5BCA58A1A1B35F8B1059B72B1212611C6152AD3DBB3CFBEE7ADC142A75D3D75971509C321C5C24A5BD51FD460F01B4E15BEB0DE1930528A5D3F15C1E3CBF5C401D6777E10ACAAB33DBE8D5B7FF5";
                var tibiaRsaIndex = IndexOfSequence(exe, Encoding.ASCII.GetBytes(TIBIA_RSA), 0);

                if (tibiaRsaIndex != -1)
                {
                    var originalExe = _tibiaDirectory + "/bin/client_original.exe";
                    Console.WriteLine($"The tibia client at {exeDirectory} is not configured for proxy through GetItemNames. Configure it (The original client will be preserved as {originalExe})? y/N");
                    var response = Console.ReadLine();
                    if (response.ToLower() == "y" || response.ToLower() == "yes")
                    {
                        File.Copy(exeDirectory, originalExe);

                        var otRsaBytes = Encoding.ASCII.GetBytes(OT_RSA);
                        GCHandle pinnedArray = GCHandle.Alloc(otRsaBytes, GCHandleType.Pinned);
                        void* otPtr = pinnedArray.AddrOfPinnedObject();
                        Buffer.MemoryCopy(&otRsaBytes, exe, otRsaBytes.Length, otRsaBytes.Length);

                        pinnedArray.Free();
                    }
                }
                */
                
                using (_client = new Client(_tibiaDirectory))
                {
                    _client.Connection.IsServerPacketModificationEnabled = false;
                    _client.serverMessageParseFilter.Add(ServerPacketType.ObjectInfo);
                    _client.StartConnection();

                    Console.WriteLine(@"Usage:
    1. Start this program (already done).
    2. Login to a Tibia server that is not protected by Battleye (Zuna/Zunera)
    3. Write 'send' in this terminal once your character is online.
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
