using System.Collections.Generic;
using System.Threading.Tasks;
using AmedasArchiveWindows.Models;

namespace AmedasArchiveWindows.Data
{
    /// <summary>
    /// 気象データおよびアメダス地点マスタデータを仲介するリポジトリインターフェース
    /// </summary>
    public interface IWeatherRepository
    {
        // --- 地点（マスター）データ関連 ---
        Task<List<Station>> GetAllStationsAsync();
        Task<List<Station>> GetStationsByPrefectureAsync(string prefecture);
        Task<List<string>> GetPrefecturesAsync();

        // --- 同期済み（アクティブ）データ関連 ---
        Task<List<string>> GetActivePrefecturesAsync();
        Task<List<Station>> GetActiveStationsAsync();
        Task<List<Station>> GetActiveStationsByPrefectureAsync(string prefecture);

        // --- 気象データ同期関連 ---
        /// <summary>
        /// 該当観測所のデータを同期。ローカルDBの最新日付を自動確認し、未取得期間の差分データのみを取得します。
        /// </summary>
        /// <param name="stationId">観測所ID</param>
        /// <param name="years">同期開始時の取得年数</param>
        Task<bool> SyncStationDataAsync(string stationId, int years = 10);

        // --- 統計計算・検索関連 ---
        /// <summary>
        /// 特定の観測所における、特定の日（MM-DD）の過去数十年の気象統計を算出。
        /// </summary>
        Task<WeatherStats?> GetSingularityStatsAsync(string stationId, string targetMonthDay);

        /// <summary>
        /// 指定された地点のDB内の最小（最古）日付と最大（最新）日付のペアを返却（期間バリデーション用）。
        /// </summary>
        Task<(string? MinDate, string? MaxDate)> GetMinMaxDateAsync(string stationId);

        /// <summary>
        /// 2地点の同一期間のデータを同時に取得して比較するためのデータリストを取得。
        /// </summary>
        Task<List<WeatherStats>> GetCompareDataAsync(string stationIdA, string stationIdB, string startDate, string endDate);

        // --- 容量管理・クリーンアップ関連 ---
        /// <summary>
        /// 都道府県ごとの保存済みレコード数と推定容量（DBサイズに換算）のリストを取得。
        /// </summary>
        Task<List<PrefectureStorageUsage>> GetStorageUsageAsync();

        /// <summary>
        /// 特定地点のローカルデータを全削除（容量解放用）
        /// </summary>
        Task DeleteStationDataAsync(string stationId);
    }
}
