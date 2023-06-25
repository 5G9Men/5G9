﻿using System;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Assertions;

public class NetworkManager
{
    private Socket socket;
    private byte[] buffer;
    public NetState netState;
    private Action<PacketBase>[] packetHandlers;

    public NetworkManager()
    {
        netState = NetState.None;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var endPoint = OJ9Function.CreateIPEndPoint(
            OJ9Const.SERVER_IP + ":" + Convert.ToString(OJ9Const.SERVER_PORT_NUM)
        );
        socket.BeginConnect(endPoint, OnConnect, null);
        buffer = new byte[OJ9Const.BUFFER_SIZE];
    }

    public void BindPacketHandler(PacketType _packetType, Action<PacketBase> _action)
    {
        packetHandlers[(int)_packetType] = _action;
    }
    
    private void OnConnect(IAsyncResult _asyncResult)
    {
        try
        {
            socket.EndConnect(_asyncResult);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        socket.BeginReceive(buffer, 0, OJ9Const.BUFFER_SIZE, SocketFlags.None, OnReceived, null);

        netState = NetState.Connected;
        Debug.Log("Server connected");
    }

    public void Send(PacketBase _packet)
    {
        Assert.IsTrue(netState != NetState.Connected);
        socket.Send(OJ9Function.ObjectToByteArray(_packet));
    }

    private void OnReceived(IAsyncResult _asyncResult)
    {
        socket.EndReceive(_asyncResult);
        var packetBase = OJ9Function.ByteArrayToObject<PacketBase>(buffer);
        if (packetHandlers[(int)packetBase.packetType] == null)
        {
            Debug.LogError("Need to be binded. Packet type is " + packetBase.packetType);
        }
        else
        {
            packetHandlers[(int)packetBase.packetType](packetBase);
        }
    }
}