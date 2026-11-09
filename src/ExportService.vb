Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Text

Public Module ExportService
    Public Sub WriteHtml(character As CharacterSheet, path As String)
        character.Normalize()
        Dim output As New StringBuilder()
        output.AppendLine("<!doctype html><html lang=""en""><head><meta charset=""utf-8""><title>NSRCG II character</title>")
        output.AppendLine("<style>body{font:15px Segoe UI,Arial,sans-serif;max-width:900px;margin:1.5rem auto;color:#222}h1{border-bottom:2px solid #333}table{border-collapse:collapse;width:100%;margin-bottom:1.3rem}th,td{padding:.4rem;border-bottom:1px solid #ccc;text-align:left}pre{white-space:pre-wrap}@media print{body{margin:0.35in}}</style></head><body>")
        output.AppendLine("<h1>" & WebUtility.HtmlEncode(character.Name) & "</h1><p>NSRCG II | Shadowrun Third Edition | " & WebUtility.HtmlEncode(character.Metatype) & "</p>")
        output.AppendLine("<h2>Attributes</h2><table><tr><th>Attribute</th><th>Base</th></tr>")
        For Each key As String In CharacterSheet.AttributeNames
            output.AppendLine("<tr><td>" & key & "</td><td>" & character.Attributes(key).ToString() & "</td></tr>")
        Next
        output.AppendLine("</table><h2>Priorities</h2><table><tr><th>Category</th><th>Priority</th></tr>")
        For Each key As String In CharacterSheet.PriorityNames
            output.AppendLine("<tr><td>" & key & "</td><td>" & WebUtility.HtmlEncode(character.Priorities(key)) & "</td></tr>")
        Next
        output.AppendLine("</table><h2>Skills</h2><table><tr><th>Skill</th><th>Rating</th><th>Type</th></tr>")
        For Each skill As SkillEntry In character.Skills
            output.AppendLine("<tr><td>" & WebUtility.HtmlEncode(skill.Name) & "</td><td>" & skill.Rating.ToString() & "</td><td>" & WebUtility.HtmlEncode(skill.Category) & "</td></tr>")
        Next
        output.AppendLine("</table><h2>Equipment / Powers / Contacts</h2><table><tr><th>Name</th><th>Category</th><th>Source</th></tr>")
        For Each item As EquipmentEntry In character.Equipment
            output.AppendLine("<tr><td>" & WebUtility.HtmlEncode(item.Name) & "</td><td>" & WebUtility.HtmlEncode(item.Category) & "</td><td>" & WebUtility.HtmlEncode(item.Source) & "</td></tr>")
        Next
        output.AppendLine("</table><h2>Notes</h2><pre>" & WebUtility.HtmlEncode(character.Notes) & "</pre>")
        output.AppendLine("<p><small>Basic editor and export. SR3 rule validation is not yet complete.</small></p></body></html>")
        File.WriteAllText(path, output.ToString(), New UTF8Encoding(False))
    End Sub

    ' Compact dependency-free PDF export. Content is split across pages and uses a standard PDF font.
    Public Sub WritePdf(character As CharacterSheet, path As String)
        character.Normalize()
        Dim lines As New List(Of String) From {"NSRCG II - Shadowrun Third Edition", "Character: " & character.Name, "Metatype: " & character.Metatype, "", "Attributes"}
        For Each key As String In CharacterSheet.AttributeNames
            lines.Add("  " & key & ": " & character.Attributes(key).ToString())
        Next
        lines.Add("")
        lines.Add("Priorities")
        For Each key As String In CharacterSheet.PriorityNames
            lines.Add("  " & key & ": " & character.Priorities(key))
        Next
        lines.Add("")
        lines.Add("Skills")
        For Each skill As SkillEntry In character.Skills
            lines.Add("  " & skill.Name & " - " & skill.Rating.ToString() & " (" & skill.Category & ")")
        Next
        lines.Add("")
        lines.Add("Equipment / Powers / Contacts")
        For Each item As EquipmentEntry In character.Equipment
            lines.Add("  " & item.Name & " [" & item.Category & "] " & item.Source)
        Next
        lines.Add("")
        lines.Add("Notes")
        For Each note As String In character.Notes.Replace(vbCrLf, vbLf).Split(ControlChars.Lf)
            lines.Add(note)
        Next
        lines.Add("")
        lines.Add("Basic editor only: verify values against SR3 rules.")
        Dim wrapped As New List(Of String)()
        For Each line As String In lines
            If line.Length = 0 Then
                wrapped.Add("")
            Else
                Dim remainder As String = line
                While remainder.Length > 90
                    wrapped.Add(remainder.Substring(0, 90))
                    remainder = "    " & remainder.Substring(90)
                End While
                wrapped.Add(remainder)
            End If
        Next
        Const pageRows As Integer = 44
        Dim pages As New List(Of List(Of String))()
        For i As Integer = 0 To wrapped.Count - 1 Step pageRows
            pages.Add(wrapped.Skip(i).Take(pageRows).ToList())
        Next
        Dim objectCount As Integer = 3 + pages.Count * 2
        Dim fontId As Integer = objectCount
        Dim objects(objectCount) As String
        objects(1) = "<< /Type /Catalog /Pages 2 0 R >>"
        Dim kids As New List(Of String)()
        For i As Integer = 0 To pages.Count - 1
            Dim pageId As Integer = 3 + (2 * i)
            Dim streamId As Integer = pageId + 1
            kids.Add(pageId.ToString() & " 0 R")
            objects(pageId) = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 " & fontId.ToString() & " 0 R >> >> /Contents " & streamId.ToString() & " 0 R >>"
            Dim commands As New StringBuilder("BT /F1 10 Tf 48 752 Td 14 TL" & vbLf)
            For Each line As String In pages(i)
                Dim safe As String = PdfEscape(line)
                commands.AppendLine("(" & safe & ") Tj T*")
            Next
            commands.AppendLine("ET")
            Dim textStream As String = commands.ToString()
            Dim size As Integer = Encoding.Latin1.GetByteCount(textStream)
            objects(streamId) = "<< /Length " & size.ToString() & " >>" & vbLf & "stream" & vbLf & textStream & "endstream"
        Next
        objects(2) = "<< /Type /Pages /Count " & pages.Count.ToString() & " /Kids [" & String.Join(" ", kids) & "] >>"
        objects(fontId) = "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"
        Using stream As New MemoryStream()
            Dim offsets As New List(Of Long) From {0}
            WriteAscii(stream, "%PDF-1.4" & vbLf)
            For id As Integer = 1 To objectCount
                offsets.Add(stream.Position)
                WriteAscii(stream, id.ToString() & " 0 obj" & vbLf & objects(id) & vbLf & "endobj" & vbLf)
            Next
            Dim xrefAt As Long = stream.Position
            WriteAscii(stream, "xref" & vbLf & "0 " & (objectCount + 1).ToString() & vbLf & "0000000000 65535 f " & vbLf)
            For id As Integer = 1 To objectCount
                WriteAscii(stream, offsets(id).ToString("D10") & " 00000 n " & vbLf)
            Next
            WriteAscii(stream, "trailer" & vbLf & "<< /Size " & (objectCount + 1).ToString() & " /Root 1 0 R >>" & vbLf & "startxref" & vbLf & xrefAt.ToString() & vbLf & "%%EOF" & vbLf)
            File.WriteAllBytes(path, stream.ToArray())
        End Using
    End Sub

    Private Function PdfEscape(text As String) As String
        Dim clean As New StringBuilder()
        For Each ch As Char In text
            Dim point As Integer = AscW(ch)
            If point < 32 OrElse point > 126 Then
                clean.Append("?"c)
            ElseIf ch = "("c OrElse ch = ")"c OrElse ch = "\"c Then
                clean.Append("\"c).Append(ch)
            Else
                clean.Append(ch)
            End If
        Next
        Return clean.ToString()
    End Function

    Private Sub WriteAscii(stream As Stream, text As String)
        Dim bytes As Byte() = Encoding.Latin1.GetBytes(text)
        stream.Write(bytes, 0, bytes.Length)
    End Sub
End Module
