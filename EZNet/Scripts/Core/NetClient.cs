using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;


namespace EZNet
{
    public class NetClient : MonoBehaviour
    {
        Socket tcp;
        Socket udp;

        public string name;
        public byte cid = 255;//registered id on server
        public bool connected;
        int udpport;
        int serverudpport;

        bool cidinit = false;

        IPEndPoint serverUDPep;

        public NetData netdata;

        public Thread tcpthread, udpthread;

        public ConcurrentQueue<Packet> TCPout, UDPout;

        public ConcurrentQueue<string> LOG;

        public static float INTERP_TIME;//Time since last NetTransform was received, used for interpolation.

        public NetClient(string name, int udpport)
        {
            LOG = new ConcurrentQueue<string>();
            UDPout = new ConcurrentQueue<Packet>();
            TCPout = new ConcurrentQueue<Packet>();
            this.name = name;
            this.udpport = udpport;
            connected = false;
            cid = 0;
            netdata = new NetData();
            BindingUtils.LoadDatastore(ref netdata);
        }

        void DebugLog(string t)
        {
            LOG.Enqueue(t);
        }


        PacketHeader phtemp;
        public void OnReceive(byte[] raw)
        {
            phtemp = PacketUtils.ReadHeader(raw);

            switch (phtemp.type)
            {
                case NetData.TYPE_NETTRANSFORM:
                    netdata.NETTRANSFORM[phtemp.id].DecodeRaw(PacketUtils.Unpack(raw));
                    break;

                case NetData.TYPE_CMD:
                    NetCMD cmd = new NetCMD();
                    cmd.DecodeRaw(PacketUtils.Unpack(raw));
                    OnCommandReceived(cmd);
                    break;

            }

        }

        public void OnCommandReceived(NetCMD cmd)
        {
            switch (NetCMD.ExtractCommand(cmd.command))
            {
                case NetCMD.CIDINIT:
                    cid = byte.Parse(NetCMD.ExtractArgs(cmd.command));
                    cidinit = true;
                    DebugLog("Received CID From Server : " + cid);
                    SendCommand("/udpinit " + udpport);
                    break;

                case NetCMD.TEST:
                    DebugLog("Received Test CMD from Server!");
                    break;

                case NetCMD.UDPINIT:
                default:
                    break;
            }
        }


        Packet pts;
        NetCMD cmdtosend = new NetCMD();
        public void SendCommand(string cmd)
        {
            cmdtosend.command = cmd;
            pts = new Packet();
            pts.data = PacketUtils.Pack(cmdtosend.EncodeRaw(), PacketUtils.GenerateHeader(cid, NetData.TYPE_CMD, 0, cmdtosend.GetLength()));
            TCPout.Enqueue(pts);
        }

        NetTransform ttosend;
        public void SendTransform(Transform t, byte id)
        {
            ttosend = new NetTransform();
            ttosend.position = t.localPosition;
            ttosend.rotation = t.localRotation.eulerAngles;
            ttosend.scale = t.localScale;
            pts = new Packet();
            pts.data = PacketUtils.Pack(ttosend.EncodeRaw(), PacketUtils.GenerateHeader(cid, NetData.TYPE_NETTRANSFORM, id, ttosend.GetLength()));
            UDPout.Enqueue(pts);
        }

        public void Connect(string serverip, int serverport, int serverudpport)
        {
            this.serverudpport = serverudpport;
            connected = true;

            serverUDPep = new IPEndPoint(IPAddress.Parse(serverip).MapToIPv4(), serverudpport);

            tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tcp.Connect(new IPEndPoint(IPAddress.Parse(serverip).MapToIPv4(), serverport));
            udp.Bind(new IPEndPoint(IPAddress.Parse(serverip).MapToIPv4(), udpport));
            

            tcpthread = new Thread(new ThreadStart(TCPLoop));
            tcpthread.Start();

            udpthread = new Thread(new ThreadStart(UDPLoop));
            udpthread.Start();

            DebugLog("Connected to Server " + serverip + ":" + serverport);
        }

        public void Disconnect()
        {
            tcpthread.Abort();
            udpthread.Abort();

            if (tcp.Connected)
                tcp.Disconnect(true);

            tcp.Shutdown(SocketShutdown.Both);
            tcp.Dispose();

            if (udp.Connected)
                udp.Disconnect(true);

            udp.Shutdown(SocketShutdown.Both);
            udp.Dispose();
        }

        public void TCPLoop()
        {
            Packet lastPacket;
            int lastavail;

            while (connected)
            {

                //Receive
                if ((lastavail = tcp.Available) > 0)
                {
                    lastPacket = new Packet();
                    lastPacket.data = new byte[lastavail];
                    tcp.Receive(lastPacket.data);
                    OnReceive(lastPacket.data);
                }

                if (cidinit)
                {
                    //Send
                    while (TCPout.Count > 0)
                    {
                        if (TCPout.TryDequeue(out lastPacket))
                        {
                            tcp.Send(lastPacket.data);
                        }
                    }
                }
            }
        }

        public void UDPLoop()
        {
            Packet lastPacket;
            int lastavail;

            while (connected)
            {

                //Receive
                if ((lastavail = udp.Available) > 0)
                {
                    lastPacket = new Packet();
                    lastPacket.data = new byte[lastavail];
                    udp.Receive(lastPacket.data);
                    OnReceive(lastPacket.data);
                }

                if (cidinit)
                {
                    //Send
                    while (UDPout.Count > 0)
                    {
                        if (UDPout.TryDequeue(out lastPacket))
                        {
                            udp.SendTo(lastPacket.data, serverUDPep);
                        }

                    }
                }
            }
        }



    }
}



