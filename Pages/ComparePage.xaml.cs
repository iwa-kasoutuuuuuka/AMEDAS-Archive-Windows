using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using AmedasArchiveWindows.Data;
using AmedasArchiveWindows.Models;

namespace AmedasArchiveWindows.Pages
{
    public sealed partial class ComparePage : Page
    {
        private readonly IWeatherRepository _repository;

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

        // グラフ描画用データキャッシュ
        private List<WeatherStats> _dataA = new();
        private List<WeatherStats> _dataB = new();
        
        // 共通で利用可能な最大/最小日付
        private DateTime? _limitMinDate;
        private DateTime? _limitMaxDate;

        public ComparePage()
        {
            this.InitializeComponent();
            _repository = new WeatherRepository();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await InitializePrefecturesAsync();
        }

        /// <summary>
        /// 同期済みデータが存在する都道府県を両方のComboBoxにロードします。
        /// </summary>
        private async Task InitializePrefecturesAsync()
        {
            var activePrefs = await _repository.GetActivePrefecturesAsync();
            var sortedPrefs = activePrefs.OrderBy(pref =>
            {
                int index = PrefectureOrder.IndexOf(pref);
                return index == -1 ? 999 : index;
            }).ToList();

            PrefectureAComboBox.ItemsSource = sortedPrefs;
            PrefectureBComboBox.ItemsSource = sortedPrefs;

            if (sortedPrefs.Count > 0)
            {
                PrefectureAComboBox.SelectedIndex = 0;
                
                // 地点Bは存在すれば2番目、なければ1番目を選択して初期の対比を面白くする
                if (sortedPrefs.Count > 1)
                {
                    PrefectureBComboBox.SelectedIndex = 1;
                }
                else
                {
                    PrefectureBComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                CommonRangeTextBlock.Text = "※ローカルDBに同期済みのデータがありません。「同期管理」からダウンロードしてください。";
            }
        }

        // --- 地点 A のComboBox変更イベント ---

        private async void PrefectureAComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrefectureAComboBox.SelectedItem is string selectedPref)
            {
                StationAComboBox.IsEnabled = false;
                var activeStations = await _repository.GetActiveStationsByPrefectureAsync(selectedPref);
                StationAComboBox.ItemsSource = activeStations;
                StationAComboBox.DisplayMemberPath = "Name";

                if (activeStations.Count > 0)
                {
                    StationAComboBox.SelectedIndex = 0;
                    StationAComboBox.IsEnabled = true;
                }
                else
                {
                    StationAComboBox.ItemsSource = null;
                }
            }
        }

