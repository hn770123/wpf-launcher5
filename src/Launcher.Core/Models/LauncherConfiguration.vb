' LauncherConfiguration.vb
' XML 設定から生成されるランチャーのドメインモデルを定義します。
' 公開コレクションを読み取り専用にし、読み込み後の意図しない変更を防ぎます。

Imports System.Collections.ObjectModel
Imports System.Linq

Namespace Models
    ''' <summary>
    ''' ランチャー全体の表示名、設定バージョン、カテゴリーを保持します。
    ''' </summary>
    Public NotInheritable Class LauncherConfiguration
        ''' <summary>
        ''' 検証済みの設定値からインスタンスを初期化します。
        ''' </summary>
        Public Sub New(title As String, version As Integer, categories As IEnumerable(Of CategoryDefinition))
            Me.Title = title
            Me.Version = version
            Me.Categories = New ReadOnlyCollection(Of CategoryDefinition)(categories.ToList())
        End Sub

        Public ReadOnly Property Title As String
        Public ReadOnly Property Version As Integer
        Public ReadOnly Property Categories As ReadOnlyCollection(Of CategoryDefinition)
    End Class

    ''' <summary>
    ''' 一つのカテゴリーと、その表示対象となるボタンを保持します。
    ''' </summary>
    Public NotInheritable Class CategoryDefinition
        ''' <summary>
        ''' 検証済みのカテゴリー値からインスタンスを初期化します。
        ''' </summary>
        Public Sub New(id As String, name As String, order As Integer, buttons As IEnumerable(Of ButtonDefinition))
            Me.Id = id
            Me.Name = name
            Me.Order = order
            Me.Buttons = New ReadOnlyCollection(Of ButtonDefinition)(buttons.ToList())
        End Sub

        Public ReadOnly Property Id As String
        Public ReadOnly Property Name As String
        Public ReadOnly Property Order As Integer
        Public ReadOnly Property Buttons As ReadOnlyCollection(Of ButtonDefinition)
    End Class

    ''' <summary>
    ''' 外部プログラムを起動するボタンの表示値と解決済みパスを保持します。
    ''' </summary>
    Public NotInheritable Class ButtonDefinition
        ''' <summary>
        ''' 検証済みのボタン値からインスタンスを初期化します。
        ''' </summary>
        Public Sub New(id As String, name As String, order As Integer, executablePath As String,
                       arguments As String, workingDirectory As String, imagePath As String,
                       isEnabled As Boolean)
            Me.Id = id
            Me.Name = name
            Me.Order = order
            Me.ExecutablePath = executablePath
            Me.Arguments = arguments
            Me.WorkingDirectory = workingDirectory
            Me.ImagePath = imagePath
            Me.IsEnabled = isEnabled
        End Sub

        Public ReadOnly Property Id As String
        Public ReadOnly Property Name As String
        Public ReadOnly Property Order As Integer
        Public ReadOnly Property ExecutablePath As String
        Public ReadOnly Property Arguments As String
        Public ReadOnly Property WorkingDirectory As String
        Public ReadOnly Property ImagePath As String
        Public ReadOnly Property IsEnabled As Boolean
    End Class
End Namespace
