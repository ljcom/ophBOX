Imports System.Collections.Generic
Imports System.IO

Module Module1
    Function addWebConfig(path As String, Optional isforced As Boolean = False) As Boolean
        Dim isLocalDB = My.Settings.isLocalDB = 1

        Dim r = False
        If File.Exists(path & "operahouse\core\sample-web.config") Then
            r = True
            If isforced And File.Exists(path & "operahouse\core\web.config") Then File.Delete(path & "operahouse\core\web.config")
            If Not File.Exists(path & "operahouse\core\web.config") Then
                Dim newfile As New List(Of String)()
                Dim newline = {}
                For Each k As String In IO.File.ReadLines(path & "operahouse\core\sample-web.config")
                    If k.Contains("<add key=""Sequoia""") Then
                        If isLocalDB Then
                            newline = {
                                    vbTab & "<add key=""Sequoia"" value=""Data Source=(localdb)\operahouse;Initial Catalog=oph_core;Integrated Security=SSPI;timeout=600"" />"}
                        Else
                            Dim serverdb = My.Settings.dbInstanceName
                            Dim uid = My.Settings.dbUser
                            Dim pwd = My.Settings.dbPassword
                            newline = {
                                    vbTab & "<add key=""Sequoia"" value=""Data Source=" & serverdb & ";Initial Catalog=oph_core;user id=" & uid & ";password=" & pwd & ";timeout=600"" />"}
                        End If

                        For Each line As String In newline
                            newfile.Add(line)
                        Next
                    Else
                        newfile.Add(k)
                    End If


                Next
                'File.Delete(path & folderTemp & "\web.config")
                System.IO.File.WriteAllLines(path & "operahouse\core\web.config", newfile.ToArray())
                r = True
            End If
        End If
        Return r
    End Function
End Module
