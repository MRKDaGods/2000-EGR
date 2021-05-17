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
        public abstract IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id);
    }

    public class MRKTileFetcherContext {
        public bool Error;
        public Texture2D Texture;
        public byte[] Data;
    }

    public class MRKFileTileFetcher : MRKTileFetcher {
        string GetFolderPath(string tileSet) {
            return $"{Application.persistentDataPath}\\Tiles\\{tileSet}";
        }

        public bool Exists(string tileSet, MRKTileID id) {
            return File.Exists($"{GetFolderPath(tileSet)}\\{id.GetHashCode()}.png");
        }

        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id) {
            string dir = GetFolderPath(tileSet);
            if (!Directory.Exists(dir)) {
                context.Error = true;
                yield break;
            }

            string path = $"{dir}\\{id.GetHashCode()}.png";
            if (!File.Exists(path)) {
                context.Error = true;
                yield break;
            }

            UnityWebRequest req = UnityWebRequestTexture.GetTexture($"file:///{path}", false);
            req.SendWebRequest();

            while (!req.isDone) {
                yield return new WaitForSeconds(0.2f);
            }

            if (req.result != UnityWebRequest.Result.Success) {
                context.Error = true;
                Debug.Log(req.error);
                yield break;
            }

            context.Texture = DownloadHandlerTexture.GetContent(req);
        }

        public async Task SaveToDisk(string tileset, MRKTileID id, byte[] tex) {
            string dir = GetFolderPath(tileset);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            string path = $"{dir}\\{id.GetHashCode()}.png";
#if MRK_PROFILE
            DateTime t = DateTime.Now;
#endif

            using (FileStream fs = File.OpenWrite(path)) {
                await fs.WriteAsync(tex, 0, tex.Length);
            }

#if MRK_PROFILE
            TimeSpan t0 = DateTime.Now - t;
            Debug.Log($"Saved {id} {t0.TotalMilliseconds}ms");
#endif
        }
    }

    public class MRKRemoteTileFetcher : MRKTileFetcher {
        public override IEnumerator Fetch(MRKTileFetcherContext context, string tileSet, MRKTileID id) {
        __start:
            MRKTilesetProvider provider = MRKTileRequestor.Instance.GetCurrentTilesetProvider();
            string path = string.Format(provider.API, id.Z, id.X, id.Y).Replace("-", "%2D");
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
