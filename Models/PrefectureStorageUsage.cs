namespace AmedasArchiveWindows.Models
{
    /// <summary>
    /// 都道府県ごとのストレージ（DBレコード数・推定容量）の使用状況を表すモデル
    /// </summary>
    public class PrefectureStorageUsage
    {
        public string Prefecture { get; set; } = string.Empty;
        public long RecordCount { get; set; }
        public long EstimatedSizeKB { get; set; }
    }
}
