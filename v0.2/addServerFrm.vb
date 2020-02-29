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
        If Me.TextBox1.Text.Substring(0, 1) = "." Then
            Me.TextBox1.Text = Replace(Me.TextBox1.Text, ".", "(local)")
        End If
        If Val(Me.TextBox4.Text) < 8080 Or Val(Me.TextBox4.Text) > 8100 Then
            MessageBox.Show("Please use port between 8080 and 8100.", "Error")
        ElseIf Not checkPort(Me.TextBox1.Text, Me.TextBox4.Text) Then
            MessageBox.Show("Your port selection is already used with another server.", "Error")
        Else
            Me.Cursor = Cursors.WaitCursor
            If f.addServer(mainFrm.TreeView1, IIf(RadioButton1.Checked, "instance", "url"), Me.TextBox1.Text, Me.TextBox2.Text, Me.TextBox3.Text, False, Me.TextBox4.Text) Then
                canClose = True
                Me.Close()
            Else
                'MessageBox.Show("Server is not exists or your password is wrong.", "Error")
            End If
            Me.Cursor = Cursors.Default

        End If
    End Sub
    Function checkPort(server, port) As Boolean
        Dim r = True
        For Each n In mainFrm.TreeView1.Nodes(0).Nodes
            Dim curPort = f.getTag(n, "port")
            Dim curServer = n.text
            If curServer <> server And port = curPort Then
                r = False
            End If
        Next
        Return r
    End Function


    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click

    End Sub

    Private Sub addServerFrm_Load(sender As Object, e As EventArgs) Handles Me.Load
        canClose = False
        Me.RadioButton1.Checked = True
        Me.TextBox1.Text = ""
        Me.TextBox2.Text = ""
        Me.TextBox3.Text = ""
        Me.TextBox4.Text = ""
        Me.TextBox1.Select()

        Dim curtype = f.getTag(mainFrm.TreeView1.SelectedNode, "type")
        Dim curNode = mainFrm.TreeView1.SelectedNode
        If curtype = "2" Then
            Me.Text = "Server Properties"
            Me.TextBox1.Text = mainFrm.TreeView1.SelectedNode.Text
            Me.TextBox1.Enabled = False
            Me.Button1.Text = "Save"
            Me.RadioButton1.Enabled = False
            Me.RadioButton2.Enabled = False

            Dim curmode = f.getTag(mainFrm.TreeView1.SelectedNode, "mode")
            If curmode = "instance" Then
                Me.RadioButton1.Checked = True
                Me.Label4.Visible = True
                Me.TextBox4.Visible = True
            Else
                Me.RadioButton2.Checked = True
                Me.Label4.Visible = False
                Me.TextBox4.Visible = False
            End If
            Me.TextBox2.Text = f.getTag(mainFrm.TreeView1.SelectedNode, "uid")
            Me.TextBox3.Text = f.getTag(mainFrm.TreeView1.SelectedNode, "pwd")
            Me.TextBox4.Text = f.getTag(mainFrm.TreeView1.SelectedNode, "port")
        Else
            Me.Text = "Add Server"
            Me.TextBox1.Enabled = True
            Me.Button1.Text = "Add"
            Me.RadioButton1.Enabled = True
            Me.RadioButton2.Enabled = True
            Me.RadioButton1.Checked = True
        End If

    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged
        checkRadio()
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        If Me.TextBox1.Text <> "" Then
            If Me.TextBox1.Text.Substring(0, 1) = "." Then
                Me.TextBox1.Text = Me.TextBox1.Text.Replace(".", "(local)")
            End If
        End If
    End Sub

    Private Sub TextBox1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles TextBox1.KeyPress
        If TextBox1.Text.IndexOf("://") > 0 Then
            If Not Me.RadioButton2.Checked Then Me.RadioButton2.Checked = True
        Else
            If Not Me.RadioButton1.Checked Then Me.RadioButton1.Checked = True
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
            Me.Label4.Visible = True
            Me.TextBox4.Visible = True
        ElseIf RadioButton2.Checked Then
            Me.Label1.Text = "URL"
            Me.Label2.Text = "User ID"
            Me.Label3.Text = "Secret"
            'Me.Label4.Visible = False
            'Me.TextBox4.Visible = False
        End If
    End Sub
End Class