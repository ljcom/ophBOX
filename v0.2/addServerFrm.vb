Imports System.ComponentModel
Imports System.IO
Imports ophBox.FunctionList

Public Class addServerFrm
    Dim f As FunctionList = FunctionList.Instance
    Dim canClose = False
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        canClose = True
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.TextBox1.Text = Replace(Me.TextBox1.Text, ".", "(local)")

        If addServer(IIf(RadioButton1.Checked, "instance", "url"), Me.TextBox1.Text, Me.TextBox2.Text, Me.TextBox3.Text, False) Then
            canClose = True
            Me.Close()
        Else
            MessageBox.Show("Server is not exists or your password is wrong.", "Error")
        End If

    End Sub
    Function addServer(mode As String, pipename As String, uid As String, pwd As String, isNew As Boolean) As Boolean
        Dim r = False

        Dim coreDB = "oph_core"
        Dim folderData As String = "data"
        Dim isLocalDb = False
        Dim ophPath = My.Settings.ophFolder
        Dim remoteUrl = My.Settings.ophServer
        Dim folderTemp = "temp"
        Dim dataAccount = "oph"
        Dim token = ""
        If mode = "url" Then
            token = f.getToken(pipename, uid, pwd)
            If token <> "" Then
                Dim curnode = mainFrm.TreeView1.SelectedNode
                If f.getTag(mainFrm.TreeView1.SelectedNode, "type") = "1" Then
                    Dim x = mainFrm.TreeView1.SelectedNode.Nodes.Add(Me.TextBox1.Text)
                    x.Tag = "type=2;mode=" & IIf(RadioButton1.Checked, "instance", "url") & ";uid=" & Me.TextBox2.Text & ";pwd=" & Me.TextBox3.Text
                    curnode = x
                Else
                    mainFrm.TreeView1.SelectedNode.Text = Me.TextBox1.Text
                    mainFrm.TreeView1.SelectedNode.Tag = "type=2;mode=" & IIf(RadioButton1.Checked, "instance", "url") & ";uid=" & Me.TextBox2.Text & ";pwd=" & Me.TextBox3.Text
                End If

                Dim urlstr = pipename & "/ophcore/api/sync.aspx?mode=dbinfo&token=" & token
                Dim dbinfo = f.postHttp(urlstr)

                Dim sep() As String = {"<?xml version=""1.0"" encoding=""utf-8""?>", "<sqroot>", "<databases>", "<database>", "</database>", "</databases>", "</sqroot>"}
                Dim r1 = dbinfo.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                Dim accountGUID As String = ""
                Dim accountid = ""
                Dim info1() As String = {"<info>", "</info>", "<data ", "/>"}
                For Each r1x In r1
                    Dim r2 = r1x.Split(info1, StringSplitOptions.RemoveEmptyEntries)
                    If r2.Length > 1 Then
                        For Each r2x In r2
                            Dim r3 = r2x.Split({"key=""", "value=""", """ "}, StringSplitOptions.RemoveEmptyEntries)
                            If r3.Length > 1 Then   'not empty
                                If r3(0) = "accountid" Then
                                    accountid = r3(1)
                                End If
                            End If
                        Next

                    End If

                Next

                Dim sep1() As String = {"<AccountGUID>", "<AccountDBGUID>", "<databasename>", "<isMaster>", "<version>", "</AccountGUID>", "</AccountDBGUID>", "</databasename>", "</isMaster>", "</version>"}
                For Each r1x In r1
                    Dim r2 = r1x.Split(sep1, StringSplitOptions.RemoveEmptyEntries)
                    If r2.Length = 5 Then
                        accountGUID = r2(0)
                        Dim accountDBGUID = r2(1)
                        Dim dbname = r2(2)
                        Dim ismaster = r2(3)
                        Dim Version = r2(4)
                        If dbname <> "" And ismaster = "1" Then
                            Dim x = curnode.Nodes.Add(accountid)
                            x.Tag = "type=3;dbname=" & dbname
                        End If

                    End If
                Next


                r = True
            End If
        Else
            Dim odbc = "Data Source=" & pipename & ";Initial Catalog=master;User Id=" & uid & ";password=" & pwd
            Dim ophcore = f.runSQLwithResult("select name from sys.databases where name='oph_core'", odbc)
            If ophcore <> "" Then
                odbc = "Data Source=" & pipename & ";Initial Catalog=" & coreDB & ";User Id=" & uid & ";password=" & pwd
                Dim listofAccount = f.runSQLwithResult("select ';accountid='+accountid+',dbname='+d.DatabaseName from acct a inner join acctdbse d on d.accountguid=a.accountguid and d.ismaster=1 order by accountid for xml path('')", odbc)
                If listofAccount <> "" Then
                    Dim curnode = mainFrm.TreeView1.SelectedNode
                    Dim curTag = mainFrm.TreeView1.SelectedNode.Tag
                    For Each t In curTag.split(";")
                        If t.split("=")(0) = "type" And t.split("=")(1) = "1" Then  'new
                            Dim x = mainFrm.TreeView1.SelectedNode.Nodes.Add(Me.TextBox1.Text)
                            x.Tag = "type=2;mode=" & IIf(RadioButton1.Checked, "instance", "url") & ";uid=" & Me.TextBox2.Text & ";pwd=" & Me.TextBox3.Text
                            curnode = x
                        Else
                            mainFrm.TreeView1.SelectedNode.Text = Me.TextBox1.Text
                            mainFrm.TreeView1.SelectedNode.Tag = "type=2;mode=" & IIf(RadioButton1.Checked, "instance", "url") & ";uid=" & Me.TextBox2.Text & ";pwd=" & Me.TextBox3.Text
                        End If
                    Next

                    curnode.Nodes.Clear()

                    For Each a In listofAccount.Split(";")
                        Dim accountid = "", dbname = ""
                        For Each ax In a.Split(",")
                            If ax.Split("=")(0) = "accountid" Then
                                accountid = ax.Split("=")(1)
                            End If
                            If ax.Split("=")(0) = "dbname" Then
                                dbname = ax.Split("=")(1)
                            End If
                        Next
                        If accountid <> "" Then
                            Dim x = curnode.Nodes.Add(accountid)
                            x.Tag = "type=3;dbname=" & dbname
                        End If
                    Next
                    r = True
                End If

            Else
                If MessageBox.Show("oph account is Not exists. Do you want to create one?", "Confirmation", MessageBoxButtons.YesNo) = vbYes Then
                    Me.Cursor = Cursors.WaitCursor
                    Dim tuser = "sam"
                    Dim secret = "D627AFEB-9D77-40E4-B060-7C976DA05260"

                    If f.createServer(pipename, uid, pwd, tuser, secret, ophPath, My.Settings.ophServer) Then
                        Dim x = mainFrm.TreeView1.SelectedNode
                        If f.getTag(mainFrm.TreeView1.SelectedNode, "type") = "1" Then
                            x = mainFrm.TreeView1.SelectedNode.Nodes.Add(Me.TextBox1.Text)
                        End If
                        x.Tag = "type=2;mode=" & IIf(RadioButton1.Checked, "instance", "url") & ";uid=" & Me.TextBox2.Text & ";pwd=" & Me.TextBox3.Text
                        x.Nodes.Clear()
                        Dim y = x.Nodes.Add("oph")
                        y.Tag = "type=3;dbname=oph_core"

                        f.SetLog("Installing core database completed.")
                        MessageBox.Show("Installing server is completed")
                    Else
                        f.SetLog("Installing core database NOT completed.")
                        MessageBox.Show("Installing server is NOT completed")
                    End If

                    Me.Cursor = Cursors.Default

                End If
            End If
        End If

        Return r
    End Function


    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click

    End Sub

    Private Sub addServerFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.RadioButton1.Checked = True
        Me.TextBox1.Text = ""
        Me.TextBox2.Text = ""
        Me.TextBox3.Text = ""
        Dim curName = mainFrm.TreeView1.SelectedNode.Text
        Dim curTag = mainFrm.TreeView1.SelectedNode.Tag
        Dim t = curTag.split(";")
        For Each ct In t
            curName = ""

            If ct.split("=").length > 1 Then curName = ct.split("=")(1)
            If ct.split("=")(0) = "type" Then
                If curName = "2" Then
                    Me.Text = "Server Properties"
                    Me.TextBox1.Text = mainFrm.TreeView1.SelectedNode.Text
                    Me.Button1.Text = "Save"
                    Me.RadioButton1.Enabled = False
                    Me.RadioButton2.Enabled = False
                Else
                    Me.Text = "Add Server"
                    Me.Button1.Text = "Add"
                    Me.RadioButton1.Enabled = True
                    Me.RadioButton2.Enabled = True
                End If
            End If
            If ct.split("=")(0) = "mode" Then
                If curName = "instance" Then
                    Me.RadioButton1.Checked = True
                Else
                    Me.RadioButton2.Checked = True
                End If
            End If
            If ct.split("=")(0) = "uid" Then
                Me.TextBox2.Text = curName
            End If
            If ct.split("=")(0) = "pwd" Then
                Me.TextBox3.Text = curName
            End If
        Next
    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged
        checkRadio()
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        If Me.TextBox1.Text.IndexOf(".") >= 0 Then
            Me.TextBox1.Text = Me.TextBox1.Text.Replace(".", "(local)")
        End If
    End Sub

    Private Sub TextBox1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles TextBox1.KeyPress
        If TextBox1.Text.IndexOf("://") > 0 Then
            Me.RadioButton2.Checked = True
        Else
            Me.RadioButton1.Checked = True

        End If
    End Sub

    Private Sub addServerFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        e.Cancel = Not canClose
    End Sub

    Private Sub RadioButton3_CheckedChanged(sender As Object, e As EventArgs)
        checkRadio()
    End Sub

    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton1.CheckedChanged
        checkRadio()
    End Sub
    Sub checkRadio()
        If RadioButton1.Checked Then
            Me.Label1.Text = "Instance Name"
            Me.Label2.Text = "User ID"
            Me.Label3.Text = "Password"
        ElseIf RadioButton2.Checked Then
            Me.Label1.Text = "URL"
            Me.Label2.Text = "User ID"
            Me.Label3.Text = "Secret"
        End If

    End Sub
End Class