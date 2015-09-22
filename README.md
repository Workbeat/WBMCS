# WBMCS .NET Client library
WorkBeat Message Controller Service

Esta es una librería en VB.NET la cual se conecta a workbeat.com y permite recibir mensajes de notificación de los eventos que suceden en la aplicacion.

## Requisitos
Esta librería se conecta a un servidor de RabbitMQ, por lo que es necesario incluir las librerias del mismo. Por facilidad, estos dlls están incluidos en este repositorio, pero recomendamos bajar la ultima versión de los mismos del sitio de
[RabbitMQ](https://www.rabbitmq.com/dotnet.html) (https://www.rabbitmq.com/dotnet.html)

## Creación de una aplicación
Para poder conectarse es necesario crear un registro de *aplicación externa* en workbeat. Es necesario obtener tu client_id y client_secret, los cuales serán utilizados para poder identificar a la aplicación y permitirle conectarse y recibir notificaciones. Estos parámetros seran incluidos en el app.config de la aplicación.

##Compilación
Esta librería esta generada en Visual Studio 2012 usando .NET 4.0, aunque puede ser compilada sin problemas en Visual Studio 2010.

##Uso

Un ejemplo sencillo de uso, 

```vbnet

	Sub Main()
		Dim qc As Workbeat.WBMCS.Client.QConnector = Nothing
		Try
			qc = New Workbeat.WBMCS.Client.QConnector
			Console.WriteLine("Consumiendo mensajes...")
			AddHandler qc.MessageReceived, AddressOf onMessageReceived
			Console.WriteLine("escuchando...")
			' Crea la conexion
			qc.CreateConnection()
			' Comienza a escuchar....
			qc.StartListening()
		Catch ex As Exception
			Console.WriteLine(ex.Message)
			If Not qc Is Nothing Then
				qc.Disconnect()
			End If
		End Try
	End Sub

	Public Sub onMessageReceived(sender As Object, e As Workbeat.WBMCS.MessageReceivedEventArgs)
		Console.WriteLine("incoming message...")
		Console.WriteLine(e.Message.messageType)
		Console.WriteLine(e.Message.data)
	End Sub

```

app.config
```xml
	<appSettings>
		<add key="WBMSC_HostName" value="notify.workbeat.com" />
		<add key="WBMCS_Port" value="5672" />
		<add key="WBMSC_ClientId" value="{sustituir con client_id (sin corchetes)}" />
		<add key="WBMCS_ClientSecret" value="{sustituir con client_secret (sin corchetes)}" />
		<add key="WBMCS_TenantName" value="NOMBRECORTO_EMPRESA" />

	</appSettings>
```
