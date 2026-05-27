using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmedasArchiveWindows.Models;

namespace AmedasArchiveWindows.Data
{
    /// <summary>
    /// 過去データから「特異日」（最も晴れやすい日、最も雨が降りやすい日）をスキャンしてランキング化するユースケース
    /// </summary>
    public class AnalyzeSingularityUseCase
    {
        private readonly IWeatherRepository _repository;

        public enum SingularityType
        {
            SUNNY, // 晴天特異日
            RAINY  // 雨天特異日
        }

        public AnalyzeSingularityUseCase(IWeatherRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 指定された地点における特異日のランキングを非同期でスキャン・取得します。
        /// </summary>
        /// <param name="stationId">観測所ID</param>
        /// <param name="type">特異日タイプ（晴天または雨天）</param>
        /// <param name="limit">取得件数（デフォルトはTop 5）</param>
        public async Task<List<SingularityResult>> ExecuteAsync(
            string stationId,
            SingularityType type,
            int limit = 5)
        {
            var allDaysResults = new List<SingularityResult>();

            // 1年365日（うるう年は除外）をループ処理して各日の統計を取得
            for (int month = 1; month <= 12; month++)
            {
                int maxDays = month switch
                {
                    2 => 28,
                    4 or 6 or 9 or 11 => 30,
                    _ => 31
                };

                for (int day = 1; day <= maxDays; day++)
                {
                    var targetMonthDay = $"{month:D2}-{day:D2}";
                    var stats = await _repository.GetSingularityStatsAsync(stationId, targetMonthDay);
                    if (stats == null) continue;

                    double sunProb = stats.SunProbability;
                    double rainProb = stats.RainProbability;
                    double avgTemp = stats.TemperatureMean ?? 0.0;

                    // 特異日としての簡易特徴付け
                    string description = "通常日";
                    if (sunProb > 70.0)
                    {
                        description = "晴天特異日";
                    }
                    else if (rainProb > 40.0)
                    {
                        description = "雨天特異日";
                    }

                    allDaysResults.Add(new SingularityResult
                    {
                        Month = month,
                        Day = day,
                        RainProbability = rainProb,
                        SunProbability = sunProb,
                        AvgTemp = avgTemp,
                        Description = description
                    });
                }
            }

            // タイプに応じてLINQでソートして上位を指定件数分返却
            if (type == SingularityType.SUNNY)
            {
                // 晴れ確率が高い順、同じなら降水確率が低い順
                return allDaysResults
                    .OrderByDescending(r => r.SunProbability)
                    .ThenBy(r => r.RainProbability)
                    .Take(limit)
                    .ToList();
            }
            else
            {
                // 降水確率が高い順、同じなら晴れ確率が低い順
                return allDaysResults
                    .OrderByDescending(r => r.RainProbability)
                    .ThenBy(r => r.SunProbability)
                    .Take(limit)
                    .ToList();
            }
        }
    }
}
