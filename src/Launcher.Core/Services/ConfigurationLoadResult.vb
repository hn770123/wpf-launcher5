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
        ''' <summary>
        ''' 読み込み結果を変更不可の警告一覧とともに初期化します。
        ''' </summary>
        Public Sub New(configuration As LauncherConfiguration, warnings As IEnumerable(Of String))
            Me.Configuration = configuration
            Me.Warnings = New ReadOnlyCollection(Of String)(warnings.ToList())
        End Sub

        Public ReadOnly Property Configuration As LauncherConfiguration
        Public ReadOnly Property Warnings As ReadOnlyCollection(Of String)
    End Class
End Namespace
