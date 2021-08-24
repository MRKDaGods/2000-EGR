using System;
using System.Security.Cryptography;
using System.Text;

namespace MRK {
    public class MRKCryptography {
        /*
        mrk
        1
        \CZ
        \CZ
        1
        mrk
        LrtLpJL4DeGxG5Atjza46OHEiyasOOXtbROGiSbP
        LrtLpJL4DeGxG5Atjza46OHEiyasOOXtbROGiSbP
        ~@F~Bx~♠vWuJusFXHS♠♦}zw[KSA}}jFP`}u[aPb
        ~@F~Bx~♠vWuJusFXHS♠♦}zw[KSA}}jFP`}u[aPb
        ~@F~Bx~♠vWuJusFXHS♠♦}zw[KSA}}jFP`}u[aPb
        KusKwMK3Cb@⌂@Fsm}f31HOBn~ftHH_seUH@nTeW
        LrtLpJL4DeGxG5Atjza46OHEiyasOOXtbROGiSbP #RAW_SALT
        DoubleShotIcedShaken #RAW_SALT key
        HvpHtNH0@aC|C1Epn~e02KLAm}ewKK\pfVKCmWfT
        DoubleShotIcedShaken
        2000EGRvERA1
        ,6!/&►+,7
         &'►+"(&-
        DoubleShotIcedShaken #RAW_SALT_DECIPHER
        EGRbyMRK #RAW_SALT_DECIPHER key
        [pj}szLwpkV|z{Lw~tzq
        EGRbyMRK #RAW_DECIPHER
        1 #RAW_DECIPHER key
        tvcSH|cz
        */
        const string RAW_SALT = @"HvpHtNH0@aC|C1Epn~e02KLAm}ewKK\pfVKCmWfT";
        const string RAW_SALT_DECIPHER = "[pj}szLwpkV|z{Lw~tzq";
        const string RAW_DECIPHER = "tvcSH|cz";

        static string ms_CookedSalt;

        public static string CookedSalt => ms_CookedSalt;

        public static void CookSalt() {
            byte m = 0x99 & 0x01;
            unchecked {
                m |= 0x91;
                m |= 0xF;
                m &= (byte)~(0x81 | 0x17);
                m |= 0x49;
                m &= 0x17 | 0x91;
                m ^= 0xF;
                m ^= (byte)(int)Math.Cosh(20d);
                m -= 0x1A;
            }

            byte[] rawDecipher = GetUTF8Mem(RAW_DECIPHER);
            Cook(rawDecipher, m); //0x31

            byte[] rawSaltDecipher = GetUTF8Mem(RAW_SALT_DECIPHER);
            Cook(rawSaltDecipher, rawDecipher);

            byte[] salt = GetUTF8Mem(RAW_SALT);
            Cook(salt, rawSaltDecipher);

            ms_CookedSalt = Encoding.UTF8.GetString(salt);
        }

        public static byte[] GetUTF8Mem(string str) {
            return Encoding.UTF8.GetBytes(str);
        }

        public static void Cook(byte[] raw, params byte[] energy) {
            for (int i = 0; i < raw.Length; i++) {
                byte b = raw[i];

                for (int j = 0; j < energy.Length; j++) {
                    b ^= energy[j];
                }

                raw[i] = b;
            }
        }

        public static string Hash(string raw) {
            //cook salt before hand
            using (MD5 md5 = MD5.Create()) {
                //apply salt
                byte[] inputBytes = Encoding.ASCII.GetBytes(raw + ms_CookedSalt);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString();
            }
        }
    }
}
