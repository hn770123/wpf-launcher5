' ProgramLauncher.vb
' 外部プログラムをシェルを経由せず安全に起動する境界を定義します。
' 起動前検証と OS 例外の利用者向け変換をこのモジュールへ集約します。

Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Security
Imports Launcher.Core.Models

Namespace Services
    ''' <summary>
    ''' ViewModel から差し替え可能なプログラム起動操作を表します。
    ''' </summary>
    Public Interface IProgramLauncher
        ''' <summary>
        ''' 指定ボタンのプログラムを起動し、プロセスの終了を待たずに戻ります。
        ''' </summary>
        Sub Launch(button As ButtonDefinition)
    End Interface

    ''' <summary>
    ''' テスト時に OS のプロセス生成だけを置換するための境界です。
    ''' </summary>
    Public Interface IProcessStarter
        ''' <summary>
        ''' 構築済みの起動情報でプロセスを開始します。
        ''' </summary>
        Function Start(startInfo As ProcessStartInfo) As Process
    End Interface

    ''' <summary>
    ''' <see cref="Process.Start(ProcessStartInfo)"/> を呼び出す既定実装です。
    ''' </summary>
    Public NotInheritable Class ProcessStarter
        Implements IProcessStarter

        ''' <summary>
        ''' OS へ起動を要求し、開始されたプロセスを返します。
        ''' </summary>
        Public Function Start(startInfo As ProcessStartInfo) As Process Implements IProcessStarter.Start
            Return Process.Start(startInfo)
        End Function
    End Class

    ''' <summary>
    ''' 実行対象と作業ディレクトリを検証してから直接プロセスを開始します。
    ''' </summary>
    Public NotInheritable Class ProgramLauncher
        Implements IProgramLauncher

        Private ReadOnly _processStarter As IProcessStarter

        ''' <summary>
        ''' OS の既定プロセス開始処理を使用します。
        ''' </summary>
        Public Sub New()
            Me.New(New ProcessStarter())
        End Sub

        ''' <summary>
        ''' 指定されたプロセス開始境界を使用します。
        ''' </summary>
        Public Sub New(processStarter As IProcessStarter)
            If processStarter Is Nothing Then Throw New ArgumentNullException(NameOf(processStarter))
            _processStarter = processStarter
        End Sub

        ''' <summary>
        ''' シェルを使わず、設定済みの引数と作業ディレクトリで一度だけ起動します。
        ''' </summary>
        Public Sub Launch(button As ButtonDefinition) Implements IProgramLauncher.Launch
            If button Is Nothing Then Throw New ArgumentNullException(NameOf(button))
            If Not File.Exists(button.ExecutablePath) Then
                Throw New ProgramLaunchException($"実行ファイルが見つかりません: {button.ExecutablePath}")
            End If

            If button.WorkingDirectory.Length > 0 AndAlso Not Directory.Exists(button.WorkingDirectory) Then
                Throw New ProgramLaunchException($"作業フォルダーが見つかりません: {button.WorkingDirectory}")
            End If

            ' UseShellExecute=False に固定し、& や | を含む設定値がコマンドとして再解釈されることを防ぎます。
            Dim startInfo As New ProcessStartInfo() With {
                .FileName = button.ExecutablePath,
                .Arguments = button.Arguments,
                .WorkingDirectory = button.WorkingDirectory,
                .UseShellExecute = False,
                .CreateNoWindow = False
            }

            Try
                Dim process As Process = _processStarter.Start(startInfo)
                If process Is Nothing Then Throw New ProgramLaunchException("OS がプログラムを開始できませんでした。")
                ' ランチャーは終了を待たないため、所有するハンドルを直ちに解放します。
                process.Dispose()
            Catch ex As ProgramLaunchException
                Throw
            Catch ex As UnauthorizedAccessException
                Throw New ProgramLaunchException("プログラムを起動する権限がありません。", ex)
            Catch ex As SecurityException
                Throw New ProgramLaunchException("セキュリティ設定によりプログラムの起動が拒否されました。", ex)
            Catch ex As Win32Exception
                Throw New ProgramLaunchException($"プログラムを起動できません: {ex.Message}", ex)
            Catch ex As IOException
                Throw New ProgramLaunchException($"プログラムの起動情報を読み取れません: {ex.Message}", ex)
            End Try
        End Sub
    End Class

    ''' <summary>
    ''' 外部プログラムを安全に開始できなかったことを表します。
    ''' </summary>
    Public NotInheritable Class ProgramLaunchException
        Inherits Exception

        ''' <summary>
        ''' 利用者向けの理由を保持します。
        ''' </summary>
        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        ''' <summary>
        ''' 利用者向けの理由と診断用の原因例外を保持します。
        ''' </summary>
        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace
