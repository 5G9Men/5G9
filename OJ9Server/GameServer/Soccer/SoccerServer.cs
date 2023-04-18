﻿using System.Net;
using System.Net.Sockets;
using System.Numerics;

public class SoccerServer : GameServer
{
    private enum Turn
    {
        A,
        B
    }
    private struct Room
    {
        public Client clientA, clientB;
        public int elapsedTime = 0;

        public Room()
        {
            clientA = default;
            clientB = default;
            elapsedTime = 0;
        }

        public Room(Client _clientA, Client _clientB)
        {
            clientA = _clientA;
            clientB = _clientB;
        }

        public void AddPlayer(Client _client)
        {
            // TODO
            //throw new FormatException("Room is full");
        }
    }

    private readonly Dictionary<int /* =roomNumber */, Room> rooms = new Dictionary<int, Room>();

    public SoccerServer()
    {
        gameType = GameType.Soccer;
    }

    public override void Start()
    {
        Console.WriteLine("Listening Starts");

        lobbyListener = new UdpClient(OJ9Const.SOCCER_SERVER_PORT_NUM);
        lobbyListener.BeginReceive(LobbyPacketReceived, null);
    }

    private void LobbyPacketReceived(IAsyncResult _asyncResult)
    {
        IPEndPoint ipEndPoint = null;
        var buffer = lobbyListener.EndReceive(_asyncResult, ref ipEndPoint);
        var packetBase = OJ9Function.ByteArrayToObject<PacketBase>(buffer);
        switch (packetBase.packetType)
        {
            case PacketType.Matched:
            {
                B2GGameMatched packet = OJ9Function.ByteArrayToObject<B2GGameMatched>(buffer);
                // TODO : Connect to clients
                //Socket firstSocket = new Socket(packet.first.ipEndPoint);
                //Socket secondSocket = new Socket(packet.second.ipEndPoint);
                
                //var newRoom = new Room(
                //    new Client(firstSocket, packet.first.userInfo),
                //    new Client(secondSocket, packet.second.userInfo));
                //rooms.Add(packet.roomNumber, newRoom);
            }
                break;
            default:
                throw new FormatException("Invalid packet from Lobby");
        }
        // 2. Create room (thread) and link two clients

    }

    private void OnGamePacketRecevied(byte[] _buffer, ref Client _client)
    {
        PacketBase packetBase = OJ9Function.ByteArrayToObject<PacketBase>(_buffer);
        switch (packetBase.packetType)
        {
            case PacketType.Ready:
            {
                var packet = OJ9Function.ByteArrayToObject<C2GReady>(_buffer);
                
                if (!rooms.TryGetValue(packet.roomNumber, out var found))
                {
                    found = new Room();
                    found.AddPlayer(_client);
                    rooms.Add(packet.roomNumber, found);

                    // Waiting for left player 
                    return;
                }
                found.AddPlayer(_client);
                
                // Start
                Random random = new Random();
                var turn = random.Next(2) == 0 ? Turn.A : Turn.B;
                var turnPacket = OJ9Function.ObjectToByteArray(new G2CStart(true));
                var waitPacket = OJ9Function.ObjectToByteArray(new G2CStart(false));

                switch (turn)
                {
                    case Turn.A:
                    {
                        found.clientA.Send(turnPacket);
                        found.clientB.Send(waitPacket);
                    }
                        break;
                    case Turn.B:
                    {
                        found.clientA.Send(turnPacket);
                        found.clientB.Send(waitPacket);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
                break;
            case PacketType.Shoot:
            {
                // TODO : Process both clients
                var packet = OJ9Function.ByteArrayToObject<C2GShoot>(_buffer);
                
                
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}