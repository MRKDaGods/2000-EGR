namespace MRK.Networking.Packets {
    [PacketRegInfo(PacketNature.In, PacketType.LGNACC)]
    public class PacketInLoginAccount : Packet {
        public EGRStandardResponse Response { get; private set; }
        public EGRProxyUser ProxyUser { get; private set; }
        public string PasswordHash { get; private set; }

        public PacketInLoginAccount() : base(PacketNature.In, PacketType.LGNACC) {
        }

        public PacketInLoginAccount(EGRProxyUser local) : this() {
            Response = EGRStandardResponse.SUCCESS;
            ProxyUser = local;
        }

        public override void Read(PacketDataStream stream) {
            Response = (EGRStandardResponse)stream.ReadByte();

            if (Response == EGRStandardResponse.SUCCESS) {
                string fname = stream.ReadString();
                string lname = stream.ReadString();
                string email = stream.ReadString();
                sbyte gender = stream.ReadSByte();
                string token = stream.ReadString();

                try {
                    //this is only valid if we login with token or an actual acc
                    PasswordHash = stream.ReadString();
                }
                catch {
                    PasswordHash = "";
                }

                ProxyUser = new EGRProxyUser {
                    FirstName = fname,
                    LastName = lname,
                    Email = email,
                    Gender = gender,
                    Token = token
                };
            }
        }
    }
}