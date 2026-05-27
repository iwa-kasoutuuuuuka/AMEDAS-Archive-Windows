namespace AmedasArchiveWindows.Models
{
    /// <summary>
    /// アメダス観測所のマスターデータ構造を表すドメインモデル
    /// </summary>
    public class Station
    {
        public string StationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Kana { get; set; } = string.Empty;
        public string Prefecture { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }
        public string StationType { get; set; } = string.Empty; // "官署" または "アメダス"
    }
}
