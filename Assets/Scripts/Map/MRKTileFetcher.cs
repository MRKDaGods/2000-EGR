#define MRK_PROFILE

using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using MRK.Networking;
using MRK.Networking.Packets;

namespace MRK {
    public abstract class MRKTileFetcher {
        public abstract IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, Reference<UnityWebRequest> request, bool low = false);
    }

    public class MRKTileFetcherContext {
        public bool Error;
        public Texture2D Texture;
        public byte[] Data;
    }

    public class MRKFileTileFetcher : MRKTileFetcher {
        public string GetFolderPath(string tileSet) {
            return $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}Tiles{Path.DirectorySeparatorChar}{tileSet}";
        }

        public bool Exists(string tileSet, MRKTileID id, bool low = false) {
            string lowPrefix = low ? "low_" : "";
            return File.Exists($"{GetFolderPath(tileSet)}{Path.DirectorySeparatorChar}{lowPrefix}{id.GetHashCode()}.png");
        }

        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, Reference<UnityWebRequest> request, bool low = false) {
            string dir = GetFolderPath(tileSet);
            if (!Directory.Exists(dir)) {
                context.Error = true;
                yield break;
            }

            string lowPrefix = low ? "low_" : "";
            string path = $"{dir}{Path.DirectorySeparatorChar}{lowPrefix}{id.GetHashCode()}.png";
            if (!File.Exists(path)) {
                context.Error = true;
                yield break;
            }

            UnityWebRequest req = UnityWebRequestTexture.GetTexture($"file:///{path}", true);
            request.Value = req;
            req.SendWebRequest();

            while (!req.isDone) {
                yield return new WaitForSeconds(0.2f);
            }

            if (req.result != UnityWebRequest.Result.Success) {
                context.Data = req.downloadHandler.data;
                context.Error = true;
                Debug.Log(req.error + req.downloadHandler.error);
                yield break;
            }

            context.Texture = DownloadHandlerTexture.GetContent(req);
            //req.downloadHandler.Dispose();
        }

        public async Task SaveToDisk(string tileset, MRKTileID id, byte[] tex, bool low) {
            string dir = GetFolderPath(tileset);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            string lowPrefix = low ? "low_" : "";
            string path = $"{dir}{Path.DirectorySeparatorChar}{lowPrefix}{id.GetHashCode()}.png";

            using (FileStream fs = File.OpenWrite(path)) {
                await fs.WriteAsync(tex, 0, tex.Length);
            }
        }
    }

    public class MRKRemoteTileFetcher : MRKTileFetcher {
        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, Reference<UnityWebRequest> request, bool low = false) {
            EGRClientSideCDNNetwork cdn = EGRMain.Instance.NetworkingClient.ClientSideCDNNetwork;
            if (cdn == null) {
                context.Error = true;
                Debug.Log("CDN is null");
                yield break;
            }

            Reference<bool> actionDoneRef = ReferencePool<bool>.Default.Rent();
            PacketInFetchTile responsePacket = null;
            actionDoneRef.Value = false;
            if (!cdn.FetchTile(tileSet, id, low, (response) => {
                if (actionDoneRef == null) //externally released
                    return;

                actionDoneRef.Value = true;
                responsePacket = response;

            })) {
                Debug.Log("CDN not connected");
                goto __end;
            }

            float time = 0f;
            while (!actionDoneRef.Value) {
                yield return new WaitForSeconds(0.2f);
                time += 0.2f;

                if (time >= 10f) {
                    context.Error = true;
                    Debug.Log("Timed out");
                    goto __end;
                }
            }

            if (responsePacket != null) {
                if (!responsePacket.Success) {
                    Debug.Log("Server returned false");
                    context.Error = true;
                    goto __end;
                }

                context.Data = responsePacket.Data;
                context.Texture = new Texture2D(1, 1);
                context.Texture.LoadImage(responsePacket.Data);
            }
            else {
                Debug.Log("ResponsePacket is null, is it even possible?");
                context.Error = true;
            }

        __end:
            ReferencePool<bool>.Default.Free(actionDoneRef);
            actionDoneRef = null;
            yield break;

            /*__start:
                MRKTilesetProvider provider = MRKTileRequestor.Instance.GetCurrentTilesetProvider();
                string path = string.Format(provider.API, id.Z, id.X, id.Y).Replace("-", "%2D");
                if (low) {
                    //lower res
                    path = path.Replace("@2x", "");
                        //.Replace("/512/", "/256/");
                }

                UnityWebRequest req = UnityWebRequestTexture.GetTexture(path, false);
                request.Value = req;
                req.SendWebRequest();

                while (!req.isDone) {
                    yield return new WaitForSeconds(0.2f);
                }

                if (req.result != UnityWebRequest.Result.Success) {
                    context.Error = true;
                    Debug.Log(req.error + req.downloadHandler.error);
                    yield break;
                }

                context.Texture = DownloadHandlerTexture.GetContent(req);
                context.Data = req.downloadHandler.data;

                if (context.Texture == null) goto __start;*/
        }
    }
}
