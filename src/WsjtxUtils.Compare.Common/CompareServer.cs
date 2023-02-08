using Serilog;
using System.Collections.Concurrent;
using System.Net;
using WsjtxUtils.Compare.Common.Settings;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxMessages.QsoParsing;
using WsjtxUtils.WsjtxUdpServer;

namespace WsjtxUtils.Compare.Common
{
    /// <summary>
    /// WSJT-X compare server
    /// </summary>
    public class CompareServer : WsjtxUdpServerBaseAsyncMessageHandler
    {
        /// <summary>
        /// State for all connected clients
        /// </summary>
        private readonly ConcurrentDictionary<string, CompareClientState> _clients = new ConcurrentDictionary<string, CompareClientState>();

        /// <summary>
        /// WSJT-X UDP Server
        /// </summary>
        private readonly WsjtxUdpServer.WsjtxUdpServer _server;

        /// <summary>
        /// Various settings for the application
        /// </summary>
        private readonly CompareSettings _settings;

        /// <summary>
        /// Compare server constructor
        /// </summary>
        /// <param name="settings"></param>
        public CompareServer(CompareSettings? settings)
        {
            _settings = settings ?? new CompareSettings();
            _server = new WsjtxUdpServer.WsjtxUdpServer(this, IPAddress.Parse(_settings.Server.Address), _settings.Server.Port);
        }

        /// <summary>
        /// Handle decode messages from the clients
        /// </summary>
        /// <param name="server"></param>
        /// <param name="message"></param>
        /// <param name="endPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task HandleDecodeMessageAsync(WsjtxUdpServer.WsjtxUdpServer server, Decode message, EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            await base.HandleDecodeMessageAsync(server, message, endPoint, cancellationToken);
            // parse the decode message to a qso
            var qso = WsjtxQsoParser.ParseDecode(message);

            // get the right state for the given client id and add the qso
            ClientStateFor(message.Id).AddQso(qso);
        }

