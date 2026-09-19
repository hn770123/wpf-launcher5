' LauncherConfigurationLoader.vb
' 埋め込み XSD で XML を検証し、安全なドメインモデルへ変換します。
' DTD、外部エンティティ、過大な文書を読み込み前に拒否します。

Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Linq
Imports System.Security
Imports System.Xml
Imports System.Xml.Linq
Imports System.Xml.Schema
Imports Launcher.Core.Models

Namespace Services
    ''' <summary>
    ''' ランチャー設定を安全に読み込み、検証済みモデルへ変換します。
    ''' </summary>
    Public NotInheritable Class LauncherConfigurationLoader
        Private Const MaximumDocumentCharacters As Long = 1048576
        Private Const SchemaResourceSuffix As String = "launcher-config.xsd"
        Private ReadOnly _pathResolver As ConfigurationPathResolver

        ''' <summary>
        ''' 既定のパス解決サービスを使用するローダーを作成します。
        ''' </summary>
        Public Sub New()
            Me.New(New ConfigurationPathResolver())
        End Sub

        ''' <summary>
        ''' 指定されたパス解決サービスを使用するローダーを作成します。
        ''' </summary>
        Public Sub New(pathResolver As ConfigurationPathResolver)
            If pathResolver Is Nothing Then Throw New ArgumentNullException(NameOf(pathResolver))
            _pathResolver = pathResolver
        End Sub

        ''' <summary>
        ''' XML ファイルを検証してモデルへ変換し、継続可能な問題を警告として返します。
        ''' </summary>
        Public Function Load(configurationPath As String) As ConfigurationLoadResult
            If String.IsNullOrWhiteSpace(configurationPath) Then
                Throw New ConfigurationException("設定ファイルのパスを指定してください。")
            End If

            Dim fullPath As String = configurationPath
            Try
                fullPath = Path.GetFullPath(configurationPath)
                If Not File.Exists(fullPath) Then
                    Throw New ConfigurationException($"設定ファイルが見つかりません: {fullPath}")
                End If

                Dim document As XDocument = ReadValidatedDocument(fullPath)
                Return ConvertToModel(document, Path.GetDirectoryName(fullPath))
            Catch ex As ConfigurationException
                Throw
            Catch ex As XmlException
                Throw New ConfigurationException(
                    $"設定ファイル '{fullPath}' の {ex.LineNumber} 行 {ex.LinePosition} 列を確認してください: {ex.Message}", ex)
            Catch ex As XmlSchemaValidationException
                Throw New ConfigurationException(
                    $"設定ファイル '{fullPath}' の {ex.LineNumber} 行 {ex.LinePosition} 列がスキーマに適合しません: {ex.Message}", ex)
            Catch ex As IOException
                Throw New ConfigurationException($"設定ファイル '{fullPath}' を読み込めません: {ex.Message}", ex)
            Catch ex As UnauthorizedAccessException
                Throw New ConfigurationException($"設定ファイル '{fullPath}' を読み込む権限がありません。", ex)
            Catch ex As SecurityException
                Throw New ConfigurationException($"設定ファイル '{fullPath}' へのアクセスがセキュリティ設定で拒否されました。", ex)
            Catch ex As ArgumentException
                Throw New ConfigurationException($"設定ファイルまたは設定内のパスが不正です: {ex.Message}", ex)
            Catch ex As NotSupportedException
                Throw New ConfigurationException($"設定ファイルまたは設定内のパス形式を使用できません: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>
        ''' DTD と外部参照を無効化した XmlReader で XSD 検証を実行します。
        ''' </summary>
        Private Shared Function ReadValidatedDocument(path As String) As XDocument
            Dim settings As New XmlReaderSettings() With {
                .DtdProcessing = DtdProcessing.Prohibit,
                .XmlResolver = Nothing,
                .MaxCharactersInDocument = MaximumDocumentCharacters,
                .ValidationType = ValidationType.Schema
            }
            settings.Schemas.Add(LoadSchema())
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
            AddHandler settings.ValidationEventHandler, AddressOf ThrowValidationError

            Using stream As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using reader As XmlReader = XmlReader.Create(stream, settings, path)
                    Return XDocument.Load(reader, LoadOptions.SetLineInfo)
                End Using
            End Using
        End Function

        ''' <summary>
        ''' アセンブリに埋め込んだ、アプリと同版のスキーマを読み込みます。
        ''' </summary>
        Private Shared Function LoadSchema() As XmlSchema
            Dim assembly As Assembly = GetType(LauncherConfigurationLoader).Assembly
            Dim resourceName As String = assembly.GetManifestResourceNames().Single(
                Function(name) name.EndsWith(SchemaResourceSuffix, StringComparison.OrdinalIgnoreCase))
            Using stream As Stream = assembly.GetManifestResourceStream(resourceName)
                Return XmlSchema.Read(stream, Nothing)
            End Using
        End Function

        ''' <summary>
        ''' XSD の警告とエラーを例外へ昇格し、不完全な設定を受理しないようにします。
        ''' </summary>
        Private Shared Sub ThrowValidationError(sender As Object, args As ValidationEventArgs)
            Throw args.Exception
        End Sub

        ''' <summary>
        ''' 検証済み XML を並び順が確定したドメインモデルへ変換します。
        ''' </summary>
        Private Function ConvertToModel(document As XDocument, baseDirectory As String) As ConfigurationLoadResult
            Dim root As XElement = document.Root
            Dim warnings As New List(Of String)()
            Dim categoryIds As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim buttonIds As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim categories As New List(Of CategoryDefinition)()

            For Each categoryElement As XElement In root.Element("categories").Elements("category")
                Dim categoryId As String = categoryElement.Attribute("id").Value
                EnsureUniqueId(categoryIds, categoryId, "カテゴリー")
                Dim buttons As New List(Of ButtonDefinition)()

                For Each buttonElement As XElement In categoryElement.Elements("button")
                    Dim buttonId As String = buttonElement.Attribute("id").Value
                    EnsureUniqueId(buttonIds, buttonId, "ボタン")
                    buttons.Add(CreateButton(buttonElement, baseDirectory, warnings))
                Next

                categories.Add(New CategoryDefinition(
                    categoryId,
                    categoryElement.Attribute("name").Value,
                    ParseOrder(categoryElement.Attribute("order").Value),
                    buttons.OrderBy(Function(button) button.Order)))
            Next

            Dim configuration As New LauncherConfiguration(
                root.Attribute("title").Value,
                Integer.Parse(root.Attribute("version").Value, CultureInfo.InvariantCulture),
                categories.OrderBy(Function(category) category.Order))
            Return New ConfigurationLoadResult(configuration, warnings)
        End Function

        ''' <summary>
        ''' ボタンのパスを解決し、欠落した起動対象を安全に無効化します。
        ''' </summary>
        Private Function CreateButton(element As XElement, baseDirectory As String,
                                      warnings As List(Of String)) As ButtonDefinition
            Dim id As String = element.Attribute("id").Value
            Dim executable As String = _pathResolver.Resolve(element.Attribute("executable").Value, baseDirectory)
            Dim workingDirectory As String = GetOptionalResolvedPath(element, "workingDirectory", baseDirectory)
            Dim image As String = GetOptionalResolvedPath(element, "image", baseDirectory)

            Dim isEnabled As Boolean = File.Exists(executable)
            If Not isEnabled Then warnings.Add($"ボタン '{id}' の実行ファイルが見つからないため無効化しました: {executable}")
            If image.Length > 0 AndAlso Not File.Exists(image) Then
                warnings.Add($"ボタン '{id}' の画像が見つからないため既定画像を使用します: {image}")
                image = String.Empty
            End If

            Return New ButtonDefinition(
                id,
                element.Attribute("name").Value,
                ParseOrder(element.Attribute("order").Value),
                executable,
                GetOptionalAttribute(element, "arguments"),
                workingDirectory,
                image,
                isEnabled)
        End Function

        ''' <summary>
        ''' 任意のパス属性を取得し、存在する場合だけ絶対パスへ変換します。
        ''' </summary>
        Private Function GetOptionalResolvedPath(element As XElement, attributeName As String,
                                                 baseDirectory As String) As String
            Return _pathResolver.Resolve(GetOptionalAttribute(element, attributeName), baseDirectory)
        End Function

        ''' <summary>
        ''' 任意属性が省略された場合に空文字を返します。
        ''' </summary>
        Private Shared Function GetOptionalAttribute(element As XElement, attributeName As String) As String
            Dim attribute As XAttribute = element.Attribute(attributeName)
            Return If(attribute Is Nothing, String.Empty, attribute.Value)
        End Function

        ''' <summary>
        ''' XSD 検証後の order をカルチャーに依存せず整数へ変換します。
        ''' </summary>
        Private Shared Function ParseOrder(value As String) As Integer
            Return Integer.Parse(value, CultureInfo.InvariantCulture)
        End Function

        ''' <summary>
        ''' 大文字小文字だけが異なる ID も重複として、修正対象を明示します。
        ''' </summary>
        Private Shared Sub EnsureUniqueId(ids As HashSet(Of String), id As String, kind As String)
            If Not ids.Add(id) Then
                Throw New ConfigurationException($"{kind} ID '{id}' が重複しています。ID は大文字小文字を区別せず一意にしてください。")
            End If
        End Sub
    End Class
End Namespace
