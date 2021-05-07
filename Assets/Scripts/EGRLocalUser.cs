using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MRK {
    public struct EGRProxyUser {
        public string FirstName;
        public string LastName;
        public string Email;
        public sbyte Gender;
        public string Token;
    }

    public class EGRLocalUser {
        public string Email { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public sbyte Gender { get; private set; }
        public string Token { get; private set; }

        public static EGRLocalUser Instance { get; private set; }

        public EGRLocalUser(EGRProxyUser user) {
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Gender = user.Gender;
            Token = user.Token;
        }

        public static void Initialize(EGRProxyUser? user) {
            if (user.HasValue) {
                Instance = new EGRLocalUser(user.Value);
                PlayerPrefs.SetString(EGRConstants.EGR_LOCALPREFS_LOCALUSER, JsonUtility.ToJson(user.Value));
            }
            else
                Instance = null;
        }

        public override string ToString() {
            return $"EMAIL={Email} | FirstName={FirstName} | LastName={LastName} | Token={Token}";
        }
    }
}
