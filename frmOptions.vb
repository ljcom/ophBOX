Imports System.IO

Public Class frmOptions
    Private Sub frmOptions_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.TextBox1.Text = My.Settings.remoteUrl

        Me.CheckBox1.Checked = My.Settings.isLocalDB
        Me.TextBox2.Text = My.Settings.dbInstanceName
        Me.TextBox3.Text = My.Settings.dbUser
        Me.TextBox4.Text = "*******"

        Me.CheckBox2.Checked = My.Settings.isIISExpress
        Me.TextBox5.Text = My.Settings.OPHPath

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
End Class