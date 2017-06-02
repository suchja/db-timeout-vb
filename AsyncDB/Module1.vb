Imports System.Data.SqlClient

Module Module1

    Sub Main()
        Using conn As New SqlConnection("Data Source=(local); Initial Catalog=NorthWind; Integrated Security=SSPI")
            Dim cmd As SqlCommand = New SqlCommand("select top 2 * from orders", conn)

            Dim result = OpenAndQueryAsync(conn, cmd).Result

            Dim reader As SqlDataReader = cmd.ExecuteReader()
            While reader.Read()
                Console.WriteLine(String.Format("{0}", reader(0)))
            End While
        End Using

        Console.ReadLine()
    End Sub

    Async Function OpenAndQueryAsync(conn As SqlConnection, cmd As SqlCommand) As Task(Of Integer)
        Await conn.OpenAsync()
        Await cmd.ExecuteNonQueryAsync()
        Return 1
    End Function

End Module
