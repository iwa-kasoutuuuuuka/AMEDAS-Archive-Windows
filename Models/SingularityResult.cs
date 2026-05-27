namespace AmedasArchiveWindows.Models
{
    /// <summary>
    /// 特異日分析の結果を表すデータモデル
    /// </summary>
    public class SingularityResult
    {
        public int Month { get; set; }
        public int Day { get; set; }
        public double RainProbability { get; set; }  // 降水確率 (%)
        public double SunProbability { get; set; }   // 晴天確率 (%)
        public double AvgTemp { get; set; }          // 平均気温 (℃)
        public string Description { get; set; } = string.Empty; // 特徴
    }
}
