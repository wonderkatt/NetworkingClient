using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    private static void SendTCPData(Packet packet)
    {
        packet.WriteLength();
        Client.Instance.tcp.SendData(packet);
    }

    private static void SendUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.Instance.udp.SendData(packet);
    }
        

    #region
    public static void WelcomeRecieved()
    {
        using(Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            packet.Write(Client.Instance.MyId);
            packet.Write(UIManager.Instance.UserNameField.text);

            SendTCPData(packet);
        }
    }

   
    #endregion
}
