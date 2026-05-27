using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmedasArchiveWindows.Models;

namespace AmedasArchiveWindows.Data
{
    /// <summary>
    /// データベースのストレージ容量可視化とクリーンアップ（データ一括削除）を制御するユースケース
    /// </summary>
    public class ManageStorageUseCase
    {
        private readonly IWeatherRepository _repository;

        public ManageStorageUseCase(IWeatherRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 各都道府県ごとの保存済み気象データ件数と推定容量（KB）を取得。
        /// </summary>
        public async Task<List<PrefectureStorageUsage>> GetStorageUsageSummaryAsync()
        {
            return await _repository.GetStorageUsageAsync();
        }

        /// <summary>
        /// データベース全体の総レコード件数および総推定サイズ（MB表記）を取得。
        /// </summary>
        public async Task<double> GetTotalStorageUsageMBAsync()
        {
            var list = await _repository.GetStorageUsageAsync();
            var totalKB = list.Sum(item => item.EstimatedSizeKB);
            return totalKB / 1024.0;
        }

        /// <summary>
        /// 特定の都道府県配下にあるすべての観測所データを削除してストレージを解放します。
        /// </summary>
        public async Task ClearPrefectureDataAsync(string prefecture)
        {
            var stations = await _repository.GetStationsByPrefectureAsync(prefecture);
            foreach (var station in stations)
            {
                await _repository.DeleteStationDataAsync(station.StationId);
            }
        }

        /// <summary>
        /// 特定の単一観測所のデータを削除してストレージを解放します。
        /// </summary>
        public async Task ClearStationDataAsync(string stationId)
        {
            await _repository.DeleteStationDataAsync(stationId);
        }
    }
}
