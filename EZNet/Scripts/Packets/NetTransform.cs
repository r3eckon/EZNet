using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EZNet
{
    public class NetTransform : IPacket
    {
        public byte id;
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.zero;

        public float INTERP_TIME;

        public Vector3 last_pos = Vector3.zero;
        public Vector3 last_rot = Vector3.zero;
        public Vector3 last_scl = Vector3.zero;


        public void DecodeRaw(byte[] raw)
        {
            last_pos = position;
            last_rot = rotation;
            last_scl = scale;

            position.x = BitConverter.ToSingle(raw, 0);
            position.y = BitConverter.ToSingle(raw, 4);
            position.z = BitConverter.ToSingle(raw, 8);
            rotation.x = BitConverter.ToSingle(raw, 12);
            rotation.y = BitConverter.ToSingle(raw, 16);
            rotation.z = BitConverter.ToSingle(raw, 20);
            scale.x = BitConverter.ToSingle(raw, 24);
            scale.y = BitConverter.ToSingle(raw, 28);
            scale.z = BitConverter.ToSingle(raw, 32);

            INTERP_TIME = 0;
        }

        public byte[] EncodeRaw()
        {
            byte[] raw = new byte[GetLength()];
            Array.ConstrainedCopy(BitConverter.GetBytes(position.x),0, raw, 0, 4);
            Array.ConstrainedCopy(BitConverter.GetBytes(position.y),0, raw, 4,4);
            Array.ConstrainedCopy(BitConverter.GetBytes(position.z), 0, raw, 8, 4);
            Array.ConstrainedCopy(BitConverter.GetBytes(rotation.x), 0, raw, 12, 4);
            Array.ConstrainedCopy(BitConverter.GetBytes(rotation.y), 0, raw, 16, 4);
            Array.ConstrainedCopy(BitConverter.GetBytes(rotation.z), 0, raw, 20, 4);
            Array.ConstrainedCopy(BitConverter.GetBytes(scale.x), 0, raw, 24, 4);
            Array.ConstrainedCopy(BitConverter.GetBytes(scale.y), 0, raw, 28, 4);
            Array.ConstrainedCopy(BitConverter.GetBytes(scale.z), 0, raw, 32, 4);
            return raw;
        }

        public short GetLength()
        {
            return sizeof(float) * 9;//3 floats for each vector
        }

        public byte GetID()
        {
            return id;
        }

        public byte GetPacketType()
        {
            return NetData.TYPE_NETTRANSFORM;
        }

    }
}


