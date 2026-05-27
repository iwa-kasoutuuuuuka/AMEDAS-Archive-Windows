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
    public sealed partial class ManagePage : Page
    {
        private readonly IWeatherRepository _repository;
        private readonly ManageStorageUseCase _manageStorageUseCase;

        // 全47都道府県の地理的順序（北から南）
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

        public ManagePage()
        {
            this.InitializeComponent();

            _repository = new WeatherRepository();
            _manageStorageUseCase = new ManageStorageUseCase(_repository);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await RefreshDashboardAsync();
        }

        /// <summary>
        /// ダッシュボード全体の表示（総レコード数・MBサイズ）および47都道府県のリスト表示を更新します。
        /// </summary>
        private async Task RefreshDashboardAsync()
        {
            try
            {
                // 総レコード数および総容量の取得
                var totalMB = await _manageStorageUseCase.GetTotalStorageUsageMBAsync();
                TotalSizeTextBlock.Text = $"{totalMB:F2} MB";

                var usageList = await _manageStorageUseCase.GetStorageUsageSummaryAsync();
                long totalRecords = usageList.Sum(u => u.RecordCount);
                TotalRecordsTextBlock.Text = $"{totalRecords:N0} 件";

                // 都道府県リストの構築
                var usageMap = usageList.ToDictionary(u => u.Prefecture, u => u);
                var displayItems = PrefectureOrder.Select(pref =>
                {
                    if (usageMap.TryGetValue(pref, out var usage) && usage.RecordCount > 0)
                    {
                        double sizeMB = usage.EstimatedSizeKB / 1024.0;
                        return new PrefectureDisplayItem
                        {
                            Prefecture = pref,
                            StatusText = $"{usage.RecordCount:N0} 件 ({sizeMB:F2} MB)",
                            RecordCount = usage.RecordCount
                        };
                    }
                    else
                    {
                        return new PrefectureDisplayItem
                        {
                            Prefecture = pref,
                            StatusText = "未同期 (0 件)",
                            RecordCount = 0
                        };
                    }
                }).ToList();

                PrefectureListView.ItemsSource = displayItems;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MANAGE] Dashboard refresh error: {ex.Message}");
            }
        }

        private System.Threading.CancellationTokenSource? _syncCancellationTokenSource;

        private async void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button syncButton && syncButton.Tag is string prefecture)
            {
                SyncStatusInfoBar.IsOpen = false;
                
                int years = 10;
                if (Years20RadioButton.IsChecked == true) years = 20;
                else if (Years30RadioButton.IsChecked == true) years = 30;

                try
                {
                    _syncCancellationTokenSource = new System.Threading.CancellationTokenSource();
                    
                    GlobalSyncProgressPanel.Visibility = Visibility.Visible;
                    SyncProgressBar.IsIndeterminate = false;
                    SyncProgressBar.Value = 0;
                    SyncProgressTextBlock.Text = $"{prefecture} の気象データを同期中... (設定: {years}年分)";
                    
                    PrefectureListView.IsEnabled = false;
                    SyncAllButton.IsEnabled = false;
                    CancelSyncButton.IsEnabled = true;

                    var stations = await _repository.GetStationsByPrefectureAsync(prefecture);
                    
                    if (stations.Count == 0)
                    {
                        ShowInfoBar("同期失敗", $"{prefecture} に属する観測所マスターが見つかりませんでした。", InfoBarSeverity.Error);
                        return;
                    }

                    int count = 0;
                    foreach (var station in stations)
                    {
                        if (_syncCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            ShowInfoBar("同期キャンセル", $"{prefecture} の同期処理がユーザーによって中断されました。途中までのデータは保存されています。", InfoBarSeverity.Warning);
                            break;
                        }

                        count++;
                        SyncProgressBar.Value = ((double)count / stations.Count) * 100;
                        SyncProgressTextBlock.Text = $"{prefecture} のデータを同期中... ({count}/{stations.Count}地点目: {station.Name})";
                        
                        await _repository.SyncStationDataAsync(station.StationId, years);
                    }

                    if (!_syncCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        ShowInfoBar("同期完了", $"{prefecture} の全 {stations.Count} 地点の気象データ（{years}年分）の同期が完了しました。", InfoBarSeverity.Success);
                    }
                    
                    await RefreshDashboardAsync();
                }
                catch (Exception ex)
                {
                    ShowInfoBar("同期エラー", $"{prefecture} の同期中にエラーが発生しました: {ex.Message}", InfoBarSeverity.Error);
                }
                finally
                {
                    GlobalSyncProgressPanel.Visibility = Visibility.Collapsed;
                    PrefectureListView.IsEnabled = true;
                    SyncAllButton.IsEnabled = true;
                    CancelSyncButton.IsEnabled = false;
                    _syncCancellationTokenSource?.Dispose();
                    _syncCancellationTokenSource = null;
                }
            }
        }

        private async void SyncAllButton_Click(object sender, RoutedEventArgs e)
        {
            SyncStatusInfoBar.IsOpen = false;
            
            int years = 10;
            if (Years20RadioButton.IsChecked == true) years = 20;
            else if (Years30RadioButton.IsChecked == true) years = 30;

            try
            {
                _syncCancellationTokenSource = new System.Threading.CancellationTokenSource();
                
                GlobalSyncProgressPanel.Visibility = Visibility.Visible;
                SyncProgressBar.IsIndeterminate = false;
                SyncProgressBar.Value = 0;
                
                PrefectureListView.IsEnabled = false;
                SyncAllButton.IsEnabled = false;
                CancelSyncButton.IsEnabled = true;

                int prefCount = 0;
                foreach (var pref in PrefectureOrder)
                {
                    if (_syncCancellationTokenSource.Token.IsCancellationRequested) break;

                    prefCount++;
                    var stations = await _repository.GetStationsByPrefectureAsync(pref);
                    
                    int stationCount = 0;
                    foreach (var station in stations)
                    {
                        if (_syncCancellationTokenSource.Token.IsCancellationRequested) break;

                        stationCount++;
                        
                        // プログレスバーは全国の都道府県進捗を示す
                        SyncProgressBar.Value = ((double)prefCount / PrefectureOrder.Count) * 100;
                        SyncProgressTextBlock.Text = $"全国一括同期中... ({prefCount}/{PrefectureOrder.Count}都道府県: {pref}) - {stationCount}/{stations.Count}地点目: {station.Name}";
                        
                        await _repository.SyncStationDataAsync(station.StationId, years);
                    }
                }

                if (_syncCancellationTokenSource.Token.IsCancellationRequested)
                {
                    ShowInfoBar("同期キャンセル", "全国一括同期が途中で中断されました。そこまでに完了したデータは保存されています。", InfoBarSeverity.Warning);
                }
                else
                {
                    ShowInfoBar("同期完了", $"全国47都道府県（設定: {years}年分）の気象データの同期がすべて完了しました！", InfoBarSeverity.Success);
                }
                
                await RefreshDashboardAsync();
            }
            catch (Exception ex)
            {
                ShowInfoBar("同期エラー", $"一括同期中に予期せぬエラーが発生しました: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                GlobalSyncProgressPanel.Visibility = Visibility.Collapsed;
                PrefectureListView.IsEnabled = true;
                SyncAllButton.IsEnabled = true;
                CancelSyncButton.IsEnabled = false;
                _syncCancellationTokenSource?.Dispose();
                _syncCancellationTokenSource = null;
            }
        }

        private void CancelSyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (_syncCancellationTokenSource != null && !_syncCancellationTokenSource.IsCancellationRequested)
            {
                _syncCancellationTokenSource.Cancel();
                CancelSyncButton.IsEnabled = false;
                SyncProgressTextBlock.Text = "キャンセルのリクエストを受信しました。現在の処理が終わり次第停止します...";
            }
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clearButton && clearButton.Tag is string prefecture)
            {
                SyncStatusInfoBar.IsOpen = false;

                try
                {
                    GlobalSyncProgressPanel.Visibility = Visibility.Visible;
                    SyncProgressTextBlock.Text = $"{prefecture} のローカル蓄積データを削除中...";
                    PrefectureListView.IsEnabled = false;

                    // 指定都道府県の全データを削除
                    await _manageStorageUseCase.ClearPrefectureDataAsync(prefecture);

                    ShowInfoBar("データ削除", $"{prefecture} のローカル気象データを完全に削除し、容量を解放しました。", InfoBarSeverity.Informational);
                    
                    // 表示更新
                    await RefreshDashboardAsync();
                }
                catch (Exception ex)
                {
                    ShowInfoBar("削除エラー", $"{prefecture} のデータクリーンアップ中にエラーが発生しました: {ex.Message}", InfoBarSeverity.Error);
                }
                finally
                {
                    GlobalSyncProgressPanel.Visibility = Visibility.Collapsed;
                    PrefectureListView.IsEnabled = true;
                }
            }
        }

        private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            SyncStatusInfoBar.Title = title;
            SyncStatusInfoBar.Message = message;
            SyncStatusInfoBar.Severity = severity;
            SyncStatusInfoBar.IsOpen = true;
        }
    }

    /// <summary>
    /// UI（ListView）バインディング専用の都道府県同期状態表示モデル
    /// </summary>
    public class PrefectureDisplayItem
    {
        public string Prefecture { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public long RecordCount { get; set; }
    }
}
