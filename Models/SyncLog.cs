namespace AmedasArchiveWindows.Models
{
    /// <summary>
    /// 観測所ごとのデータ同期ログを表すモデル
    /// </summary>
    public class SyncLog
    {
        public string StationId { get; set; } = string.Empty;
        public string LastSyncedDate { get; set; } = string.Empty; // "YYYY-MM-DD"
        public long UpdatedAt { get; set; } // エポックミリ秒
    }
}
