' MainWindow.xaml.vb
' ランチャーのメイン画面を構築するコードビハインドです。
' 表示内容は XAML に限定し、業務ロジックは後続ステップで ViewModel へ分離します。

Imports System.Windows

Namespace Launcher.App
    ''' <summary>
    ''' カテゴリーと起動ボタンのサンプルを表示するメインウィンドウです。
    ''' </summary>
    Partial Public Class MainWindow
        ''' <summary>
        ''' XAML コンポーネントを読み込み、ウィンドウを初期化します。
        ''' </summary>
        Public Sub New()
            InitializeComponent()
        End Sub
    End Class
End Namespace
