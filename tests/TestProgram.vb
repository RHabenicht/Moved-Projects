Option Strict On
Imports System
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports NSRCGII

Public Module TestProgram
    Private _passes As Integer
    Private _failures As Integer

    <STAThread>
    Public Sub Main()
        Dim root As String = Directory.GetCurrentDirectory()
        Dim scratch As String = Path.Combine(Path.GetTempPath(), "nsrcgii-test-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(scratch)
        Try
            RunTest("New character defaults and basic validation", Sub() TestDefaults())
            RunTest("Rejects duplicate priorities and invalid ratings", Sub() TestInvalid())
            RunTest("JSON round-trip and overwrite", Sub() TestSaveOpen(scratch))
            RunTest("Rejects unsupported edition and malformed JSON", Sub() TestUnsafeFiles(scratch))
            RunTest("Original NSRCG .sr3 import retains source and data", Sub() TestLegacy(Path.Combine(root, "samples", "archStreetSamurai.sr3")))
            RunTest("Catalog reads approved sources and preserves skill kind", Sub() TestCatalog(Path.Combine(root, "LegacyData")))
            RunTest("HTML export escapes user input", Sub() TestHtml(scratch))
            RunTest("Multi-page PDF has valid cross-reference offsets", Sub() TestPdf(scratch))
            RunTest("Windows Forms window opens, edits skills and preserves draft", Sub() TestGui())
        Finally
            Try
                Directory.Delete(scratch, True)
            Catch
                ' A failed cleanup should not conceal a test failure.
            End Try
        End Try
        Console.WriteLine("RESULT: " & _passes & " passed, " & _failures & " failed.")
        If _failures > 0 Then Environment.ExitCode = 1
    End Sub

    Private Sub RunTest(label As String, action As Action)
        Try
            action()
            _passes += 1
            Console.WriteLine("PASS " & label)
        Catch ex As Exception
            _failures += 1
            Console.Error.WriteLine("FAIL " & label & ": " & ex.ToString())
        End Try
    End Sub

    Private Sub Require(condition As Boolean, explanation As String)
        If Not condition Then Throw New InvalidOperationException(explanation)
    End Sub

    Private Sub ExpectFailure(action As Action)
        Dim failed As Boolean = False
        Try
            action()
        Catch ex As Exception When TypeOf ex Is InvalidDataException OrElse
                                   TypeOf ex Is System.Text.Json.JsonException
            failed = True
        End Try
        Require(failed, "Expected a rejected file or unsupported edition")
    End Sub

    Private Sub TestDefaults()
        Dim c As New CharacterSheet()
        Require(c.Edition = 3 AndAlso c.Format = "nsrcgii.vbnet.1", "Incorrect SR3 format")
        Require(c.ValidateBasic().Count = 0, "Default character failed basic validation")
        Require(c.Attributes.Count = 6 AndAlso c.Priorities.Count = 5, "Missing default fields")
    End Sub

    Private Sub TestInvalid()
        Dim c As New CharacterSheet()
        c.Priorities("Skills") = "A"
        c.Attributes("Body") = 0
        c.Skills.Add(New SkillEntry With {.Name = "Bad skill", .Rating = 0})
        Dim warnings As String = String.Join(" | ", c.ValidateBasic())
        Require(warnings.Contains("assigned more than once", StringComparison.Ordinal), "Duplicate priority undetected")
        Require(warnings.Contains("Body base rating", StringComparison.Ordinal), "Attribute limit undetected")
        Require(warnings.Contains("Skill name/rating", StringComparison.Ordinal), "Skill rating undetected")
    End Sub

    Private Sub TestSaveOpen(folder As String)
        Dim path As String = System.IO.Path.Combine(folder, "character.json")
        Dim c As New CharacterSheet With {.Name = "Míra & <Runner>", .Notes = "Line 1" & vbLf & "Line 2", .LegacyOriginal = "[General]" & vbCrLf & "CharName=Demo"}
        c.Attributes("Body") = 5
        c.Skills.Add(New SkillEntry With {.Name = "Pistols", .Rating = 6})
        c.Equipment.Add(New EquipmentEntry With {.Name = "Ares Alpha", .Category = "Gear", .Source = "sr3.278"})
        c.Save(path)
        Dim loaded As CharacterSheet = CharacterSheet.Open(path)
        Require(loaded.Name = c.Name AndAlso loaded.Notes = c.Notes, "Unicode name or notes lost")
        Require(loaded.LegacyOriginal = c.LegacyOriginal, "Original legacy text not preserved")
        Require(loaded.Attributes("Body") = 5 AndAlso loaded.Skills.Count = 1 AndAlso loaded.Equipment.Count = 1, "Fields lost")
        loaded.Name = "Changed"
        loaded.Save(path)
        Require(CharacterSheet.Open(path).Name = "Changed", "Overwriting an existing file failed")
        Require(Not Directory.GetFiles(folder, ".nsrcg-save-*").Any(), "Temporary save file leaked")
    End Sub

    Private Sub TestUnsafeFiles(folder As String)
        Dim path As String = System.IO.Path.Combine(folder, "invalid.json")
        File.WriteAllText(path, "{not-json")
        ExpectFailure(Sub() CharacterSheet.Open(path))
        File.WriteAllText(path, "{""Edition"":2,""Name"":""Unsupported""}")
        ExpectFailure(Sub() CharacterSheet.Open(path))
        Dim unsupported As New CharacterSheet With {.Edition = 2}
        ExpectFailure(Sub() unsupported.Save(path))
    End Sub

    Private Sub TestLegacy(sample As String)
        Require(File.Exists(sample), "The original .sr3 sample was not found")
        Dim c As CharacterSheet = CharacterSheet.ImportLegacy(sample)
        Require(c.Name = "Street Samurai" AndAlso c.Metatype = "Human", "Legacy identity mismatch")
        Require(c.Attributes("Body") = 4 AndAlso c.Attributes("Quickness") = 6, "Legacy attributes mismatch")
        Require(c.Skills.Any(Function(s) s.Name.StartsWith("Pistols", StringComparison.Ordinal) AndAlso s.Rating = 6), "Pistols rating lost")
        Require(c.Equipment.Count >= 10, "Legacy equipment not imported")
        Require(c.LegacyOriginal.Contains("[Cyberware]", StringComparison.Ordinal) AndAlso c.LegacyOriginal.Contains("[Gear]", StringComparison.Ordinal), "Legacy raw source missing")
    End Sub

    Private Sub TestCatalog(path As String)
        Dim catalog As New LegacyCatalog()
        catalog.LoadDirectory(path)
        Require(catalog.Count > 200, "Legacy catalog unexpectedly small")
        Dim matches = catalog.Search("MA:Aikido")
        Require(matches.Count > 0, "Martial Arts skill absent")
        Require(matches(0).Kind = "Skill", "Martial Arts skill misclassified")
        Require(Not matches(0).Category.Contains("skill", StringComparison.OrdinalIgnoreCase), "Regression case does not exercise non-skill heading")
        Require(catalog.Search("Pistols").Any(Function(x) x.Kind = "Skill"), "Pistols skill missing")
        Require(catalog.Search("sr1.").Count = 0, "Excluded source leaked into catalog")
    End Sub

    Private Sub TestHtml(folder As String)
        Dim c As New CharacterSheet With {.Name = "<script>alert(1)</script>", .Notes = "2 < 3 & 4 > 2"}
        Dim path As String = System.IO.Path.Combine(folder, "character.html")
        ExportService.WriteHtml(c, path)
        Dim html As String = File.ReadAllText(path, Encoding.UTF8)
        Require(html.Contains("&lt;script&gt;", StringComparison.Ordinal), "Character name not HTML-escaped")
        Require(Not html.Contains("<script>", StringComparison.Ordinal), "Raw HTML injection included")
        Require(html.Contains("&amp;", StringComparison.Ordinal), "HTML ampersand not escaped")
        Require(html.Contains("Attributes", StringComparison.Ordinal) AndAlso html.Contains("Priorities", StringComparison.Ordinal), "Required sections missing")
    End Sub

    Private Sub TestPdf(folder As String)
        Dim c As New CharacterSheet With {.Name = "PDF (test) \\ character", .Notes = String.Join(vbLf, Enumerable.Repeat("Long report note () \\ content", 95))}
        For i As Integer = 0 To 90
            c.Skills.Add(New SkillEntry With {.Name = "Skill " & i.ToString(), .Rating = 3})
        Next
        Dim path As String = System.IO.Path.Combine(folder, "character.pdf")
        ExportService.WritePdf(c, path)
        Dim pdf As String = Encoding.Latin1.GetString(File.ReadAllBytes(path))
        Require(pdf.StartsWith("%PDF-1.4", StringComparison.Ordinal) AndAlso pdf.Contains("%%EOF", StringComparison.Ordinal), "Malformed PDF wrapper")
        Dim start As Match = Regex.Match(pdf, "startxref\s+(\d+)")
        Require(start.Success, "Missing PDF startxref")
        Dim at As Integer = Integer.Parse(start.Groups(1).Value)
        Require(at >= 0 AndAlso at + 4 <= pdf.Length AndAlso pdf.Substring(at, 4) = "xref", "PDF cross-reference starts at incorrect position")
        Dim objects As MatchCollection = Regex.Matches(pdf, "(?m)^(\d+) 0 obj$")
        Dim refs As MatchCollection = Regex.Matches(pdf, "(?m)^(\d{10}) 00000 n $")
        Require(objects.Count = refs.Count AndAlso objects.Count > 5, "PDF object table missing items")
        For i As Integer = 0 To refs.Count - 1
            Dim offset As Integer = Integer.Parse(refs(i).Groups(1).Value)
            Require(offset >= 0 AndAlso offset + 5 < pdf.Length, "PDF xref entry out of bounds")
            Require(pdf.Substring(offset).StartsWith((i + 1).ToString() & " 0 obj", StringComparison.Ordinal), "PDF xref points to wrong object")
        Next
        Dim count As Match = Regex.Match(pdf, "/Type /Pages /Count (\d+)")
        Require(count.Success AndAlso Integer.Parse(count.Groups(1).Value) >= 3, "Expected multi-page export")
    End Sub

    Private Function PrivateField(Of T As Class)(instance As Object, name As String) As T
        Dim field As FieldInfo = instance.GetType().GetField(name, BindingFlags.Instance Or BindingFlags.NonPublic)
        Require(field IsNot Nothing, "Missing private control: " & name)
        Return DirectCast(field.GetValue(instance), T)
    End Function

    Private Sub InvokePrivate(instance As Object, name As String)
        Dim method As MethodInfo = instance.GetType().GetMethod(name, BindingFlags.Instance Or BindingFlags.NonPublic)
        Require(method IsNot Nothing, "Missing UI method: " & name)
        method.Invoke(instance, Nothing)
    End Sub

    Private Sub TestGui()
        Using ui As New MainForm()
            ui.WindowState = FormWindowState.Normal
            ui.Size = New Drawing.Size(960, 680)
            ui.Show()
            Application.DoEvents()
            Require(ui.IsHandleCreated AndAlso ui.Visible, "WinForms did not create a visible main window")
            Require(ui.MainMenuStrip IsNot Nothing AndAlso ui.MainMenuStrip.Items.Count > 0, "Application menu missing")
            Dim tabs As TabControl = PrivateField(Of TabControl)(ui, "tabs")
            Require(tabs.TabPages.Count = 3, "Unexpected missing/unfinished tabs")
            Dim name As TextBox = PrivateField(Of TextBox)(ui, "nameBox")
            Dim skill As TextBox = PrivateField(Of TextBox)(ui, "skillName")
            Dim rating As NumericUpDown = PrivateField(Of NumericUpDown)(ui, "skillRating")
            Dim skills As ListBox = PrivateField(Of ListBox)(ui, "skillsList")
            Dim equipment As ListBox = PrivateField(Of ListBox)(ui, "equipmentList")
            Dim catalogList As ListBox = PrivateField(Of ListBox)(ui, "catalogList")
            Dim search As TextBox = PrivateField(Of TextBox)(ui, "searchBox")
            name.Text = "Tester"
            skill.Text = "Pistols"
            rating.Value = 4
            InvokePrivate(ui, "AddSkill")
            Require(skills.Items.Count = 1, "AddSkill failed")
            name.Text = "Unsaved edit retained"
            skills.SelectedIndex = 0
            InvokePrivate(ui, "RemoveSkill")
            Require(name.Text = "Unsaved edit retained" AndAlso skills.Items.Count = 0, "Removing a skill lost an unsaved name edit")
            search.Text = "MA:Aikido"
            InvokePrivate(ui, "RefreshSearch")
            Require(catalogList.Items.Count > 0, "Bundled catalog did not load into GUI")
            catalogList.SelectedIndex = 0
            InvokePrivate(ui, "AddCatalogItem")
            Require(skills.Items.Count = 1 AndAlso equipment.Items.Count = 0, "Martial Arts skill added as equipment")
            ui.Dispose() ' Do not trigger a confirmation dialog in CI.
        End Using
    End Sub
End Module
