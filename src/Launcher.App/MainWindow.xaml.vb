' MainWindow.xaml.vb
' ランチャーのメイン画面に固有なダイアログ表示だけを担当します。
' 選択や起動の業務ロジックは ViewModel に保持します。

Imports System.ComponentModel
Imports System.Windows
Imports Launcher.Core.ViewModels

Namespace Launcher.App
    ''' <summary>
    ''' カテゴリーと起動ボタンを表示するメインウィンドウです。
    ''' </summary>
    Partial Public Class MainWindow
        ''' <summary>
        ''' XAML コンポーネントを読み込み、ViewModel の差し替えを監視します。
        ''' </summary>
        Public Sub New()
            InitializeComponent()
            AddHandler DataContextChanged, AddressOf OnDataContextChanged
        End Sub

        ''' <summary>
        ''' 現在の ViewModel だけからエラー通知を受け取るよう購読を更新します。
        ''' </summary>
        Private Sub OnDataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            Dim oldViewModel As LauncherViewModel = TryCast(e.OldValue, LauncherViewModel)
            If oldViewModel IsNot Nothing Then RemoveHandler oldViewModel.PropertyChanged, AddressOf OnViewModelPropertyChanged
            Dim newViewModel As LauncherViewModel = TryCast(e.NewValue, LauncherViewModel)
            If newViewModel IsNot Nothing Then AddHandler newViewModel.PropertyChanged, AddressOf OnViewModelPropertyChanged
        End Sub

        ''' <summary>
        ''' 起動失敗が通知された時だけ、簡潔な案内を利用者へ表示します。
        ''' </summary>
        Private Sub OnViewModelPropertyChanged(sender As Object, e As PropertyChangedEventArgs)
            If Not String.Equals(e.PropertyName, NameOf(LauncherViewModel.ErrorMessage), StringComparison.Ordinal) Then Return
            Dim viewModel As LauncherViewModel = DirectCast(sender, LauncherViewModel)
            If String.IsNullOrWhiteSpace(viewModel.ErrorMessage) Then Return
            MessageBox.Show(Me, viewModel.ErrorMessage, "プログラムを起動できません",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Sub
    End Class
End Namespace
