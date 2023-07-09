﻿using System.Collections.Concurrent;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using MySql.Data.MySqlClient;

public class LobbyServer
{
    private static int INVALID_INDEX = -1;
    private UdpClient udpClient;
    private MySqlConnection mysql;
    private readonly ConcurrentQueue<ConnectionInfo>[] waitingPlayers = new ConcurrentQueue<ConnectionInfo>[1];
    private int roomNumber;
    private readonly ConcurrentBag<UserInfo> userInfos = new ConcurrentBag<UserInfo>();

    private readonly object lockObject = new object();

    public void Start()
    {
        try
        {
            roomNumber = 0;
            userInfos.Clear();
            
            for (var i = 0; i < 1; i++)
            {
                waitingPlayers[i] = new ConcurrentQueue<ConnectionInfo>();
            }

            StartDB();
            udpClient = new UdpClient(
                Convert.ToInt32(
                    ConfigurationManager.AppSettings.Get("lobbyServerPort")
                )
            );
            
            Console.WriteLine("Listening started");
            udpClient.BeginReceive(DataReceived, null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void StartDB()
    {
        string dbServerString = string.Format(
            "Server=localhost;" +
            "Port={0};" +
            "Database={1};" +
            "Uid={2};" +
            "Pwd={3}",
            ConfigurationManager.AppSettings.Get("dbPort"),
            ConfigurationManager.AppSettings.Get("lobbyDatabase"),
            ConfigurationManager.AppSettings.Get("dbUserId"),
            ConfigurationManager.AppSettings.Get("dbUserPw")
        );

        mysql = new MySqlConnection(dbServerString);
        mysql.Open();
        
        Console.WriteLine("DB started");
    }

    private void DataReceived(IAsyncResult _asyncResult)
    {
        IPEndPoint ipEndPoint = null;
        var buffer = udpClient.EndReceive(_asyncResult, ref ipEndPoint);
        Console.WriteLine("Get from[login server] : " + ipEndPoint);
        var packBase = OJ9Function.ByteArrayToObject<PacketBase>(buffer);
        switch (packBase.packetType)
        {
            case PacketType.L2BError:
            {
                L2BError packet = OJ9Function.ByteArrayToObject<L2BError>(buffer);

                byte[] clientBuff =
                    OJ9Function.ObjectToByteArray(new B2CError(packet.errorType));
                udpClient.Send(clientBuff, clientBuff.Length, OJ9Function.CreateIPEndPoint(packet.clientEndPoint));
            }
                break;
            case PacketType.CheckLobbyAccount:
            {
                L2BCheckAccount packet = OJ9Function.ByteArrayToObject<L2BCheckAccount>(buffer);
                if (!packet.IsLoginSuccess())
                {
                    byte[] sendBuff =
                        OJ9Function.ObjectToByteArray(new B2CError(ErrorType.Unknown));
                    udpClient.Send(sendBuff, sendBuff.Length, OJ9Function.CreateIPEndPoint(packet.clientEndPoint));
                }
                try
                {
                    UserInfo userInfo = GetAccount(packet.guid);
                    if (!userInfo.IsValid())
                    {
                        userInfo = AddAccountDb(packet.guid);
                        Console.WriteLine("New account was added : " + userInfo.guid + ", Client ip : " + packet.clientEndPoint);
                    }
                    else
                    {
                        Console.WriteLine("Already exist account. Pass creating new account.");
                    }

                    if (!userInfo.IsValid())
                    {
                        throw new FormatException("Userinfo does not exist and cannot create");
                    }

                    userInfos.Add(userInfo);
                    EnterLobby(userInfo, OJ9Function.CreateIPEndPoint(packet.clientEndPoint));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
                break;
            case PacketType.QueueGame:
            {
                C2BQueueGame packet = OJ9Function.ByteArrayToObject<C2BQueueGame>(buffer);
                if (ipEndPoint == null)
                {
                    throw new FormatException("ipEndPoint is not valid");
                }
                waitingPlayers[(int)packet.gameType].Enqueue(
                    new ConnectionInfo(packet.userInfo, ipEndPoint)
                );
                Console.WriteLine(packet.userInfo.nickname + " is now in queue.");
            }
                break;
            case PacketType.CancelQueue:
            {
                C2BCancelQueue packet = OJ9Function.ByteArrayToObject<C2BCancelQueue>(buffer);
                if (ipEndPoint == null)
                {
                    throw new FormatException("ipEndPoint is not valid");
                }

                var players = waitingPlayers[(int)packet.gameType];
                while (players.TryDequeue(out var removeElem))
                {
                    if (removeElem.userInfo.guid != packet.userInfo.guid)
                    {
                        players.Enqueue(removeElem);
                    }
                }
            }
                break;
            default:
                throw new FormatException("Invalid packet type in LoginServer");
        }

        udpClient.BeginReceive(DataReceived, null);
    }

    private void EnterLobby(UserInfo _userInfo, IPEndPoint _ipEndPoint)
    {
        var sendBuff =
            OJ9Function.ObjectToByteArray(new B2CEnterLobby(_userInfo));
        udpClient.Send(sendBuff, sendBuff.Length, _ipEndPoint);
    }

    public void Update()
    {
        SpinQueue();
    }

    private void SpinQueue()
    {
        var gameIndex = INVALID_INDEX;
        for (var index = 0; index < waitingPlayers.Length; index++)
        {
            if (waitingPlayers[index].Count < 2)
            {
                continue;
            }
            
            gameIndex = index;
            break;
        }

        if (gameIndex == INVALID_INDEX) // No game which is ready to go
        {
            return;
        }

        if (!waitingPlayers[gameIndex].TryDequeue(out var first) || !waitingPlayers[gameIndex].TryDequeue(out var second))
        {
            throw new FormatException("dequeue failed");
        }
        
        // Get 2 players

        lock (lockObject)
        {
            byte[] buffer =
                OJ9Function.ObjectToByteArray(
                    new B2CGameMatched((roomNumber)));

            udpClient.Send(
                buffer,
                buffer.Length,
                first.ipEndPoint
            );
            
            udpClient.Send(
                buffer,
                buffer.Length,
                second.ipEndPoint
            );
            
            ++roomNumber;

            if (roomNumber == int.MaxValue)
            {
                roomNumber = 0;
            }
        }
    }
}