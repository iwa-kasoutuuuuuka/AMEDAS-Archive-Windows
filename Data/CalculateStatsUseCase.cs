using System;
using System.Threading.Tasks;
using AmedasArchiveWindows.Models;

namespace AmedasArchiveWindows.Data
{
    /// <summary>
    /// 特定の観測所・日付（MM-DD）における過去の気候統計を算出するユースケース
    /// </summary>
    public class CalculateStatsUseCase
    {
        private readonly IWeatherRepository _repository;

        public CalculateStatsUseCase(IWeatherRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 指定された地点、月日における過去統計データを取得します。
        /// </summary>
        public async Task<WeatherStats?> ExecuteAsync(string stationId, int month, int day)
        {
            var targetMonthDay = $"{month:D2}-{day:D2}";
            return await _repository.GetSingularityStatsAsync(stationId, targetMonthDay);
        }

        /// <summary>
        /// 指定された地点の利用可能なデータ期間（最古年〜最新年）を取得します（バリデーション用）。
        /// </summary>
        public async Task<(int? MinYear, int? MaxYear)> GetAvailableYearsRangeAsync(string stationId)
        {
            var minMax = await _repository.GetMinMaxDateAsync(stationId);
            
            int? minYear = null;
            int? maxYear = null;

            if (minMax.MinDate != null)
            {
                var parts = minMax.MinDate.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[0], out int year))
                {
                    minYear = year;
                }
            }

            if (minMax.MaxDate != null)
            {
                var parts = minMax.MaxDate.Split('-');
                if (parts.Length > 0 && int.TryParse(parts[0], out int year))
                {
                    maxYear = year;
                }
            }

            return (minYear, maxYear);
        }
    }
}
