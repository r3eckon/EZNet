using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EZNet
{
    public class NetCMD : IPacket
    {
        public const string TEST = "/test";
        public const string UDPINIT = "/udpinit";
        public const string CIDINIT = "/setid";

        public string command;
        public byte id;

        public void DecodeRaw(byte[] raw)
        {
            command = Encoding.ASCII.GetString(raw);
        }

        public byte[] EncodeRaw()
        {
            return Encoding.ASCII.GetBytes(command);
        }

        public byte GetID()
        {
            return 0;//Doesnt need ID to pass commands
        }

        public short GetLength()
        {
            return (short)Encoding.ASCII.GetBytes(command).Length;
        }

        public byte GetPacketType()
        {
            return NetData.TYPE_CMD;
        }

        public static string ExtractCommand(string full)
        {
            return full.Split(' ')[0];
        }

        public static string ExtractArgs(string full)
        {
            return full.Replace(ExtractCommand(full), "");
        }

    }

}

