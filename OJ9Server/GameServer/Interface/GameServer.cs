﻿using System.Net;
using System.Net.Sockets;
using System.Text;

public struct Client
{
    private readonly Socket socket;
    public byte[] buffer = new byte[OJ9Const.BUFFER_SIZE];
    public int roomNumber = 0;

    public UserInfo userInfo;

    public bool IsValid()
    {
        return userInfo.guid != Guid.Empty;
    }
    
    public Client(Socket _socket)
    {
        socket = _socket;
    }

    public void InitUserInfo(UserInfo _userInfo)
    {
        userInfo = _userInfo;
    }

    public delegate void OnReceivedCallback(byte[] _buffer, ref Client _client);
    
    public void BeginReceive(OnReceivedCallback _onReceivedCallback)
    {
        socket.BeginReceive(
            buffer,
            0,
            buffer.Length,
            SocketFlags.None,
            EndReceive,
            _onReceivedCallback
        );
    }

    public void Send(byte[] packet)
    {
        socket.Send(packet);
    }

    private void EndReceive(IAsyncResult _asyncResult)
    {
        var callback = (OnReceivedCallback)_asyncResult.AsyncState!;
        var size = socket.EndReceive(_asyncResult);
        var stringFromBuffer = Encoding.UTF8.GetString(buffer, 0, size);
        callback(Encoding.UTF8.GetBytes(stringFromBuffer), ref this);
        
        socket.BeginReceive(
            buffer,
            0,
            buffer.Length,
            SocketFlags.None,
            EndReceive,
            callback
        );
    }
}

public abstract class GameServer
{
    // Listen
    protected Socket listener;
    protected IPEndPoint listenEndPoint;

    protected List<Client> clients;
    protected GameType gameType;
    protected int maxRoomNumber;

    protected GameServer(int _inMaxRoomNumber)
    {
        maxRoomNumber = _inMaxRoomNumber;
        clients = new List<Client>();
    }

    public abstract void Start();
}