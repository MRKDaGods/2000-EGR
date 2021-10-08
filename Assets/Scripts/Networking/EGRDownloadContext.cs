using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK.Networking {
    public class EGRDownloadContext {
        int m_Sections;
        byte[][] m_Data;
        int m_Length;

        public ulong ID { get; private set; }
        public byte[] Data { get; private set; }
        public bool Complete { get; private set; }

        public EGRDownloadContext(ulong id, int sections) {
            ID = id;
            m_Sections = sections;
        }

        public void AllocateBuffer() {
            m_Data = new byte[m_Sections][];
        }

        public void SetData(int progress, byte[] data) {
            if (progress < m_Sections) {
                //MRKLogger.Log($"{ID} prog={progress} set {data.Length}");
                m_Data[progress] = data;
                m_Length += data.Length;
            }
            else
                BuildMainBuffer();
        }

        void BuildMainBuffer() {
            MRKLogger.Log($"{ID} building");
            Data = new byte[m_Length];
            for (int i = 0; i < m_Sections; i++) {
                for (int j = 0; j < m_Data[i].Length; j++) {
                    Data[i * 20000 + j] = m_Data[i][j];
                }
            }

            Complete = true;
        }
    }
}
