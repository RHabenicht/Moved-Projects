Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.Json

Public Class SkillEntry
    Public Property Name As String = ""
    Public Property Rating As Integer = 1
    Public Property Category As String = "Active"
End Class

Public Class EquipmentEntry
    Public Property Name As String = ""
    Public Property Category As String = "Gear"
    Public Property Source As String = ""
End Class

Public Class CharacterSheet
    Public Property Format As String = "nsrcgii.vbnet.1"
    Public Property Edition As Integer = 3
    Public Property Name As String = "New Character"
    Public Property RealName As String = ""
    Public Property Metatype As String = "Human"
    Public Property Attributes As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
        {"Body", 1}, {"Quickness", 1}, {"Strength", 1}, {"Charisma", 1}, {"Intelligence", 1}, {"Willpower", 1}
    }
    Public Property Priorities As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"Resources", "A"}, {"Attributes", "B"}, {"Race", "E"}, {"Skills", "C"}, {"Magic", "D"}
    }
    Public Property Skills As List(Of SkillEntry) = New List(Of SkillEntry)()
    Public Property Equipment As List(Of EquipmentEntry) = New List(Of EquipmentEntry)()
    Public Property Notes As String = ""
    Public Property LegacyOriginal As String = ""

    Public Shared ReadOnly AttributeNames As String() = {"Body", "Quickness", "Strength", "Charisma", "Intelligence", "Willpower"}
    Public Shared ReadOnly PriorityNames As String() = {"Resources", "Attributes", "Race", "Skills", "Magic"}

    Public Sub Normalize()
        If Attributes Is Nothing Then Attributes = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        If Priorities Is Nothing Then Priorities = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        If Skills Is Nothing Then Skills = New List(Of SkillEntry)()
        If Equipment Is Nothing Then Equipment = New List(Of EquipmentEntry)()
        For Each key As String In AttributeNames
            If Not Attributes.ContainsKey(key) Then Attributes(key) = 1
        Next
        Dim defaults As New CharacterSheet()
        For Each key As String In PriorityNames
            If Not Priorities.ContainsKey(key) Then Priorities(key) = defaults.Priorities(key)
        Next
        If String.IsNullOrWhiteSpace(Metatype) Then Metatype = "Human"
        If Name Is Nothing Then Name = ""
        If Notes Is Nothing Then Notes = ""
        If LegacyOriginal Is Nothing Then LegacyOriginal = ""
    End Sub

    Public Function ValidateBasic() As List(Of String)
        Normalize()
        Dim issues As New List(Of String)()
        If Edition <> 3 Then issues.Add("This version supports Shadowrun Third Edition only.")
        Dim assigned As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each key As String In PriorityNames
            Dim value As String = Priorities(key).Trim().ToUpperInvariant()
            If value.Length <> 1 OrElse "ABCDE".IndexOf(value, StringComparison.Ordinal) < 0 Then
                issues.Add("Invalid priority for " & key & ".")
            ElseIf Not assigned.Add(value) Then
                issues.Add("Priority " & value & " is assigned more than once.")
            End If
        Next
        For Each key As String In AttributeNames
            If Attributes(key) < 1 OrElse Attributes(key) > 6 Then issues.Add(key & " base rating must be 1–6.")
        Next
        For Each item As SkillEntry In Skills
            If String.IsNullOrWhiteSpace(item.Name) OrElse item.Rating < 1 OrElse item.Rating > 6 Then
                issues.Add("Skill name/rating needs review: " & item.Name)
            End If
        Next
        Return issues
    End Function

    Public Sub Save(path As String)
        Normalize()
        If Edition <> 3 Then Throw New InvalidDataException("Only SR3 characters are supported.")
        Dim options As New JsonSerializerOptions With {.WriteIndented = True}
        Dim content As String = JsonSerializer.Serialize(Me, options)
        Dim temp As String = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)), ".nsrcg-save-" & Guid.NewGuid().ToString("N") & ".tmp")
        Try
            File.WriteAllText(temp, content, New UTF8Encoding(False))
            File.Move(temp, path, True)
        Finally
            If File.Exists(temp) Then File.Delete(temp)
        End Try
    End Sub

    Public Shared Function Open(path As String) As CharacterSheet
        Dim text As String = File.ReadAllText(path, Encoding.UTF8)
        Dim result As CharacterSheet = JsonSerializer.Deserialize(Of CharacterSheet)(text)
        If result Is Nothing Then Throw New InvalidDataException("Invalid character file.")
        result.Normalize()
        If result.Edition <> 3 Then Throw New InvalidDataException("Only SR3 characters are supported.")
        Return result
    End Function

    ' .sr3 files are INI-like. A conservative import retains the complete original text.
    Public Shared Function ImportLegacy(path As String) As CharacterSheet
        Dim text As String = File.ReadAllText(path, Encoding.Latin1)
        Dim result As New CharacterSheet With {.LegacyOriginal = text}
        Dim section As String = ""
        For Each raw As String In text.Replace(vbCrLf, vbLf).Split(ControlChars.Lf)
            Dim line As String = raw.Trim()
            If line.StartsWith("[") AndAlso line.EndsWith("]") Then
                section = line.Substring(1, line.Length - 2).ToLowerInvariant()
                Continue For
            End If
            Dim eq As Integer = line.IndexOf("="c)
            If eq < 1 Then Continue For
            Dim key As String = line.Substring(0, eq).Trim()
            Dim value As String = line.Substring(eq + 1).Trim()
            Select Case section
                Case "general"
                    If key.Equals("CharName", StringComparison.OrdinalIgnoreCase) Then result.Name = UnderscoresToSpaces(value)
                    If key.Equals("RealName", StringComparison.OrdinalIgnoreCase) Then result.RealName = UnderscoresToSpaces(value)
                    If key.Equals("Notes", StringComparison.OrdinalIgnoreCase) Then result.Notes = UnderscoresToSpaces(value)
                Case "race"
                    If key.Equals("RaceName", StringComparison.OrdinalIgnoreCase) Then result.Metatype = UnderscoresToSpaces(value)
                Case "attributes"
                    For Each attr As String In AttributeNames
                        If key.Equals(attr, StringComparison.OrdinalIgnoreCase) Then
                            Dim n As Integer
                            If Integer.TryParse(value.Split("|"c)(0), NumberStyles.Integer, CultureInfo.InvariantCulture, n) Then
                                result.Attributes(attr) = n
                            End If
                        End If
                    Next
                Case "skills"
                    If key.StartsWith("Skill", StringComparison.OrdinalIgnoreCase) AndAlso key.Length > 5 AndAlso Char.IsDigit(key(5)) Then
                        Dim item As SkillEntry = ParseLegacySkill(value)
                        If item IsNot Nothing Then result.Skills.Add(item)
                    End If
                Case "gear", "cyberware", "vech", "deck", "contacts", "magic"
                    Dim prefix As String = If(section = "vech", "Vehicle", If(section = "deck", "Cyberdeck", If(section = "magic", "Magic", section)))
                    Dim matchesPrefix As Boolean = key.StartsWith(If(section = "gear", "Gear", If(section = "cyberware", "Cyber", If(section = "vech", "Vech", If(section = "deck", "Deck", If(section = "contacts", "Contact", "Spell"))))), StringComparison.OrdinalIgnoreCase)
                    If matchesPrefix AndAlso key.Length > 4 AndAlso Char.IsDigit(key(key.Length - 1)) Then
                        Dim label As String = value.Split("~"c)(0).Trim()
                        If Not String.IsNullOrEmpty(label) Then result.Equipment.Add(New EquipmentEntry With {.Name = UnderscoresToSpaces(label), .Category = prefix})
                    End If
            End Select
        Next
        result.Normalize()
        Return result
    End Function

    Private Shared Function ParseLegacySkill(value As String) As SkillEntry
        Dim label As String = value.Split("~"c)(0).Trim()
        If label.Length = 0 Then Return Nothing
        Dim rating As Integer = 1
        Dim lastEq As Integer = value.LastIndexOf("="c)
        If lastEq >= 0 Then
            Dim parsed As Integer
            If Integer.TryParse(value.Substring(lastEq + 1).Trim(), parsed) AndAlso parsed >= 1 AndAlso parsed <= 12 Then rating = parsed
        End If
        Dim category As String = If(label.Contains("(KNO)", StringComparison.OrdinalIgnoreCase) OrElse label.Contains("(LAN)", StringComparison.OrdinalIgnoreCase), "Knowledge/Language", "Active")
        Return New SkillEntry With {.Name = UnderscoresToSpaces(label), .Rating = rating, .Category = category}
    End Function

    Private Shared Function UnderscoresToSpaces(value As String) As String
        Return value.Replace("_"c, " "c).Trim()
    End Function
End Class
