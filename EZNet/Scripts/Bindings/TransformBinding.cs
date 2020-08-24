using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EZNet
{
    public class TransformBinding : MonoBehaviour, INetBinding
    {
        public NetBindingMode mode;
        public NetData.ID_NETTRANSFORM id;

        private byte idb;
        private bool idbinit;

        [Header("Interpolation Settings (Read Mode Only)")]
        public bool interpolatePosition;
        public bool interpolateRotation;

        public void SyncBinding()
        {

            BindingUtils.datastore.NETTRANSFORM[idb].INTERP_TIME += Time.deltaTime;

            switch (mode)
            {
                case NetBindingMode.Read:
                    //No interpolation used on scale vector
                    transform.localScale = BindingUtils.datastore.NETTRANSFORM[idb].scale;

                    //First step of interpolation directly reads the received value since 
                    //no previous values exist yet. OR no interpolation used at all.
                    if (!interpolatePosition && !interpolateRotation)
                    {
                        transform.localPosition = BindingUtils.datastore.NETTRANSFORM[idb].position;
                        transform.localEulerAngles = BindingUtils.datastore.NETTRANSFORM[idb].rotation;
                    }
                    else //can now interpolate
                    {
                        //Both
                        if(interpolatePosition && interpolateRotation)
                        {
                            transform.localPosition = Vector3.Lerp(BindingUtils.datastore.NETTRANSFORM[idb].last_pos, BindingUtils.datastore.NETTRANSFORM[idb].position, BindingUtils.datastore.NETTRANSFORM[idb].INTERP_TIME / NetServer.TICKRATE);
                            transform.localRotation = Quaternion.Lerp(Quaternion.Euler(BindingUtils.datastore.NETTRANSFORM[idb].last_rot), Quaternion.Euler(BindingUtils.datastore.NETTRANSFORM[idb].rotation), BindingUtils.datastore.NETTRANSFORM[idb].INTERP_TIME / NetServer.TICKRATE);
                        }
                        else if (interpolatePosition)//Or just position
                        {
                            transform.localPosition = Vector3.Lerp(BindingUtils.datastore.NETTRANSFORM[idb].last_pos, BindingUtils.datastore.NETTRANSFORM[idb].position, BindingUtils.datastore.NETTRANSFORM[idb].INTERP_TIME / NetServer.TICKRATE);
                            transform.localRotation = Quaternion.Euler(BindingUtils.datastore.NETTRANSFORM[idb].rotation);
                        }
                        else if (interpolateRotation)
                        {
                            transform.localPosition = BindingUtils.datastore.NETTRANSFORM[idb].position;
                            transform.localRotation = Quaternion.Lerp(Quaternion.Euler(BindingUtils.datastore.NETTRANSFORM[idb].last_rot), Quaternion.Euler(BindingUtils.datastore.NETTRANSFORM[idb].rotation), BindingUtils.datastore.NETTRANSFORM[idb].INTERP_TIME / NetServer.TICKRATE);
                        }

                    }
                    
                    break;

                case NetBindingMode.Write:
                    BindingUtils.datastore.NETTRANSFORM[idb].position = transform.localPosition;
                    BindingUtils.datastore.NETTRANSFORM[idb].rotation = transform.localEulerAngles;
                    BindingUtils.datastore.NETTRANSFORM[idb].scale = transform.localScale;
                    break;

                case NetBindingMode.None:
                default:
                    break;
            }
        }

        public byte GetBindingDataID()
        {
            return (byte)id;
        }

        public byte GetBindingDataType()
        {
            return NetData.TYPE_NETTRANSFORM;
        }

        public NetBindingMode GetBindingMode()
        {
            return mode;
        }

        public void SetBindingMode(NetBindingMode mode)
        {
            this.mode = mode;
        }

        // Update is called once per frame
        void Update()
        {
            if (BindingUtils.Ready)
            {
                if (!idbinit)
                {
                    idb = (byte)id;
                    idbinit = true;
                }

                SyncBinding();
            }
        }

        
    }

}
