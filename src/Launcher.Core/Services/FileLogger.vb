' FileLogger.vb
' ランチャーの診断情報をユーザー単位のログへ保存します。
' 認証情報を受け取らず、操作名と例外情報だけを記録する小さな境界です。

Imports System.Globalization
Imports System.IO
Imports System.Text

Namespace Services
    ''' <summary>
    ''' ViewModel が利用する診断ログ操作を定義します。
    ''' </summary>
    Public Interface ILogger
        ''' <summary>
        ''' 操作失敗の概要と例外チェーンを記録します。
        ''' </summary>
        Sub [Error](operation As String, exception As Exception)

        ''' <summary>
        ''' 利用者が参照できるログ保存先を返します。
        ''' </summary>
        ReadOnly Property LogDirectory As String
    End Interface

    ''' <summary>
    ''' 日単位の UTF-8 テキストログをローカルアプリデータへ追記します。
    ''' </summary>
    Public NotInheritable Class FileLogger
        Implements ILogger

        Private ReadOnly _logDirectory As String

        ''' <summary>
        ''' 既定の製品別ログフォルダーを使用します。
        ''' </summary>
        Public Sub New()
            Me.New(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "WpfLauncher", "Logs"))
        End Sub

        ''' <summary>
        ''' テスト可能な指定フォルダーをログ保存先にします。
        ''' </summary>
        Public Sub New(logDirectory As String)
            If String.IsNullOrWhiteSpace(logDirectory) Then Throw New ArgumentException("ログ保存先を指定してください。", NameOf(logDirectory))
            _logDirectory = Path.GetFullPath(logDirectory)
        End Sub

        ''' <summary>
        ''' 操作名、例外型、メッセージ、スタック情報を一件追記します。
        ''' </summary>
        Public Sub [Error](operation As String, exception As Exception) Implements ILogger.Error
            If exception Is Nothing Then Throw New ArgumentNullException(NameOf(exception))
            Directory.CreateDirectory(_logDirectory)
            Dim logPath As String = Path.Combine(_logDirectory, DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture) & ".log")
            Dim entry As String = String.Format(CultureInfo.InvariantCulture,
                "{0:O} [ERROR] {1}{2}{3}{2}", DateTime.UtcNow, operation, Environment.NewLine, exception.ToString())
            File.AppendAllText(logPath, entry, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
        End Sub

        ''' <summary>
        ''' 利用者向けダイアログに表示するログフォルダーを返します。
        ''' </summary>
        Public ReadOnly Property LogDirectory As String Implements ILogger.LogDirectory
            Get
                Return _logDirectory
            End Get
        End Property
    End Class
End Namespace
