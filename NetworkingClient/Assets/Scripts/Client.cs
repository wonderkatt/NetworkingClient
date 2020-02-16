using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Client : MonoBehaviour
{
    public static Client Instance;
    public static int DataBufferSize = 4096;

    public string Ip = "127.0.0.1";
    public int Port = 26950;
    public int MyId = 0;
    public TCP tcp;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Debug.Log("Instance exists, destroying obect");
            Destroy(this);
        }
    }

    private void Start()
    {
        tcp = new TCP();
    }

    public void ConenctToServer()
    {
        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient Socket;

        public NetworkStream Stream;
        public byte[] RecieveBuffer;

        public void Connect()
        {
            Socket = new TcpClient
            {
                ReceiveBufferSize = DataBufferSize,
                SendBufferSize = DataBufferSize
            };
            RecieveBuffer = new byte[DataBufferSize];
            Socket.BeginConnect(Instance.Ip, Instance.Port, ConnectCallback, Socket);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            Socket.EndConnect(result);

            if(!Socket.Connected)
            {
                return;
            }

            Stream = Socket.GetStream();

            Stream.BeginRead(RecieveBuffer, 0, DataBufferSize, RecieveCallback, null);
        }

        private void RecieveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = Stream.EndRead(result);
                if (byteLength <= 0)
                {
                    //TODO Disconnect
                    return;
                }
                byte[] data = new byte[byteLength];
                Array.Copy(RecieveBuffer, data, byteLength);

                //TODO handle data
                Stream.BeginRead(RecieveBuffer, 0, DataBufferSize, RecieveCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error recieving TCP data" + ex);
                //TODO disconnect
            }
        }
    }
}
