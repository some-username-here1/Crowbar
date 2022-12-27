Imports System.IO
Imports System.Web.Script.Serialization

Public Class PackUserControl

#Region "Creation and Destruction"

	Public Sub New()
		MyBase.New()
		' This call is required by the Windows Form Designer.
		InitializeComponent()

		Me.theGmaGarrysModTagsUserControlIsBeingChangedByMe = False
	End Sub

#End Region

#Region "Init and Free"

	Protected Overrides Sub Init()
		Me.InputComboBox.DisplayMember = "Value"
		Me.InputComboBox.ValueMember = "Key"
		Me.InputComboBox.DataSource = EnumHelper.ToList(GetType(PackInputOptions))
		Me.InputComboBox.DataBindings.Add("SelectedValue", TheApp.Settings, "PackMode", False, DataSourceUpdateMode.OnPropertyChanged)

		Me.InputPathFileNameTextBox.DataBindings.Add("Text", TheApp.Settings, "PackInputPath", False, DataSourceUpdateMode.OnValidation)

		Me.OutputPathTextBox.DataBindings.Add("Text", TheApp.Settings, "PackOutputPath", False, DataSourceUpdateMode.OnValidation)
		Me.OutputParentPathTextBox.DataBindings.Add("Text", TheApp.Settings, "PackOutputParentPath", False, DataSourceUpdateMode.OnValidation)
		Me.InitOutputPathComboBox()
		Me.UpdateOutputPathWidgets()

		'NOTE: Prevent changing this combobox's SelectedIndex when another combobox's (which also accesses "SelectedIndex" and TheApp.Settings) SelectedIndex changes.
		Me.GameSetupComboBox.BindingContext = New BindingContext()
		'NOTE: The DataSource, DisplayMember, and ValueMember need to be set before DataBindings, or else an exception is raised.
		Me.GameSetupComboBox.DisplayMember = "GameName"
		Me.GameSetupComboBox.ValueMember = "GameName"
		Me.GameSetupComboBox.DataSource = TheApp.Settings.GameSetups
		Me.GameSetupComboBox.DataBindings.Add("SelectedIndex", TheApp.Settings, "PackGameSetupSelectedIndex", False, DataSourceUpdateMode.OnPropertyChanged)

		Me.InitCrowbarOptions()
		Me.InitPackerOptions()

		Me.thePackedRelativePathFileNames = New BindingListEx(Of String)
		Me.PackedFilesComboBox.DataSource = Me.thePackedRelativePathFileNames

		Me.UpdateWidgets(False)
		Me.UpdatePackerOptions()

		AddHandler TheApp.Settings.PropertyChanged, AddressOf AppSettings_PropertyChanged
		AddHandler TheApp.Packer.ProgressChanged, AddressOf Me.PackerBackgroundWorker_ProgressChanged
		AddHandler TheApp.Packer.RunWorkerCompleted, AddressOf Me.PackerBackgroundWorker_RunWorkerCompleted

		AddHandler Me.InputPathFileNameTextBox.DataBindings("Text").Parse, AddressOf FileManager.ParsePath
		AddHandler Me.OutputPathTextBox.DataBindings("Text").Parse, AddressOf FileManager.ParsePathFileName
	End Sub

	Private Sub InitOutputPathComboBox()
		Dim anEnumList As IList

		anEnumList = EnumHelper.ToList(GetType(PackOutputPathOptions))
		Me.OutputPathComboBox.DataBindings.Clear()
		Try
			Me.OutputPathComboBox.DisplayMember = "Value"
			Me.OutputPathComboBox.ValueMember = "Key"
			Me.OutputPathComboBox.DataSource = anEnumList
			Me.OutputPathComboBox.DataBindings.Add("SelectedValue", TheApp.Settings, "PackOutputFolderOption", False, DataSourceUpdateMode.OnPropertyChanged)

			' Do not use this line because it will override the value automatically assigned by the data bindings above.
			'Me.OutputPathComboBox.SelectedIndex = 0
		Catch ex As Exception
			Dim debug As Integer = 4242
		End Try
	End Sub

	Private Sub InitCrowbarOptions()
		Me.LogFileCheckBox.DataBindings.Add("Checked", TheApp.Settings, "PackLogFileIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)
	End Sub

	Private Sub InitPackerOptions()
		Me.theSelectedPackerOptions = New List(Of String)()
		Me.MultiFileVpkCheckBox.DataBindings.Add("Checked", TheApp.Settings, "PackOptionMultiFileVpkIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)
		Me.IgnoreWhitelistWarningsCheckBox.DataBindings.Add("Checked", TheApp.Settings, "PackOptionIgnoreWhitelistWarningsIsChecked", False, DataSourceUpdateMode.OnPropertyChanged)

		Me.GmaTitleTextBox.DataBindings.Add("Text", TheApp.Settings, "PackGmaTitle", False, DataSourceUpdateMode.OnValidation)
		'NOTE: There is no automatic data-binding with TagsWidget, so manually bind from object to widget here.
		Me.theGmaGarrysModTagsUserControlIsBeingChangedByMe = True
		Me.GmaGarrysModTagsUserControl.ItemTags = TheApp.Settings.PackGmaItemTags
		Me.theGmaGarrysModTagsUserControlIsBeingChangedByMe = False
		AddHandler Me.GmaGarrysModTagsUserControl.TagsPropertyChanged, AddressOf Me.GmaGarrysModTagsUserControl_TagsPropertyChanged
	End Sub

	' Do not need Free() because this widget is destroyed only on program exit.
	'Protected Overrides Sub Free()
	'	RemoveHandler Me.InputPathFileNameTextBox.DataBindings("Text").Parse, AddressOf FileManager.ParsePath
	'	RemoveHandler Me.OutputPathTextBox.DataBindings("Text").Parse, AddressOf FileManager.ParsePathFileName
	'	RemoveHandler TheApp.Settings.PropertyChanged, AddressOf AppSettings_PropertyChanged
	'	RemoveHandler TheApp.Packer.ProgressChanged, AddressOf Me.PackerBackgroundWorker_ProgressChanged
	'	RemoveHandler TheApp.Packer.RunWorkerCompleted, AddressOf Me.PackerBackgroundWorker_RunWorkerCompleted

	'	Me.InputPathFileNameTextBox.DataBindings.Clear()

	'	Me.OutputPathTextBox.DataBindings.Clear()
	'	Me.OutputParentPathTextBox.DataBindings.Clear()

	'	Me.GameSetupComboBox.DataSource = Nothing
	'	Me.GameSetupComboBox.DataBindings.Clear()

	'	Me.FreeCrowbarOptions()
	'	Me.FreePackerOptions()

	'	Me.InputComboBox.DataBindings.Clear()

	'	Me.PackedFilesComboBox.DataBindings.Clear()
	'End Sub

	'Private Sub FreeCrowbarOptions()
	'	Me.LogFileCheckBox.DataBindings.Clear()
	'End Sub

	'Private Sub FreePackerOptions()
	'	Me.MultiFileVpkCheckBox.DataBindings.Clear()

	'	Me.GmaTitleTextBox.DataBindings.Clear()
	'	If Me.GmaGarrysModTagsUserControl IsNot Nothing Then
	'		RemoveHandler Me.GmaGarrysModTagsUserControl.TagsPropertyChanged, AddressOf Me.GmaGarrysModTagsUserControl_TagsPropertyChanged
	'	End If
	'End Sub

#End Region

#Region "Properties"

#End Region

#Region "Widget Event Handlers"

	Private Sub PackUserControl_Resize(sender As Object, e As EventArgs) Handles Me.Resize
		'NOTE: This code prevents Visual Studio or Windows often inexplicably extending the right side of these widgets.
		Workarounds.WorkaroundForFrameworkAnchorRightSizingBug(Me.InputPathFileNameTextBox, Me.BrowseForInputFolderOrFileNameButton)
		Workarounds.WorkaroundForFrameworkAnchorRightSizingBug(Me.OutputPathTextBox, Me.BrowseForOutputPathButton)
		Workarounds.WorkaroundForFrameworkAnchorRightSizingBug(Me.OutputParentPathTextBox, Me.BrowseForOutputPathButton)
	End Sub

#End Region

#Region "Child Widget Event Handlers"

	Private Sub BrowseForInputFolderOrFileNameButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BrowseForInputFolderOrFileNameButton.Click
		Dim openFileWdw As New OpenFileDialog()

		openFileWdw.Title = "Open the folder you want to pack"
		If Directory.Exists(TheApp.Settings.PackInputPath) Then
			openFileWdw.InitialDirectory = TheApp.Settings.PackInputPath
		Else
			openFileWdw.InitialDirectory = FileManager.GetLongestExtantPath(TheApp.Settings.PackInputPath)
			If openFileWdw.InitialDirectory = "" Then
				openFileWdw.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
			End If
		End If
		openFileWdw.FileName = "[Folder Selection]"
		openFileWdw.AddExtension = False
		openFileWdw.CheckFileExists = False
		openFileWdw.Multiselect = False
		openFileWdw.ValidateNames = False

		If openFileWdw.ShowDialog() = Windows.Forms.DialogResult.OK Then
			' Allow dialog window to completely disappear.
			Application.DoEvents()

			TheApp.Settings.PackInputPath = FileManager.GetPath(openFileWdw.FileName)
		End If
	End Sub

	Private Sub GotoInputPathButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GotoInputPathButton.Click
		FileManager.OpenWindowsExplorer(TheApp.Settings.PackInputPath)
	End Sub

	Private Sub OutputPathTextBox_DragDrop(sender As Object, e As DragEventArgs) Handles OutputPathTextBox.DragDrop
		Dim pathFileNames() As String = CType(e.Data.GetData(DataFormats.FileDrop), String())
		Dim pathFileName As String = pathFileNames(0)
		If Directory.Exists(pathFileName) Then
			TheApp.Settings.PackOutputPath = pathFileName
		End If
	End Sub

	Private Sub OutputPathTextBox_DragEnter(sender As Object, e As DragEventArgs) Handles OutputPathTextBox.DragEnter
		If e.Data.GetDataPresent(DataFormats.FileDrop) Then
			e.Effect = DragDropEffects.Copy
		End If
	End Sub

	Private Sub OutputPathTextBox_Validated(sender As Object, e As EventArgs) Handles OutputPathTextBox.Validated
		Me.UpdateOutputPathTextBox()
	End Sub

	Private Sub BrowseForOutputPathButton_Click(sender As Object, e As EventArgs) Handles BrowseForOutputPathButton.Click
		Me.BrowseForOutputPath()
	End Sub

	Private Sub GotoOutputPathButton_Click(sender As Object, e As EventArgs) Handles GotoOutputPathButton.Click
		Me.GotoFolder()
	End Sub

	'NOTE: There is no automatic data-binding with TagsWidget, so manually bind from widget to object here.
	Private Sub GmaGarrysModTagsUserControl_TagsPropertyChanged(sender As Object, e As EventArgs)
		If Not Me.theGmaGarrysModTagsUserControlIsBeingChangedByMe Then
			TheApp.Settings.PackGmaItemTags = Me.GmaGarrysModTagsUserControl.ItemTags
		End If
	End Sub

	Private Sub DirectPackerOptionsTextBox_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DirectPackerOptionsTextBox.TextChanged
		Me.SetPackerOptionsText()
	End Sub

	Private Sub PackButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PackButton.Click
		Me.RunPacker()
	End Sub

	Private Sub SkipCurrentFolderButtonButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SkipCurrentFolderButton.Click
		TheApp.Packer.SkipCurrentFolder()
	End Sub

	Private Sub CancelPackButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CancelPackButton.Click
		TheApp.Packer.CancelAsync()
	End Sub

	Private Sub UseAllInPublishButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UseAllInPublishButton.Click
		'NOTE: It might not be good idea to try to auto-publish more than one workshop item at a time.
	End Sub

	Private Sub UseInPublishButton_Click(sender As Object, e As EventArgs) Handles UseInPublishButton.Click
		'TODO: Use the output folder (including file name when needed) as the Publish tab's input file or folder.
		'Dim pathFileName As String
		'pathFileName = TheApp.Packer.GetOutputPathFileName(Me.thePackedRelativePathFileNames(Me.PackedFilesComboBox.SelectedIndex))
		'TheApp.Settings.Publish = pathFileName
	End Sub

	Private Sub GotoPackedFileButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GotoPackedFileButton.Click
		Dim pathFileName As String
		pathFileName = TheApp.Packer.GetOutputPathFileName(Me.thePackedRelativePathFileNames(Me.PackedFilesComboBox.SelectedIndex))
		FileManager.OpenWindowsExplorer(pathFileName)
	End Sub

#End Region

#Region "Core Event Handlers"

	Private Sub AppSettings_PropertyChanged(ByVal sender As System.Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs)
		If e.PropertyName = "PackInputPath" Then
			Me.DeleteWarningMessage()
			Me.SetPackerOptionsText()
			Me.UpdateOutputPathWidgets()
		ElseIf e.PropertyName = "PackMode" Then
			Me.DeleteWarningMessage()
			Me.SetPackerOptionsText()
			Me.UpdateOutputPathWidgets()
		ElseIf e.PropertyName = "PackOutputFolderOption" Then
			Me.UpdateOutputPathWidgets()
		ElseIf e.PropertyName = "PackGameSetupSelectedIndex" Then
			Me.DeleteWarningMessage()
			Me.UpdatePackerOptions()
		ElseIf e.PropertyName = "PackOptionMultiFileVpkIsChecked" Then
			'Me.EditPackerOptionsText(Me.GetMultiFileArgumentText(), TheApp.Settings.PackOptionMultiFileVpkIsChecked)
			Me.SetPackerOptionsText()
		ElseIf e.PropertyName = "PackOptionIgnoreWhitelistWarningsIsChecked" Then
			'Me.EditPackerOptionsText("warninvalid", TheApp.Settings.PackOptionIgnoreWhitelistWarningsIsChecked)
			Me.SetPackerOptionsText()
		ElseIf e.PropertyName.StartsWith("Pack") AndAlso e.PropertyName.EndsWith("IsChecked") Then
			Me.UpdateWidgets(TheApp.Settings.PackerIsRunning)
		End If
	End Sub

	Private Sub PackerBackgroundWorker_ProgressChanged(ByVal sender As System.Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs)
		Dim line As String
		line = CStr(e.UserState)

		If e.ProgressPercentage = 0 Then
			Me.LogRichTextBox.Text = ""
			Me.LogRichTextBox.AppendText(line + vbCr)
			Me.UpdateWidgets(True)
		ElseIf e.ProgressPercentage = 1 Then
			Me.LogRichTextBox.AppendText(line + vbCr)
		ElseIf e.ProgressPercentage = 100 Then
			Me.LogRichTextBox.AppendText(line + vbCr)
		End If
	End Sub

	Private Sub PackerBackgroundWorker_RunWorkerCompleted(ByVal sender As System.Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs)
		Dim statusText As String

		If e.Cancelled Then
			statusText = "Pack canceled"
		Else
			Dim packResultInfo As PackerOutputInfo
			packResultInfo = CType(e.Result, PackerOutputInfo)
			If packResultInfo.theStatus = StatusMessage.Error Then
				statusText = "Pack failed; check the log"
			Else
				statusText = "Pack succeeded"
			End If
			Me.UpdatePackedRelativePathFileNames(packResultInfo.thePackedRelativePathFileNames)
		End If

		Me.UpdateWidgets(False)
	End Sub

#End Region

#Region "Private Methods"

	Private Sub UpdateOutputPathWidgets()
		Me.OutputPathTextBox.Visible = (TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.WorkFolder)
		Me.OutputParentPathTextBox.Visible = (TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.ParentFolder)
		Me.BrowseForOutputPathButton.Visible = (TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.ParentFolder) OrElse (TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.WorkFolder)
		'Me.GotoOutputPathButton.Visible = (TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.ParentFolder) OrElse (TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.WorkFolder)
		Me.UpdateOutputPathWidgets(TheApp.Settings.PackerIsRunning)

		If TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.ParentFolder Then
			If TheApp.Settings.PackMode = PackInputOptions.ParentFolder Then
				TheApp.Settings.PackOutputParentPath = TheApp.Settings.PackInputPath
			ElseIf TheApp.Settings.PackMode = PackInputOptions.Folder Then
				Dim parentPath As String = FileManager.GetPath(TheApp.Settings.PackInputPath)
				TheApp.Settings.PackOutputParentPath = parentPath
			End If
		End If
	End Sub

	Private Sub UpdateOutputPathWidgets(ByVal packerIsRunning As Boolean)
		Me.BrowseForOutputPathButton.Enabled = (Not packerIsRunning) AndAlso (TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.WorkFolder)
		Me.GotoOutputPathButton.Enabled = (Not packerIsRunning)
	End Sub

	Private Sub UpdateOutputPathTextBox()
		If TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.WorkFolder Then
			If String.IsNullOrEmpty(Me.OutputPathTextBox.Text) Then
				Try
					TheApp.Settings.PackOutputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
				Catch ex As Exception
					Dim debug As Integer = 4242
				End Try
			End If
		End If
	End Sub

	Private Sub UpdateWidgets(ByVal packerIsRunning As Boolean)
		TheApp.Settings.PackerIsRunning = packerIsRunning

		Me.InputComboBox.Enabled = Not packerIsRunning
		Me.InputPathFileNameTextBox.Enabled = Not packerIsRunning
		Me.BrowseForInputFolderOrFileNameButton.Enabled = Not packerIsRunning

		Me.OutputPathComboBox.Enabled = Not packerIsRunning
		Me.OutputPathTextBox.Enabled = Not packerIsRunning
		Me.OutputParentPathTextBox.Enabled = Not packerIsRunning
		Me.UpdateOutputPathWidgets(packerIsRunning)

		Me.OptionsGroupBox.Enabled = Not packerIsRunning

		Me.PackButton.Enabled = Not packerIsRunning
		Me.SkipCurrentFolderButton.Enabled = packerIsRunning
		Me.CancelPackButton.Enabled = packerIsRunning
		Me.SkipCurrentFolderButton.Enabled = packerIsRunning
		Me.UseAllInPublishButton.Enabled = Not packerIsRunning AndAlso Me.thePackedRelativePathFileNames.Count > 0

		Me.PackedFilesComboBox.Enabled = Not packerIsRunning AndAlso Me.thePackedRelativePathFileNames.Count > 0
		'TODO: Check for the various Pack extensions instead of just for "vpk".
		Me.UseInPublishButton.Enabled = Not packerIsRunning AndAlso Me.thePackedRelativePathFileNames.Count > 0 AndAlso (Path.GetExtension(Me.thePackedRelativePathFileNames(Me.PackedFilesComboBox.SelectedIndex)) = ".vpk")
		Me.GotoPackedFileButton.Enabled = Not packerIsRunning AndAlso Me.thePackedRelativePathFileNames.Count > 0
	End Sub

	Private Sub UpdatePackedRelativePathFileNames(ByVal iPackedRelativePathFileNames As BindingListEx(Of String))
		If iPackedRelativePathFileNames IsNot Nothing Then
			Me.thePackedRelativePathFileNames = iPackedRelativePathFileNames
			'NOTE: Do not sort because the list is already sorted by file and then by folder.
			'Me.theCompiledRelativePathFileNames.Sort()
			'NOTE: Need to set to nothing first to force it to update.
			Me.PackedFilesComboBox.DataSource = Nothing
			Me.PackedFilesComboBox.DataSource = Me.thePackedRelativePathFileNames
		End If
	End Sub

	Private Sub DeleteWarningMessage()
		If Me.LogRichTextBox.Text.StartsWith(Me.theWarningMessgeAboutInvalidJsonFormat) Then
			Me.LogRichTextBox.Text = ""
		End If
	End Sub

	Private Sub UpdatePackerOptions()
		'TODO: Add 'Write multi-file VPK' option.
		Dim gameSetup As GameSetup
		gameSetup = TheApp.Settings.GameSetups(TheApp.Settings.PackGameSetupSelectedIndex)
		If Path.GetFileName(gameSetup.PackerPathFileName) = "gmad.exe" Then
			Me.MultiFileVpkCheckBox.Visible = False
			'Me.EditPackerOptionsText(Me.GetMultiFileArgumentText(), False)

			Me.IgnoreWhitelistWarningsCheckBox.Visible = True
			'Me.EditPackerOptionsText("warninvalid", TheApp.Settings.PackOptionIgnoreWhitelistWarningsIsChecked)
			Me.GmaPanel.Visible = True
		Else
			Me.MultiFileVpkCheckBox.Visible = True
			'Me.EditPackerOptionsText(Me.GetMultiFileArgumentText(), TheApp.Settings.PackOptionMultiFileVpkIsChecked)

			Me.IgnoreWhitelistWarningsCheckBox.Visible = False
			'Me.EditPackerOptionsText("warninvalid", False)
			Me.GmaPanel.Visible = False
		End If

		Me.SetPackerOptionsText()
	End Sub

	'Private Function GetMultiFileArgumentText() As String
	'	Dim text As String
	'	Dim inputPath As String = TheApp.Settings.PackInputPath
	'	Dim inputFolder As String = Path.GetFileName(inputPath)

	'	text = "M a "
	'	text += inputFolder
	'	text += " @list.txt"

	'	Return text
	'End Function

	'Private Sub EditPackerOptionsText(ByVal iCompilerOption As String, ByVal optionIsEnabled As Boolean)
	'	Dim compilerOption As String

	'	compilerOption = "-" + iCompilerOption

	'	If optionIsEnabled Then
	'		If Not Me.theSelectedPackerOptions.Contains(compilerOption) Then
	'			Me.theSelectedPackerOptions.Add(compilerOption)
	'			Me.theSelectedPackerOptions.Sort()
	'		End If
	'	Else
	'		If Me.theSelectedPackerOptions.Contains(compilerOption) Then
	'			Me.theSelectedPackerOptions.Remove(compilerOption)
	'		End If
	'	End If
	'End Sub

	Private Sub SetPackerOptionsText()
		Dim selectedIndex As Integer = TheApp.Settings.PackGameSetupSelectedIndex
		Dim gameSetup As GameSetup = TheApp.Settings.GameSetups(selectedIndex)
		Dim gamePackerFileName As String = Path.GetFileName(gameSetup.PackerPathFileName)
		Dim inputPath As String = TheApp.Settings.PackInputPath

		'TODO: [14-May-2022] This block of code should be moved elsewhere.
		If gamePackerFileName = "gmad.exe" Then
			Dim pathFileName As String = Path.Combine(inputPath, "addon.json")
			Dim garrysModAppInfo As GarrysModSteamAppInfo = New GarrysModSteamAppInfo()
			Dim readIsSuccess As AppEnums.StatusMessage = garrysModAppInfo.ReadDataFromAddonJsonFile(pathFileName, TheApp.Settings.PackGmaTitle, TheApp.Settings.PackGmaItemTags)
			If readIsSuccess = StatusMessage.Success Then
				Me.theGmaGarrysModTagsUserControlIsBeingChangedByMe = True
				Me.GmaGarrysModTagsUserControl.ItemTags = TheApp.Settings.PackGmaItemTags
				Me.theGmaGarrysModTagsUserControlIsBeingChangedByMe = False
			Else
				Me.LogRichTextBox.AppendText(Me.theWarningMessgeAboutInvalidJsonFormat + vbCr)
			End If
		End If

		Dim inputFolder As String = Path.GetFileName(inputPath)

		Dim packOptionsText As String = ""
		'NOTE: Available in Framework 4.0:
		'TheApp.Settings.PackOptionsText = String.Join(" ", Me.packerOptions)
		'------
		For Each packerOption As String In Me.theSelectedPackerOptions
			packOptionsText += " "
			packOptionsText += packerOption

			'TODO: Special case for multi-file VPK option. Need to use "response" file.
			'If packerOption = "M" AndAlso gamePackerFileName <> "gmad.exe" Then
			'	'a <vpkfile> @<filename>
			'	If TheApp.Settings.PackMode = PackInputOptions.ParentFolder Then
			'	ElseIf TheApp.Settings.PackMode = PackInputOptions.Folder Then
			'	End If
			'End If
		Next
		If Me.DirectPackerOptionsTextBox.Text.Trim() <> "" Then
			packOptionsText += " "
			packOptionsText += Me.DirectPackerOptionsTextBox.Text
		End If
		TheApp.Settings.PackOptionsText = packOptionsText.Trim()

		Me.PackerOptionsTextBox.Text = """"
		Me.PackerOptionsTextBox.Text += gameSetup.PackerPathFileName
		Me.PackerOptionsTextBox.Text += """"

		If packOptionsText <> "" Then
			Me.PackerOptionsTextBox.Text += packOptionsText
		End If
		Me.PackerOptionsTextBox.Text += " "

		If gamePackerFileName = "gmad.exe" Then
			Me.PackerOptionsTextBox.Text += "create -folder "
		ElseIf TheApp.Settings.PackOptionMultiFileVpkIsChecked Then
			Me.PackerOptionsTextBox.Text += "-M a "
		End If

		Me.PackerOptionsTextBox.Text += """"
		Me.PackerOptionsTextBox.Text += inputFolder
		Me.PackerOptionsTextBox.Text += """"

		If gamePackerFileName = "gmad.exe" AndAlso TheApp.Settings.PackOptionIgnoreWhitelistWarningsIsChecked Then
			Me.PackerOptionsTextBox.Text += " -warninvalid"
		ElseIf TheApp.Settings.PackOptionMultiFileVpkIsChecked Then
			Me.PackerOptionsTextBox.Text += " @filelist.txt"
		End If
	End Sub

	Private Sub BrowseForOutputPath()
		If TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.WorkFolder Then
			'NOTE: Using "open file dialog" instead of "open folder dialog" because the "open folder dialog" 
			'      does not show the path name bar nor does it scroll to the selected folder in the folder tree view.
			Dim outputPathWdw As New OpenFileDialog()

			outputPathWdw.Title = "Open the folder you want as Output Folder"
			outputPathWdw.InitialDirectory = FileManager.GetLongestExtantPath(TheApp.Settings.PackOutputPath)
			If outputPathWdw.InitialDirectory = "" Then
				If File.Exists(TheApp.Settings.PackInputPath) Then
					outputPathWdw.InitialDirectory = FileManager.GetPath(TheApp.Settings.PackInputPath)
				ElseIf Directory.Exists(TheApp.Settings.PackInputPath) Then
					outputPathWdw.InitialDirectory = TheApp.Settings.PackInputPath
				Else
					outputPathWdw.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
				End If
			End If
			outputPathWdw.FileName = "[Folder Selection]"
			outputPathWdw.AddExtension = False
			outputPathWdw.CheckFileExists = False
			outputPathWdw.Multiselect = False
			outputPathWdw.ValidateNames = False

			If outputPathWdw.ShowDialog() = Windows.Forms.DialogResult.OK Then
				' Allow dialog window to completely disappear.
				Application.DoEvents()

				TheApp.Settings.PackOutputPath = FileManager.GetPath(outputPathWdw.FileName)
			End If
		End If
	End Sub

	Private Sub GotoFolder()
		If TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.ParentFolder Then
			FileManager.OpenWindowsExplorer(TheApp.Settings.PackOutputParentPath)
		ElseIf TheApp.Settings.PackOutputFolderOption = PackOutputPathOptions.WorkFolder Then
			FileManager.OpenWindowsExplorer(TheApp.Settings.PackOutputPath)
		End If
	End Sub

	Private Sub RunPacker()
		TheApp.Packer.Run()
	End Sub

#End Region

#Region "Data"

	Private theWarningMessgeAboutInvalidJsonFormat As String = "WARNING: The addon.json file is invalid json format. Crowbar will overwrite the file with current options."
	Private theSelectedPackerOptions As List(Of String)
	Private thePackedRelativePathFileNames As BindingListEx(Of String)
	Private theGmaGarrysModTagsUserControlIsBeingChangedByMe As Boolean

#End Region

End Class
