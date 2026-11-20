# FEFWRNGSolver

## 配布用ビルド

.NET 10 SDK をインストールした Windows 環境で、リポジトリ直下から PowerShell で実行します。

```powershell
.\publish.ps1
```

両版の Release ビルドが成功すると、`artifacts/release/` に今回の配布物が揃います。再実行すると前回の配布物を置き換えます。

| 版 | 配布物 | 配布方法 |
| --- | --- | --- |
| Windows x64 | `artifacts/release/windows-x64/FEFWRNGSolver.exe` | exe 単体を配布します。利用者側での .NET のインストールは不要です。 |
| Web | `artifacts/release/web/` | `index.html` が公開先の直下に来るように、`.nojekyll`、`_framework/`、`_content/` を含む中身をすべて配置します。 |

Web 版は GitHub Pages の `https://<ユーザー名>.github.io/<リポジトリ名>/` のようなサブディレクトリでも動作します。

## 謝辞

『ファイアーエムブレム 万紫千紅』の乱数生成とレベルアップ時の成長判定を調べるにあたり、
[vexcited/fefw](https://github.com/vexcited/fefw) を参考にしました。
解析成果を公開してくださった vexcited さんに感謝します。
