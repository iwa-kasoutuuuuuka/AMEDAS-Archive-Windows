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
    public sealed partial class SingularityPage : Page
    {
        private readonly IWeatherRepository _repository;
        private readonly AnalyzeSingularityUseCase _analyzeUseCase;

        // 都道府県の表示順
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

        public SingularityPage()
        {
            this.InitializeComponent();

            _repository = new WeatherRepository();
            _analyzeUseCase = new AnalyzeSingularityUseCase(_repository);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadPrefecturesAsync();
        }

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

        private void StationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // パラメータが揃っているかの簡単なバリデーション管理
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ResultSection.Visibility = Visibility.Collapsed;

            if (StationComboBox.SelectedItem is not Station selectedStation)
            {
                ShowError("※分析を行う観測所を選択してください。");
                return;
            }

            try
            {
                // UI状態を「読込中」に切り替え
                AnalyzeButton.IsEnabled = false;
                LoadingPanel.Visibility = Visibility.Visible;

                var isSunny = SunnyRadioButton.IsChecked == true;
                var scanType = isSunny 
                    ? AnalyzeSingularityUseCase.SingularityType.SUNNY 
                    : AnalyzeSingularityUseCase.SingularityType.RAINY;

                // バックグラウンドで365日の気象実績データをローカル走査
                var rawResults = await Task.Run(async () => 
                    await _analyzeUseCase.ExecuteAsync(selectedStation.StationId, scanType, limit: 5)
                );

                if (rawResults.Count > 0)
                {
                    // UI表示用の整形モデルリストを構築
                    var displayItems = rawResults.Select((r, index) => new SingularityDisplayItem
                    {
                        Rank = index + 1,
                        MonthDayString = $"{r.Month:D2}月{r.Day:D2}日",
                        SunProbabilityString = $"{r.SunProbability:F1}%",
                        RainProbabilityString = $"{r.RainProbability:F1}%",
                        AvgTempString = $"{r.AvgTemp:F1} ℃",
                        Description = r.Description
                    }).ToList();

                    ResultListView.ItemsSource = displayItems;

                    // タイトルを動的に切り替え
                    ResultHeaderTextBlock.Text = isSunny 
                        ? $"🏆 {selectedStation.Name} - 晴天特異日 Top 5 ランキング" 
                        : $"🏆 {selectedStation.Name} - 雨天特異日 Top 5 ランキング";

                    ResultSection.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowError("※指定観測所のデータが不十分です。同期管理画面からデータをダウンロードしてください。");
                }
            }
            catch (Exception ex)
            {
                ShowError($"※分析処理エラー: {ex.Message}");
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// UI（ListView）バインディング専用の整形済みデータモデル
    /// </summary>
    public class SingularityDisplayItem
    {
        public int Rank { get; set; }
        public string MonthDayString { get; set; } = string.Empty;
        public string SunProbabilityString { get; set; } = string.Empty;
        public string RainProbabilityString { get; set; } = string.Empty;
        public string AvgTempString { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
