# アプリの概要
このプロジェクトは、『ファイアーエムブレム 万紫千紅』(Fire Emblem Fortune's Weave)の乱数調整を行うためのツールです。
ロジック部分を共通化した上で、.NETによるWindowsスタンドアロンアプリと、BlazorによるWebブラウザ版を提供します。
* プラットフォーム: PC(.NET 10)、Webブラウザ(Blazor)

# エージェント運用ルール
* 必ず日本語で回答する。
* コードから容易に読み取れないものの、作業に必要な情報がある場合は、無理に推測せず質問する。
* 概ね200行以上の大きな変更を行う場合は、事前に設計方針を示して確認を取る。
* 変更は差分を最小にとどめる。広い範囲から参照されているクラスやメソッドのシグネチャを変更する場合は、影響範囲を十分に考慮する。

# READMEの運用

* リポジトリ直下の `README.md` は、公開済みのツールを使う人を主な読者とする。冒頭にツールの概要とWeb版・Windows版（Releases）へのリンクを置き、起動に必要な手順を簡潔に書く。配布状況や起動方法が変わったら記載も更新する。
* 翻訳への参加方法と謝辞を掲載する。言語登録の例や辞書の仕様は [翻訳ガイド](docs/translation.md) にまとめ、READMEから案内する。
* 開発者・配布担当者向けの操作は [ビルド・公開手順](docs/build-and-release.md) にまとめ、READMEの末尾からリンクする。

# アーキテクチャ・設計指針
全体設計はClean Architectureを基本とし、MVVMパターンを採用する。

# プロジェクト構成
Domain -> App -> Presentation -> UI 層の依存方向を厳守し、ライブラリとして依存関係を設定する。
ライブラリプロジェクトは以下の構成とする。また、Infrastracture層などが必要な場合は追加を提案してもよい。
* FEFWRNGSolver.Domain
* FEFWRNGSolver.App
* FEFWRNGSolver.Presentation
* FEFWRNGSolver.UI
* FEFWRNGSolver.UI.Blazor
テストプロジェクトは FEFWRNGSolver.Domain.Testsなど、本体の名前に.Testsをつけたプロジェクト名とする。
.NETやBlazorプラットフォームへの依存はUI層のみに留める。
FEFWRNGSolver本体はProgram.csを持つWindows版のエントリポイントとして使用する。

# コーディングスタイル
* プライベートフィールド: `m_PascalCase` 形式（例: `m_HomeMenuNavigation`, `m_Disposables`）
* 具象クラスの修飾子: 原則 `internal sealed class`
