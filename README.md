# FEFWRNGSolver / 万紫千紅 乱数調整ツール

『ファイアーエムブレム 万紫千紅』の乱数調整ツールです。ゲーム内で観測したレベルアップから成長seedの候補を絞り込み、そのseedを使って次のレベルアップでどの能力が伸びるかを予測できます。

This RNG tool for *Fire Emblem: Fortune’s Weave* uses the level-ups you observe in the game to narrow down the possible growth seeds. You can then use a seed to predict which stats will increase on the next level-up.

## Getting started / 使い方

- **[Web版を開く][web]** — ブラウザですぐに使えます。
- **[Windows x64版の配布先（GitHub Releases）][releases]** — 初回リリースの公開後、配布ファイルをダウンロードして展開し、`FEFWRNGSolver.exe` を起動してください。`languages/` はexeと同じフォルダーに置きます。.NETのインストールは不要です。

表示言語は「設定」→「言語」で切り替えられます。

- **[Open the web app][web]** — Use it directly in your browser.
- **[Windows x64 downloads (GitHub Releases)][releases]** — Once the first release is available, download and extract the archive, then run `FEFWRNGSolver.exe`. Keep the `languages/` folder alongside the executable. The .NET runtime is included.

To change the display language, go to **Settings → Language**.

## Contributing a translation / 翻訳への参加

新しい言語を追加するには、[言語データ][languages]の `en/` または `jp/` を新しい言語コード名でコピーし、JSONの表示文字列を翻訳します。[languages.json][language-list] に言語を登録し、変更をプルリクエストで送ってください。登録例や表示の確認方法は[翻訳ガイド（日本語）][translation]にまとめています。

To add a language, copy `en/` or `jp/` in the [language data directory][languages] to a folder named after the new language code. Translate the display text in the JSON files, register the language in [languages.json][language-list], and submit a pull request. The [translation guide (in Japanese)][translation] includes a registration example and explains how to check your translation in the app.

## License / ライセンス

本ツール独自のコードは [MITライセンス](LICENSE) で提供します。同梱する .NET などの第三者コンポーネントには、それぞれのライセンスが適用されます。

The original code of this tool is available under the [MIT License](LICENSE). Bundled third-party components, including .NET, are subject to their respective licenses.

## Acknowledgments / 謝辞

『ファイアーエムブレム 万紫千紅』の乱数生成とレベルアップ時の成長判定を調べるにあたり、
[vexcited/fefw][fefw] を参考にしました。
解析成果を公開してくださった vexcited さんに感謝します。

[開発者向けのビルド・公開手順（日本語）][build]

We consulted [vexcited/fefw][fefw] to understand how *Fire Emblem: Fortune’s Weave* generates random numbers and determines stat gains on level-up. Thank you to vexcited for sharing this research.

[Build and release guide for developers (in Japanese)][build]

[web]: https://rusted-coil.github.io/FEFWRNGSolver/
[releases]: https://github.com/rusted-coil/FEFWRNGSolver/releases
[languages]: FEFWRNGSolver.Web/wwwroot/languages/
[language-list]: FEFWRNGSolver.Web/wwwroot/languages/languages.json
[translation]: docs/translation.md
[fefw]: https://github.com/vexcited/fefw
[build]: docs/build-and-release.md
