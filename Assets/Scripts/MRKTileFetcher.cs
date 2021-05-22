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
        public abstract IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, bool low = false);
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

        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, bool low = false) {
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
        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id, bool low = false) {
        __start:
            MRKTilesetProvider provider = MRKTileRequestor.Instance.GetCurrentTilesetProvider();
            string path = string.Format(provider.API, id.Z, id.X, id.Y).Replace("-", "%2D");
            if (low) {
                //lower res
                path = path.Replace("@2x", "")
                    .Replace(".png", ".png32");
            }

            UnityWebRequest req = UnityWebRequestTexture.GetTexture(path, false);
            req.SendWebRequest();

            while (!req.isDone) {
                yield return new WaitForSeconds(0.2f);
            }

            if (req.result != UnityWebRequest.Result.Success) {
                context.Error = true;
                yield break;
            }

            context.Texture = DownloadHandlerTexture.GetContent(req);
            context.Data = req.downloadHandler.data;

            if (context.Texture == null) goto __start;
        }

        /*public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id) {
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

            if (!EGRMain.Instance.NetFetchTile(tileSet, id, __cb)) {
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
            tex.LoadImage(downloadContext.Data, false); //upload to gpu
            context.Texture = tex;
        }*/
    }
}
