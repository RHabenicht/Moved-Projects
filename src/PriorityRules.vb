Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Linq

' SR3 core Priority table only. This is not the Companion point-buy system.
' Attribute and resource allocations are starting limits before advancement or purchases.
Public Module PriorityRules
    Private ReadOnly AttributePoints As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
        {"A", 30}, {"B", 27}, {"C", 24}, {"D", 21}, {"E", 18}
    }
    Private ReadOnly SkillPoints As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
        {"A", 50}, {"B", 40}, {"C", 34}, {"D", 30}, {"E", 27}
    }
    Private ReadOnly ResourceNuyen As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
        {"A", 1000000}, {"B", 400000}, {"C", 90000}, {"D", 20000}, {"E", 5000}
    }

    Private Function Lookup(table As Dictionary(Of String, Integer), priority As String) As Integer
        Dim found As Integer
        If table.TryGetValue(If(priority, "").Trim(), found) Then Return found
        Return 0
    End Function

    Public Function AttributeBudget(priority As String) As Integer
        Return Lookup(AttributePoints, priority)
    End Function

    Public Function SkillBudget(priority As String) As Integer
        Return Lookup(SkillPoints, priority)
    End Function

    Public Function StartingResources(priority As String) As Integer
        Return Lookup(ResourceNuyen, priority)
    End Function

    Public Function RequiredRacePriority(metatype As String) As String
        Select Case If(metatype, "").Trim().ToUpperInvariant()
            Case "HUMAN" : Return "E"
            Case "DWARF", "ORK" : Return "D"
            Case "ELF", "TROLL" : Return "C"
            Case Else : Return ""
        End Select
    End Function

    Public Function AttributeSpent(character As CharacterSheet) As Integer
        character.Normalize()
        Return CharacterSheet.AttributeNames.Sum(Function(name As String) character.Attributes(name))
    End Function

    Public Function NaturalAttribute(metatype As String, attributeName As String, baseRating As Integer) As Integer
        Dim delta As Integer = 0
        Select Case If(metatype, "").Trim().ToUpperInvariant()
            Case "DWARF"
                Select Case attributeName
                    Case "Body", "Willpower" : delta = 1
                    Case "Strength" : delta = 2
                End Select
            Case "ELF"
                Select Case attributeName
                    Case "Quickness" : delta = 1
                    Case "Charisma" : delta = 2
                End Select
            Case "ORK"
                Select Case attributeName
                    Case "Body" : delta = 3
                    Case "Strength" : delta = 2
                    Case "Charisma", "Intelligence" : delta = -1
                End Select
            Case "TROLL"
                Select Case attributeName
                    Case "Body" : delta = 5
                    Case "Strength" : delta = 4
                    Case "Quickness" : delta = -1
                    Case "Charisma", "Intelligence" : delta = -2
                End Select
        End Select
        Return baseRating + delta
    End Function

    Public Function NaturalReaction(character As CharacterSheet) As Integer
        character.Normalize()
        Return (NaturalAttribute(character.Metatype, "Quickness", character.Attributes("Quickness")) +
                NaturalAttribute(character.Metatype, "Intelligence", character.Attributes("Intelligence"))) \ 2
    End Function

    Public Function NaturalCombatPool(character As CharacterSheet) As Integer
        character.Normalize()
        Return (NaturalAttribute(character.Metatype, "Quickness", character.Attributes("Quickness")) +
                NaturalAttribute(character.Metatype, "Intelligence", character.Attributes("Intelligence")) +
                NaturalAttribute(character.Metatype, "Willpower", character.Attributes("Willpower"))) \ 2
    End Function
End Module
