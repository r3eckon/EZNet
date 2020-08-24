using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EZNet
{
    public enum NetBindingMode { None, Read, Write }

    public interface INetBinding
    {
        NetBindingMode GetBindingMode();
        byte GetBindingDataType();
        byte GetBindingDataID();
        void SyncBinding();
        void SetBindingMode(NetBindingMode mode);
    }

    public static class BindingUtils
    {
        public static NetData datastore;
        private static bool ready = false;

        public static bool Ready { get => ready; }

        public static void LoadDatastore(ref NetData ds)
        {
            datastore = ds;
            ready = true;
        }
    }

}


