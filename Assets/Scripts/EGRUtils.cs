using UnityEngine;

namespace MRK {
    public class EGRUtils {
        static readonly string ms_Charset;

        static EGRUtils() {
            ms_Charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        }

        public static string GetRandomString(int len) {
            string str = "";
            for (int i = 0; i < len; i++)
                str += ms_Charset[Random.Range(0, ms_Charset.Length)];

            return str;
        }
    }
}
