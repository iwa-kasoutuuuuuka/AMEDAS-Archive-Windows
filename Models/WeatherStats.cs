namespace AmedasArchiveWindows.Models
{
    /// <summary>
    /// 気象実績データ、および特定日の長年平均の統計データを表すモデル
    /// </summary>
    public class WeatherStats
    {
        public string StationId { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        
        // 日付または「MM-DD」などの集計対象
        public string TargetDateOrDay { get; set; } = string.Empty;
        
        // 算出時の合計年数
        public int TotalYears { get; set; }
        
        // 気温統計データ
        public double? TemperatureMean { get; set; }
        public double? TemperatureMax { get; set; }
        public double? TemperatureMin { get; set; }
        
        // 降水量・降水確率データ
        public double? PrecipitationMean { get; set; }
        public double RainProbability { get; set; } // 1.0mm以上の雨の確率 (%)
        
        // 晴天確率データ
        public double SunProbability { get; set; }  // 日照時間3.0時間以上の確率 (%)
    }
}
