using System.Collections.Concurrent;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxMessages.QsoParsing;

namespace WsjtxUtils.Compare.Common
{
    /// <summary>
    /// Class used to manage the state of a connected WSJT-X client
    /// </summary>
    public class CompareClientState
    {
        public CompareClientState(string id, Status? status = null)
        {
            Id = id;
            Status = status;
            DecodedStations = new ConcurrentDictionary<string, ConcurrentBag<WsjtxQso>>();
        }

        /// <summary>
        /// WSJT-X client id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Status for the client
        /// </summary>
        public Status? Status { get; set; }

        /// <summary>
        /// Dictionary of decoded stations keyed by remote callsign
        /// </summary>
        public ConcurrentDictionary<string, ConcurrentBag<WsjtxQso>> DecodedStations { get; private set; }

        /// <summary>
        /// Add a qso to the dictionary of decoded stations
        /// </summary>
        /// <param name="decode"></param>
        /// <param name="expiryInSeconds"></param>
        public void AddQso(WsjtxQso qso)
        {
            DecodedStations.AddOrUpdate(qso.DECallsign, (deCallsign) =>
            {
                var bag = new ConcurrentBag<WsjtxQso>
                {
                    qso
                };
                return bag;
            },
            (deCallsign, wsjtxQsoBag) =>
            {
                wsjtxQsoBag.Add(qso);
                return wsjtxQsoBag;
            });
        }

        /// <summary>
        /// Remove a qso from the dictionary of decoded stations
        /// </summary>
        /// <param name="qso"></param>
        public void RemoveQso(WsjtxQso qso)
        {
            if (!DecodedStations.TryGetValue(qso.DECallsign, out ConcurrentBag<WsjtxQso>? bag))
                return;

            if (bag == null || !bag.Contains(qso))
                return;

            if (bag.TryTake(out WsjtxQso? removedQso))
            {
                //TODO: remove the dictonary if there are no items in the bag?
            }
        }
    }
}
