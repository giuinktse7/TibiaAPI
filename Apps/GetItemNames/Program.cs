using System;
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

        static string _tibiaDirectory = "<path to your tibia installation>/packages/Tibia";

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

                foreach (var (Id, _, Name) in objectInfoPacket.Objects)
                {
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

        static void Main(string[] args)
        {
            try
            {
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
