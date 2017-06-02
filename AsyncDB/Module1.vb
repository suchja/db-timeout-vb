Imports System.Data.SqlClient
Imports System.Threading

Module Module1

    Sub Main()
        Using conn As New SqlConnection("Data Source=(local); Initial Catalog=NorthWind; Integrated Security=SSPI")
            Dim cmd As SqlCommand = New SqlCommand("select top 2 * from orders", conn)

            Dim isTaskCompleted = OpenAndQueryAsync(conn, cmd).Wait(TimeSpan.FromSeconds(5))

            If isTaskCompleted Then
                Console.WriteLine("Connection is now open")
            Else
                Console.WriteLine("Timeout while connecting")
            End If

            If conn.State = ConnectionState.Open Then
                Dim reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Console.WriteLine(String.Format("{0}", reader(0)))
                End While
            End If
        End Using

        Console.ReadLine()
    End Sub

    Async Function OpenAndQueryAsync(conn As SqlConnection, cmd As SqlCommand) As Task
        Await conn.OpenAsync()
        Await cmd.ExecuteNonQueryAsync()
    End Function

End Module
