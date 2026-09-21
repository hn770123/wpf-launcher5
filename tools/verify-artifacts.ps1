# verify-artifacts.ps1
# GitHub Actions から取得した成果物が対象コミットの完全な診断セットかを検証します。
# 古い run のログや、一部だけ生成された成果物を Codex が誤って解析することを防ぎます。

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Path,

    [ValidatePattern('^[0-9a-fA-F]{7,40}$')]
    [string]$ExpectedCommit,

    [ValidateSet('ci', 'ui')]
    [string]$Kind
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-SingleArtifactFile {
    <#
    .SYNOPSIS
    展開方法によるサブフォルダー差を吸収し、指定パターンに一致する単一ファイルを返します。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Filter,
        [switch]$Optional
    )

    $files = @(Get-ChildItem -LiteralPath $Root -Filter $Filter -File -Recurse)
    if ($files.Count -eq 0 -and $Optional) {
        return $null
    }
    if ($files.Count -ne 1) {
        throw "成果物 '$Filter' は 1 件必要ですが、$($files.Count) 件でした。"
    }
    return $files[0]
}

function Test-PngFile {
    <#
    .SYNOPSIS
    UI の撮影結果が空でなく、PNG シグネチャを持つことを確認します。
    #>
    param([Parameter(Mandatory = $true)][System.IO.FileInfo]$File)

    $bytes = [System.IO.File]::ReadAllBytes($File.FullName)
    $signature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
    if ($bytes.Length -le $signature.Length) {
        throw "PNG が空です: $($File.FullName)"
    }
    for ($index = 0; $index -lt $signature.Length; $index++) {
        if ($bytes[$index] -ne $signature[$index]) {
            throw "PNG シグネチャが不正です: $($File.FullName)"
        }
    }
}

$root = (Resolve-Path -LiteralPath $Path).Path
$manifests = @(Get-ChildItem -LiteralPath $root -Filter 'run-manifest.json' -File -Recurse)
if ($manifests.Count -eq 0) {
    throw 'run-manifest.json がありません。旧形式または不完全な成果物です。'
}

# 同じダウンロード先に CI と UI の成果物があっても、種類ごとに検証対象を絞ります。
$selected = @($manifests | Where-Object {
    $manifest = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
    -not $Kind -or
        ($Kind -eq 'ci' -and $manifest.workflow -eq 'CI') -or
        ($Kind -eq 'ui' -and $manifest.workflow -eq 'UI smoke')
})
if ($selected.Count -ne 1) {
    throw "検証対象の manifest は 1 件必要ですが、$($selected.Count) 件でした。-Kind を確認してください。"
}

$manifest = Get-Content -LiteralPath $selected[0].FullName -Raw | ConvertFrom-Json
if ($ExpectedCommit -and -not $manifest.commit.StartsWith($ExpectedCommit, [StringComparison]::OrdinalIgnoreCase)) {
    throw "成果物の commit '$($manifest.commit)' は期待値 '$ExpectedCommit' と一致しません。"
}

if ($manifest.workflow -eq 'CI') {
    $binaryLog = Get-SingleArtifactFile -Root $root -Filter 'Launcher.Release.x64.binlog'
    if ($binaryLog.Length -eq 0) { throw 'MSBuild バイナリログが空です。' }

    # ビルド失敗時には TRX/ZIP がないことも診断情報なので、存在する場合だけ内容を検証します。
    $testResult = Get-SingleArtifactFile -Root $root -Filter '*.trx' -Optional
    if ($testResult -and $testResult.Length -eq 0) { throw 'TRX が空です。' }
    $package = Get-SingleArtifactFile -Root $root -Filter 'Launcher.App-win-x64.zip' -Optional
    if ($package -and $package.Length -eq 0) { throw '配布 ZIP が空です。' }
} elseif ($manifest.workflow -eq 'UI smoke') {
    $uiLog = Get-SingleArtifactFile -Root $root -Filter 'ui-smoke.log'
    if ($uiLog.Length -eq 0) { throw 'UI スモークログが空です。' }
    $png = Get-SingleArtifactFile -Root $root -Filter 'launcher-window.png'
    Test-PngFile -File $png
} else {
    throw "未対応の workflow です: $($manifest.workflow)"
}

Write-Host "成果物を検証しました。Workflow=$($manifest.workflow) Commit=$($manifest.commit) Run=$($manifest.runId)"
