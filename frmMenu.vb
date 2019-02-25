Imports System.IO
Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq
Imports System.Data
Imports ophBox.functions

Public Class frmMenu
    Dim lastCmd As Long
    Dim odbc As String = "", pipename = "", uid = "", pwd = ""
    Private accountList As New Dictionary(Of String, accountType)
    Dim actionNo As Long

    Private Sub Button1_Click_1(sender As Object, e As EventArgs)

    End Sub

    Private Sub btnInit_Click(sender As Object, e As EventArgs) Handles btnInit.Click
        lastCmd = 0
        clearOptions()
        Dim accountName = "oph_core"
        Dim curAccount = accountList(accountName)
        Dim ophPath = IIf(My.Settings.isIISExpress = 1 Or My.Settings.OPHPath = "", Directory.GetCurrentDirectory, My.Settings.OPHPath)
        Dim folder = "", folderData = "", folderTemp = ""
        CreateAccount(Me, accountName, True, curAccount, ophPath, folder, folderData, folderTemp)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        lastCmd = 1
        Me.lblSecret.Visible = True
        Me.lblUserName.Visible = True
        Me.tbSecret.Visible = True
        Me.tbUserName.Visible = True
        Me.btnInit2.Visible = True
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        lastCmd = 2
        Me.lblSecret.Visible = True
        Me.lblUserName.Visible = True
        Me.tbSecret.Visible = True
        Me.tbUserName.Visible = True
        Me.btnInit2.Visible = True
    End Sub

    Private Sub btnInit2_Click(sender As Object, e As EventArgs) Handles btnInit2.Click
        If lastCmd = 0 Then
            'init
        ElseIf lastCmd = 1 Then
            'clone
        Else
            'sync
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        openSetup()
    End Sub
    Sub clearOptions()
        Me.lblSecret.Visible = False
        Me.lblUserName.Visible = False
        Me.tbSecret.Visible = False
        Me.tbUserName.Visible = False
        Me.btnInit2.Visible = False
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Label16.Visible = True
        Me.TextBox4.Visible = True

        Me.Label15.Visible = True
        Me.TextBox3.Visible = True

        Me.Label14.Visible = False
        Me.TextBox2.Visible = False

        Me.Label13.Visible = False
        Me.TextBox1.Visible = False

        Me.Button7.Visible = True
        Label11.Text = "(New)"
        Label9.Text = "No Action."
        Me.Button5.Enabled = False   'delete

        actionNo = 4
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        Me.lbAccount.ClearSelected()

        Me.Label16.Visible = True
        Me.TextBox4.Visible = True

        Me.Label15.Visible = True
        Me.TextBox3.Visible = True

        Me.Label14.Visible = True
        Me.TextBox2.Visible = True

        Me.Label13.Visible = True
        Me.TextBox1.Visible = True
        Label11.Text = "(New)"
        Label9.Text = "No Action."
        Me.Button7.Visible = True

        Me.Button5.Enabled = False   'delete
        actionNo = 5

    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        If MsgBox("Are you sure want to delete this account?", vbYesNo) = vbYes Then

        End If
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        Dim sqlstr = ""
        If actionNo = 4 Then
            'create new
            If TextBox3.Text = "" Then
                sqlstr = "insert into oph_core.acct(accountid) values ('" & Me.TextBox4.Text & "')"
            Else
                sqlstr = "insert into oph_core.acct(accountid, migratedb) values ('" & Me.TextBox4.Text & "', '" & Me.TextBox3.Text & "')"
            End If
            Dim r = runSQL(sqlstr, odbc)

        ElseIf actionNo = 5 Then
            'clone

        End If
    End Sub

    Private Sub frmMenu_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Directory.Exists(My.Settings.OPHPath) Then
            'check git
            'check sql
            Dim coredb = "oph_core"
            If My.Settings.isLocalDB Then
                'localdb
                Me.Label4.Text = "(localdb)\OPERAHOUSE"
                pipename = "OPERAHOUSE"
                odbc = "Data Source=" & pipename & ";Initial Catalog=" & coredb & ";Integrated Security=True" 'My.Settings.odbc
                Me.btnInit.Enabled = checkInstance(pipename) <> pipename
            Else
                'sql
                pipename = My.Settings.dbInstanceName
                uid = My.Settings.dbUser
                pwd = My.Settings.dbPassword
                odbc = "Data Source=" & pipename & ";Initial Catalog=" & coredb & ";uid=" & uid & ";pwd=" & pwd 'My.Settings.odbc
                Me.Label4.Text = Replace(pipename, ".", "(localdb)")
                Me.btnInit.Enabled = Not syncLocalScript("use " & coredb, "master", pipename, uid, pwd)
                'core exists
            End If
            Me.Label4.Text &= IIf(btnInit.Enabled, " not", "") & " is installed"

            'check iis

            'add account
            Dim sqlstr = "select accountid from acct"
            Dim ds As DataSet = SelectSqlSrvRows(sqlstr, odbc)
            If ds.Tables.Count > 0 AndAlso ds.Tables(0).Rows.Count > 0 Then
                For Each r In ds.Tables(0).Rows
                    lbAccount.Items.Add(r.item(0))
                Next
            End If
        Else
            openSetup()
        End If
    End Sub
    Sub openSetup()
        clearOptions()
        Dim o = New frmOptions()
        o.ShowDialog(Me)
    End Sub

    Private Sub lbAccount_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lbAccount.SelectedIndexChanged
        Me.Label16.Visible = False
        Me.TextBox4.Visible = False

        Me.Label15.Visible = False
        Me.TextBox3.Visible = False

        Me.Label14.Visible = False
        Me.TextBox2.Visible = False

        Me.Label13.Visible = False
        Me.TextBox1.Visible = False
        Me.Button7.Visible = False


        Me.Button5.Enabled = True   'delete

        If Me.lbAccount.SelectedItems.Count = 1 Then
            If Not IsNothing(Me.lbAccount.SelectedItem) Then
                Dim datadb = Me.lbAccount.SelectedItem
                Me.Label11.Text = datadb
                Me.Label9.Text = IIf(Not syncLocalScript("use " & datadb & "_data", datadb & "_data", pipename, uid, pwd), "Not ", "") & "Active"
            End If
            'ElseIf Me.lbAccount.SelectedItems.Count > 1 Then

        End If

    End Sub
End Class