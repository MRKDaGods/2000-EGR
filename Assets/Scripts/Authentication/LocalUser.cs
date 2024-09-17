using UnityEngine;

namespace MRK.Authentication
{
    public struct ProxyUser
    {
        public string FirstName;
        public string LastName;
        public string Email;
        public sbyte Gender;
        public string Token;
    }

    public class LocalUser
    {
        public static string PasswordHash;

        public string Email
        {
            get; private set;
        }

        public string FirstName
        {
            get; private set;
        }

        public string LastName
        {
            get; private set;
        }

        public sbyte Gender
        {
            get; private set;
        }

        public string Token
        {
            get; private set;
        }

        public string FullName
        {
            get
            {
                return $"{FirstName} {LastName}";
            }
        }

        public static LocalUser Instance
        {
            get; private set;
        }

        public LocalUser(ProxyUser user)
        {
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Gender = user.Gender;
            Token = user.Token;
        }

        public static void Initialize(ProxyUser? user)
        {
            if (user.HasValue)
            {
                Instance = new LocalUser(user.Value);
                CryptoPlayerPrefs.Set<string>(EGRConstants.EGR_LOCALPREFS_LOCALUSER, JsonUtility.ToJson(user.Value));
                CryptoPlayerPrefs.Save();
            }
            else
            {
                Instance = null;
            }
        }

        public bool IsDeviceID()
        {
            return Email.EndsWith("@egr.com");
        }

        public override string ToString()
        {
            return $"EMAIL={Email} | FirstName={FirstName} | LastName={LastName} | Token={Token}";
        }
    }
}
