namespace MRK.Networking.Packets {
    public enum PacketType : ushort {
        None = 0x00,

        //xor key
        XKEY = 0x01,
        //device info
        DEVINFO = 0x02,
        //download request
        DWNLDREQ = 0x03,
        //download
        DWNLD = 0x04,
        //network info
        NETINFO = 0x05,

        //register account
        REGACC = 0x10,
        //login
        LGNACC = 0x11,
        //login with token
        LGNACCTK = 0x12,
        //login with device
        LGNACCDEV = 0x13,
        //logout
        LGNOUT = 0x14,
        //update account info
        UPDACC = 0x15,
        //update account password
        UPDACCPWD = 0x16,

        //standard response
        STDRSP = 0x20,
        //test packet
        TEST = 0x21,
        //standard json response
        STDJSONRSP = 0x22,

        //fetch places
        PLCFETCH = 0x30,
        //fetch tile
        TILEFETCH = 0x31,
        //fetch place id
        PLCIDFETCH = 0x32,
        //fetch place v2
        PLCFETCHV2 = 0x33,

        //geo autocomplete
        GEOAUTOCOMPLETE = 0x40,
        //query directions
        QUERYDIRS = 0x41,

        //wte query
        WTEQUERY = 0x50,

        //cdn request
        CDNRESOURCE = 0x60,

        MAX
    }
}