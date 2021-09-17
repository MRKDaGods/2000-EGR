namespace MRK {
    public enum EGRAuthenticationType {
        Default,
        Device,
        Token
    }

    public struct EGRAuthenticationData {
        public EGRAuthenticationType Type;
        public string Reserved0; //email/token
        public string Reserved1; //pwd
        public string Reserved2; //ex info
        public bool Reserved3; //remember
        public bool Reserved4; //skip anims
    }
}