        /// <summary>
        /// Runs the compare server
        /// </summary>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationTokenSource cancellationTokenSource)
        {
            Log.Information("Starting server at {server}:{port}", _settings.Server.Address, _settings.Server.Port);
            Log.Information("Press CTRL-C to stop");
            _server.Start();

            try
            {
                // write the csv headers
                CsvWriterForWsjtx.WriteHeadersIfNeeded(_settings);

                // kick off the periodic task to perform the comparison
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PeriodicTimerSeconds));
                while (!cancellationTokenSource.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(cancellationTokenSource.Token))
                    ProcessDecodes();
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (AggregateException aggregateException)
            {
                aggregateException.Handle((innerException) =>
                {
                    if (innerException is not TaskCanceledException)
                    {
                        Log.Error(innerException, "An exception occured.");
                        return false;
                    }
                    return true;
                });
            }
            finally
            {
                Log.Information("Stopping server at {server}:{port}", _settings.Server.Address, _settings.Server.Port);
                _server.Stop();
            }
        }


        #region private methods

        /// <summary>
        /// Process the clients decoded message
        /// </summary>
        private void ProcessDecodes()
        {
            // get the clients
            var clients = TopTwoClientsAlphabetized();
            if (clients.Count < 2)
            {
                Log.Information("Waiting for clients & data...");
                return;
            }
            var clientA = clients[0];
            var clientB = clients[1];

            // check for decoded messages
            if (!HasAnyDecodes(clientA.DecodedStations) && !HasAnyDecodes(clientB.DecodedStations))
            {
                Log.Information("No decodes found for {sourceA} and {sourceB}, waiting for more messages.", clientA.Id, clientB.Id);
                return;
            }

            // check for correlated decodes
            ProcessCorrelated(clientA, clientB);

            // check for uncorrelated decodes
            ProcessUncorrelated(clientA, _settings.UncorrelatedDecodesFileSourceA);
            ProcessUncorrelated(clientB, _settings.UncorrelatedDecodesFileSourceB);
        }

        /// <summary>
        /// Process any correlated decodes if found
        /// </summary>
        /// <param name="clientA"></param>
        /// <param name="clientB"></param>
        private void ProcessCorrelated(CompareClientState clientA, CompareClientState clientB)
        {
            // find correlated decodes
            var correlatedDecodes = FindCorrelatedDecodes(clientA, clientB);
            if (!correlatedDecodes.Any())
                return;

            // write correlated decodes to a file
            Log.Information("Writing {num} correlated decode(s) for {sourceA} and {sourceB}", correlatedDecodes.Count, clientA.Id, clientB.Id);
            CsvWriterForWsjtx.WriteCorrelatedDecodesToFile(correlatedDecodes, _settings.CorrelatedDecodesFile);

            // remove any correlated decodes from their associated client
            RemoveCorrelatedDecodes(correlatedDecodes, clientA, clientB);
        }

        /// <summary>
        /// Process any uncorrelated decodes if found
        /// </summary>
        /// <param name="client"></param>
        /// <param name="filename"></param>
        private void ProcessUncorrelated(CompareClientState client, string filename)
        {
            if (!HasAnyDecodes(client.DecodedStations))
                return;

            var uncorrelated = FindUncorrelated(client, _settings.AmountOfSecondsInBufferWithNoMatchIsUncorrelated);
            if (!uncorrelated.Any())
            {
                Log.Information("Found {num} decode(s) remaining in the buffer for {source}", NumberOfDecodes(client.DecodedStations), client.Id);
                return;
            }

            Log.Information("Writing {num} uncorrelated decode(s) for {source}", uncorrelated.Count(), client.Id);
            CsvWriterForWsjtx.WriteUncorrelatedDecodesToFile(uncorrelated, filename);

            // remove any uncorrelated decodes from the client
            foreach (var qso in uncorrelated)
                client.RemoveQso(qso);
        }

        /// <summary>
        /// Searches for correlated decodes for two clients
        /// </summary>
        /// <param name="clientA"></param>
        /// <param name="clientB"></param>
        /// <returns></returns>
        private List<(WsjtxQso SourceA, WsjtxQso SourceB)> FindCorrelatedDecodes(CompareClientState clientA, CompareClientState clientB)
        {
            List<(WsjtxQso SourceA, WsjtxQso SourceB)> results = new();
            var qsosClientA = clientA.DecodedStations.Values.ToList();
            var qsosClientB = clientB.DecodedStations.Values.ToList();

            foreach (var qsos in qsosClientA)
            {
                foreach (var qso in qsos)
                {
                    // compare A vs the list from B
                    var correlated = CompareDecodes(qso, qsosClientB);
                    if (!correlated.HasValue)
                        continue;

                    results.Add((correlated.Value.SourceA, correlated.Value.SourceB));
                }
            }

            return results;
        }

        /// <summary>
        /// Given a source <see cref="WsjtxQso"/> compare if for matches in the collections of bags
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private (WsjtxQso SourceA, WsjtxQso SourceB)? CompareDecodes(WsjtxQso source, ICollection<ConcurrentBag<WsjtxQso>> targets)
        {
            List<(WsjtxQso SourceA, WsjtxQso SourceB)> results = new List<(WsjtxQso SourceA, WsjtxQso SourceB)>();
            foreach (ConcurrentBag<WsjtxQso> bag in targets)
            {
                var correlated = bag.Where(decode => IsCorrelatedDecode(source, decode)).ToList();
                if (correlated.Count == 1)
                {
                    return (source, correlated.First());
                }
                else if (correlated.Count > 1)
                    Log.Warning("Expecting only 1 correlated decode, found {num}?", correlated.Count);
            }
            return null;
        }


        /// <summary>
        /// Fetch the current state for a given WSJT-X client
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        private CompareClientState ClientStateFor(string clientId)
        {
            return _clients.AddOrUpdate(clientId, (id) =>
            {
                return new CompareClientState(id);
            },
            (id, state) =>
            {
                return state;
            });
        }


        /// <summary>
        /// Sort the client list alphabetically by client name
        /// </summary>
        /// <returns></returns>
        private List<CompareClientState> TopTwoClientsAlphabetized()
        {
            return _clients.OrderBy(station => station.Key).Take(2).Select(kvp => kvp.Value).ToList();
        }

        /// <summary>
        /// Check if two decoded messages have the same callsigns, state, and are within the wiggle time
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="wiggleTimeInSeconds"></param>
        /// <returns></returns>
        private bool IsCorrelatedDecode(WsjtxQso a, WsjtxQso b)
        {
            if (a.DECallsign != b.DECallsign)
                return false;

            if (a.DXCallsign != b.DXCallsign)
                return false;

            if (a.QsoState != b.QsoState)
                return false;

            if (Math.Abs(a.Time.Subtract(b.Time).Duration().Seconds) > _settings.IsCorrelatedDecodeWiggleTimeInSeconds)
                return false;

            return true;
        }
        #endregion

        #region static methods
        /// <summary>
        /// Check if the target has any values and a count greater than 0
        /// </summary>
        /// <param name="decodedStations"></param>
        /// <returns></returns>
        private static bool HasAnyDecodes(ConcurrentDictionary<string, ConcurrentBag<WsjtxQso>> decodedStations)
        {
            return !decodedStations.IsEmpty && decodedStations.Values.Any(bag => bag.Count > 0);
        }

        /// <summary>
        /// The number of decodes for all entries in the decoded stations list
        /// </summary>
        /// <param name="decodedStations"></param>
        /// <returns></returns>
        private static int NumberOfDecodes(ConcurrentDictionary<string, ConcurrentBag<WsjtxQso>> decodedStations)
        {
            if (decodedStations.IsEmpty)
                return 0;

            return decodedStations.Values.Sum(bag => bag.Count);
        }

        /// <summary>
        /// Searches for uncorrelated decodes for the client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="maxSecondsInQueue"></param>
        /// <returns></returns>
        private static List<WsjtxQso> FindUncorrelated(CompareClientState client, double maxSecondsInQueue)
        {
            List<WsjtxQso> results = new();
            foreach (var decodes in client.DecodedStations.Values)
            {
                if (decodes == null)
                    continue;

                foreach (var decode in decodes)
                    if (decode != null && (DateTime.UtcNow - decode.Time).TotalSeconds > maxSecondsInQueue)
                        results.Add(decode);
            }
            return results;
        }

        /// <summary>
        /// Remove the correlated decodes from the client state
        /// </summary>
        /// <param name="correlated"></param>
        /// <param name="clientA"></param>
        /// <param name="clientB"></param>
        private static void RemoveCorrelatedDecodes(List<(WsjtxQso SourceA, WsjtxQso SourceB)> correlated, CompareClientState clientA, CompareClientState clientB)
        {
            foreach (var itemToRemove in correlated)
            {
                clientA.RemoveQso(itemToRemove.SourceA);
                clientB.RemoveQso(itemToRemove.SourceB);
            }
        }
        #endregion
    }
}
