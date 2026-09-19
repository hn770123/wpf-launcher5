# capture-window.ps1
# 指定プロセスのトップレベルウィンドウを有限時間だけ待ち、対象ウィンドウだけを PNG に保存します。
# デスクトップ全体を撮影しないことで、無関係な画面や情報が成果物へ混入することを防ぎます。

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, [int]::MaxValue)]
    [int]$ProcessId,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath,

    [ValidateRange(1, 300)]
    [int]$TimeoutSeconds = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Win32 API の宣言と撮影処理を一か所に閉じ込め、PowerShell 側ではハンドル検証に集中します。
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/// <summary>指定されたトップレベルウィンドウを画像化する Win32 境界です。</summary>
public static class NativeWindowCapture
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr windowHandle, out RECT rectangle);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr windowHandle, IntPtr deviceContext, uint flags);

    /// <summary>対象ウィンドウの描画結果だけを PNG ファイルへ保存します。</summary>
    public static Size SaveAsPng(IntPtr windowHandle, string outputPath)
    {
        RECT rectangle;
        if (!GetWindowRect(windowHandle, out rectangle))
        {
            throw new InvalidOperationException("対象ウィンドウの領域を取得できませんでした。");
        }

        int width = rectangle.Right - rectangle.Left;
        int height = rectangle.Bottom - rectangle.Top;
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("対象ウィンドウの大きさが不正です。");
        }

        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr deviceContext = graphics.GetHdc();
            try
            {
                // PW_RENDERFULLCONTENT により、対象ウィンドウ自身へ全体描画を依頼します。
                if (!PrintWindow(windowHandle, deviceContext, 2))
                {
                    throw new InvalidOperationException("対象ウィンドウの描画を取得できませんでした。");
                }
            }
            finally
            {
                graphics.ReleaseHdc(deviceContext);
            }
            // 真っ黒な画面など、単色しか得られなかった場合も PNG 自体は診断用に残します。
            bitmap.Save(outputPath, ImageFormat.Png);
            EnsureImageIsNotSolid(bitmap);
        }

        return new Size(width, height);
    }

    /// <summary>描画失敗を成功扱いしないよう、画像内に複数の色があることを確認します。</summary>
    private static void EnsureImageIsNotSolid(Bitmap bitmap)
    {
        int horizontalStep = Math.Max(1, bitmap.Width / 32);
        int verticalStep = Math.Max(1, bitmap.Height / 32);
        int firstColor = bitmap.GetPixel(0, 0).ToArgb();

        for (int y = 0; y < bitmap.Height; y += verticalStep)
        {
            for (int x = 0; x < bitmap.Width; x += horizontalStep)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != firstColor)
                {
                    return;
                }
            }
        }

        throw new InvalidOperationException("撮影結果が単色のため、ウィンドウ描画を確認できませんでした。");
    }
}
'@

function Wait-TargetWindow {
    <#
    .SYNOPSIS
    指定プロセスに表示可能なメインウィンドウが現れるまで有限時間ポーリングします。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [int]$Id,
        [Parameter(Mandatory = $true)]
        [int]$Timeout
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($Timeout)
    do {
        $process = Get-Process -Id $Id -ErrorAction SilentlyContinue
        if (-not $process) {
            throw "撮影前に対象プロセスが終了しました。ProcessId=$Id"
        }

        $process.Refresh()
        if ($process.MainWindowHandle -ne [IntPtr]::Zero -and
            -not [string]::IsNullOrWhiteSpace($process.MainWindowTitle)) {
            return $process
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "対象ウィンドウが $Timeout 秒以内に表示されませんでした。ProcessId=$Id"
}

function Test-PngSignature {
    <#
    .SYNOPSIS
    出力ファイルが空でなく PNG シグネチャを持つことを検証します。
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $signature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
    if ($bytes.Length -le $signature.Length) {
        throw "撮影した PNG が空です: $Path"
    }
    for ($index = 0; $index -lt $signature.Length; $index++) {
        if ($bytes[$index] -ne $signature[$index]) {
            throw "撮影結果が PNG 形式ではありません: $Path"
        }
    }
}

# 出力先を事前に絶対パス化し、現在ディレクトリの変更に影響されないようにします。
$fullOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($fullOutputPath)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$target = Wait-TargetWindow -Id $ProcessId -Timeout $TimeoutSeconds
$size = [NativeWindowCapture]::SaveAsPng($target.MainWindowHandle, $fullOutputPath)
Test-PngSignature -Path $fullOutputPath

Write-Host "対象ウィンドウを撮影しました。"
Write-Host "Title=$($target.MainWindowTitle)"
Write-Host "ProcessId=$ProcessId"
Write-Host "ImageSize=$($size.Width)x$($size.Height)"
Write-Host "OutputPath=$fullOutputPath"
