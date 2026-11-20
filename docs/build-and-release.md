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

## 配布物のライセンス

ビルド時に、リポジトリの [LICENSE](../LICENSE) と [licenses/](../licenses/) の原文から、各版で配布する文書が自動でコピーされます。依存関係を更新したときは、配布するバージョンに合わせて原文も更新してください。Web版では画面下部の「ライセンス / Licenses」から全文を開けます。

Windows版のZIPには、本ツールの [LICENSE](../LICENSE) を入れます。`licenses/` には、ビルドに使ったバージョンの .NET ランタイムと Windows Forms のMITライセンス全文（著作権表示を含む）、それぞれの第三者通知、Microsoft .NET Library License の全文を収録します。v1.0.0 のランタイムは 10.0.13 です。[.NET のバイナリ配布方針](https://github.com/dotnet/runtime/blob/main/docs/project/licensing-assets.md)・[ランタイムの第三者通知](https://github.com/dotnet/runtime/blob/v10.0.13/THIRD-PARTY-NOTICES.TXT)・[Windows Forms の第三者通知](https://github.com/dotnet/winforms/blob/v10.0.13/THIRD-PARTY-NOTICES.TXT)

Windows単一exeに組み込まれるランタイムには Microsoft .NET Library License が適用されます。第3条(a)(ii)に従い、同梱ランタイムについて、再配布者と外部エンドユーザーに同契約と同等以上の保護を与える条件への同意を求めます。配布ページからも利用前にその条件を読めるようにします。[Windows版 .NET のライセンス案内](https://github.com/dotnet/core/blob/main/license-information-windows.md)・[Microsoft .NET Library License 全文](https://dotnet.microsoft.com/dotnet_library_license.htm)

Web版もブラウザーにコードを配信します。本ツールの LICENSE と、配信する WebAssembly ランタイムおよび Blazor などの依存パッケージのライセンス・第三者通知を、使用バージョンに合わせて公開します。[Blazor を含む ASP.NET Core の第三者通知（10.0.13）](https://github.com/dotnet/aspnetcore/blob/v10.0.13/THIRD-PARTY-NOTICES.txt)

## GitHub Pages に公開する

fork先で初めて公開するときは、fork先の **Actions** タブでワークフローを有効にし、**Settings → Pages → Build and deployment → Source** で **GitHub Actions** を選びます。

公開するときは、対象リポジトリの **Actions → Deploy Web to GitHub Pages → Run workflow** を開き、ブランチ（通常は `main`）を選んで **Run workflow** を押します。[公開ワークフロー](../.github/workflows/pages.yml)が手動実行時点のリモートブランチの最新コミットをビルドし、そのリポジトリのPagesに公開します。
