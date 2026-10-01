' ConfigurationLoadResult.vb
' 設定モデルと、利用継続が可能な問題の警告をまとめて返します。

Imports System.Collections.ObjectModel
Imports System.Linq
Imports Launcher.Core.Models

Namespace Services
    ''' <summary>
    ''' 正常に構築した設定と画像・実行ファイル欠落の警告を保持します。
    ''' </summary>
    Public NotInheritable Class ConfigurationLoadResult
        Private ReadOnly _configuration As LauncherConfiguration
        Private ReadOnly _warnings As ReadOnlyCollection(Of String)

        ''' <summary>
        ''' 読み込み結果を変更不可の警告一覧とともに初期化します。
        ''' </summary>
        Public Sub New(configuration As LauncherConfiguration, warnings As IEnumerable(Of String))
            _configuration = configuration
            _warnings = New ReadOnlyCollection(Of String)(warnings.ToList())
        End Sub

        Public ReadOnly Property Configuration As LauncherConfiguration
            Get
                Return _configuration
            End Get
        End Property

        Public ReadOnly Property Warnings As ReadOnlyCollection(Of String)
            Get
                Return _warnings
            End Get
        End Property
    End Class
End Namespace
