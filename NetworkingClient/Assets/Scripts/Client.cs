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
    public UDP udp;

    private delegate void PacketHandler(Packet packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

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
        udp = new UDP();
    }

    public void ConenctToServer()
    {
        InitalizeClientData();
        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient Socket;

        public NetworkStream Stream;
        private Packet recievedData;
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

            recievedData = new Packet();

            Stream.BeginRead(RecieveBuffer, 0, DataBufferSize, RecieveCallback, null);
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (Socket != null)
                {
                    Stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending data to server via TCP: {ex}");
            }
            

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

                recievedData.Reset(HandleData(data));
                Stream.BeginRead(RecieveBuffer, 0, DataBufferSize, RecieveCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error recieving TCP data" + ex);
                //TODO disconnect
            }
        }

        private bool HandleData(byte[] data)
        {
            int packetLength = 0;
            recievedData.SetBytes(data);

            if(recievedData.UnreadLength() >= 4)
            {
                packetLength = recievedData.ReadInt();
                if(packetLength <= 0)
                {
                    return true;
                }
            }

            while (packetLength > 0 && packetLength <= recievedData.UnreadLength())
            {
                byte[] packetBytes = recievedData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        packetHandlers[packetId](packet);
                    }
                });

                packetLength = 0;
                if (recievedData.UnreadLength() >= 4)
                {
                    packetLength = recievedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if(packetLength <= 1)
            {
                return true;
            }

            return false;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(Instance.Ip), Instance.Port);
        }

        public void Connect(int localPort)
        {
            socket = new UdpClient(localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(RecieveCallback, null);

            using(Packet packet = new Packet())
            {
                SendData(packet);
            }
        }


        public void SendData(Packet packet)
        {
            try
            {
                packet.InsertInt(Instance.MyId);
                if(socket != null)
                {
                    socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending data via UDP: {ex}");
              
            }
        }

        private void RecieveCallback(IAsyncResult result)
        {
            try
            {
                byte[] data = socket.EndReceive(result, ref endPoint);
                socket.BeginReceive(RecieveCallback, null);

                if(data.Length < 4)
                {
                    //TODO disconnect
                    return;
                }

                HandleData(data);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void HandleData(byte[] data)
        {
            using(Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                data = packet.ReadBytes(packetLength);
            }
            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(data))
                {
                    int packetId = packet.ReadInt();
                    packetHandlers[packetId](packet);
                }
            });
        }
    }

    private void InitalizeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },

        };

        Debug.Log("Initialized packets");
    }
}
