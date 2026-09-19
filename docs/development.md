# 開発・CI 基準

## 1. この文書の目的

この文書は、フェーズ1で使用する開発環境、プロジェクト形式、ビルド・テスト手順、GitHub Actions の基準を固定する。外部仕様は **2026-09-19 (UTC)** に公式文書と公式リポジトリで確認した。

## 2. 採用するプロジェクト構成

### 2.1 フェーズ1

- `Launcher.App` は VB.NET の SDK 形式 WPF プロジェクトとし、`TargetFramework` を `net462`、`UseWPF` を `true` にする。
- `Launcher.Core` と `Launcher.Core.Tests` も SDK 形式とし、すべて `net462` に統一する。
- テストには MSTest と `Microsoft.NET.Test.Sdk` を使用する。テストの実行基盤は Visual Studio に含まれる VSTest とし、テスト結果を TRX 形式で保存する。
- .NET Framework 4.6.2 の参照アセンブリが必要なため、CI は Windows x64 runner 上の Visual Studio 2022 と `Microsoft.Net.Component.4.6.2.TargetingPack` を使用する。

SDK 形式を選ぶ理由は、プロジェクトファイルを簡潔に保ち、`PackageReference` と `/restore` を標準化するためである。一方、WPF と .NET Framework 4.6.2 は Windows 固有なので、正式な build/test 結果は Windows runner の結果を正とする。

### 2.2 フェーズ2

フェーズ2の認証サービスは C# の .NET 10 SDK 形式プロジェクトとし、画面には Razor Pages、HTTP API には Minimal API を使用する。Microsoft Learn では Razor Pages の雛形は `dotnet new webapp` で作成でき、Minimal API は新規 HTTP API に推奨されている。認証方式を実装する時点で .NET 10 のサポート状況とセキュリティ文書を再確認する。

## 3. Windows ビルド環境

### 3.1 runner の固定

CI では `windows-latest` を使用せず、次を明示する。

```yaml
runs-on: windows-2022
```

確認時点で `windows-latest` は Windows Server 2025 / Visual Studio 2026 を指す。一方、公式の Windows Server 2022 イメージには Visual Studio Enterprise 2022、MSBuild、NuGet build tools、`.NET Framework 4.6.2 Targeting Pack` が掲載されている。このため、古いターゲットとの互換性を優先して `windows-2022` を採用する。

runner イメージは継続的に更新されるため、ジョブの冒頭で次の情報をログへ出す。これにより、イメージ名だけに暗黙依存せず、障害発生時に実際のツールチェーンを特定できる。

```powershell
Write-Host "ImageOS=$env:ImageOS"
Write-Host "ImageVersion=$env:ImageVersion"
Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, OsBuildNumber

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -format json
& $vswhere -latest -products * -requires Microsoft.Net.Component.4.6.2.TargetingPack -format json

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
  -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild -version
dotnet --info
```

`.NET Framework 4.6.2 Targeting Pack` の検索結果または MSBuild が空の場合は、別バージョンでの暗黙のビルドを行わずジョブを失敗させる。ローカル Windows 環境には Microsoft Learn の案内に従って .NET Framework 4.6.2 Developer Pack を導入する。Developer Pack は対象バージョン向けの Targeting Pack、SDK、および IntelliSense 用ファイルを提供する。

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

& $vstest tests\Launcher.Core.Tests\bin\x64\Release\net462\Launcher.Core.Tests.dll `
  /Platform:x64 `
  /Logger:"trx;LogFileName=Launcher.Core.Tests.trx" `
  /ResultsDirectory:artifacts\test-results
```

テスト DLL が存在しない場合や VSTest が見つからない場合はジョブを失敗させる。テスト失敗時にも TRX と binlog を artifact として保存する。

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
- `windows-2022` の廃止告知が出た場合は、移行先に `.NET Framework 4.6.2 Targeting Pack` があることを確認し、同じ PR で環境ログと build/test の成功を証明する。
- .NET 10 の feature band を固定する必要が生じた場合は `global.json` を追加し、`setup-dotnet` の指定と同期させる。
