using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using AmedasArchiveWindows.Models;

namespace AmedasArchiveWindows.Data
{
    /// <summary>
    /// 気象庁が提供する過去気象データCSVの非同期ストリームパーサー
    /// </summary>
    public class AmedasCsvParser
    {
        static AmedasCsvParser()
        {
            // .NET 8+ では Shift-JIS (CodePages) が標準で無効化されているため、
            // 静的コンストラクタでエンコーディングプロバイダを一度登録します。
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// CSVのストリームデータを非同期で解析し、DailyWeatherのリストに変換します。
        /// </summary>
        /// <param name="stream">CSVファイルのストリーム</param>
        /// <param name="stationId">観測所ID</param>
        public async Task<List<DailyWeather>> ParseAsync(Stream stream, string stationId)
        {
            var weatherList = new List<DailyWeather>();
            
            // 気象庁のデータは Shift_JIS でエンコードされています
            using var reader = new StreamReader(stream, Encoding.GetEncoding("Shift_JIS"));

            try
            {
                string? line;
                var isDataHeaderReached = false;
                var dataStartIndex = 0;
                var lineCount = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineCount++;
                    
                    // 1. ヘッダー部の判定（「年月日」または「日付」を含む行を探す）
                    if (!isDataHeaderReached)
                    {
                        if (line.Contains("年月日") || line.Contains("日付"))
                        {
                            isDataHeaderReached = true;
                            // 通常、ヘッダー行の直下2行は品質情報などの付加情報であるため、スキップカウントを設定します
                            dataStartIndex = lineCount + 2;
                        }
                        continue;
                    }

                    // ヘッダー検出後、データ開始位置に達するまではスキップ
                    if (lineCount <= dataStartIndex)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // 2. CSVデータの分解とパース
                    var tokens = line.Split(',');
                    if (tokens.Length < 4) continue;

                    // 1列目: 日付 (ダブルクォートを排除)
                    var rawDate = tokens[0].Trim().Replace("\"", "");
                    var formattedDate = NormalizeDate(rawDate);
                    if (formattedDate == null) continue;

                    // 2列目以降: 気温、降水量、日照時間などを取得してパース
                    double? tempMean = GetDoubleOrNull(tokens, 1);
                    double? tempMax = GetDoubleOrNull(tokens, 2);
                    double? tempMin = GetDoubleOrNull(tokens, 3);
                    double? precipitation = GetDoubleOrNull(tokens, 4);
                    double? sunshine = GetDoubleOrNull(tokens, 5);

                    var dailyWeather = new DailyWeather
                    {
                        StationId = stationId,
                        Date = formattedDate,
                        TemperatureMean = tempMean,
                        TemperatureMax = tempMax,
                        TemperatureMin = tempMin,
                        Precipitation = precipitation,
                        SunshineHours = sunshine,
                        SnowDepth = null,
                        HumidityMean = null,
                        WindSpeedMean = null
                    };

                    weatherList.Add(dailyWeather);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CSV PARSER] Error during parsing: {ex.Message}");
            }

            return weatherList;
        }

        /// <summary>
        /// CSVの指定トークン位置から値を安全に double? に変換します。
        /// </summary>
        private double? GetDoubleOrNull(string[] tokens, int index)
        {
            if (index >= tokens.Length) return null;
            
            var rawValue = tokens[index].Trim().Replace("\"", "");
            if (double.TryParse(rawValue, out double result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// 表記ゆれのある日付（"2023/10/1", "2023-10-01"等）を "YYYY-MM-DD" フォーマットに統一します。
        /// </summary>
        private string? NormalizeDate(string rawDate)
        {
            try
            {
                var cleaned = rawDate.Replace("/", "-");
                var parts = cleaned.Split('-');
                if (parts.Length != 3) return null;

                var year = parts[0];
                var month = parts[1].PadLeft(2, '0');
                var day = parts[2].PadLeft(2, '0');

                return $"{year}-{month}-{day}";
            }
            catch
            {
                return null;
            }
        }
    }
}
