# 開発・CI 基準

## 1. この文書の目的

この文書は、フェーズ1で使用する開発環境、プロジェクト形式、ビルド・テスト手順、GitHub Actions の基準を固定する。外部仕様は **2026-09-19 (UTC)** に公式文書と公式リポジトリで確認した。

## 2. 採用するプロジェクト構成

### 2.1 フェーズ1

- `Launcher.App` は VB.NET の従来形式 WPF プロジェクトとし、`TargetFrameworkVersion` を `v4.6.1` にする。
- `Launcher.Core` と `Launcher.Core.Tests` も従来形式とし、すべて .NET Framework 4.6.1 に統一する。
- テストには MSTest と `Microsoft.NET.Test.Sdk` を使用する。テストの実行基盤は Visual Studio に含まれる VSTest とし、テスト結果を TRX 形式で保存する。
- .NET Framework 4.6.1 の参照アセンブリには Microsoft の `Microsoft.NETFramework.ReferenceAssemblies.net461` 1.0.3 を使用し、CI は Windows x64 runner 上の Visual Studio 2022 と MSBuild を使用する。

保守環境で .NET SDK を必要としないよう、プロジェクトは Visual Studio の MSBuild で扱える従来形式とする。依存パッケージの復元には `PackageReference` と `/restore` を使用する。参照アセンブリパッケージはコンパイル時だけ使用し、配布物には含めない。WPF と .NET Framework 4.6.1 は Windows 固有なので、正式な build/test 結果は Windows runner の結果を正とする。

### 2.2 フェーズ2

フェーズ2の認証サービスは C# の .NET 10 SDK 形式プロジェクトとし、画面には Razor Pages、HTTP API には Minimal API を使用する。Microsoft Learn では Razor Pages の雛形は `dotnet new webapp` で作成でき、Minimal API は新規 HTTP API に推奨されている。認証方式を実装する時点で .NET 10 のサポート状況とセキュリティ文書を再確認する。

## 3. Windows ビルド環境

### 3.1 runner の固定

CI では `windows-latest` を使用せず、次を明示する。

```yaml
runs-on: windows-2022
```

確認時点で `windows-latest` は Windows Server 2025 / Visual Studio 2026 を指す。一方、公式の Windows Server 2022 イメージには Visual Studio Enterprise 2022、MSBuild、NuGet build tools が掲載されている。このため、ビルド環境を安定させるために `windows-2022` を採用する。4.6.1 Targeting Pack は runner の標準搭載を前提にせず、Microsoft の参照アセンブリパッケージから復元する。

runner イメージは継続的に更新されるため、ジョブの冒頭で次の情報をログへ出す。これにより、イメージ名だけに暗黙依存せず、障害発生時に実際のツールチェーンを特定できる。

```powershell
Write-Host "ImageOS=$env:ImageOS"
Write-Host "ImageVersion=$env:ImageVersion"
Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, OsBuildNumber

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -format json
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
  -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild -version
```

MSBuild が空の場合はジョブを失敗させる。4.6.1 の参照アセンブリは `/restore` により NuGet から取得するため、ローカル Windows 環境にも .NET SDK や 4.6.1 Developer Pack の追加導入を必須としない。オフラインで保守する場合は、復元済みパッケージを社内 NuGet ソースへ保存する。

### 3.2 restore と build

Visual Studio の MSBuild を `vswhere` で取得し、ソリューション単位で次を実行する。

```powershell
& $msbuild Launcher.sln /restore /m `
  /p:Configuration=Release /p:Platform=x64 `
  /bl:artifacts/logs/Launcher.Release.x64.binlog

& $msbuild Launcher.sln /restore /m `
  /p:Configuration=Debug '/p:Platform=Any CPU' `
  /bl:artifacts/logs/Launcher.Debug.AnyCPU.binlog
```

`/restore` と build を同じ MSBuild 呼び出しで実行し、バイナリログを必ず保存する。MSBuild のコマンドライン構文は `MSBuild.exe [Switches] [ProjectFile]` であり、`/m` は並列ビルド、`/bl` は診断用バイナリログに使用する。

### 3.3 test

Visual Studio に同梱された `vstest.console.exe` を取得して、Release/x64 のテスト DLL を実行する。

```powershell
$vstest = & $vswhere -latest -products * `
  -find 'Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' |
  Select-Object -First 1

& $vstest tests\Launcher.Core.Tests\bin\x64\Release\Launcher.Core.Tests.dll `
  /Platform:x64 `
  /Logger:"trx;LogFileName=Launcher.Core.Tests.trx" `
  /ResultsDirectory:artifacts\test-results
```

テスト DLL が存在しない場合や VSTest が見つからない場合はジョブを失敗させる。テスト失敗時にも TRX と binlog を artifact として保存する。

### 3.4 自動検証 workflow

