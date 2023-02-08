namespace WsjtxUtils.Compare.Common.Settings
{
    public class ServerSettings
    {
        /// <summary>
        /// Constructs server settings
        /// </summary>
        public ServerSettings() : this("127.0.0.1", 2237)
        {
        }

        /// <summary>
        /// Constructs server settings
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public ServerSettings(string address, int port)
        {
            Address = address;
            Port = port;
        }

        /// <summary>
        /// IP Address for the server
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Port for the server
        /// </summary>
        public int Port { get; set; }
    }
}
