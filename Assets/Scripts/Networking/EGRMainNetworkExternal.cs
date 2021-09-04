using MRK.Networking.Packets;
using UnityEngine;

namespace MRK.Networking {
    public class EGRMainNetworkExternal : IEGRNetworkExternal {
        EGRNetwork m_Network;

        public void SetNetwork(EGRNetwork network) {
            m_Network = network;
        }

        /// <summary>
        /// Attempts to send a network request to register an account
        /// </summary>
        /// <param name="name">Full name</param>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <param name="callback">Response callback</param>
        public bool RegisterAccount(string name, string email, string password, EGRPacketReceivedCallback<PacketInStandardResponse> callback) {
            //make sure we hash the password
            return m_Network.SendPacket(new PacketOutRegisterAccount(name, email, MRKCryptography.Hash(password)), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to log into an account
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool LoginAccount(string email, string password, EGRPacketReceivedCallback<PacketInLoginAccount> callback) {
            //hash the password again
            return m_Network.SendPacket(new PacketOutLoginAccount(email, MRKCryptography.Hash(password)), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to log into an account with a token
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool LoginAccountToken(string token, EGRPacketReceivedCallback<PacketInLoginAccount> callback) {
            return m_Network.SendPacket(new PacketOutLoginAccountToken(token), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to log into an account with a device id
        /// </summary>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool LoginAccountDev(EGRPacketReceivedCallback<PacketInLoginAccount> callback) {
            return m_Network.SendStationaryPacket(PacketType.LGNACCDEV, DeliveryMethod.ReliableOrdered, callback, writer => {
                //we'll send the deviceName and deviceModel as our first/last name
                writer.WriteString(SystemInfo.deviceName);
                writer.WriteString(SystemInfo.deviceModel);
            });
        }

        /// <summary>
        /// Attempts to send a network request to update an account's information
        /// </summary>
        /// <param name="name">Full name</param>
        /// <param name="email">Email</param>
        /// <param name="gender">Gender</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool UpdateAccountInfo(string name, string email, sbyte gender, EGRPacketReceivedCallback<PacketInStandardResponse> callback) {
            return m_Network.SendPacket(new PacketOutUpdateAccountInfo(EGRLocalUser.Instance.Token, name, email, gender), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to fetch a place's data
        /// </summary>
        /// <param name="cid">Place ID</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool FetchPlace(ulong cid, EGRPacketReceivedCallback<PacketInFetchPlaces> callback) {
            return m_Network.SendPacket(new PacketOutFetchPlaces(cid), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to fetch places' IDs inside a bounding box at a certain zoom level
        /// </summary>
        /// <param name="ctx">Fetching context ID</param>
        /// <param name="minLat">Minimum latitude</param>
        /// <param name="minLng">Minimum longitude</param>
        /// <param name="maxLat">Maximum latitude</param>
        /// <param name="maxLng">Maximum longitude</param>
        /// <param name="zoom">Zoom level</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool FetchPlacesIDs(ulong ctx, double minLat, double minLng, double maxLat, double maxLng, int zoom, EGRPacketReceivedCallback<PacketInFetchPlacesIDs> callback) {
            return m_Network.SendPacket(new PacketOutFetchPlacesIDs(ctx, minLat, minLng, maxLat, maxLng, zoom), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to fetch places' data inside a bounding box at a certain zoom level
        /// </summary>
        /// <param name="hash">Tile hash</param>
        /// <param name="minLat">Minimum latitude</param>
        /// <param name="minLng">Minimum longitude</param>
        /// <param name="maxLat">Maximum latitude</param>
        /// <param name="maxLng">Maximum longitude</param>
        /// <param name="zoom">Zoom level</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool FetchPlacesV2(int hash, double minLat, double minLng, double maxLat, double maxLng, int zoom, EGRPacketReceivedCallback<PacketInFetchPlacesV2> callback) {
            return m_Network.SendPacket(new PacketOutFetchPlacesV2(hash, minLat, minLng, maxLat, maxLng, zoom), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to fetch a raster tile
        /// </summary>
        /// <param name="tileSet">Tileset</param>
        /// <param name="id">Tile ID</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool FetchTile(string tileSet, MRKTileID id, EGRPacketReceivedCallback<PacketInFetchTile> callback) {
            return false; // return m_Network.SendPacket(new PacketOutFetchTile(tileSet, id), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to update an account's password
        /// </summary>
        /// <param name="pass">New password</param>
        /// <param name="logoutAll">Logout from other sessions?</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool UpdateAccountPassword(string pass, bool logoutAll, EGRPacketReceivedCallback<PacketInStandardResponse> callback) {
            return m_Network.SendPacket(new PacketOutUpdatePassword(EGRLocalUser.Instance.Token, pass, logoutAll), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to autocomplete a geographical query
        /// </summary>
        /// <param name="query">Query</param>
        /// <param name="proximity">Proximity location</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool GeoAutoComplete(string query, Vector2d proximity, EGRPacketReceivedCallback<PacketInGeoAutoComplete> callback) {
            //query cannot be of length greater than 256
            if (query.Length > 256)
                return false;

            return m_Network.SendPacket(new PacketOutGeoAutoComplete(query, proximity), DeliveryMethod.ReliableOrdered, callback);
        }

        /// <summary>
        /// Attempts to send a network request to query directions between 2 points
        /// </summary>
        /// <param name="from">Start location</param>
        /// <param name="to">End location</param>
        /// <param name="profile">Routing profile</param>
        /// <param name="callback">Response callback</param>
        /// <returns></returns>
        public bool QueryDirections(Vector2d from, Vector2d to, byte profile, EGRPacketReceivedCallback<PacketInStandardJSONResponse> callback) {
            //0=driving, 1=walking, 2=cycling, 3+ = invalid
            if (profile > 2)
                return false;

            return m_Network.SendPacket(new PacketOutQueryDirections(from, to, profile), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool WTEQuery(byte people, int price, string cuisine, EGRPacketReceivedCallback<PacketInWTEQuery> callback) {
            return m_Network.SendPacket(new PacketOutWTEQuery(people, price, cuisine), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool RetrieveNetInfo(EGRPacketReceivedCallback<PacketInRetrieveNetInfo> callback) {
            return m_Network.SendPacket(new PacketOutRetrieveNetInfo(), DeliveryMethod.ReliableOrdered, callback);
        }

        public bool CancelTileFetch(string tileset, int hash, bool low) {
            return m_Network.SendPacket(new PacketOutCancelFetchTile(tileset, hash, low), DeliveryMethod.ReliableUnordered);
        }
    }
}
