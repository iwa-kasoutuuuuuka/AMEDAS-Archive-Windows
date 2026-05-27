namespace AmedasArchiveWindows.Models
{
    /// <summary>
    /// 日々の気象観測データ（daily_weather テーブルに対応）を表すモデル
    /// </summary>
    public class DailyWeather
    {
        public int Id { get; set; } // 自動インクリメントプライマリキー
        public string StationId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty; // "YYYY-MM-DD"
        
        public double? TemperatureMean { get; set; }
        public double? TemperatureMax { get; set; }
        public double? TemperatureMin { get; set; }
        
        public double? Precipitation { get; set; }
        public double? SunshineHours { get; set; }
        public double? SnowDepth { get; set; }
        public double? HumidityMean { get; set; }
        public double? WindSpeedMean { get; set; }
    }
}
