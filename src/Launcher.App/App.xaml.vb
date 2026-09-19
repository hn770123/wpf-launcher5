' App.xaml.vb
' WPF アプリケーションのライフサイクルを定義するモジュールです。
' 起動ウィンドウは App.xaml に宣言し、ここでは最小限の初期化だけを扱います。

Imports System
Imports System.Windows
Imports System.Windows.Markup

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

            ' StartupUri や共有リソースを読み込んでから、ウィンドウのメッセージループへ入ります。
            application.InitializeComponent()
            application.Run()
        End Sub

        ''' <summary>
        ''' VB.NET の XAML 接続インターフェースから生成済み初期化処理へ処理を渡します。
        ''' </summary>
        Private Sub InitializeApplicationComponent() Implements IComponentConnector.InitializeComponent
            ' Application 用 XAML が生成する公開メソッドを呼び、リソースを一度だけ読み込みます。
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' WPF がアプリケーションを開始した直後の初期化を行います。
        ''' </summary>
        ''' <param name="e">起動時のイベント情報。</param>
        Protected Overrides Sub OnStartup(e As StartupEventArgs)
            ' 基底処理が MainWindow の生成に必要な WPF 初期化を進めます。
            MyBase.OnStartup(e)
        End Sub
    End Class
End Namespace
