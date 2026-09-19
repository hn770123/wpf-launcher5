' App.xaml.vb
' WPF アプリケーションの起動、設定読み込み、依存関係の組み立てを担当します。
' 設定エラーはウィンドウ生成前に利用者へ伝え、安全に終了します。

Imports System
Imports System.IO
Imports System.Windows
Imports System.Windows.Markup
Imports Launcher.Core.Services
Imports Launcher.Core.ViewModels

Namespace Launcher.App
    ''' <summary>
    ''' 業務ランチャーのアプリケーションエントリーポイントです。
    ''' </summary>
    Partial Public Class App
        ''' <summary>
        ''' STA スレッドで WPF アプリケーションを生成し、メッセージループを開始します。
        ''' </summary>
        <STAThread>
        Public Shared Sub Main()
            Dim application As New App()
            application.InitializeComponent()
            application.Run()
        End Sub

        ''' <summary>
        ''' VB.NET の XAML 接続インターフェースから生成済み初期化処理へ処理を渡します。
        ''' </summary>
        Private Sub InitializeApplicationComponent() Implements IComponentConnector.InitializeComponent
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' 設定を読み込み、サービスを注入したメイン画面を表示します。
        ''' </summary>
        Protected Overrides Sub OnStartup(e As StartupEventArgs)
            MyBase.OnStartup(e)
            Try
                Dim configurationPath As String = ResolveConfigurationPath(e.Args)
                Dim result As ConfigurationLoadResult = New LauncherConfigurationLoader().Load(configurationPath)
                Dim logger As New FileLogger()
                Dim viewModel As New LauncherViewModel(result.Configuration, New ProgramLauncher(), logger)
                Dim window As New MainWindow() With {.DataContext = viewModel}
                MainWindow = window

                If result.Warnings.Count > 0 Then
                    MessageBox.Show(String.Join(Environment.NewLine, result.Warnings), "設定の警告",
                                    MessageBoxButton.OK, MessageBoxImage.Warning)
                End If
                window.Show()
            Catch ex As ConfigurationException
                MessageBox.Show(ex.Message, "設定を読み込めません", MessageBoxButton.OK, MessageBoxImage.Error)
                Shutdown(1)
            End Try
        End Sub

        ''' <summary>
        ''' --config 引数を検証し、省略時は実行ファイル隣接の既定設定を返します。
        ''' </summary>
        Private Shared Function ResolveConfigurationPath(arguments As String()) As String
            If arguments.Length = 0 Then
                Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.config.xml")
            End If
            If arguments.Length = 2 AndAlso String.Equals(arguments(0), "--config", StringComparison.OrdinalIgnoreCase) Then
                Return arguments(1)
            End If
            Throw New ConfigurationException("起動引数は '--config <設定ファイル>' の形式で指定してください。")
        End Function
    End Class
End Namespace