- `.github/workflows/ci.yml` は pull request と `main` / `master` への push で実行し、Release/x64 の restore、build、test、配布 ZIP 作成を順に行う。
- `.github/workflows/ui-smoke.yml` は警告ダイアログの出ない `samples/ui-smoke.config.xml` でアプリを起動し、30 秒以内に対象ウィンドウが現れることを確認する。
- UI 撮影は `tools/capture-window.ps1` が `PrintWindow` を使って対象ウィンドウだけを PNG 化する。PNG シグネチャ、画像サイズ、単色でないことを検証し、タイトルとプロセス ID をスクリプトログへ残す。
- すべての artifact 名には run ID と再実行番号を含める。TRX、binlog、配布 ZIP、画面 PNG、スクリプトログ、存在する場合のアプリログを 14 日間保存する。
- 正常起動中にアプリログが作られなかった場合は、その旨を示す `README.txt` をログ用フォルダーへ保存し、ログ収集処理自体が実行されたことを確認可能にする。

## 4. GitHub Actions の基準

### 4.1 権限

workflow または job の既定権限を次のように明示し、ソース取得以外の書き込み権限を付与しない。

```yaml
permissions:
  contents: read
```

フェーズ1は secrets を使用しない。fork 由来の pull request でも資格情報を必要とせず build/test/UI smoke を完結させる。

### 4.2 Action の採用バージョン

公式リポジトリのリリースと README を確認し、次のバージョンを採用する。workflow へ追加するときは可変のメジャータグではなく、確認済みコミット SHA に固定し、行末コメントに人が読めるバージョンを残す。

| Action | 採用版 | 固定するコミット SHA | 用途・注意事項 |
|---|---:|---|---|
| `actions/checkout` | v7.0.1 | `3d3c42e5aac5ba805825da76410c181273ba90b1` | ソース取得。認証情報を後続処理で不要にする場合は `persist-credentials: false` を指定する。コンテナ Action から認証済み Git を使う場合は runner v2.329.0 以上が必要だが、本計画では使用しない。 |
| `actions/upload-artifact` | v7.0.1 | `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` | TRX、binlog、ZIP、PNG、ログの保存。同名 artifact への複数 job からの追記は行わず、job ごとに一意な名前を付ける。hidden file は既定で除外されるため、秘密値を含まないことを確認した場合に限り明示的に含める。 |
| `actions/setup-dotnet` | v6.0.0 | `a98b56852c35b8e3190ac28c8c2271da59106c68` | フェーズ2の .NET 10 SDK を `dotnet-version: 10.0.x` で準備する。フェーズ1の .NET Framework Targeting Pack の代替にはしない。 |

`setup-dotnet` v5 以降と `checkout` v5 以降では Node.js 24 対応により runner v2.327.1 以上が必要である。GitHub-hosted runner を使うため条件は runner 側で満たすが、self-hosted runner を追加する場合は事前に runner バージョンを検査する。`upload-artifact` v4 以降の artifact は immutable であり、同じ名前への追記を前提にしない。また v4 以降は GitHub Enterprise Server では未対応であるため、本基準は GitHub.com の GitHub-hosted runner を対象とする。

## 5. 外部仕様の参照先

すべて **2026-09-19 (UTC)** に確認した。

### Microsoft

