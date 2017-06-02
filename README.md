# Verwendung von TPL (Async / Await) zum abbrechen von DB-Aktionen

## Ausgangspunkt

Der Ausgangspunkt f�r dieses Projekt war die Anfrage wie eine Methode, die verschiedene Interaktionen mit einer Datenbank abwickelt nach einer definierten Zeit abgebrochen werden kann. Offensichtlich war in diesem konkreten Fall der Server nicht erreichbar oder wurde w�hrend der Kommunikation beendet.

Ganz allgemein stellen die verschiedenen Datenbank-Klassen (in diesem konkreten Beispiel wurde `OdbcConnection` und `OdbcCommand` verwendet) ein eigenes Timeout-Handling zur Verf�gung. Ich denke, dass es auch Sinn macht die bereitgestellten Mechanismen der Datenbank-Klassen zu verwenden anstatt einen zus�tzliches Timeout zu realisieren. Da momentan jedoch Zeit und M�glichkeit fehlen eine umfangreichere Fehlersuche zu machen, verwende ich dieses Projekt zum testen wie eine m�gliche L�sung mit TPL (Task Parallel Library) aussehen k�nnte. 

## TPL & Async / Await

Eine M�glichkeit ein zus�tzliches Timeout (unabh�ngig von den `Odbc` Klassen) zu realisieren ist die Auslagerung der Interaktion mit der Datenbank in einen separaten Task. Mit Async, Await und der TPL ist das relativ einfach m�glich. Ein impliziter Vorteil ist, dass die Anwendung nicht mehr "einfriert" bei Datenbankabfragen.

Ein Risiko ist, dass die Anwendung durch die Nebenl�ufigkeiten komplexer wird und eine solche �nderung weitere Umbauten nach sich ziehen kann.

## Variante 1 - CancellationToken

Der �bliche und empfohlene Weg einen Task nach einer gewissen Zeit zu beenden ist die Verwendung eines `CancellationToken`.

Eine solche L�sung k�nnte wie folgt aussehen:

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

Da in diesem konkreten Fall die nebenl�ufige Ausf�hrung nicht unbedingt ben�tigt wird, kann auch die `Task.Wait` Methode verwendet werden. Diese hat eine �berladung bei der es m�glich ist ein Timeout anzugeben:

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
