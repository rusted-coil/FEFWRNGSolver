# FEFWRNGSolver

『ファイアーエムブレム 万紫千紅』の乱数調整ツールです。ゲーム内で観測したレベルアップから成長seedの候補を絞り込み、そのseedを使って次のレベルアップでどの能力が伸びるかを予測できます。

## 使い方

- **[Web版を開く](https://rusted-coil.github.io/FEFWRNGSolver/)** — ブラウザですぐに使えます。
- **[Windows x64版の配布先（GitHub Releases）](https://github.com/rusted-coil/FEFWRNGSolver/releases)** — 初回リリースの公開後、配布ファイルをダウンロードして展開し、`FEFWRNGSolver.exe` を起動してください。`languages/` はexeと同じフォルダーに置きます。.NETのインストールは不要です。

表示言語は「設定」→「言語」で切り替えられます。

## 翻訳への参加

新しい言語を追加するには、[言語データ](FEFWRNGSolver.Web/wwwroot/languages/)の `en/` または `jp/` を新しい言語コード名でコピーし、JSONの表示文字列を翻訳します。[languages.json](FEFWRNGSolver.Web/wwwroot/languages/languages.json) に言語を登録し、変更をプルリクエストで送ってください。登録例や表示の確認方法は[翻訳ガイド](docs/translation.md)にまとめています。

## 謝辞

『ファイアーエムブレム 万紫千紅』の乱数生成とレベルアップ時の成長判定を調べるにあたり、
[vexcited/fefw](https://github.com/vexcited/fefw) を参考にしました。
解析成果を公開してくださった vexcited さんに感謝します。

[開発者向けのビルド・公開手順](docs/build-and-release.md)
