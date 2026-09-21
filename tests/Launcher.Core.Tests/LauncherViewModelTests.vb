' LauncherViewModelTests.vb
' カテゴリー選択、表示絞り込み、二重起動防止、失敗後の復旧を検証します。
' 実プロセスや WPF 画面を使わず ViewModel の分岐だけを確認します。

Imports System.Linq
Imports Launcher.Core.Models
Imports Launcher.Core.Services
Imports Launcher.Core.ViewModels
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' <see cref="LauncherViewModel"/> の画面非依存な振る舞いを検証します。
''' </summary>
<TestClass>
Public Class LauncherViewModelTests
    ''' <summary>
    ''' 初期カテゴリーと選択後のボタンがカテゴリー単位で絞り込まれることを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub NewAndSelectCategory_FiltersVisibleButtons()
        Dim firstButton As ButtonDefinition = CreateButton("first", enabled:=True)
        Dim secondButton As ButtonDefinition = CreateButton("second", enabled:=True)
        Dim first As New CategoryDefinition("one", "一", 1, {firstButton})
        Dim second As New CategoryDefinition("two", "二", 2, {secondButton})
        Dim viewModel As LauncherViewModel = CreateViewModel({first, second}, New RecordingLauncher())

        Assert.AreSame(first, viewModel.SelectedCategory)
        Assert.AreSame(firstButton, viewModel.VisibleButtons.Single())

        viewModel.SelectedCategory = second

        Assert.AreSame(secondButton, viewModel.VisibleButtons.Single())
    End Sub

    ''' <summary>
    ''' ボタンを持たないカテゴリーを選択しても空表示として継続できることを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub SelectCategory_EmptyCategory_ShowsEmptyCollection()
        Dim populated As New CategoryDefinition("one", "一", 1, {CreateButton("first", enabled:=True)})
        Dim emptyCategory As New CategoryDefinition("empty", "空", 2, Array.Empty(Of ButtonDefinition)())
        Dim viewModel As LauncherViewModel = CreateViewModel({populated, emptyCategory}, New RecordingLauncher())

        viewModel.SelectedCategory = emptyCategory

        Assert.AreEqual(0, viewModel.VisibleButtons.Count)
    End Sub

    ''' <summary>
    ''' 無効ボタンは実行できず、有効ボタンは一度だけ起動されることを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub LaunchCommand_RespectsEnabledStateAndLaunchesOnce()
        Dim launcher As New RecordingLauncher()
        Dim enabledButton As ButtonDefinition = CreateButton("enabled", enabled:=True)
        Dim disabledButton As ButtonDefinition = CreateButton("disabled", enabled:=False)
        Dim viewModel As LauncherViewModel = CreateViewModel(
            {New CategoryDefinition("category", "分類", 1, {enabledButton, disabledButton})}, launcher)

        Assert.IsFalse(viewModel.LaunchCommand.CanExecute(disabledButton))
        viewModel.LaunchCommand.Execute(disabledButton)
        viewModel.LaunchCommand.Execute(enabledButton)

        Assert.AreEqual(1, launcher.LaunchCount)
    End Sub

    ''' <summary>
    ''' 起動中の再入操作を拒否し、終了後に操作可能へ戻ることを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub LaunchCommand_DuringLaunch_PreventsSecondOperation()
        Dim button As ButtonDefinition = CreateButton("app", enabled:=True)
        Dim launcher As New RecordingLauncher()
        Dim viewModel As LauncherViewModel = CreateViewModel(
            {New CategoryDefinition("category", "分類", 1, {button})}, launcher)
        launcher.OnLaunch = Sub()
                                Assert.IsTrue(viewModel.IsLaunching)
                                Assert.IsFalse(viewModel.LaunchCommand.CanExecute(button))
                                viewModel.LaunchCommand.Execute(button)
                            End Sub

        viewModel.LaunchCommand.Execute(button)

        Assert.AreEqual(1, launcher.LaunchCount)
        Assert.IsFalse(viewModel.IsLaunching)
        Assert.IsTrue(viewModel.LaunchCommand.CanExecute(button))
    End Sub

    ''' <summary>
    ''' 一件の起動失敗をログへ残し、同じセッションで再操作できることを確認します。
    ''' </summary>
    <TestMethod>
    Public Sub LaunchCommand_WhenLaunchFails_ReportsErrorAndRemainsUsable()
        Dim button As ButtonDefinition = CreateButton("app", enabled:=True)
        Dim launcher As New RecordingLauncher() With {.ExceptionToThrow = New ProgramLaunchException("拒否されました")}
        Dim logger As New RecordingLogger()
        Dim viewModel As LauncherViewModel = CreateViewModel(
            {New CategoryDefinition("category", "分類", 1, {button})}, launcher, logger)

        viewModel.LaunchCommand.Execute(button)

        StringAssert.Contains(viewModel.ErrorMessage, "拒否されました")
        StringAssert.Contains(viewModel.ErrorMessage, logger.LogDirectory)
        Assert.AreEqual(1, logger.ErrorCount)
        Assert.IsTrue(viewModel.LaunchCommand.CanExecute(button))

        launcher.ExceptionToThrow = Nothing
        viewModel.LaunchCommand.Execute(button)
        Assert.AreEqual(2, launcher.LaunchCount)
    End Sub

    ''' <summary>
    ''' テスト用設定と依存サービスから ViewModel を作成します。
    ''' </summary>
    Private Shared Function CreateViewModel(categories As IEnumerable(Of CategoryDefinition), launcher As IProgramLauncher,
                                            Optional logger As ILogger = Nothing) As LauncherViewModel
        Return New LauncherViewModel(New LauncherConfiguration("テスト", 1, categories), launcher,
                                     If(logger, New RecordingLogger()))
    End Function

    ''' <summary>
    ''' 指定した活性状態の最小ボタン定義を作成します。
    ''' </summary>
    Private Shared Function CreateButton(id As String, enabled As Boolean) As ButtonDefinition
        Return New ButtonDefinition(id, id, 1, "C:\test.exe", String.Empty, String.Empty, String.Empty, enabled)
    End Function

    ''' <summary>
    ''' 呼び出し回数と例外を制御できる起動サービスです。
    ''' </summary>
    Private NotInheritable Class RecordingLauncher
        Implements IProgramLauncher

        Public Property LaunchCount As Integer
        Public Property ExceptionToThrow As Exception
        Public Property OnLaunch As Action

        ''' <summary>
        ''' 呼び出しを記録し、テスト指定のコールバックと例外を処理します。
        ''' </summary>
        Public Sub Launch(button As ButtonDefinition) Implements IProgramLauncher.Launch
            LaunchCount += 1
            ' Action 型のプロパティは、VB がプロパティ参照文と誤解しないよう明示的に呼び出します。
            If OnLaunch IsNot Nothing Then OnLaunch.Invoke()
            If ExceptionToThrow IsNot Nothing Then Throw ExceptionToThrow
        End Sub
    End Class

    ''' <summary>
    ''' エラー件数だけを記録するログサービスです。
    ''' </summary>
    Private NotInheritable Class RecordingLogger
        Implements ILogger

        Public Property ErrorCount As Integer
        Public ReadOnly Property LogDirectory As String Implements ILogger.LogDirectory
            Get
                Return "C:\logs"
            End Get
        End Property

        ''' <summary>
        ''' ログ記録要求を数えます。
        ''' </summary>
        Public Sub [Error](operation As String, exception As Exception) Implements ILogger.Error
            ErrorCount += 1
        End Sub
    End Class
End Class
