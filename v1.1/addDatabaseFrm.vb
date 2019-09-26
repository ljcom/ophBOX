Imports System.Data.SqlClient
Imports ophBox.FunctionList
Public Class addDatabaseFrm
    Dim f As FunctionList = FunctionList.Instance

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim type = f.getTag(mainFrm.TreeView1.SelectedNode, "type")
        If type = "2" Then
            Dim pipename = mainFrm.TreeView1.SelectedNode.Text
            Dim uid = f.getTag(mainFrm.TreeView1.SelectedNode, "uid")
            Dim pwd = f.getTag(mainFrm.TreeView1.SelectedNode, "pwd")
            If addDB(Me.TextBox1.Text, pipename, uid, pwd) Then
                Dim x = mainFrm.TreeView1.SelectedNode.Nodes.Add(Me.TextBox1.Text)
                x.Tag = "type=3;dbname="

            End If
        Else
            Dim pipename = f.getTag(mainFrm.TreeView1.SelectedNode.Parent, "server")
            Dim uid = f.getTag(mainFrm.TreeView1.SelectedNode.Parent, "uid")
            Dim pwd = f.getTag(mainFrm.TreeView1.SelectedNode.Parent, "pwd")
            If addDB(Me.TextBox1.Text, pipename, uid, pwd) Then
                mainFrm.TreeView1.SelectedNode.Text = Me.TextBox1.Text
                mainFrm.TreeView1.SelectedNode.Tag = "type=3;dbname="
            End If
        End If

        Me.Close()
    End Sub
    Function addDB(accountid, pipename, uid, pwd) As Boolean
        Me.Cursor = Cursors.WaitCursor
        Dim r = False
        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd
        Dim sqlstr = "if not exists(select * from acct where accountid='" & accountid & "') insert into acct (accountid) values('" & accountid & "')"
        f.runSQLwithResult(sqlstr, odbc)
        Me.Cursor = Cursors.Default
        r = True
        Return r
    End Function

    Private Sub addDatabaseFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.TextBox1.Text = ""

        Dim curName = mainFrm.TreeView1.SelectedNode.Text
        'If curName = "Servers" Then
        'Me.Text = "Add Server"
        'Else
        Dim curTag = mainFrm.TreeView1.SelectedNode.Tag
        Dim t = curTag.split(";")
        For Each ct In t
            curName = ""

            If ct.split("=").length > 1 Then curName = ct.split("=")(1)
            If ct.split("=")(0) = "type" Then
                If curName = "3" Then
                    Me.Text = "Server Properties"
                    Me.TextBox1.Text = mainFrm.TreeView1.SelectedNode.Text
                    Me.Button1.Text = "Save"
                Else
                    curName = "Add Database"
                    Me.TextBox1.Text = ""
                    Me.Button1.Text = "Add"
                End If
            End If
        Next

        'End If

    End Sub

End Class