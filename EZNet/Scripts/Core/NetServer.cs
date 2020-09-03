using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace EZNet
{
    public class NetServer
    {
        public static byte IDSTORE = 1;//CID 0 refers to server

        public static float TICKRATE; 

        public bool running;
        public byte maxClients;
        public int tcpport;
        public int udpport;

        Thread clthread;
        Thread tcpdatathread;
        Thread udpdatathread;
        Thread dataInThread;

        ConcurrentQueue<Packet> TCPin;

        ConcurrentQueue<Packet> UDPin;

        ConcurrentQueue<Packet> TCPOut;

        ConcurrentQueue<Packet> UDPOut;

        Socket connectionListener;
        Socket udpdata;

        ConcurrentDictionary<byte, ConnectedClient> clients;

        public ConcurrentQueue<string> LOG;

        NetData netdata;

        HashSet<byte> ClientAuthorityTransforms = new HashSet<byte>();

        public NetServer(byte maxClients, int tcpport, int udpport)
        {
            TCPin = new ConcurrentQueue<Packet>();
            TCPOut = new ConcurrentQueue<Packet>();
            UDPin = new ConcurrentQueue<Packet>();
            UDPOut = new ConcurrentQueue<Packet>();

            this.maxClients = maxClients;
            this.tcpport = tcpport;
            this.udpport = udpport;
            clients = new ConcurrentDictionary<byte, ConnectedClient>();
            LOG = new ConcurrentQueue<string>();
            netdata = new NetData();
            BindingUtils.LoadDatastore(ref netdata);
        }


        PacketHeader phtemp;
        public void OnReceive(Packet p)
        {
            phtemp = PacketUtils.ReadHeader(p.data);

            switch (phtemp.type)
            {
                case NetData.TYPE_CMD:
                    NetCMD cmd = new NetCMD();
                    cmd.DecodeRaw(PacketUtils.Unpack(p.data));
                    OnClientCommandReceived(phtemp.cid, cmd);
                    break;

                case NetData.TYPE_NETTRANSFORM:
                    NetTransform nt = new NetTransform();
                    nt.DecodeRaw(PacketUtils.Unpack(p.data));
                    OnClientTransformReceived(phtemp.cid, nt);
                    break;

                default:
                    DebugLog("Undefined Packet Type : " + phtemp.type);

                    break;
            }


        }

        ConnectedClient cc;
        public void OnClientCommandReceived(byte cid, NetCMD cmd)
        {

            switch (NetCMD.ExtractCommand(cmd.command))
            {
                case NetCMD.TEST:
                    DebugLog("RECEIVED TEST COMMAND FROM CLIENT " + cid);
                    break;

                case NetCMD.UDPINIT:
                    DebugLog("[" + cid + "]" + "UDP Socket Initialized on port " + clients[cid].udpep.Port);
                    break;


                default:
                    DebugLog("Undefined Command Type : " + cmd.command);
                    break;
            }
        }


        public void OnClientTransformReceived(byte cid, NetTransform t)
        {
            //Only use data when received from client if clients have some authority over that data
            if (ClientAuthorityTransforms.Contains(t.GetID()))
            {
                //Now directly storing at the same ID, however it could be better to switch over the ID
                //And for instance if clients use a SelfTransform ID the server can store it somewhere else
                //Potentially even outside the shared datastore
                netdata.NETTRANSFORM[t.GetID()] = t;
            }
        }

        public void StartServer()
        {
            if (running)
                return;

            running = true;

            udpdata = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpdata.Ttl = 255;
            udpdata.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpdata.Bind(new IPEndPoint(IPAddress.Any, udpport));

            clthread = new Thread(new ThreadStart(ConnectionListenerLoop));
            clthread.Start();

            tcpdatathread = new Thread(new ThreadStart(TCPDataLoop));
            tcpdatathread.Start();

            udpdatathread = new Thread(new ThreadStart(UDPDataLoop));
            udpdatathread.Start();

            dataInThread = new Thread(new ThreadStart(DataInLoop));
            dataInThread.Start();

            DebugLog("[" + System.DateTime.Now + "] Server Started!");
        }

        public void StopServer()
        {
            running = false;
            clthread.Abort();
            tcpdatathread.Abort();
            udpdatathread.Abort();

            foreach (ConnectedClient c in clients.Values)
            {
                if (c.tcpsock.Connected)
                    c.tcpsock.Disconnect(true);

                c.tcpsock.Close();
                c.tcpsock.Dispose();

            }

            udpdata.Close();
            udpdata.Dispose();

            connectionListener.Close();
            connectionListener.Dispose();

            DebugLog("[" + System.DateTime.Now + "] Server Stopped!");

        }

        void ConnectionListenerLoop()
        {
            connectionListener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            connectionListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            connectionListener.Ttl = 255;
            connectionListener.Bind(new IPEndPoint(IPAddress.Any, tcpport));
            connectionListener.Listen(maxClients);


            ConnectedClient newClient;

            while (running)
            {
                newClient = new ConnectedClient();
                newClient.id = IDSTORE++;
                newClient.tcpsock = connectionListener.Accept();
                newClient.tcpsock.Ttl = 255;
                newClient.tcpep = (IPEndPoint)newClient.tcpsock.RemoteEndPoint;
                newClient.udpep = new IPEndPoint(newClient.tcpep.Address.MapToIPv4(), newClient.tcpep.Port);

                while(!clients.TryAdd(newClient.id, newClient));//Keep trying to add new client

                SendCommand(newClient.id, "/setid " + newClient.id);
                SendCommand(newClient.id, "/udpinit");

                DebugLog("[" + System.DateTime.Now.ToShortTimeString() + "]" + "Client ID " + newClient.id + " Connected from " + newClient.tcpep.Address.MapToIPv4().ToString());
            }
        }

        Packet pts;
        NetCMD cmdtosend = new NetCMD();
        public void SendCommand(byte cid, string cmd )
        {
            cmdtosend.command = cmd;
            Packet pts = new Packet();
            pts.data = PacketUtils.Pack(cmdtosend.EncodeRaw(), PacketUtils.GenerateHeader(0, NetData.TYPE_CMD, 0, cmdtosend.GetLength()));
            pts.cid = cid;
            TCPOut.Enqueue(pts);
        }


        public void SendTransform(byte cid, NetData.ID_NETTRANSFORM transformid)
        {
            Packet pts = new Packet();
            pts.cid = cid;
            pts.data = PacketUtils.Pack(cid, netdata.NETTRANSFORM[(byte)transformid]);
            UDPOut.Enqueue(pts);
        }

        public void SendTransformToAll(NetData.ID_NETTRANSFORM transformid)
        {
            foreach(ConnectedClient c in clients.Values)
            {
                if(c.udpinit)
                    SendTransform(c.id, transformid);
            }
        }

        public void SendTCP(byte cid, IPacket p)
        {
            Packet pts = new Packet();
            pts.cid = cid;
            pts.data = PacketUtils.Pack(cid, p);
            TCPOut.Enqueue(pts);
        }

        public void SendAllTCP(IPacket p)
        {
            foreach (ConnectedClient c in clients.Values)
            {
                SendTCP(c.id, p);
            }
        }

        public void SendUDP(byte cid, IPacket p)
        {
            Packet pts = new Packet();
            pts.cid = cid;
            pts.data = PacketUtils.Pack(cid, p);
            UDPOut.Enqueue(pts);
        }

        public void SendAllUDP(IPacket p)
        {
            foreach (ConnectedClient c in clients.Values)
            {
                if(c.udpinit)
                    SendUDP(c.id, p);
            }
        }


        void DebugLog(string t)
        {
            LOG.Enqueue(t);
        }

        byte[][] tcpsplit;

        void TCPDataLoop()
        {
            Packet lastPacket;
            int lastavail;

            while (running)
            {
                //Receive
                foreach (ConnectedClient c in clients.Values)
                {
                    if ((lastavail = c.tcpsock.Available) > 0)
                    {
                        lastPacket = new Packet();
                        lastPacket.cid = c.id;
                        lastPacket.data = new byte[lastavail];
                        c.tcpsock.Receive(lastPacket.data);

                        tcpsplit = PacketUtils.PacketSplit(lastPacket.data);

                        for (int i = 0; i < tcpsplit.GetLength(0); i++)
                        {
                            lastPacket.data = tcpsplit[i];
                            TCPin.Enqueue(lastPacket);
                        }
                    }
                }

                //Send
                while (TCPOut.Count > 0)
                {
                    if (TCPOut.TryDequeue(out lastPacket))
                    {
                        clients[lastPacket.cid].tcpsock.SendBufferSize = lastPacket.data.Length;
                        clients[lastPacket.cid].tcpsock.Send(lastPacket.data);
                    }

                }
            }
        }

        void UDPDataLoop()
        {
            Packet lastPacket;
            int lastavail;
            EndPoint lastep;

            while (running)
            {

                //Receive
                if ((lastavail = udpdata.Available) > 0)
                {
                    lastep = new IPEndPoint(IPAddress.Any, udpport);
                    lastPacket = new Packet();
                    lastPacket.data = new byte[lastavail];
                    udpdata.ReceiveFrom(lastPacket.data, ref lastep);
                    lastPacket.cid = lastPacket.data[2];

                    //Automatic UDP handshake
                    if (!clients[lastPacket.cid].udpinit)
                    {
                        clients[lastPacket.cid].udpep = (IPEndPoint)lastep;
                        clients[lastPacket.cid].udpinit = true;
                    }

                    UDPin.Enqueue(lastPacket);
                }

                //Send
                while (UDPOut.Count > 0)
                {
                    UDPOut.TryDequeue(out lastPacket);

                    if (clients[lastPacket.cid].udpinit)
                    {
                        udpdata.SendBufferSize = lastPacket.data.Length;
                        udpdata.SendTo(lastPacket.data, clients[lastPacket.cid].udpep);
                    }
                    else
                        DebugLog("ERROR SENDING PACKET : CLIENT " + lastPacket.cid + " HAS NOT SENT UDP INIT COMMAND");

                }
            }

        }

        void DataInLoop()
        {
            Packet cpack;

            while (running)
            {

                while(UDPin.Count > 0)
                {
                    if(UDPin.TryDequeue(out cpack))
                        OnReceive(cpack);
                }

                while (TCPin.Count > 0)
                {
                    if(TCPin.TryDequeue(out cpack))
                        OnReceive(cpack);
                }

            }
        }

    }
    public class ConnectedClient
    {
        public byte id;
        public IPEndPoint tcpep;
        public IPEndPoint udpep;
        public Socket tcpsock;
        public bool udpinit;

    }

}

