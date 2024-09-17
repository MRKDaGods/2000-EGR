namespace MRK.Authentication
{
    public enum AuthenticationType
    {
        Default,
        Device,
        Token
    }

    public struct AuthenticationData
    {
        public AuthenticationType Type;
        public string Reserved0; //email/token
        public string Reserved1; //pwd
        public string Reserved2; //ex info
        public bool Reserved3; //remember
        public bool Reserved4; //skip anims
    }
}
