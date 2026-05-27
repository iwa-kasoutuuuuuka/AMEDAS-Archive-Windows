using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AmedasArchiveWindows.Data;
using AmedasArchiveWindows.Models;

namespace AmedasArchiveWindows.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly IWeatherRepository _repository;
        private readonly CalculateStatsUseCase _calculateStatsUseCase;

        // 都道府県の地理的表示順序（北から南）
        private static readonly List<string> PrefectureOrder = new()
        {
            "北海道", "青森県", "岩手県", "宮城県", "秋田県", "山形県", "福島県",
            "茨城県", "栃木県", "群馬県", "埼玉県", "千葉県", "東京都", "神奈川県",
            "新潟県", "富山県", "石川県", "福井県", "山梨県", "長野県", "岐阜県",
            "静岡県", "愛知県", "三重県", "滋賀県", "京都府", "大阪府", "兵庫県",
            "奈良県", "和歌山県", "鳥取県", "島根県", "岡山県", "広島県", "山口県",
            "徳島県", "香川県", "愛媛県", "高知県", "福岡県", "佐賀県", "長崎県",
            "熊本県", "大分県", "宮崎県", "鹿児島県", "沖縄県"
        };

        public HomePage()
        {
            this.InitializeComponent();

            _repository = new WeatherRepository();
            _calculateStatsUseCase = new CalculateStatsUseCase(_repository);

            // 初期入力値の設定
            YearTextBox.Text = DateTime.Today.Year.ToString();
            MonthTextBox.Text = "7";
            DayTextBox.Text = "7";
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadPrefecturesAsync();
        }

        /// <summary>
        /// 同期データが存在する都道府県リストを非同期ロードして ComboBox に設定します。
        /// </summary>
        private async Task LoadPrefecturesAsync()
        {
            var activePrefs = await _repository.GetActivePrefecturesAsync();
            var sortedPrefs = activePrefs.OrderBy(pref =>
            {
                int index = PrefectureOrder.IndexOf(pref);
                return index == -1 ? 999 : index;
            }).ToList();

            PrefectureComboBox.ItemsSource = sortedPrefs;
            
            if (sortedPrefs.Count > 0)
            {
                PrefectureComboBox.SelectedIndex = 0;
            }
            else
            {
                DateRangeTextBlock.Text = "※ローカルDBに同期済みのデータがありません。「同期管理」からダウンロードしてください。";
            }
        }

        private async void PrefectureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrefectureComboBox.SelectedItem is string selectedPref)
            {
                StationComboBox.IsEnabled = false;
                var activeStations = await _repository.GetActiveStationsByPrefectureAsync(selectedPref);
                
                StationComboBox.ItemsSource = activeStations;
                StationComboBox.DisplayMemberPath = "Name";

                if (activeStations.Count > 0)
                {
                    StationComboBox.SelectedIndex = 0;
                    StationComboBox.IsEnabled = true;
                }
                else
                {
                    StationComboBox.ItemsSource = null;
                }
            }
        }

        private async void StationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StationComboBox.SelectedItem is Station selectedStation)
            {
                var minMax = await _repository.GetMinMaxDateAsync(selectedStation.StationId);
                var minDate = minMax.MinDate ?? "未取得";
                var maxDate = minMax.MaxDate ?? "未取得";
                DateRangeTextBlock.Text = $"DBデータ期間: {minDate} ~ {maxDate}";
            }
            else
            {
                DateRangeTextBlock.Text = "DBデータ期間: 未取得";
            }
        }

        private async void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ResultCard.Visibility = Visibility.Collapsed;

            if (StationComboBox.SelectedItem is not Station selectedStation)
            {
                ShowError("※観測所を選択してください。");
                return;
            }

            // 入力バリデーション
            if (!int.TryParse(YearTextBox.Text, out int year) ||
                !int.TryParse(MonthTextBox.Text, out int month) ||
                !int.TryParse(DayTextBox.Text, out int day))
            {
                ShowError("※数値の入力形式が正しくありません。");
                return;
            }

            if (month < 1 || month > 12 || day < 1 || day > 31)
            {
                ShowError("※月日は有効な範囲（月: 1〜12, 日: 1〜31）で指定してください。");
                return;
            }

            // SQLite内の保存済み期間範囲を取得
            var range = await _calculateStatsUseCase.GetAvailableYearsRangeAsync(selectedStation.StationId);
            if (range.MinYear == null || range.MaxYear == null)
            {
                ShowError("※ローカルDBにデータがありません。同期管理からダウンロードしてください。");
                return;
            }

            if (year < range.MinYear || year > range.MaxYear)
            {
                ShowError($"※エラー: 入力された年({year})はDBの保存範囲外です。範囲: {range.MinYear}年〜{range.MaxYear}年");
                return;
            }

            try
            {
                CalculateButton.IsEnabled = false;

                // 特異日（同月同日）の集計計算を取得
                var stats = await _calculateStatsUseCase.ExecuteAsync(selectedStation.StationId, month, day);
                
                if (stats != null)
                {
                    ResultTitleTextBlock.Text = $"📊 {stats.StationName} - {month:D2}月{day:D2}日の過去{stats.TotalYears}年間統計";
                    SunnyProbabilityTextBlock.Text = $"{stats.SunProbability:F1}%";
                    RainProbabilityTextBlock.Text = $"{stats.RainProbability:F1}%";

                    TempMeanTextBlock.Text = stats.TemperatureMean.HasValue ? $"{stats.TemperatureMean.Value:F1} ℃" : "--";
                    TempMaxTextBlock.Text = stats.TemperatureMax.HasValue ? $"{stats.TemperatureMax.Value:F1} ℃" : "--";
                    TempMinTextBlock.Text = stats.TemperatureMin.HasValue ? $"{stats.TemperatureMin.Value:F1} ℃" : "--";

                    ResultCard.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowError("※統計データの算出に失敗しました。対象日のデータが存在しない可能性があります。");
                }
            }
            catch (Exception ex)
            {
                ShowError($"※統計処理エラー: {ex.Message}");
            }
            finally
            {
                CalculateButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
