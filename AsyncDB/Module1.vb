Imports System.Data.SqlClient
Imports System.Threading

Module Module1

    Sub Main()
        Using conn As New SqlConnection("Data Source=(local); Initial Catalog=NorthWind; Integrated Security=SSPI")
            Dim cmd As SqlCommand = New SqlCommand("select top 2 * from orders", conn)

            Dim result = OpenAndQueryAsync(conn, cmd).Result
            Console.WriteLine("Result from async Connection: " + result)

            If conn.State = ConnectionState.Open Then
                Dim reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Console.WriteLine(String.Format("{0}", reader(0)))
                End While
            End If
        End Using

        Console.ReadLine()
    End Sub

    Async Function OpenAndQueryAsync(conn As SqlConnection, cmd As SqlCommand) As Task(Of String)
        Dim cts As CancellationTokenSource = New CancellationTokenSource(10000)
        Try
            Await conn.OpenAsync(cts.Token)
            Await cmd.ExecuteNonQueryAsync(cts.Token)
        Catch tcex As TaskCanceledException
            Return "Timeout"
        Finally
            cts.Dispose()
        End Try

        Return "Success"
    End Function

End Module
