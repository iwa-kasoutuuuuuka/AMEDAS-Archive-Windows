using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Dapper;
using AmedasArchiveWindows.Models;
using Windows.Storage;

namespace AmedasArchiveWindows.Data
{
    /// <summary>
    /// SQLiteデータベースの接続管理・スキーマ構築・初期マスターデータ（stations.csv）のインポート処理を担うクラス
    /// </summary>
    public static class AmedasDatabase
    {
        private static readonly string DbFileName = "AmedasLocalDb.db";
        
        /// <summary>
        /// データベース接続文字列の取得。
        /// データ損失を防ぎ権限エラーを避けるため、安全な LocalApplicationData ディレクトリにDBを配置します。
        /// </summary>
        public static string GetConnectionString()
        {
            // 非パッケージ(Unpackaged)アプリの場合、ApplicationData.Current にアクセスすると例外が出る可能性があるため、
            // 標準の LocalAppData ディレクトリを使用します。
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "AmedasArchiveWindows");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            var dbPath = Path.Combine(appFolder, DbFileName);
            return $"Data Source={dbPath}";
        }

        /// <summary>
        /// データベースおよびテーブルを初期化し、非同期でアメダスマスタのシード処理を実行します。
        /// </summary>
        public static async Task InitializeDatabaseAsync()
        {
            var connectionString = GetConnectionString();
            
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // 1. スキーマ（テーブル）の自動生成
            const string createTablesSql = @"
                CREATE TABLE IF NOT EXISTS stations (
                    stationId TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    kana TEXT NOT NULL,
                    prefecture TEXT NOT NULL,
                    latitude REAL NOT NULL,
                    longitude REAL NOT NULL,
                    elevation REAL NOT NULL,
                    stationType TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS daily_weather (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    stationId TEXT NOT NULL,
                    date TEXT NOT NULL,
                    temperatureMean REAL,
                    temperatureMax REAL,
                    temperatureMin REAL,
                    precipitation REAL,
                    sunshineHours REAL,
                    snowDepth REAL,
                    humidityMean REAL,
                    windSpeedMean REAL,
                    UNIQUE(stationId, date)
                );

                CREATE TABLE IF NOT EXISTS sync_logs (
                    stationId TEXT PRIMARY KEY,
                    lastSyncedDate TEXT NOT NULL,
                    updatedAt INTEGER NOT NULL
                );
            ";

            await connection.ExecuteAsync(createTablesSql);

            // 2. 全国主要176地点アメダスマスタ (stations.csv) のインポート処理
            await SeedStationsAsync(connection);
        }

        /// <summary>
        /// Assets/stations.csv から観測所データを読み込み、stationsテーブルへインポートします。
        /// Android版の仕様に基づき、起動時に一度 clearAll() してからバルクインサートします。
        /// </summary>
        private static async Task SeedStationsAsync(SqliteConnection connection)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
            Action<string> log = msg => { try { File.AppendAllText(logPath, msg + Environment.NewLine); } catch { } };
            
            try
            {
                log("[DB SEED] Started SeedStationsAsync");
                // 非パッケージ(Unpackaged)版の場合、Package.Current は例外となるため AppContext.BaseDirectory を使用します
                var installPath = AppContext.BaseDirectory;
                var csvPath = Path.Combine(installPath, "Assets", "stations.csv");

                if (!File.Exists(csvPath))
                {
                    log($"[DB SEED] warning: stations.csv not found at {csvPath}");
                    return;
                }
                
                log($"[DB SEED] CSV found at {csvPath}");

                var stationsList = new List<Station>();

                // Shift-JIS / UTF-8 両面に対処できるよう念のため UTF-8 および規定のエンコーディングでパース
                using (var reader = new StreamReader(csvPath, Encoding.UTF8))
                {
                    string? line;
                    int count = 0;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        count++;
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var tokens = line.Split(',');
                        if (tokens.Length >= 8)
                        {
                            double.TryParse(tokens[4].Trim(), out double lat);
                            double.TryParse(tokens[5].Trim(), out double lon);
                            double.TryParse(tokens[6].Trim(), out double elev);

                            var station = new Station
                            {
                                StationId = tokens[0].Trim(),
                                Name = tokens[1].Trim(),
                                Kana = tokens[2].Trim(),
                                Prefecture = tokens[3].Trim(),
                                Latitude = lat,
                                Longitude = lon,
                                Elevation = elev,
                                StationType = tokens[7].Trim()
                            };
                            stationsList.Add(station);
                        }
                    }
                    log($"[DB SEED] Read {count} lines, parsed {stationsList.Count} valid stations.");
                }

                if (stationsList.Count > 0)
                {
                    // データベース内の既存マスタを一度クリアして最新マスタに入れ替える
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        await connection.ExecuteAsync("DELETE FROM stations", transaction: transaction);
                        
                        const string insertSql = @"
                            INSERT OR REPLACE INTO stations (stationId, name, kana, prefecture, latitude, longitude, elevation, stationType)
                            VALUES (@StationId, @Name, @Kana, @Prefecture, @Latitude, @Longitude, @Elevation, @StationType)
                        ";

                        // 高速な一括バルクインサート（トランザクション保護）
                        foreach (var station in stationsList)
                        {
                            await connection.ExecuteAsync(insertSql, station, transaction: transaction);
                        }
                        transaction.Commit();
                        log($"[DB SEED] Successfully imported {stationsList.Count} stations.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        log($"[DB SEED] Error during transaction: {ex.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                log($"[DB SEED] Critical seeding error: {ex.ToString()}");
            }
        }
    }
}
