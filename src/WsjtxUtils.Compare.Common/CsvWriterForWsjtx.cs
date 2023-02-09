using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using WsjtxUtils.Compare.Common.Settings;
using WsjtxUtils.WsjtxMessages.QsoParsing;

namespace WsjtxUtils.Compare.Common
{
    /// <summary>
    /// Helper class for CSV writing
    /// </summary>
    public static class CsvWriterForWsjtx
    {
        /// <summary>
        /// <see cref="CsvConfiguration"/> that prevent headers from being written
        /// </summary>
        private static CsvConfiguration NoHeadersConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        };

        /// <summary>
        /// Write all the headers for all files
        /// </summary>
        /// <param name="settings"></param>
        public static void WriteHeadersIfNeeded(CompareSettings settings)
        {
            WriteCorrelatedDecodesHeadersIfNeeded(settings.CorrelatedDecodesFile);
            WriteUncorrelatedDecodesHeadersIfNeeded(settings.UncorrelatedDecodesFileSourceA);
            WriteUncorrelatedDecodesHeadersIfNeeded(settings.UncorrelatedDecodesFileSourceB);
        }

        /// <summary>
        /// Write correlated header row if the file does not exist
        /// </summary>
        /// <param name="filename"></param>
        public static void WriteCorrelatedDecodesHeadersIfNeeded(string filename)
        {
            if (!File.Exists(filename))
                using (var writer = new StreamWriter(filename))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<QsoTupleMap>();

                    csv.WriteHeader<(WsjtxQso SourceA, WsjtxQso SourceB)>();
                    csv.NextRecord();
                }
        }

        /// <summary>
        /// Write uncorrelated header row if the file does not exist
        /// </summary>
        /// <param name="filename"></param>
        public static void WriteUncorrelatedDecodesHeadersIfNeeded(string filename)
        {
            if (!File.Exists(filename))
                using (var writer = new StreamWriter(filename))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<DecodeMap>();

                    csv.WriteHeader<WsjtxQso>();
                    csv.NextRecord();
                }
        }

        /// <summary>
        /// Writes a list of correlated decodes to the specified filename
        /// </summary>
        /// <param name="correlatedDecodes"></param>
        public static void WriteCorrelatedDecodesToFile(List<(WsjtxQso SourceA, WsjtxQso SourceB)> correlatedDecodes, string filename)
        {
            using (var writer = new StreamWriter(filename, true))
            using (var csv = new CsvWriter(writer, NoHeadersConfig))
            {
                csv.Context.RegisterClassMap<QsoTupleMap>();
                csv.WriteRecords(correlatedDecodes);
            }
        }

        /// <summary>
        ///  Writes a list of uncorrelated decodes to the specified filename
        /// </summary>
        /// <param name="source"></param>
        /// <param name="filename"></param>
        public static void WriteUncorrelatedDecodesToFile(List<WsjtxQso> source, string filename)
        {
            using (var writer = new StreamWriter(filename, true))
            using (var csv = new CsvWriter(writer, NoHeadersConfig))
            {
                csv.Context.RegisterClassMap<DecodeMap>();
                csv.WriteRecords(source);
            }
        }
    }
}
