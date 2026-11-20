# FEFWRNGSolver

## 配布用ビルド

.NET 10 SDK をインストールした Windows 環境で、リポジトリ直下から PowerShell で実行します。

```powershell
.\publish.ps1
```

両版の Release ビルドが成功すると、`artifacts/release/` に今回の配布物が揃います。再実行すると前回の配布物を置き換えます。

| 版 | 配布物 | 配布方法 |
| --- | --- | --- |
| Windows x64 | `artifacts/release/windows-x64/` | `FEFWRNGSolver.exe` と同じフォルダに `languages/` を置いて配布します。利用者側での .NET のインストールは不要です。 |
| Web | `artifacts/release/web/` | `index.html` が公開先の直下に来るように、`.nojekyll`、`_framework/`、`_content/` を含む中身をすべて配置します。 |

Web 版は GitHub Pages の `https://<ユーザー名>.github.io/<リポジトリ名>/` のようなサブディレクトリでも動作します。

## 表示言語

初回は Windows の表示言語またはブラウザの優先言語を照合し、対応言語が見つからない場合は英語で起動します。「設定」→「言語」で表示言語を切り替えると、その選択を次回の起動にも引き継ぎます。

Windows 版では exe の隣、Web 版では公開先の直下にある `languages/` を編集できます。リポジトリでは両版の原本を `FEFWRNGSolver.Web/wwwroot/languages/` に置いています。言語を追加するには、`languages/languages.json` の配列に以下のような項目を追加し、言語コードと同じ名前のフォルダを作成します。

```json
{ "code": "fr", "displayName": "Français", "matchCodes": ["fr"] }
```

`displayName` はどの言語でもメニューにそのまま表示されます。`matchCodes` は初回判定に使う言語タグです。例えば `fr-CA` は `fr-CA`、`fr` の順で照合し、日本語の `ja-JP` は `ja` を通じてフォルダ `jp` に対応します。

各言語フォルダに置くファイルは、いずれも ID をキー、表示文字列を値とする UTF-8 の JSON オブジェクトです。

| ファイル | キー |
| --- | --- |
| `system.json` | メニュー、能力名、動物名、メッセージなどの `SYS_…` ラベル |
| `character.json` | ユニットID（例 `PID_カイ`） |
| `job.json` | 職ID（例 `JID_闘士`） |
| `skill.json` | スキルID（例 `SID_戦車兵の道`） |

文中の `{0}` や `{1:N0}` には実行時に値が入ります。翻訳ではこれらを残し、語順に合わせて位置を変えられます。ファイルや項目が欠けている場合、各IDを選択言語、英語、日本語の順で探し、すべてに訳がなければIDを表示します。ファイルの編集後はアプリを再起動するか、Webページを再読み込みしてください。

## 謝辞

『ファイアーエムブレム 万紫千紅』の乱数生成とレベルアップ時の成長判定を調べるにあたり、
[vexcited/fefw](https://github.com/vexcited/fefw) を参考にしました。
解析成果を公開してくださった vexcited さんに感謝します。
