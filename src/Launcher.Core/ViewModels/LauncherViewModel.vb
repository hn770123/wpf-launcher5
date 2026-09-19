' LauncherViewModel.vb
' カテゴリー選択、ボタン表示、外部プログラム起動の画面状態を管理します。
' WPF の View を参照せず、分岐と障害復旧を単体テストできるようにします。

Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Windows.Input
Imports Launcher.Core.Models
Imports Launcher.Core.Services

Namespace ViewModels
    ''' <summary>
    ''' ランチャー画面全体のバインディング状態を提供します。
    ''' </summary>
    Public NotInheritable Class LauncherViewModel
        Implements INotifyPropertyChanged

        Private ReadOnly _programLauncher As IProgramLauncher
        Private ReadOnly _logger As ILogger
        Private ReadOnly _launchCommand As RelayCommand
        Private _selectedCategory As CategoryDefinition
        Private _visibleButtons As ReadOnlyCollection(Of ButtonDefinition)
        Private _isLaunching As Boolean
        Private _errorMessage As String

        ''' <summary>
        ''' 検証済み設定と差し替え可能なサービスから画面状態を作成します。
        ''' </summary>
        Public Sub New(configuration As LauncherConfiguration, programLauncher As IProgramLauncher, logger As ILogger)
            If configuration Is Nothing Then Throw New ArgumentNullException(NameOf(configuration))
            If programLauncher Is Nothing Then Throw New ArgumentNullException(NameOf(programLauncher))
            If logger Is Nothing Then Throw New ArgumentNullException(NameOf(logger))

            Title = configuration.Title
            Categories = configuration.Categories
            _programLauncher = programLauncher
            _logger = logger
            _launchCommand = New RelayCommand(AddressOf Launch, AddressOf CanLaunch)
            _visibleButtons = EmptyButtons()
            SelectedCategory = Categories.FirstOrDefault()
        End Sub

        Public ReadOnly Property Title As String
        Public ReadOnly Property Categories As ReadOnlyCollection(Of CategoryDefinition)
        Public ReadOnly Property LaunchCommand As ICommand

            Get
                Return _launchCommand
            End Get
        End Property

        Public ReadOnly Property VisibleButtons As ReadOnlyCollection(Of ButtonDefinition)
            Get
                Return _visibleButtons
            End Get
        End Property

        Public Property SelectedCategory As CategoryDefinition
            Get
                Return _selectedCategory
            End Get
            Set(value As CategoryDefinition)
                If ReferenceEquals(_selectedCategory, value) Then Return
                _selectedCategory = value
                _visibleButtons = If(value Is Nothing, EmptyButtons(), value.Buttons)
                OnPropertyChanged()
                OnPropertyChanged(NameOf(VisibleButtons))
            End Set
        End Property

        Public ReadOnly Property IsLaunching As Boolean
            Get
                Return _isLaunching
            End Get
        End Property

        ''' <summary>
        ''' View が一度だけ表示する利用者向けエラーを保持します。
        ''' </summary>
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Private Set(value As String)
                If String.Equals(_errorMessage, value, StringComparison.Ordinal) Then Return
                _errorMessage = value
                OnPropertyChanged()
            End Set
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        ''' <summary>
        ''' 有効なボタンかつ別の起動処理が実行中でない場合だけ操作を許可します。
        ''' </summary>
        Private Function CanLaunch(parameter As Object) As Boolean
            Dim button As ButtonDefinition = TryCast(parameter, ButtonDefinition)
            Return Not _isLaunching AndAlso button IsNot Nothing AndAlso button.IsEnabled
        End Function

        ''' <summary>
        ''' 一度だけプログラムを起動し、失敗しても状態を復元して次の操作を許可します。
        ''' </summary>
        Private Sub Launch(parameter As Object)
            Dim button As ButtonDefinition = DirectCast(parameter, ButtonDefinition)
            SetLaunching(True)
            ErrorMessage = Nothing
            Try
                _programLauncher.Launch(button)
            Catch ex As Exception
                ' 例外を UI スレッド外へ漏らさず、詳細はログ、簡潔な案内は画面へ分けます。
                Try
                    _logger.Error("プログラム起動: " & button.Id, ex)
                Catch logException As Exception
                    ' ログ障害でもランチャーを終了させず、保存できなかった事実だけを案内します。
                    ErrorMessage = $"「{button.Name}」を起動できませんでした。ログも保存できませんでした: {logException.Message}"
                    Return
                End Try
                ErrorMessage = $"「{button.Name}」を起動できませんでした。{ex.Message}{Environment.NewLine}ログ: {_logger.LogDirectory}"
            Finally
                SetLaunching(False)
            End Try
        End Sub

        ''' <summary>
        ''' 実行状態と全コマンドの活性状態を同時に更新します。
        ''' </summary>
        Private Sub SetLaunching(value As Boolean)
            If _isLaunching = value Then Return
            _isLaunching = value
            OnPropertyChanged(NameOf(IsLaunching))
            _launchCommand.RaiseCanExecuteChanged()
        End Sub

        ''' <summary>
        ''' 空カテゴリー表示用の変更不能なコレクションを作成します。
        ''' </summary>
        Private Shared Function EmptyButtons() As ReadOnlyCollection(Of ButtonDefinition)
            Return New ReadOnlyCollection(Of ButtonDefinition)(New List(Of ButtonDefinition)())
        End Function

        ''' <summary>
        ''' 指定プロパティの変更を View へ通知します。
        ''' </summary>
        Private Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub
    End Class

    ''' <summary>
    ''' 実行可否を再評価できる汎用的な同期コマンドです。
    ''' </summary>
    Public NotInheritable Class RelayCommand
        Implements ICommand

        Private ReadOnly _execute As Action(Of Object)
        Private ReadOnly _canExecute As Predicate(Of Object)

        ''' <summary>
        ''' 実行処理と任意の活性判定を保持します。
        ''' </summary>
        Public Sub New(execute As Action(Of Object), canExecute As Predicate(Of Object))
            If execute Is Nothing Then Throw New ArgumentNullException(NameOf(execute))
            _execute = execute
            _canExecute = canExecute
        End Sub

        Public Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged

        ''' <summary>
        ''' 現在の画面状態で操作できるかを返します。
        ''' </summary>
        Public Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
            Return _canExecute Is Nothing OrElse _canExecute(parameter)
        End Function

        ''' <summary>
        ''' 保持した画面操作を実行します。
        ''' </summary>
        Public Sub Execute(parameter As Object) Implements ICommand.Execute
            If CanExecute(parameter) Then _execute(parameter)
        End Sub

        ''' <summary>
        ''' View に実行可否の再評価を要求します。
        ''' </summary>
        Public Sub RaiseCanExecuteChanged()
            RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
        End Sub
    End Class
End Namespace
