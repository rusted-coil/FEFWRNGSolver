# ビルド・公開手順

## 配布用ビルド

.NET 10 SDK をインストールした Windows 環境で、リポジトリ直下から PowerShell で実行します。

```powershell
.\publish.ps1
```

Windows版とWeb版の両方のビルドが成功すると、`artifacts/release/` の配布物が今回のビルドに置き換わります。

| 版 | 出力先 | 配布方法 |
| --- | --- | --- |
| Windows x64 | `artifacts/release/windows-x64/` | `FEFWRNGSolver.exe` と `languages/` を含むフォルダーの中身をまとめて配布します。 |
| Web | `artifacts/release/web/` | `index.html` が公開先の直下に来るように、`.nojekyll`、`_framework/`、`_content/` を含む中身をすべて配置します。サブディレクトリにも配置できます。 |

手元でWeb版の配布物を確認するには、リポジトリ直下でローカルHTTPサーバーを起動します。

```powershell
dnx dotnet-serve --yes -- -d ./artifacts/release/web -a 127.0.0.1 -p 8571
```

ブラウザで `http://127.0.0.1:8571/` を開きます。サーバーは `Ctrl+C` で終了します。

## GitHub Pages に公開する

fork先で初めて公開するときは、fork先の **Actions** タブでワークフローを有効にし、**Settings → Pages → Build and deployment → Source** で **GitHub Actions** を選びます。

公開するときは、対象リポジトリの **Actions → Deploy Web to GitHub Pages → Run workflow** を開き、ブランチ（通常は `main`）を選んで **Run workflow** を押します。[公開ワークフロー](../.github/workflows/pages.yml)が手動実行時点のリモートブランチの最新コミットをビルドし、そのリポジトリのPagesに公開します。
