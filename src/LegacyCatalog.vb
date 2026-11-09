Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions

Public Class CatalogEntry
    Public Property Name As String = ""
    Public Property Category As String = ""
    Public Property Source As String = ""
    Public Property OriginalLine As String = ""
    Public Overrides Function ToString() As String
        Return Name & "  [" & Category & "]" & If(Source = "", "", "  " & Source)
    End Function
End Class

Public Class LegacyCatalog
    Private ReadOnly _items As New List(Of CatalogEntry)()
    Public ReadOnly Property Count As Integer
        Get
            Return _items.Count
        End Get
    End Property

    ' These book codes belong to sources explicitly excluded from NSRCG II.
    Private Shared ReadOnly BannedSource As New Regex("(?i)(^|[^a-z0-9])(sr1|sr2|cb1|cb2|cb3|cb4|cp|pw|n/sl|nagee\d*|adh|bjf|ray|r3)(?=\.|\b)", RegexOptions.Compiled)
    Private Shared ReadOnly SourceCode As New Regex("(?i)\b[a-z][a-z0-9/]*\.\d+\b", RegexOptions.Compiled)
    Private Shared ReadOnly DataPrefix As New Regex("^\d+\s*-\s*([*+]?\s*)", RegexOptions.Compiled)

    Public Sub LoadDirectory(folder As String)
        If Not Directory.Exists(folder) Then Throw New DirectoryNotFoundException(folder)
        Dim files As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {"SKILLS.DAT", "Skill"}, {"GEAR.DAT", "Gear"}, {"cyber.dat", "Cyberware"},
            {"bioware.dat", "Bioware"}, {"SPELLS.DAT", "Spell"}, {"adept.dat", "Adept Power"},
            {"vehicles.dat", "Vehicle"}, {"DECK.dat", "Cyberdeck"}, {"EDGE.DAT", "Edge/Flaw"},
            {"MAGEGEAR.DAT", "Magic Gear"}, {"contacts.dat", "Contact"}, {"TOTEMS.DAT", "Totem"}
        }
        _items.Clear()
        For Each entry As KeyValuePair(Of String, String) In files
            Dim path As String = Path.Combine(folder, entry.Key)
            If File.Exists(path) Then ReadFile(path, entry.Value)
        Next
    End Sub

    Private Sub ReadFile(path As String, kind As String)
        Dim currentGroup As String = kind
        For Each raw As String In File.ReadLines(path, Encoding.Latin1)
            Dim line As String = raw.Trim()
            If line = "" OrElse line.StartsWith("!") Then Continue For
            If BannedSource.IsMatch(line) Then Continue For
            If kind = "Skill" Then
                If line.StartsWith("#") Then
                    currentGroup = line.TrimStart("#"c).Trim()
                    Continue For
                End If
                If line.StartsWith(" ") OrElse line.StartsWith("weapon->", StringComparison.OrdinalIgnoreCase) Then Continue For
                If Not line.Contains("("c) OrElse Not line.Contains("|"c) Then Continue For
            Else
                If line.StartsWith("0-") OrElse line.StartsWith("1-") Then Continue For
                If Not DataPrefix.IsMatch(line) Then Continue For
                line = DataPrefix.Replace(line, "").Trim()
            End If
            Dim name As String = line.Split("|"c)(0).Trim()
            If kind <> "Skill" Then name = Regex.Replace(name, "\s+\d+$", "").Trim()
            If String.IsNullOrWhiteSpace(name) OrElse name.Length > 110 Then Continue For
            Dim citation As Match = SourceCode.Match(raw)
            _items.Add(New CatalogEntry With {
                .Name = name.Replace("_"c, " "c),
                .Category = If(kind = "Skill", currentGroup, kind),
                .Source = If(citation.Success, citation.Value, ""),
                .OriginalLine = raw
            })
        Next
    End Sub

    Public Function Search(query As String, Optional limit As Integer = 300) As List(Of CatalogEntry)
        Dim needle As String = If(query, "").Trim()
        Return _items.Where(Function(x) needle = "" OrElse
                            x.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) OrElse
                            x.Category.Contains(needle, StringComparison.OrdinalIgnoreCase) OrElse
                            x.Source.Contains(needle, StringComparison.OrdinalIgnoreCase)).Take(limit).ToList()
    End Function
End Class
