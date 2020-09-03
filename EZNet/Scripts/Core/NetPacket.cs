using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EZNet
{

    public class Packet
    {
        public byte[] data;
        public byte cid;
    }

    public class NetData
    {
        //Add one of those for each class implementing IPacket
        public const byte TYPE_CMD = 0;
        public const byte TYPE_NETTRANSFORM = 1;

        //Enums used to declare id values to label datastore items
        public enum ID_CMD { LastCommand }
        public enum ID_NETTRANSFORM { TestTransform, InterpLast }

        public NetCMD[] CMD;
        public NetTransform[] NETTRANSFORM;

        public NetData()
        {
            //For each type of packet we need to init the array and fill in with empty objects to avoid nullreferences
            //Before a write has filled the slot with data.
            CMD = new NetCMD[Enum.GetValues(typeof(ID_CMD)).Length];
            for (int i = 0; i < Enum.GetValues(typeof(ID_CMD)).Length; i++)
            {
                CMD[i] = new NetCMD();
            }

            NETTRANSFORM = new NetTransform[Enum.GetValues(typeof(ID_NETTRANSFORM)).Length];
            for(int i=0; i < Enum.GetValues(typeof(ID_NETTRANSFORM)).Length; i++)
            {
                NETTRANSFORM[i] = new NetTransform();
            }

        }

    }

    public interface IPacket
    {
        byte GetPacketType();
        byte GetID();
        ushort GetLength();
        byte[] EncodeRaw();
        void DecodeRaw(byte[] raw);
    }

    public static class PacketUtils
    {

        public const byte HEADERSIZE = 5;

        public static byte[] lastPack;
        public static byte[] lastUnpack;

        //Combines header and packet data
        public static byte[] Pack(byte[] raw, byte[] head)
        {
            lastPack = new byte[raw.Length + head.Length];
            Array.Copy(head, lastPack, HEADERSIZE);
            Array.ConstrainedCopy(raw, 0, lastPack, HEADERSIZE, raw.Length);
            return lastPack;
        }

        public static byte[] Pack(byte cid, IPacket p)
        {
            lastPack = new byte[p.GetLength() + HEADERSIZE];
            Array.Copy(GenerateHeader(cid, p), lastPack, HEADERSIZE);
            Array.ConstrainedCopy(p.EncodeRaw(), 0, lastPack, HEADERSIZE, p.GetLength());
            return lastPack;
        }

        //Removes header, returns only packet data
        public static byte[] Unpack(byte[] raw)
        {
            lastUnpack = new byte[raw.Length - HEADERSIZE];
            Array.ConstrainedCopy(raw, HEADERSIZE, lastUnpack, 0, raw.Length - HEADERSIZE);
            return lastUnpack;
        }

        static byte[][] toRetSplit;
        static Dictionary<uint, byte[]> splitdict;
        static ushort left, ccount;
        static uint pcount;
        public static byte[][] PacketSplit(byte[] full)
        {
            pcount = 0;
            left = (ushort)full.Length;
            splitdict = new Dictionary<uint, byte[]>();

            while (left > 1)
            {
                ccount = BytesToUShort(full[full.Length - left], full[full.Length - left + 1]);
                if (ccount == 0)
                    break;
                splitdict.Add(pcount, new byte[ccount]);
                Array.ConstrainedCopy(full, full.Length - left, splitdict[pcount], 0, ccount);
                left -= ccount;
                pcount++;
            }

            toRetSplit = new byte[splitdict.Keys.Count][];

            foreach (uint k in splitdict.Keys)
            {
                toRetSplit[k] = splitdict[k];
            }

            return toRetSplit;


        }

        public static byte[] GenerateHeader(byte cid, byte type, byte id, ushort length)
        {
            byte[] toret = new byte[5];
            toret[0] = (byte)((length + HEADERSIZE) << 8 >> 8);
            toret[1] = (byte)((length + HEADERSIZE) >> 8);
            toret[2] = cid;
            toret[3] = type;
            toret[4] = id;
            return toret;
        }

        public static byte[] GenerateHeader(byte cid, IPacket p)
        {
            byte[] toret = new byte[5];
            toret[0] = (byte)((p.GetLength() + HEADERSIZE) << 8 >> 8);
            toret[1] = (byte)((p.GetLength() + HEADERSIZE) >> 8);
            toret[2] = cid;
            toret[3] = p.GetPacketType();
            toret[4] = p.GetID();
            return toret;
        }

        static PacketHeader toRetHeader = new PacketHeader();
        public static PacketHeader ReadHeader(byte[] raw)
        {
            toRetHeader.length = BytesToUShort(raw[0], raw[1]);
            toRetHeader.cid = raw[2];
            toRetHeader.type = raw[3];
            toRetHeader.id = raw[4];
            return toRetHeader;
        }

        public static short BytesToShort(params byte[] b)
        {
            return (short)((b[1] << 8) | b[0]);
        }

        public static ushort BytesToUShort(params byte[] b)
        {
            return (ushort)((b[1] << 8) | b[0]);
        }

        public static byte[] UShortToBytes(ushort val)
        {
            byte[] toRet = new byte[2];
            toRet[0] = (byte)(val << 8 >> 8);
            toRet[1] = (byte)(val >> 8);
            return toRet;
        }

        public static byte[] ShortToBytes(short val)
        {
            byte[] toRet = new byte[2];
            toRet[0] = (byte)(val << 8 >> 8);
            toRet[1] = (byte)(val >> 8);
            return toRet;
        }

    }

    public struct PacketHeader
    {
        public byte cid;
        public byte type;
        public byte id;
        public ushort length;
    }

    

}



