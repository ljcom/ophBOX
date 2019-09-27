Imports System.ComponentModel
Imports System.Data.SqlClient
Imports ophBox.FunctionList

Public Class addDatabaseFrm
    Dim f As FunctionList = FunctionList.Instance
    Dim canClose = False

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        canClose = True
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim type = f.getTag(mainFrm.TreeView1.SelectedNode, "type")
        Dim curNode As TreeNode = mainFrm.TreeView1.SelectedNode
        If type = "3" Then
            curNode = mainFrm.TreeView1.SelectedNode.Parent
        End If
        Dim pipename = curNode.Text
        Dim uid = f.getTag(curNode, "uid")
        Dim pwd = f.getTag(curNode, "pwd")

        If checkDB(Me.TextBox1.Text, pipename, uid, pwd) Then
            If addDB(Me.TextBox1.Text, pipename, uid, pwd, "", "") Then
                Dim x = curNode.Nodes.Add(Me.TextBox1.Text)
                x.Tag = "type=3;dbname=" & Me.TextBox1.Text & "_data"
                canClose = True
                Me.Close()
            End If
        Else
            If Me.TextBox3.Text = "" Then
                MessageBox.Show("The password cannot be empty")
            ElseIf Me.TextBox3.Text.Length < 8 Then
                MessageBox.Show("The password  not long enough. (8 character min.) and must use number and special character")
            ElseIf Me.TextBox3.Text <> Me.TextBox4.Text Then
                MessageBox.Show("Your password is not match")
            Else
                Dim adminID = Me.TextBox2.Text
                Dim adminPwd = Me.TextBox3.Text

                If addDB(Me.TextBox1.Text, pipename, uid, pwd, adminID, adminPwd) Then
                    Dim x = curNode.Nodes.Add(Me.TextBox1.Text)
                    x.Tag = "type=3;dbname=" & Me.TextBox1.Text & "_data"
                    canClose = True
                    Me.Close()
                End If
            End If
        End If
    End Sub
    Function checkDB(accountid, pipename, uid, pwd) As Boolean
        Dim r = False
        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=master;User Id=" & uid & ";password=" & pwd
        Dim sqlstr = "select name from sys.databases where name='" & accountid & "_data'"
        Dim x = f.runSQLwithResult(sqlstr, odbc)
        If x <> "" Then r = True
        Return r
    End Function
    Function addDB(accountid, pipename, uid, pwd, adminid, adminpwd) As Boolean
        Me.Cursor = Cursors.WaitCursor
        Dim r = False
        Dim odbc = "Data Source=" & pipename & ";Initial Catalog=oph_core;User Id=" & uid & ";password=" & pwd
        Dim sqlstr = "if not exists(select * from acct where accountid='" & accountid & "') insert into acct (accountid) values('" & accountid & "')"
        f.runSQLwithResult(sqlstr, odbc)

        'sqlstr = "if not exists(select * from acct where accountid='" & accountid & "') insert into acct (accountid) values('" & accountid & "')"
        'insert user n password

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
                    Me.TextBox1.Enabled = False
                    Me.TextBox2.Enabled = False
                    Me.TextBox3.Enabled = False
                    Me.TextBox4.Enabled = False
                Else
                    curName = "Add Database"
                    Me.TextBox1.Text = ""
                    Me.Button1.Text = "Add"
                    Me.TextBox1.Enabled = True
                    Me.TextBox2.Enabled = True
                    Me.TextBox3.Enabled = True
                    Me.TextBox4.Enabled = True
                End If
            End If
        Next

        'End If

    End Sub

    Private Sub addDatabaseFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        e.Cancel = Not canClose
    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBox2.TextChanged

    End Sub

    Private Sub Label4_Click(sender As Object, e As EventArgs) Handles Label4.Click

    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub TextBox4_TextChanged(sender As Object, e As EventArgs) Handles TextBox4.TextChanged

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        Dim type = f.getTag(mainFrm.TreeView1.SelectedNode, "type")
        Dim curNode = mainFrm.TreeView1.SelectedNode
        If type = "3" Then
            curNode = mainFrm.TreeView1.SelectedNode.Parent
        End If
        Dim pipename = f.getTag(curNode, "server")
        Dim uid = f.getTag(curNode, "uid")
        Dim pwd = f.getTag(curNode, "pwd")
        If checkDB(Me.TextBox1.Text, pipename, uid, pwd) Then
            Me.TextBox2.Enabled = False
            Me.TextBox3.Enabled = False
            Me.TextBox4.Enabled = False
        End If
    End Sub
End Class