        private async void StationAComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StationAComboBox.SelectedItem is Station selectedStation)
            {
                var minMax = await _repository.GetMinMaxDateAsync(selectedStation.StationId);
                var minDate = minMax.MinDate ?? "未取得";
                var maxDate = minMax.MaxDate ?? "未取得";
                DateRangeATextBlock.Text = $"DBデータ期間: {minDate} ~ {maxDate}";
                
                LegendATextBlock.Text = selectedStation.Name;
                HeaderATextBlock.Text = selectedStation.Name;
            }
            else
            {
                DateRangeATextBlock.Text = "DBデータ期間: 未取得";
            }
            await UpdateCommonRangeAsync();
        }

        // --- 地点 B のComboBox変更イベント ---

        private async void PrefectureBComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrefectureBComboBox.SelectedItem is string selectedPref)
            {
                StationBComboBox.IsEnabled = false;
                var activeStations = await _repository.GetActiveStationsByPrefectureAsync(selectedPref);
                StationBComboBox.ItemsSource = activeStations;
                StationBComboBox.DisplayMemberPath = "Name";

                if (activeStations.Count > 0)
                {
                    StationBComboBox.SelectedIndex = 0;
                    StationBComboBox.IsEnabled = true;
                }
                else
                {
                    StationBComboBox.ItemsSource = null;
                }
            }
        }

        private async void StationBComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StationBComboBox.SelectedItem is Station selectedStation)
            {
                var minMax = await _repository.GetMinMaxDateAsync(selectedStation.StationId);
                var minDate = minMax.MinDate ?? "未取得";
                var maxDate = minMax.MaxDate ?? "未取得";
                DateRangeBTextBlock.Text = $"DBデータ期間: {minDate} ~ {maxDate}";
                
                LegendBTextBlock.Text = selectedStation.Name;
                HeaderBTextBlock.Text = selectedStation.Name;
            }
            else
            {
                DateRangeBTextBlock.Text = "DBデータ期間: 未取得";
            }
            await UpdateCommonRangeAsync();
        }

        /// <summary>
        /// 2地点の共通データ期間を算出してUIに反映し、デフォルトの開始・終了日を設定します。
        /// </summary>
        private async Task UpdateCommonRangeAsync()
        {
            if (StationAComboBox.SelectedItem is not Station stationA ||
                StationBComboBox.SelectedItem is not Station stationB)
            {
                CommonRangeTextBlock.Text = "共通データ期間: 地点を選択してください";
                _limitMinDate = null;
                _limitMaxDate = null;
                return;
            }

            var rangeA = await _repository.GetMinMaxDateAsync(stationA.StationId);
            var rangeB = await _repository.GetMinMaxDateAsync(stationB.StationId);

            if (rangeA.MinDate == null || rangeA.MaxDate == null || rangeB.MinDate == null || rangeB.MaxDate == null)
            {
                CommonRangeTextBlock.Text = "共通データ期間: 同期データがありません";
                _limitMinDate = null;
                _limitMaxDate = null;
                return;
            }

            DateTime minA = DateTime.Parse(rangeA.MinDate);
            DateTime maxA = DateTime.Parse(rangeA.MaxDate);
            DateTime minB = DateTime.Parse(rangeB.MinDate);
            DateTime maxB = DateTime.Parse(rangeB.MaxDate);

            // 重なる期間の算出
            DateTime commonMin = minA > minB ? minA : minB;
            DateTime commonMax = maxA < maxB ? maxA : maxB;

            if (commonMin > commonMax)
            {
                CommonRangeTextBlock.Text = "共通データ期間: 重なり合う期間がありません";
                _limitMinDate = null;
                _limitMaxDate = null;
                return;
            }

            _limitMinDate = commonMin;
            _limitMaxDate = commonMax;

            CommonRangeTextBlock.Text = $"共通データ期間: {commonMin:yyyy-MM-dd} ~ {commonMax:yyyy-MM-dd}";

            // デフォルト比較期間の挿入（共通期間の最終1年間、または共通期間全体が1年未満ならその全体）
            DateTime defaultStart = commonMax.AddYears(-1).AddDays(1);
            if (defaultStart < commonMin)
            {
                defaultStart = commonMin;
            }

            StartDateTextBox.Text = defaultStart.ToString("yyyy-MM-dd");
            EndDateTextBox.Text = commonMax.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 比較実行処理
        /// </summary>
        private async void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Collapsed;

            if (StationAComboBox.SelectedItem is not Station stationA ||
                StationBComboBox.SelectedItem is not Station stationB)
            {
                ShowError("※比較地点AおよびBの両方を選択してください。");
                return;
            }

            if (stationA.StationId == stationB.StationId)
            {
                ShowError("※同じ観測所を比較することはできません。異なる観測所を選択してください。");
                return;
            }

            if (!_limitMinDate.HasValue || !_limitMaxDate.HasValue)
            {
                ShowError("※選択された地点に共通するデータが存在しません。");
                return;
            }

            // 日付パース
            if (!DateTime.TryParse(StartDateTextBox.Text, out DateTime start) ||
                !DateTime.TryParse(EndDateTextBox.Text, out DateTime end))
            {
                ShowError("※日付の形式が正しくありません。YYYY-MM-DDの形式で入力してください。");
                return;
            }

            if (start > end)
            {
                ShowError("※開始日は終了日より前の日付を指定してください。");
                return;
            }

            // 境界チェック
            if (start < _limitMinDate.Value || end > _limitMaxDate.Value)
            {
                ShowError($"※指定された期間は共通データ範囲外です。範囲: {_limitMinDate.Value:yyyy-MM-dd} 〜 {_limitMaxDate.Value:yyyy-MM-dd}");
                return;
            }

            // 期間幅チェック (最長1年間)
            double diffDays = (end - start).TotalDays;
            if (diffDays > 366)
            {
                ShowError($"※比較期間は最長で1年間（366日以内）に制限されています。現在の指定: {(int)diffDays}日間");
                return;
            }

            try
            {
                CompareButton.IsEnabled = false;

                string startStr = start.ToString("yyyy-MM-dd");
                string endStr = end.ToString("yyyy-MM-dd");

                // リポジトリから2地点の並列気象データをロード
                var allStats = await _repository.GetCompareDataAsync(stationA.StationId, stationB.StationId, startStr, endStr);

                // 各地点ごとのリストに分類
                _dataA = allStats.Where(x => x.StationId == stationA.StationId).OrderBy(x => x.TargetDateOrDay).ToList();
                _dataB = allStats.Where(x => x.StationId == stationB.StationId).OrderBy(x => x.TargetDateOrDay).ToList();

                if (_dataA.Count == 0 || _dataB.Count == 0)
                {
                    ShowError("※指定された期間内のデータが十分に取得できませんでした。");
                    return;
                }

                // 1. 総合統計対比表の計算およびテキスト反映
                CalculateAndPopulateSummary();

                // 2. グラフ描画
                DrawChart();

                // 結果表示
                ResultPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError($"※比較処理中にエラーが発生しました: {ex.Message}");
            }
            finally
            {
                CompareButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// ロードしたデータに基づいて平均・極値統計を計算し、テーブルUIへバインドします。
        /// </summary>
        private void CalculateAndPopulateSummary()
        {
            // --- 地点Aの統計計算 ---
            double meanTempA = _dataA.Where(x => x.TemperatureMean.HasValue).Average(x => x.TemperatureMean!.Value);
            double maxTempA = _dataA.Where(x => x.TemperatureMax.HasValue).Max(x => x.TemperatureMax!.Value);
            double minTempA = _dataA.Where(x => x.TemperatureMin.HasValue).Min(x => x.TemperatureMin!.Value);
            double totalPrecipA = _dataA.Where(x => x.PrecipitationMean.HasValue).Sum(x => x.PrecipitationMean!.Value);
            
            double sunnyDaysA = _dataA.Count(x => x.SunProbability >= 100.0);
            double rainDaysA = _dataA.Count(x => x.RainProbability >= 100.0);
            double sunnyRatioA = (sunnyDaysA * 100.0) / _dataA.Count;
            double rainRatioA = (rainDaysA * 100.0) / _dataA.Count;

            // --- 地点Bの統計計算 ---
            double meanTempB = _dataB.Where(x => x.TemperatureMean.HasValue).Average(x => x.TemperatureMean!.Value);
            double maxTempB = _dataB.Where(x => x.TemperatureMax.HasValue).Max(x => x.TemperatureMax!.Value);
            double minTempB = _dataB.Where(x => x.TemperatureMin.HasValue).Min(x => x.TemperatureMin!.Value);
            double totalPrecipB = _dataB.Where(x => x.PrecipitationMean.HasValue).Sum(x => x.PrecipitationMean!.Value);
            
            double sunnyDaysB = _dataB.Count(x => x.SunProbability >= 100.0);
            double rainDaysB = _dataB.Count(x => x.RainProbability >= 100.0);
            double sunnyRatioB = (sunnyDaysB * 100.0) / _dataB.Count;
            double rainRatioB = (rainDaysB * 100.0) / _dataB.Count;

            // --- UIテキストの設定 ---
            MeanTempATextBlock.Text = $"{meanTempA:F1} ℃";
            MeanTempBTextBlock.Text = $"{meanTempB:F1} ℃";

            MaxTempATextBlock.Text = $"{maxTempA:F1} ℃";
            MaxTempBTextBlock.Text = $"{maxTempB:F1} ℃";

            MinTempATextBlock.Text = $"{minTempA:F1} ℃";
            MinTempBTextBlock.Text = $"{minTempB:F1} ℃";

            TotalPrecipATextBlock.Text = $"{totalPrecipA:F1} mm";
            TotalPrecipBTextBlock.Text = $"{totalPrecipB:F1} mm";

            SunnyRatioATextBlock.Text = $"{sunnyRatioA:F1}% ({(int)sunnyDaysA}日)";
            SunnyRatioBTextBlock.Text = $"{sunnyRatioB:F1}% ({(int)sunnyDaysB}日)";

            RainRatioATextBlock.Text = $"{rainRatioA:F1}% ({(int)rainDaysA}日)";
            RainRatioBTextBlock.Text = $"{rainRatioB:F1}% ({(int)rainDaysB}日)";
        }

        /// <summary>
        /// Canvasを使用した美麗な2地点折れ線グラフ描画エンジン
        /// </summary>
        private void DrawChart()
        {
            ChartCanvas.Children.Clear();
            GridCanvas.Children.Clear();

            if (_dataA.Count == 0 || _dataB.Count == 0) return;

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;

            // Canvasサイズがまだ確定していない場合はスルー（SizeChangedイベントで再描画される）
            if (width <= 0 || height <= 0) return;

            // 可視化対象項目の判別
            int selectedIndex = CompareTypeComboBox.SelectedIndex;
            string valueName = selectedIndex switch
            {
                0 => "平均気温",
                1 => "最高気温",
                2 => "最低気温",
                3 => "降水量",
                _ => "平均気温"
            };

            string unit = selectedIndex == 3 ? "mm" : "℃";
            GraphTitleTextBlock.Text = $"📈 {valueName} ({unit}) の時系列推移比較";

            // 描画データ抽出用の関数
            Func<WeatherStats, double?> valExtractor = selectedIndex switch
            {
                0 => (w) => w.TemperatureMean,
                1 => (w) => w.TemperatureMax,
                2 => (w) => w.TemperatureMin,
                3 => (w) => w.PrecipitationMean,
                _ => (w) => w.TemperatureMean
            };

            // 有効な値のリスト
            var valsA = _dataA.Select(valExtractor).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            var valsB = _dataB.Select(valExtractor).Where(v => v.HasValue).Select(v => v!.Value).ToList();

            if (valsA.Count == 0 && valsB.Count == 0) return;

            // 最小値・最大値のスキャン
            double maxVal = double.MinValue;
            double minVal = double.MaxValue;

            if (valsA.Count > 0)
            {
                maxVal = Math.Max(maxVal, valsA.Max());
                minVal = Math.Min(minVal, valsA.Min());
            }
            if (valsB.Count > 0)
            {
                maxVal = Math.Max(maxVal, valsB.Max());
                minVal = Math.Min(minVal, valsB.Min());
            }

            // 降水量の最小は常に0
            if (selectedIndex == 3)
            {
                minVal = 0;
            }

            double range = maxVal - minVal;
            if (range <= 0) range = 10.0;

            // 描画マージンを追加して見やすくする
            maxVal += range * 0.1;
            if (selectedIndex != 3) // 気温の場合は下限もマージン
            {
                minVal -= range * 0.1;
            }
            range = maxVal - minVal;

            // 余白設定
            double paddingLeft = 46.0;
            double paddingRight = 16.0;
            double paddingTop = 16.0;
            double paddingBottom = 24.0;

            double drawWidth = width - paddingLeft - paddingRight;
            double drawHeight = height - paddingTop - paddingBottom;

            // --- 1. 背景グリッドとY軸ラベルの描画 ---
            int gridLines = 5;
            for (int i = 0; i < gridLines; i++)
            {
                double ratio = (double)i / (gridLines - 1);
                double val = maxVal - (ratio * range);
                double y = paddingTop + (ratio * drawHeight);

                // 横グリッド線
                Line line = new Line
                {
                    X1 = paddingLeft,
                    Y1 = y,
                    X2 = width - paddingRight,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)),
                    StrokeThickness = 1.0
                };
                GridCanvas.Children.Add(line);

                // Y軸目盛りテキスト
                TextBlock label = new TextBlock
                {
                    Text = $"{val:F1}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 128, 128, 128)),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Canvas.SetLeft(label, 2);
                Canvas.SetTop(label, y - 7);
                GridCanvas.Children.Add(label);
            }

            // --- 2. X軸（日付）目盛りラベルの描画 ---
            int totalPoints = Math.Max(_dataA.Count, _dataB.Count);
            if (totalPoints > 1)
            {
                // 日付ラベルは5〜6点程度に間引いて表示
                int labelInterval = Math.Max(1, totalPoints / 5);
                for (int i = 0; i < totalPoints; i += labelInterval)
                {
                    double x = paddingLeft + ((double)i / (totalPoints - 1)) * drawWidth;
                    var dateStr = i < _dataA.Count ? _dataA[i].TargetDateOrDay : _dataB[i].TargetDateOrDay;
                    
                    // MM/DD 形式に成形
                    if (DateTime.TryParse(dateStr, out DateTime dt))
                    {
                        dateStr = dt.ToString("MM/dd");
                    }

                    // X軸目盛り縦線
                    Line tick = new Line
                    {
                        X1 = x,
                        Y1 = height - paddingBottom,
                        X2 = x,
                        Y2 = height - paddingBottom + 4,
                        Stroke = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)),
                        StrokeThickness = 1.0
                    };
                    GridCanvas.Children.Add(tick);

                    // X軸日付テキスト
                    TextBlock label = new TextBlock
                    {
                        Text = dateStr,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromArgb(160, 128, 128, 128))
                    };
                    Canvas.SetLeft(label, x - 12);
                    Canvas.SetTop(label, height - paddingBottom + 6);
                    GridCanvas.Children.Add(label);
                }
            }

            // 基底線(X軸)
            Line xAxis = new Line
            {
                X1 = paddingLeft,
                Y1 = height - paddingBottom,
                X2 = width - paddingRight,
                Y2 = height - paddingBottom,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
                StrokeThickness = 1.2
            };
            GridCanvas.Children.Add(xAxis);

            // --- 3. 地点Aのデータ線（ブルー）描画 ---
            if (_dataA.Count > 0)
            {
                Polyline polylineA = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)), // Fluent Blue
                    StrokeThickness = 2.5,
                    StrokeLineJoin = PenLineJoin.Round
                };

                for (int i = 0; i < _dataA.Count; i++)
                {
                    double? val = valExtractor(_dataA[i]);
                    if (val.HasValue)
                    {
                        double x = paddingLeft + ((double)i / (_dataA.Count - 1)) * drawWidth;
                        double y = paddingTop + (1.0 - ((val.Value - minVal) / range)) * drawHeight;
                        polylineA.Points.Add(new Windows.Foundation.Point(x, y));
                    }
                }
                ChartCanvas.Children.Add(polylineA);
            }

            // --- 4. 地点Bのデータ線（マゼンタピンク）描画 ---
            if (_dataB.Count > 0)
            {
                Polyline polylineB = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 233, 30, 99)), // Magenta
                    StrokeThickness = 2.5,
                    StrokeLineJoin = PenLineJoin.Round
                };

                for (int i = 0; i < _dataB.Count; i++)
                {
                    double? val = valExtractor(_dataB[i]);
                    if (val.HasValue)
                    {
                        double x = paddingLeft + ((double)i / (_dataB.Count - 1)) * drawWidth;
                        double y = paddingTop + (1.0 - ((val.Value - minVal) / range)) * drawHeight;
                        polylineB.Points.Add(new Windows.Foundation.Point(x, y));
                    }
                }
                ChartCanvas.Children.Add(polylineB);
            }
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Canvasサイズが変更されたら、自動で再描画してレスポンシブFluentレイアウトを維持します
            DrawChart();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