- [.NET Framework Developer Pack または再頒布可能パッケージのインストール](https://learn.microsoft.com/en-us/dotnet/framework/install/guide-for-developers)
- [Microsoft.NETFramework.ReferenceAssemblies.net461 (NuGet)](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies.net461/)
- [WPF の概要](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/?view=netframeworkdesktop-4.8)
- [MSBuild コマンドライン リファレンス](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2022)
- [ASP.NET Core Razor Pages のアーキテクチャと概念 (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/?view=aspnetcore-10.0)
- [ASP.NET Core API の概要／Minimal API (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview?view=aspnetcore-10.0)

### GitHub

- [GitHub-hosted runners リファレンス](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)
- [workflow 構文の `permissions`](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#permissions)
- [GitHub Actions runner images と利用可能なラベル](https://github.com/actions/runner-images)
- [Windows Server 2022 runner の収録ソフトウェア](https://github.com/actions/runner-images/blob/main/images/windows/Windows2022-Readme.md)
- [`actions/checkout` v7.0.1](https://github.com/actions/checkout/releases/tag/v7.0.1)
- [`actions/upload-artifact` v7.0.1](https://github.com/actions/upload-artifact/releases/tag/v7.0.1)
- [`actions/setup-dotnet` v6.0.0](https://github.com/actions/setup-dotnet/releases/tag/v6.0.0)

## 6. 更新ルール

- runner イメージまたは Action を更新する PR では、公式のリリースノートと breaking changes を再確認する。
- Action の SHA は Dependabot 等で更新しても、対応するタグ、Node.js runtime、最低 runner バージョンをレビューする。
- `windows-2022` の廃止告知が出た場合は、移行先で Visual Studio の MSBuild と 4.6.1 参照アセンブリパッケージによる build/test が成功することを同じ PR で証明する。
- .NET 10 の feature band を固定する必要が生じた場合は `global.json` を追加し、`setup-dotnet` の指定と同期させる。

## 7. Actions と Codex のフィードバック経路

### 7.1 保存済みログから判明した問題

`logs/ci.yml-logs.txt` と `logs/ui-smaoke.yml-log.txt` は、現在の HEAD より前の SDK 形式プロジェクトをビルドした run のログである。両方に共通して、次の独立したエラーが記録されている。

1. WPF のマークアップコンパイルが `mscorlib` を解決できず `MC1000` で停止している。
2. 当時の `ProgramLauncherTests.vb` に VB の構文エラー (`BC30035` / `BC30198`) があり、テスト実行前のコンパイルで停止している。
3. ビルドが失敗したため、テスト、ZIP 作成、UI 起動、PNG 撮影まで到達していない。従って、画面の問題を PNG から判断できる段階ではない。

その後のコミットで全プロジェクトは .NET Framework 4.6.1 の従来形式に移行し、該当テストソースも修正されている。したがって、保存済みログだけで現在の失敗を断定せず、**現在の commit SHA で新しい run を実行する必要がある**。

根本的な運用上の問題は、artifact のアップロードだけでは Codex へ自動的に結果が返らない点にある。Codex の実行環境に GitHub の認証とリポジトリ remote がなければ、run の起動、待機、artifact の取得はできない。また、従来は workflow に `workflow_dispatch` がなかったため、文書に記載していた `gh workflow run` 自体が成立しなかった。さらに成果物に commit SHA が入っておらず、手動配置されたログがどのソースに対応するかを機械判定できなかった。

### 7.2 改善後の実行手順

`CI` と `UI smoke` は `workflow_dispatch` に対応する。GitHub CLI にリポジトリの Actions 読み取り・実行権限がある環境では、次の閉ループを使用する。

```powershell
$branch = git branch --show-current
$commit = git rev-parse HEAD

gh workflow run ci.yml --ref $branch
$ciRuns = gh run list --workflow ci.yml --branch $branch `
  --event workflow_dispatch --limit 10 --json databaseId,headSha | ConvertFrom-Json
$ciRun = $ciRuns | Where-Object headSha -EQ $commit |
  Select-Object -First 1 -ExpandProperty databaseId
gh run watch $ciRun --exit-status
gh run download $ciRun --dir "artifacts/$ciRun"
pwsh ./tools/verify-artifacts.ps1 -Path "artifacts/$ciRun" -ExpectedCommit $commit -Kind ci

gh workflow run ui-smoke.yml --ref $branch
$uiRuns = gh run list --workflow ui-smoke.yml --branch $branch `
  --event workflow_dispatch --limit 10 --json databaseId,headSha | ConvertFrom-Json
$uiRun = $uiRuns | Where-Object headSha -EQ $commit |
  Select-Object -First 1 -ExpandProperty databaseId
gh run watch $uiRun --exit-status
gh run download $uiRun --dir "artifacts/$uiRun"
pwsh ./tools/verify-artifacts.ps1 -Path "artifacts/$uiRun" -ExpectedCommit $commit -Kind ui
```

`gh workflow run` の直後は run が一覧に現れるまで時間差があり得るため、実運用では `gh run list` を有限回ポーリングする。ブランチ名だけで最新 run を選ばず、workflow、event、commit SHA をすべて照合する。各 workflow は job summary と `run-manifest.json` に workflow 名、run ID、再実行番号、commit SHA、run URL を記録する。

ビルド失敗時は binlog だけが生成され、TRX や ZIP が存在しない場合がある。`verify-artifacts.ps1` はこの状態を「成果物の破損」と誤判定せず、存在する診断物を検証する。UI workflow は PNG と UI ログを必須とするため、ビルドまたは起動で止まった run は検証に失敗し、未撮影であることが明確になる。

### 7.3 推奨する修正の順序

1. 現在の commit で `CI` を再実行し、従来形式への移行で `MC1000` と VB 構文エラーが解消したか確認する。
2. CI が成功してから `UI smoke` を実行する。失敗時は最初に binlog と `ui-smoke.log` を読み、PNG が生成されている場合だけ表示内容を確認する。
3. 修正ごとに commit を作成して push し、同じ commit SHA の manifest を持つ成果物だけをフィードバックに使う。
4. Codex 環境へ GitHub 認証を渡せない場合は、利用者が上記コマンドで取得した artifact 一式をリポジトリ外の一時フォルダーに配置し、commit SHA と run URL を明示して解析を依頼する。ログファイルだけをソース管理へ追加する運用は、陳腐化と機密情報混入を避けるため行わない。

### 7.4 参照した公式仕様

以下は **2026-09-21 (UTC)** に再確認した。

- [ワークフローの手動実行 (`workflow_dispatch`)](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow)
- [workflow artifact のダウンロード](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts)
- [job summary (`GITHUB_STEP_SUMMARY`)](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands#adding-a-job-summary)
- [`gh run download`](https://cli.github.com/manual/gh_run_download)
