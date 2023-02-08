using CsvHelper.Configuration;
using System.Globalization;
using WsjtxUtils.WsjtxMessages.QsoParsing;

namespace WsjtxUtils.Compare.Common
{
    /// <summary>
    /// The CSV map object for a WSJT-X decode
    /// </summary>
    internal sealed class DecodeMap : ClassMap<WsjtxQso>
    {
        public DecodeMap()
        {
            var index = 0;

            Map(qso => qso.Time).TypeConverterOption.Format("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK")
                .TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture)
                .Name($"DateTime").Index(index++);

            Map(qso => qso.Source.Id).Name($"ClientID").Index(index++);
            Map(qso => qso.DECallsign).Name($"DECallsign").Index(index++);
            Map(qso => qso.DXCallsign).Name($"DXCallsign").Index(index++);
            Map(qso => qso.Report).Name($"Report").Index(index++);
            Map(qso => qso.QsoState).Name($"QsoState").Index(index++);
            Map(qso => qso.Source.OffsetFrequencyHz).Name($"OffsetFrequencyHz").Index(index++);
            Map(qso => qso.Source.OffsetTimeSeconds).Name($"OffsetTimeSeconds").Index(index++);
            Map(qso => qso.Source.Snr).Name($"ReceiverSNR").Index(index++);
            Map(qso => qso.Source.Time).Name($"Milliseconds Since Midnight").Index(index++);
            Map(qso => qso.LowConfidence).Name($"LowConfidence").Index(index++);
        }
    }

    /// <summary>
    /// The CSV map object for correlated WSJT-X decodes
    /// </summary>
    internal class CorrelateDecodeMap : ClassMap<WsjtxQso>
    {
        public CorrelateDecodeMap(string source, int index = 0)
        {
            Map(qso => qso.Time).TypeConverterOption.Format("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK")
                .TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture)
                .Name($"{source} DateTime").Index(index++);

            Map(qso => qso.Source.Id).Name($"{source} ClientID").Index(index++);
            Map(qso => qso.DECallsign).Name($"{source} DECallsign").Index(index++);
            Map(qso => qso.DXCallsign).Name($"{source} DXCallsign").Index(index++);
            Map(qso => qso.Report).Name($"{source} Report").Index(index++);
            Map(qso => qso.QsoState).Name($"{source} QsoState").Index(index++);
            Map(qso => qso.Source.OffsetFrequencyHz).Name($"{source} OffsetFrequencyHz").Index(index++);
            Map(qso => qso.Source.OffsetTimeSeconds).Name($"{source} OffsetTimeSeconds").Index(index++);
            Map(qso => qso.Source.Snr).Name($"{source} ReceiverSNR").Index(index++);
            Map(qso => qso.Source.Time).Name($"{source} Milliseconds Since Midnight").Index(index++);
            Map(qso => qso.LowConfidence).Name($"{source} LowConfidence").Index(index++);
        }
    }

    /// <summary>
    /// The QSO Tuple map used for CSV writing the two sources
    /// </summary>
    internal sealed class QsoTupleMap : ClassMap<(WsjtxQso SourceA, WsjtxQso SourceB)>
    {
        public QsoTupleMap()
        {
            References<SourceACorrelateDecodeMap>(m => m.SourceA);
            References<SourceBCorrelateDecodeMap>(m => m.SourceB);
        }
    }

    /// <summary>
    /// Override the col name for Source A
    /// </summary>
    internal sealed class SourceACorrelateDecodeMap : CorrelateDecodeMap
    {
        public SourceACorrelateDecodeMap() : base("Source A")
        {
        }
    }

    /// <summary>
    /// Override the col name for Source B
    /// </summary>
    internal sealed class SourceBCorrelateDecodeMap : CorrelateDecodeMap
    {
        public SourceBCorrelateDecodeMap() : base("Source B", 11)
        {
        }
    }
}
