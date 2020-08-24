using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EZNet;

public class BasicNetManager : MonoBehaviour
{
    public bool servermode;
    public int TCPPort = 7000;

    [Header("Must be different to test on same computer.")]
    public int ServerUDPPort = 7001;
    public int ClientUDPPort = 7002;

    [Header("Server Specific Settings")]
    public int ticksPerSecond = 30;

    NetServer server;
    NetClient client;

    void ServerTick()
    {
        server.SendTransformToAll(NetData.ID_NETTRANSFORM.TestTransform);
    }

    // Start is called before the first frame update
    void Start()
    {
        NetServer.TICKRATE = 1.0f / ticksPerSecond;

        if (servermode)
        {
            server = new NetServer(10, TCPPort, ServerUDPPort);
        }
        else
        {
            client = new NetClient("TestClient", ClientUDPPort);
        }
            

    }

    string log;
    float timer = 0;
    // Update is called once per frame
    void Update()
    {

        if (servermode)
        {
            
            if (server.running)
            {
                //Basic tickrate implementation
                if((timer+= Time.deltaTime)> NetServer.TICKRATE)
                {
                    ServerTick();
                    timer = 0;
                }
            }

            //Some input processing

            if (Input.GetKeyDown(KeyCode.S))
            {
                server.StartServer();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                server.StopServer();
            }

            //Empty threaded log messages queue
            while (server.LOG.Count > 0)
            {
                if (server.LOG.TryDequeue(out log))
                    Debug.Log(log);
            }


        }
        else
        {

            if (Input.GetKeyDown(KeyCode.C))
            {
                client.Connect("127.0.0.1", TCPPort, ServerUDPPort);
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                client.SendCommand("/test");
            }

            while (client.LOG.Count > 0)
            {
                if (client.LOG.TryDequeue(out log))
                    Debug.Log(log);
            }

        }

        


    }
}
