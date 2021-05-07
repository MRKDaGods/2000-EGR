using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRK.Networking;
using UnityEngine.UI;
using MRK.Networking.Packets;
using System;
using System.Reflection;

namespace MRK {
    public class TestNetworkDownload : MonoBehaviour {
        EGRNetwork m_Network;
        [SerializeField]
        Button m_Button;
        [SerializeField]
        Image m_Image;

        void Start() {
            m_Button.onClick.AddListener(OnDownloadClick);

            foreach (Type type in Assembly.GetExecutingAssembly().ManifestModule.GetTypes()) {
                if (type.Namespace != "MRK.Networking.Packets")
                    continue;

                PacketRegInfo regInfo = type.GetCustomAttribute<PacketRegInfo>();
                if (regInfo != null) {
                    if (regInfo.PacketNature == PacketNature.Out)
                        Packet.RegisterOut(regInfo.PacketType, type);
                    else
                        Packet.RegisterIn(regInfo.PacketType, type);
                }
            }

            m_Network = new EGRNetwork("127.0.0.1", EGRConstants.EGR_MAIN_NETWORK_PORT, EGRConstants.EGR_MAIN_NETWORK_KEY);
            m_Network.Connect();
        }

        bool NetFetchTile(string tileSet, MRKTileID id, EGRPacketReceivedCallback<PacketInFetchTile> callback) {
            return m_Network.SendPacket(new PacketOutFetchTile(tileSet, id), DeliveryMethod.ReliableSequenced, callback);
        }

        IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id) {
            PacketInFetchTile response = null;

            void __cb(PacketInFetchTile _response) {
                response = _response;
                EGREventManager.Instance.Register<EGREventNetworkDownloadRequest>(__reqCb);
            }

            EGRDownloadContext downloadContext = null;
            void __reqCb(EGREventNetworkDownloadRequest evt) {
                if (evt.Context.ID == response.DownloadID) {
                    evt.IsAccepted = true;
                    downloadContext = evt.Context;
                    EGREventManager.Instance.Unregister<EGREventNetworkDownloadRequest>(__reqCb);
                }
            }

            if (!NetFetchTile(tileSet, id, __cb)) {
                context.Error = true;
                yield break;
            }

            float timer = 0f;
            bool timedOut = false;
            while (response == null) {
                timer += 0.2f;
                yield return new WaitForSeconds(0.2f);

                if (timer > 3f) {
                    timedOut = true;
                    break;
                }
            }

            if (timedOut || response.Response != EGRStandardResponse.SUCCESS) {
                context.Error = true;
                yield break;
            }

            timer = 0f;
            while (downloadContext == null) {
                timer += 0.2f;
                yield return new WaitForSeconds(0.2f);

                if (timer > 3f) {
                    timedOut = true;
                    break;
                }
            }

            if (timedOut) {
                context.Error = true;
                yield break;
            }

            timer = 0f;
            while (!downloadContext.Complete) {
                timer += 0.2f;
                yield return new WaitForSeconds(0.2f);

                if (timer > 5f) {
                    timedOut = true;
                    break;
                }
            }

            Texture2D tex = new Texture2D(0, 0);
            tex.LoadImage(downloadContext.Data, true); //upload to gpu
            context.Texture = tex;
        }

        IEnumerator Download() {
            MRKTileFetcherContext ctx = new MRKTileFetcherContext();
            yield return Fetch(ctx, "main", new MRKTileID(0, 0, 0));

            if (ctx.Error) {
                Debug.Log("error");
            }
            else
                m_Image.material.mainTexture = ctx.Texture;
        }

        void OnDownloadClick() {
            StartCoroutine(Download());
        }

        void Update() {
            m_Network.UpdateNetwork();
        }
    }
}
