# AMEDAS-Archive-Windows

「AMEDAS-Archive-Windows」は、気象庁の過去の気象データ（アメダス観測データ）をWindows端末内にローカル蓄積し、民間API等に依存することなく独自の気象統計や特異日解析、地域間の気候比較を行うことができる、高速かつ高機能なネイティブデスクトップアプリケーションです。

## 特徴 (Features)

*   **完全ローカル・オフライン動作**
    *   取得した気象データはローカルの SQLite データベースに蓄積され、オフラインでも超高速で分析・検索が可能です。
*   **美麗なネイティブUI (WinUI 3)**
    *   Windows 11 に最適化された Fluent Design と Mica エフェクトを採用。滑らかで直感的な操作感を提供します。
*   **多様な分析機能**
    *   **単一検索**: 任意の観測所と期間を指定し、平均気温・降水量・日照時間などを一覧表示。
    *   **地域比較**: 2つの観測所を選び、グラフ (Canvas) で直感的に気候の違いを比較・対比できます。
    *   **特異日解析**: 指定した日付の前後のデータを複数年にわたって集計し、特定の天候になりやすい日（特異日）を自動検出します。
*   **自己完結ポータブル版の提供**
    *   Windows App SDK をアプリ内に内包しているため、ランタイムの事前インストールが不要な `.exe` シングルファイルでポータブルに動作します。

## 動作要件 (System Requirements)

*   **OS**: Windows 11 または Windows 10 (バージョン 1809 以降)
*   **アーキテクチャ**: x64

## インストールと実行 (How to run)

1.  GitHub の Releases ページから `AmedasArchiveWindows_Portable_v1.0.0.zip` をダウンロードします。
2.  任意のフォルダに解凍します。
3.  `AmedasArchiveWindows.exe` をダブルクリックするだけで起動します。

## 開発環境 (Development)

*   C# / .NET 10
*   WinUI 3 / Windows App SDK 2.1
*   SQLite / Dapper

## ライセンス (License)

This project is licensed under the MIT License.
