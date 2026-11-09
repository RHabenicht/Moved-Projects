Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private _character As New CharacterSheet()
    Private ReadOnly _catalog As New LegacyCatalog()
    Private _currentPath As String = ""
    Private _dirty As Boolean
    Private _updating As Boolean

    Private ReadOnly nameBox As New TextBox()
    Private ReadOnly realNameBox As New TextBox()
    Private ReadOnly raceBox As New ComboBox()
    Private ReadOnly notesBox As New TextBox()
    Private ReadOnly attributes As New Dictionary(Of String, NumericUpDown)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly priorities As New Dictionary(Of String, ComboBox)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly skillName As New TextBox()
    Private ReadOnly skillRating As New NumericUpDown()
    Private ReadOnly skillsList As New ListBox()
    Private ReadOnly equipmentList As New ListBox()
    Private ReadOnly searchBox As New TextBox()
    Private ReadOnly catalogList As New ListBox()
    Private ReadOnly summaryBox As New TextBox()
    Private ReadOnly statusLabel As New ToolStripStatusLabel("Ready")
    Private ReadOnly tabs As New TabControl()

    Public Sub New()
        Text = "NSRCG II — Visual Basic Edition (early build)"
        MinimumSize = New Size(800, 560)
        Size = New Size(1100, 720)
        StartPosition = FormStartPosition.CenterScreen
        WindowState = FormWindowState.Maximized
        AutoScaleMode = AutoScaleMode.Dpi
        BuildInterface()
        RefreshCharacter()
        Dim dataFolder As String = Path.Combine(AppContext.BaseDirectory, "LegacyData")
        If Directory.Exists(dataFolder) Then
            Try
                _catalog.LoadDirectory(dataFolder)
                RefreshSearch()
                SetStatus("Loaded " & _catalog.Count.ToString() & " legacy catalog entries")
            Catch ex As Exception
                SetStatus("Catalog not loaded: " & ex.Message)
            End Try
        End If
    End Sub

    Private Shared Function ButtonOf(label As String, handler As EventHandler) As Button
        Dim b As New Button With {.Text = label, .AutoSize = True, .Margin = New Padding(4), .MinimumSize = New Size(86, 28)}
        AddHandler b.Click, handler
        Return b
    End Function

    Private Shared Function RowPanel() As FlowLayoutPanel
        Return New FlowLayoutPanel With {.Dock = DockStyle.Fill, .WrapContents = True, .AutoScroll = True, .Padding = New Padding(8)}
    End Function

    Private Sub BuildInterface()
        Dim menu As New MenuStrip()
        Dim fileMenu As New ToolStripMenuItem("File")
        AddMenu(fileMenu, "New", Sub() NewCharacter())
        AddMenu(fileMenu, "Open JSON…", Sub() OpenCharacter())
        AddMenu(fileMenu, "Save", Sub() SaveCharacter(False))
        AddMenu(fileMenu, "Save As…", Sub() SaveCharacter(True))
        fileMenu.DropDownItems.Add(New ToolStripSeparator())
        AddMenu(fileMenu, "Import legacy .sr3…", Sub() ImportCharacter())
        AddMenu(fileMenu, "Load legacy DAT folder…", Sub() LoadCatalogFolder())
        fileMenu.DropDownItems.Add(New ToolStripSeparator())
        AddMenu(fileMenu, "Export HTML…", Sub() ExportHtml())
        AddMenu(fileMenu, "Export PDF…", Sub() ExportPdf())
        fileMenu.DropDownItems.Add(New ToolStripSeparator())
        AddMenu(fileMenu, "Exit", Sub() Close())
        menu.Items.Add(fileMenu)
        MainMenuStrip = menu
        Controls.Add(menu)

        Dim status As New StatusStrip()
        status.Items.Add(statusLabel)
        Controls.Add(status)

        Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .AutoScroll = True, .Padding = New Padding(10, 8, 10, 8)}
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 84))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 182))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 130))
        Controls.Add(root)
        root.BringToFront()
        menu.BringToFront()
        status.BringToFront()

        Dim identity As New GroupBox With {.Text = "Character", .Dock = DockStyle.Fill}
        Dim idRow As FlowLayoutPanel = RowPanel()
        idRow.Controls.Add(New Label With {.Text = "Character name", .AutoSize = True, .Padding = New Padding(0, 6, 0, 0)})
        nameBox.Width = 245
        idRow.Controls.Add(nameBox)
        idRow.Controls.Add(New Label With {.Text = "Real name", .AutoSize = True, .Padding = New Padding(0, 6, 0, 0)})
        realNameBox.Width = 225
        idRow.Controls.Add(realNameBox)
        idRow.Controls.Add(New Label With {.Text = "Metatype", .AutoSize = True, .Padding = New Padding(0, 6, 0, 0)})
        raceBox.DropDownStyle = ComboBoxStyle.DropDownList
        raceBox.Items.AddRange(New Object() {"Human", "Dwarf", "Elf", "Ork", "Troll"})
        raceBox.Width = 135
        idRow.Controls.Add(raceBox)
        identity.Controls.Add(idRow)
        root.Controls.Add(identity, 0, 0)
        AddHandler nameBox.TextChanged, AddressOf MarkDirty
        AddHandler realNameBox.TextChanged, AddressOf MarkDirty
        AddHandler raceBox.SelectedIndexChanged, AddressOf MarkDirty

        Dim setup As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
        setup.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 59))
        setup.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 41))
        root.Controls.Add(setup, 0, 1)
        Dim attrPanel As New GroupBox With {.Text = "Base attributes (before metatype adjustments)", .Dock = DockStyle.Fill}
        Dim attrFlow As FlowLayoutPanel = RowPanel()
        For Each key As String In CharacterSheet.AttributeNames
            Dim cell As New FlowLayoutPanel With {.FlowDirection = FlowDirection.TopDown, .WrapContents = False, .Width = 98, .Height = 84, .Margin = New Padding(2)}
            cell.Controls.Add(New Label With {.Text = key, .AutoSize = True})
            Dim counter As New NumericUpDown With {.Minimum = 1, .Maximum = 6, .Width = 85, .Value = 1}
            attributes.Add(key, counter)
            cell.Controls.Add(counter)
            AddHandler counter.ValueChanged, AddressOf MarkDirty
            attrFlow.Controls.Add(cell)
        Next
        attrPanel.Controls.Add(attrFlow)
        setup.Controls.Add(attrPanel, 0, 0)
        Dim priorityPanel As New GroupBox With {.Text = "Priority assignment (A–E)", .Dock = DockStyle.Fill}
        Dim priorityFlow As FlowLayoutPanel = RowPanel()
        For Each key As String In CharacterSheet.PriorityNames
            Dim cell As New FlowLayoutPanel With {.FlowDirection = FlowDirection.TopDown, .WrapContents = False, .Width = 108, .Height = 74, .Margin = New Padding(2)}
            cell.Controls.Add(New Label With {.Text = key, .AutoSize = True})
            Dim choice As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 94}
            choice.Items.AddRange(New Object() {"A", "B", "C", "D", "E"})
            priorities.Add(key, choice)
            cell.Controls.Add(choice)
            AddHandler choice.SelectedIndexChanged, AddressOf MarkDirty
            priorityFlow.Controls.Add(cell)
        Next
        priorityPanel.Controls.Add(priorityFlow)
        setup.Controls.Add(priorityPanel, 1, 0)

        tabs.Dock = DockStyle.Fill
        tabs.TabPages.Add(BuildSkillsTab())
        tabs.TabPages.Add(BuildEquipmentTab())
        tabs.TabPages.Add(BuildCatalogTab())
        root.Controls.Add(tabs, 0, 2)

        Dim bottom As New GroupBox With {.Text = "Notes and validation", .Dock = DockStyle.Fill}
        Dim bottomSplit As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2}
        bottomSplit.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60))
        bottomSplit.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
        notesBox.Multiline = True
        notesBox.ScrollBars = ScrollBars.Vertical
        notesBox.Dock = DockStyle.Fill
        summaryBox.Multiline = True
        summaryBox.ScrollBars = ScrollBars.Vertical
        summaryBox.ReadOnly = True
        summaryBox.Dock = DockStyle.Fill
        bottomSplit.Controls.Add(notesBox, 0, 0)
        bottomSplit.Controls.Add(summaryBox, 1, 0)
        bottom.Controls.Add(bottomSplit)
        root.Controls.Add(bottom, 0, 3)
        AddHandler notesBox.TextChanged, AddressOf MarkDirty
    End Sub

    Private Sub AddMenu(parent As ToolStripMenuItem, name As String, action As Action)
        Dim item As New ToolStripMenuItem(name)
        AddHandler item.Click, Sub(sender As Object, args As EventArgs) action()
        parent.DropDownItems.Add(item)
    End Sub

    Private Function BuildSkillsTab() As TabPage
        Dim page As New TabPage("Skills")
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 2}
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 48))
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        Dim actions As FlowLayoutPanel = RowPanel()
        actions.Controls.Add(New Label With {.Text = "Skill", .AutoSize = True, .Padding = New Padding(0, 5, 0, 0)})
        skillName.Width = 300
        actions.Controls.Add(skillName)
        actions.Controls.Add(New Label With {.Text = "Rating", .AutoSize = True, .Padding = New Padding(0, 5, 0, 0)})
        skillRating.Minimum = 1
        skillRating.Maximum = 6
        skillRating.Width = 65
        actions.Controls.Add(skillRating)
        actions.Controls.Add(ButtonOf("Add skill", Sub(s, e) AddSkill()))
        actions.Controls.Add(ButtonOf("Remove selected", Sub(s, e) RemoveSkill()))
        skillsList.Dock = DockStyle.Fill
        layout.Controls.Add(actions, 0, 0)
        layout.Controls.Add(skillsList, 0, 1)
        page.Controls.Add(layout)
        Return page
    End Function

    Private Function BuildEquipmentTab() As TabPage
        Dim page As New TabPage("Equipment / Powers")
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 2}
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 45))
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        Dim actions As FlowLayoutPanel = RowPanel()
        actions.Controls.Add(ButtonOf("Remove selected", Sub(s, e) RemoveEquipment()))
        actions.Controls.Add(New Label With {.Text = "Add entries through the Catalog tab.", .AutoSize = True, .Padding = New Padding(8, 6, 0, 0)})
        equipmentList.Dock = DockStyle.Fill
        layout.Controls.Add(actions, 0, 0)
        layout.Controls.Add(equipmentList, 0, 1)
        page.Controls.Add(layout)
        Return page
    End Function

    Private Function BuildCatalogTab() As TabPage
        Dim page As New TabPage("Legacy Catalog")
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 2}
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 48))
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        Dim actions As FlowLayoutPanel = RowPanel()
        searchBox.Width = 310
        actions.Controls.Add(searchBox)
        actions.Controls.Add(ButtonOf("Search", Sub(s, e) RefreshSearch()))
        actions.Controls.Add(ButtonOf("Add selected to character", Sub(s, e) AddCatalogItem()))
        actions.Controls.Add(ButtonOf("Load DAT folder", Sub(s, e) LoadCatalogFolder()))
        catalogList.Dock = DockStyle.Fill
        layout.Controls.Add(actions, 0, 0)
        layout.Controls.Add(catalogList, 0, 1)
        page.Controls.Add(layout)
        AddHandler searchBox.KeyDown, Sub(s, e)
                                          If e.KeyCode = Keys.Enter Then
                                              RefreshSearch()
                                              e.SuppressKeyPress = True
                                          End If
                                      End Sub
        Return page
    End Function

    Private Sub MarkDirty(sender As Object, e As EventArgs)
        If _updating Then Return
        _dirty = True
        Text = "NSRCG II — Visual Basic Edition *"
        ValidatePreview()
    End Sub

    Private Sub SyncCharacter()
        _character.Name = nameBox.Text
        _character.RealName = realNameBox.Text
        _character.Metatype = Convert.ToString(raceBox.SelectedItem)
        _character.Notes = notesBox.Text
        For Each key As String In CharacterSheet.AttributeNames
            _character.Attributes(key) = Decimal.ToInt32(attributes(key).Value)
        Next
        For Each key As String In CharacterSheet.PriorityNames
            _character.Priorities(key) = Convert.ToString(priorities(key).SelectedItem)
        Next
    End Sub

    Private Sub RefreshCharacter()
        _character.Normalize()
        _updating = True
        Try
            nameBox.Text = _character.Name
            realNameBox.Text = _character.RealName
            raceBox.SelectedItem = _character.Metatype
            If raceBox.SelectedIndex < 0 Then raceBox.SelectedIndex = 0
            notesBox.Text = _character.Notes
            For Each key As String In CharacterSheet.AttributeNames
                attributes(key).Value = Math.Max(1, Math.Min(6, _character.Attributes(key)))
            Next
            For Each key As String In CharacterSheet.PriorityNames
                priorities(key).SelectedItem = _character.Priorities(key)
            Next
            skillsList.Items.Clear()
            For Each item As SkillEntry In _character.Skills
                skillsList.Items.Add(item.Name & " — " & item.Rating.ToString() & " (" & item.Category & ")")
            Next
            equipmentList.Items.Clear()
            For Each item As EquipmentEntry In _character.Equipment
                equipmentList.Items.Add(item.Name & "  [" & item.Category & "]  " & item.Source)
            Next
            _dirty = False
            Text = "NSRCG II — Visual Basic Edition (early build)"
        Finally
            _updating = False
        End Try
        ValidatePreview()
    End Sub

    Private Sub ValidatePreview()
        If _updating Then Return
        SyncCharacter()
        Dim warnings As List(Of String) = _character.ValidateBasic()
        summaryBox.Text = If(warnings.Count = 0, "Basic value checks passed. Full SR3 rules are not implemented.", String.Join(Environment.NewLine, warnings)) & Environment.NewLine &
                          "Skills: " & _character.Skills.Count.ToString() & " | Equipment: " & _character.Equipment.Count.ToString()
    End Sub

    Private Function ConfirmAbandon() As Boolean
        If Not _dirty Then Return True
        Dim choice As DialogResult = MessageBox.Show(Me, "Discard unsaved changes?", "NSRCG II", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        Return choice = DialogResult.Yes
    End Function

    Private Sub NewCharacter()
        If Not ConfirmAbandon() Then Return
        _character = New CharacterSheet()
        _currentPath = ""
        RefreshCharacter()
        SetStatus("New character")
    End Sub

    Private Sub OpenCharacter()
        If Not ConfirmAbandon() Then Return
        Using dlg As New OpenFileDialog With {.Filter = "NSRCG II character (*.json)|*.json|All files (*.*)|*.*"}
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                Dim loaded As CharacterSheet = CharacterSheet.Open(dlg.FileName)
                _character = loaded
                _currentPath = dlg.FileName
                RefreshCharacter()
                SetStatus("Opened " & Path.GetFileName(_currentPath))
            Catch ex As Exception
                ShowError("Open", ex)
            End Try
        End Using
    End Sub

    Private Sub SaveCharacter(saveAs As Boolean)
        SyncCharacter()
        Dim destination As String = _currentPath
        If saveAs OrElse destination = "" Then
            Using dlg As New SaveFileDialog With {.Filter = "NSRCG II character (*.json)|*.json", .DefaultExt = "json", .FileName = If(_character.Name, "Character") & ".json"}
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                destination = dlg.FileName
            End Using
        End If
        Try
            _character.Save(destination)
            _currentPath = destination
            _dirty = False
            Text = "NSRCG II — Visual Basic Edition (early build)"
            SetStatus("Saved " & Path.GetFileName(destination))
        Catch ex As Exception
            ShowError("Save", ex)
        End Try
    End Sub

    Private Sub ImportCharacter()
        If Not ConfirmAbandon() Then Return
        Using dlg As New OpenFileDialog With {.Filter = "Original NSRCG character (*.sr3)|*.sr3|All files (*.*)|*.*"}
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                _character = CharacterSheet.ImportLegacy(dlg.FileName)
                _currentPath = ""
                RefreshCharacter()
                _dirty = True
                Text = "NSRCG II — Visual Basic Edition *"
                SetStatus("Imported " & Path.GetFileName(dlg.FileName) & "; original text retained")
            Catch ex As Exception
                ShowError("Legacy import", ex)
            End Try
        End Using
    End Sub

    Private Sub LoadCatalogFolder()
        Using dlg As New FolderBrowserDialog With {.Description = "Choose the folder containing the original NSRCG .DAT files"}
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                _catalog.LoadDirectory(dlg.SelectedPath)
                RefreshSearch()
                SetStatus("Catalog loaded: " & _catalog.Count.ToString() & " entries (not fully rules-verified)")
            Catch ex As Exception
                ShowError("Catalog", ex)
            End Try
        End Using
    End Sub

    Private Sub RefreshSearch()
        catalogList.Items.Clear()
        For Each entry As CatalogEntry In _catalog.Search(searchBox.Text)
            catalogList.Items.Add(entry)
        Next
    End Sub

    Private Sub AddSkill()
        Dim name As String = skillName.Text.Trim()
        If name = "" Then
            MessageBox.Show(Me, "Enter a skill name.", "NSRCG II")
            Return
        End If
        SyncCharacter()
        _character.Skills.Add(New SkillEntry With {.Name = name, .Rating = Decimal.ToInt32(skillRating.Value)})
        skillName.Clear()
        RefreshCharacter()
        SetDirty()
    End Sub

    Private Sub RemoveSkill()
        Dim n As Integer = skillsList.SelectedIndex
        If n < 0 Then Return
        SyncCharacter()
        _character.Skills.RemoveAt(n)
        RefreshCharacter()
        SetDirty()
    End Sub

    Private Sub AddCatalogItem()
        Dim entry As CatalogEntry = TryCast(catalogList.SelectedItem, CatalogEntry)
        If entry Is Nothing Then Return
        SyncCharacter()
        If entry.Kind.Equals("Skill", StringComparison.OrdinalIgnoreCase) Then
            _character.Skills.Add(New SkillEntry With {.Name = entry.Name, .Rating = 1})
        Else
            _character.Equipment.Add(New EquipmentEntry With {.Name = entry.Name, .Category = entry.Category, .Source = entry.Source})
        End If
        RefreshCharacter()
        SetDirty()
        SetStatus("Added " & entry.Name)
    End Sub

    Private Sub RemoveEquipment()
        Dim n As Integer = equipmentList.SelectedIndex
        If n < 0 Then Return
        SyncCharacter()
        _character.Equipment.RemoveAt(n)
        RefreshCharacter()
        SetDirty()
    End Sub

    Private Sub SetDirty()
        _dirty = True
        Text = "NSRCG II — Visual Basic Edition *"
        ValidatePreview()
    End Sub

    Private Sub ExportHtml()
        SyncCharacter()
        Using dlg As New SaveFileDialog With {.Filter = "HTML character sheet (*.html)|*.html", .DefaultExt = "html"}
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                ExportService.WriteHtml(_character, dlg.FileName)
                SetStatus("HTML exported")
            Catch ex As Exception
                ShowError("HTML export", ex)
            End Try
        End Using
    End Sub

    Private Sub ExportPdf()
        SyncCharacter()
        Using dlg As New SaveFileDialog With {.Filter = "PDF character sheet (*.pdf)|*.pdf", .DefaultExt = "pdf"}
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                ExportService.WritePdf(_character, dlg.FileName)
                SetStatus("PDF exported")
            Catch ex As Exception
                ShowError("PDF export", ex)
            End Try
        End Using
    End Sub

    Private Sub SetStatus(message As String)
        statusLabel.Text = message
    End Sub

    Private Sub ShowError(action As String, ex As Exception)
        MessageBox.Show(Me, action & " failed:" & Environment.NewLine & ex.Message, "NSRCG II", MessageBoxButtons.OK, MessageBoxIcon.Error)
        SetStatus(action & " failed")
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not ConfirmAbandon() Then e.Cancel = True
        MyBase.OnFormClosing(e)
    End Sub
End Class
