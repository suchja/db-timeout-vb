# Verwendung von TPL (Async / Await) zum abbrechen von DB-Aktionen

## Ausgangspunkt

Der Ausgangspunkt für dieses Projekt war die Anfrage wie eine Methode, die verschiedene Interaktionen mit einer Datenbank abwickelt nach einer definierten Zeit abgebrochen werden kann. Offensichtlich war in diesem konkreten Fall der Server nicht erreichbar oder wurde während der Kommunikation beendet.

Ganz allgemein stellen die verschiedenen Datenbank-Klassen (in diesem konkreten Beispiel wurde `OdbcConnection` und `OdbcCommand` verwendet) ein eigenes Timeout-Handling zur Verfügung. Ich denke, dass es auch Sinn macht die bereitgestellten Mechanismen der Datenbank-Klassen zu verwenden anstatt einen zusätzliches Timeout zu realisieren. Da momentan jedoch Zeit und Möglichkeit fehlen eine umfangreichere Fehlersuche zu machen, verwende ich dieses Projekt zum testen wie eine mögliche Lösung mit TPL (Task Parallel Library) aussehen könnte. 

## TPL & Async / Await

Eine Möglichkeit ein zusätzliches Timeout (unabhängig von den `Odbc` Klassen) zu realisieren ist die Auslagerung der Interaktion mit der Datenbank in einen separaten Task. Mit Async, Await und der TPL ist das relativ einfach möglich. Ein impliziter Vorteil ist, dass die Anwendung nicht mehr "einfriert" bei Datenbankabfragen.

Ein Risiko ist, dass die Anwendung durch die Nebenläufigkeiten komplexer wird und eine solche Änderung weitere Umbauten nach sich ziehen kann.

## Variante 1 - CancellationToken

Der übliche und empfohlene Weg einen Task nach einer gewissen Zeit zu beenden ist die Verwendung eines `CancellationToken`.

Eine solche Lösung könnte wie folgt aussehen:

```vb
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
```

TBD: Vor- und Nachteile analysieren und beschreiben

## Variante 2 - Task.Wait( Timeout )

Da in diesem konkreten Fall die nebenläufige Ausführung nicht unbedingt benötigt wird, kann auch die `Task.Wait` Methode verwendet werden. Diese hat eine Überladung bei der es möglich ist ein Timeout anzugeben:

```vb
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
```
