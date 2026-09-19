' ProgramLauncherTests.vb
' プログラム起動前の検証とシェル非経由の起動情報を検証します。

Imports System.Diagnostics
Imports System.IO
Imports System.Security
Imports Launcher.Core.Models
Imports Launcher.Core.Services
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' <see cref="ProgramLauncher"/> の安全なプロセス境界を検証します。
''' </summary>
<TestClass>
Public Class ProgramLauncherTests
    Private _temporaryDirectory As String

    ''' <summary>
    ''' 各テスト専用の疑似実行ファイル用フォルダーを作成します。
    ''' </summary>
    <TestInitialize>
    Public Sub Initialize()
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "ProgramLauncherTests", Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(_temporaryDirectory)
    End Sub

    ''' <summary>
    ''' テスト用ファイルを削除します。
    ''' </summary>
    <TestCleanup>
    Public Sub Cleanup()
        If Directory.Exists(_temporaryDirectory) Then Directory.Delete(_temporaryDirectory, recursive:=True)
    End Sub

    ''' <summary>
    ''' 引数をシェルに渡さず、設定値を個別プロパティへ保持することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Launch_ValidDefinition_StartsWithoutShell()
        Dim executable As String = Path.Combine(_temporaryDirectory, "app.exe")
        File.WriteAllText(executable, String.Empty)
        Dim starter As New RecordingProcessStarter()
        Dim button As New ButtonDefinition("app", "アプリ", 1, executable, "--name A&B | echo", _temporaryDirectory, String.Empty, True)
        Dim launcher As New ProgramLauncher(starter)

        launcher.Launch(button)

        Assert.AreEqual(executable, starter.StartInfo.FileName)
        Assert.AreEqual("--name A&B | echo", starter.StartInfo.Arguments)
        Assert.AreEqual(_temporaryDirectory, starter.StartInfo.WorkingDirectory)
        Assert.IsFalse(starter.StartInfo.UseShellExecute)
        Assert.AreEqual(1, starter.StartCount)
    End Sub

    ''' <summary>
    ''' 存在しない実行ファイルを OS 呼び出し前に拒否することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Launch_MissingExecutable_ThrowsWithoutStarting()
        Dim starter As New RecordingProcessStarter()
        Dim button As New ButtonDefinition("app", "アプリ", 1, Path.Combine(_temporaryDirectory, "missing.exe"),
                                           String.Empty, String.Empty, String.Empty, True)
        Dim launcher As New ProgramLauncher(starter)

        Dim exception As ProgramLaunchException = Assert.ThrowsException(Of ProgramLaunchException)(
            Sub() launcher.Launch(button))

        StringAssert.Contains(exception.Message, "実行ファイルが見つかりません")
        Assert.AreEqual(0, starter.StartCount)
    End Sub

    ''' <summary>
    ''' 不正な作業フォルダーを OS 呼び出し前に拒否することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Launch_MissingWorkingDirectory_ThrowsWithoutStarting()
        Dim executable As String = Path.Combine(_temporaryDirectory, "app.exe")
        File.WriteAllText(executable, String.Empty)
        Dim starter As New RecordingProcessStarter()
        Dim button As New ButtonDefinition("app", "アプリ", 1, executable, String.Empty,
                                           Path.Combine(_temporaryDirectory, "missing"), String.Empty, True)
        Dim launcher As New ProgramLauncher(starter)

        Dim exception As ProgramLaunchException = Assert.ThrowsException(Of ProgramLaunchException)(
            Sub() launcher.Launch(button))

        StringAssert.Contains(exception.Message, "作業フォルダーが見つかりません")
        Assert.AreEqual(0, starter.StartCount)
    End Sub

    ''' <summary>
    ''' OS のアクセス拒否を利用者向けの起動例外へ変換することを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub Launch_AccessDenied_ThrowsActionableException()
        Dim executable As String = Path.Combine(_temporaryDirectory, "app.exe")
        File.WriteAllText(executable, String.Empty)
        Dim starter As New RecordingProcessStarter() With {.ExceptionToThrow = New UnauthorizedAccessException("denied")}
        Dim button As New ButtonDefinition("app", "アプリ", 1, executable, String.Empty, String.Empty, String.Empty, True)
        Dim launcher As New ProgramLauncher(starter)

        Dim exception As ProgramLaunchException = Assert.ThrowsException(Of ProgramLaunchException)(
            Sub() launcher.Launch(button))

        StringAssert.Contains(exception.Message, "権限がありません")
    End Sub

    ''' <summary>
    ''' OS への起動要求を記録するテスト用境界です。
    ''' </summary>
    Private NotInheritable Class RecordingProcessStarter
        Implements IProcessStarter

        Public Property StartCount As Integer
        Public Property StartInfo As ProcessStartInfo
        Public Property ExceptionToThrow As Exception

        ''' <summary>
        ''' 起動情報を保存し、破棄可能な未開始 Process を返します。
        ''' </summary>
        Public Function Start(startInfo As ProcessStartInfo) As Process Implements IProcessStarter.Start
            StartCount += 1
            Me.StartInfo = startInfo
            If ExceptionToThrow IsNot Nothing Then Throw ExceptionToThrow
            Return New Process()
        End Function
    End Class
End Class
