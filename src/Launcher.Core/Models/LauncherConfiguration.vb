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
        Private ReadOnly _title As String
        Private ReadOnly _version As Integer
        Private ReadOnly _categories As ReadOnlyCollection(Of CategoryDefinition)

        ''' <summary>
        ''' 検証済みの設定値からインスタンスを初期化します。
        ''' </summary>
        Public Sub New(title As String, version As Integer, categories As IEnumerable(Of CategoryDefinition))
            _title = title
            _version = version
            _categories = New ReadOnlyCollection(Of CategoryDefinition)(categories.ToList())
        End Sub

        Public ReadOnly Property Title As String
            Get
                Return _title
            End Get
        End Property

        Public ReadOnly Property Version As Integer
            Get
                Return _version
            End Get
        End Property

        Public ReadOnly Property Categories As ReadOnlyCollection(Of CategoryDefinition)
            Get
                Return _categories
            End Get
        End Property
    End Class

    ''' <summary>
    ''' 一つのカテゴリーと、その表示対象となるボタンを保持します。
    ''' </summary>
    Public NotInheritable Class CategoryDefinition
        Private ReadOnly _id As String
        Private ReadOnly _name As String
        Private ReadOnly _order As Integer
        Private ReadOnly _buttons As ReadOnlyCollection(Of ButtonDefinition)

        ''' <summary>
        ''' 検証済みのカテゴリー値からインスタンスを初期化します。
        ''' </summary>
        Public Sub New(id As String, name As String, order As Integer, buttons As IEnumerable(Of ButtonDefinition))
            _id = id
            _name = name
            _order = order
            _buttons = New ReadOnlyCollection(Of ButtonDefinition)(buttons.ToList())
        End Sub

        Public ReadOnly Property Id As String
            Get
                Return _id
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public ReadOnly Property Order As Integer
            Get
                Return _order
            End Get
        End Property

        Public ReadOnly Property Buttons As ReadOnlyCollection(Of ButtonDefinition)
            Get
                Return _buttons
            End Get
        End Property
    End Class

    ''' <summary>
    ''' 外部プログラムを起動するボタンの表示値と解決済みパスを保持します。
    ''' </summary>
    Public NotInheritable Class ButtonDefinition
        Private ReadOnly _id As String
        Private ReadOnly _name As String
        Private ReadOnly _order As Integer
        Private ReadOnly _executablePath As String
        Private ReadOnly _arguments As String
        Private ReadOnly _workingDirectory As String
        Private ReadOnly _imagePath As String
        Private ReadOnly _isEnabled As Boolean

        ''' <summary>
        ''' 検証済みのボタン値からインスタンスを初期化します。
        ''' </summary>
        Public Sub New(id As String, name As String, order As Integer, executablePath As String,
                       arguments As String, workingDirectory As String, imagePath As String,
                       isEnabled As Boolean)
            _id = id
            _name = name
            _order = order
            _executablePath = executablePath
            _arguments = arguments
            _workingDirectory = workingDirectory
            _imagePath = imagePath
            _isEnabled = isEnabled
        End Sub

        Public ReadOnly Property Id As String
            Get
                Return _id
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public ReadOnly Property Order As Integer
            Get
                Return _order
            End Get
        End Property

        Public ReadOnly Property ExecutablePath As String
            Get
                Return _executablePath
            End Get
        End Property

        Public ReadOnly Property Arguments As String
            Get
                Return _arguments
            End Get
        End Property

        Public ReadOnly Property WorkingDirectory As String
            Get
                Return _workingDirectory
            End Get
        End Property

        Public ReadOnly Property ImagePath As String
            Get
                Return _imagePath
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean
            Get
                Return _isEnabled
            End Get
        End Property
    End Class
End Namespace
