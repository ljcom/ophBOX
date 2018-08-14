Imports IWshRuntimeLibrary
Imports System.IO
Imports System.Windows.Forms


Public Class frmOptions
    Private Sub frmOptions_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.TextBox1.Text = My.Settings.remoteUrl

        Me.CheckBox1.Checked = My.Settings.isLocalDB
        Me.TextBox2.Text = My.Settings.dbInstanceName
        Me.TextBox3.Text = My.Settings.dbUser
        Me.TextBox4.Text = "*******"
        Me.TextBox6.Text = My.Settings.delayTime

        Me.CheckBox2.Checked = My.Settings.isIISExpress
        Me.TextBox5.Text = My.Settings.OPHPath
        Me.CheckBox3.Checked = My.Settings.isStartMenu
        CheckBox1_afterclick()
        CheckBox1_afterclick()

    End Sub

    Private Sub CheckBox1_Click(sender As Object, e As EventArgs) Handles CheckBox1.Click
        CheckBox1_afterclick()
    End Sub
    Sub CheckBox1_afterclick()
        Me.TextBox2.Enabled = Not Me.CheckBox1.Checked
        Me.TextBox3.Enabled = Not Me.CheckBox1.Checked
        Me.TextBox4.Enabled = Not Me.CheckBox1.Checked

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        My.Settings.remoteUrl = Me.TextBox1.Text

        My.Settings.isLocalDB = IIf(Me.CheckBox1.Checked, 1, 0)
        My.Settings.dbInstanceName = Me.TextBox2.Text
        My.Settings.dbUser = Me.TextBox3.Text
        If Me.TextBox4.Text <> "*******" Then My.Settings.dbPassword = Me.TextBox4.Text

        My.Settings.isIISExpress = IIf(Me.CheckBox2.Checked, 1, 0)
        My.Settings.OPHPath = Me.TextBox5.Text

        If Me.CheckBox2.Checked And Not Directory.Exists(Me.TextBox5.Text) Then
            Directory.CreateDirectory(Me.TextBox5.Text)
        End If
        My.Settings.delayTime = Me.TextBox6.Text
        If CheckBox3.Checked And My.Settings.isStartMenu = 0 Then
            My.Settings.isStartMenu = 1
            CreateShortcutInStartUp("OPH Box")
        ElseIf Not CheckBox3.Checked And My.Settings.isStartMenu = 1 Then
            'remove shortcut
            My.Settings.isStartMenu = 0
            DeleteShortcutInStartUp()
        End If
        My.Settings.Save()
        Me.Close()

        'setup applicationhost.config
        Dim r = addWebConfig(Directory.GetCurrentDirectory() & "\", True)
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        CheckBox2_afterclick()
    End Sub
    Sub CheckBox2_afterclick()
        Me.TextBox5.Enabled = Not Me.CheckBox2.Checked
    End Sub
    Public Sub CreateShortcutInStartUp(ByVal Descrip As String)


        Dim WshShell As WshShell = New WshShell()
        Dim ShortcutPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Startup)
        Dim Shortcut As IWshShortcut = CType(WshShell.CreateShortcut(System.IO.Path.Combine(ShortcutPath, "OPHBOX") & ".lnk"), IWshShortcut)
        Shortcut.TargetPath = Application.ExecutablePath
        Shortcut.WorkingDirectory = Application.StartupPath
        Shortcut.Description = Descrip
        Shortcut.Save()
    End Sub
    Public Sub DeleteShortcutInStartUp()
        Dim WshShell As WshShell = New WshShell()
        Dim ShortcutPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Startup)
        If IO.File.Exists(System.IO.Path.Combine(ShortcutPath, "OPHBOX") & ".lnk") Then
            IO.File.Delete(System.IO.Path.Combine(ShortcutPath, "OPHBOX") & ".lnk")
        End If

    End Sub

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged

    End Sub
End Class