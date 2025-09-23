Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Xml

Namespace Microsoft.SmallVisualBasic.LanguageService
    Public Class ModuleDocumentation
        Private _itemDocMap As Dictionary(Of String, CompletionItemDocumentation)

        Public Sub New(modulePath As String)
            Dim localizedModuleDocPath = GetLocalizedModuleDocPath(modulePath)

            If File.Exists(localizedModuleDocPath) Then
                ProcessDocumentation(localizedModuleDocPath)
            End If
        End Sub

        Public Function GetItemDocumentation(itemName As String) As CompletionItemDocumentation
            Dim value As CompletionItemDocumentation = Nothing

            If _itemDocMap IsNot Nothing Then
                _itemDocMap.TryGetValue(itemName, value)
            End If

            Return value
        End Function

        Private Function GetLocalizedModuleDocPath(modulePath As String) As String
            Dim directoryName = Path.GetDirectoryName(modulePath)
            Dim fileName = Path.GetFileNameWithoutExtension(modulePath)

            Dim docPath = GetLocalizedDocPath(directoryName, fileName, CultureInfo.CurrentUICulture)
            If docPath = "" Then
                docPath = GetLocalizedDocPath(directoryName, fileName, CultureInfo.CurrentCulture)
            End If

            If docPath = "" Then
                docPath = Path.Combine(directoryName, fileName & ".xml")
            End If

            Return docPath
        End Function

        Private Shared Function GetLocalizedDocPath(directoryName As String, fileName As String, cult As CultureInfo) As String
            Dim ietfLanguageTag = cult.IetfLanguageTag
            Dim text = Path.Combine(directoryName, $"{fileName}.{ietfLanguageTag}.xml")
            If File.Exists(text) Then Return text

            ietfLanguageTag = cult.TwoLetterISOLanguageName
            text = Path.Combine(directoryName, $"{fileName}.{ietfLanguageTag}.xml")
            If File.Exists(text) Then Return text

            If cult.Parent IsNot Nothing Then
                ietfLanguageTag = cult.Parent.IetfLanguageTag
                text = Path.Combine(directoryName, $"{fileName}.{ietfLanguageTag}.xml")
                If File.Exists(text) Then Return text
            End If

            Return ""
        End Function

        Private Sub ProcessDocumentation(xmlFilePath As String)
            _itemDocMap = New Dictionary(Of String, CompletionItemDocumentation)()

            Try
                Dim xmlDocument As XmlDocument = New XmlDocument()
                xmlDocument.PreserveWhitespace = False
                xmlDocument.Load(xmlFilePath)
                Dim xmlNodeList = xmlDocument.SelectNodes("doc/members/member")

                For Each item As XmlNode In xmlNodeList
                    Dim xmlAttribute = item.Attributes("name")

                    If xmlAttribute Is Nothing Then Continue For

                    Dim value = xmlAttribute.Value
                    Dim documentation As New CompletionItemDocumentation()
                    _itemDocMap(value) = documentation

                    Dim xmlNode2 = item.SelectSingleNode("summary")
                    documentation.Summary = GetTextFromXmlNode(xmlNode2)

                    Dim xmlNode3 = item.SelectSingleNode("returns")
                    documentation.Returns = GetTextFromXmlNode(xmlNode3)

                    Dim xmlNode4 = item.SelectSingleNode("example")
                    documentation.Example = GetTextFromXmlNode(xmlNode4)

                    Dim xmlNodeList2 = item.SelectNodes("param")
                    If xmlNodeList2 Is Nothing Then Continue For

                    For Each item2 As XmlNode In xmlNodeList2
                        Dim xmlAttribute2 = item2.Attributes("name")

                        If xmlAttribute2 IsNot Nothing Then
                            Dim value2 = xmlAttribute2.Value
                            documentation.ParamsDoc(value2) = GetTextFromXmlNode(item2)
                        End If
                    Next
                Next

            Catch
            End Try
        End Sub

        Private Function GetTextFromXmlNode(xmlNode As XmlNode) As String
            If xmlNode Is Nothing Then Return Nothing

            Dim stringBuilder As New StringBuilder()
            Dim stringReader As New StringReader(xmlNode.InnerText.Trim())

            Do
                Dim text As String = stringReader.ReadLine()
                If text Is Nothing Then Exit Do

                If text.StartsWith("            ") Then text = text.Substring(12)
                stringBuilder.AppendLine(text)
            Loop

            Return stringBuilder.ToString().TrimEnd
        End Function
    End Class
End Namespace
