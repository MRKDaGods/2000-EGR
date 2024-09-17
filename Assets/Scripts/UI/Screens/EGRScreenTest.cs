using MRK.Networking.Packets;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI
{
    public class EGRScreenTest : Screen
    {
        private RawImage m_Image;

        protected override void OnScreenInit()
        {
            m_Image = GetElement<RawImage>("Image");
            GetElement<Button>("Button").onClick.AddListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            NetworkingClient.ClientSideCDNNetwork.RequestCDNResource("testImage", new byte[] { 0x0 }, OnCDNResourceReceived);
        }

        private void OnCDNResourceReceived(PacketInRequestCDNResource response)
        {
            Client.Runnable.RunOnMainThread(() =>
            {
                Debug.Log($"CDNRES={response.Response}");

                if (response.Response == Networking.EGRStandardResponse.SUCCESS)
                {
                    Debug.Log($"Loading CDN resource, sz={response.Resource.Length}");
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(response.Resource);

                    m_Image.texture = tex;
                }
            });
        }
    }
}
