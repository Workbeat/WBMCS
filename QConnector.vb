Imports RabbitMQ.Client
Imports System.Configuration
Imports System.Web.Script.Serialization

Namespace Client

	Public Class QConnector
		Implements IDisposable

		Public Event MessageReceived As EventHandler

		Private m_conn As RabbitMQ.Client.IConnection
		Private m_client_id As String
		Private m_client_secret As String
		Private m_tenantName As String
		Private m_queueName As String
		Private m_listening As Boolean = False
		Private m_port As Integer = 5671
		Private m_channel As RabbitMQ.Client.IModel


		Private Delegate Sub ListenToQueueDelegate()

		Public Sub New()

		End Sub

		Sub CreateConnection()

			Dim factory As ConnectionFactory = New ConnectionFactory()
			Dim hostName As String = ConfigurationManager.AppSettings("WBMSC_HostName")
			m_port = ConfigurationManager.AppSettings("WBMCS_Port")
			m_client_id = ConfigurationManager.AppSettings("WBMSC_ClientId")
			m_client_secret = ConfigurationManager.AppSettings("WBMCS_ClientSecret")
			m_tenantName = ConfigurationManager.AppSettings("WBMCS_TenantName")
			m_queueName = m_client_id & "-" & m_client_secret
			If m_port <= 1 Then
				m_port = 5672	' usando TLS es 5671 el default
			End If
			factory.UserName = m_client_id
			factory.Password = m_client_secret
			factory.VirtualHost = m_tenantName
			factory.HostName = hostName
			factory.Port = m_port
			m_conn = factory.CreateConnection()

			m_channel = m_conn.CreateModel()
		End Sub

		Public Sub StartListening()
			Dim ltq As ListenToQueueDelegate = AddressOf ListenToQueue
			ltq.BeginInvoke(Nothing, Nothing)
		End Sub


		Private Sub ListenToQueue()
			If Not m_conn.IsOpen Then
				CreateConnection()
			End If
			If m_channel Is Nothing OrElse Not m_channel.IsOpen Then
				m_channel = m_conn.CreateModel()
				m_conn.AutoClose = True
			End If
			'm_channel.QueueDeclare(m_queueName, True, False, False, Nothing)
			Dim consumer As RabbitMQ.Client.QueueingBasicConsumer = New QueueingBasicConsumer(m_channel)
			m_channel.BasicConsume(m_queueName, False, consumer)
			m_listening = True
			While (m_listening)
				Try
					Dim ea As RabbitMQ.Client.Events.BasicDeliverEventArgs = consumer.Queue.Dequeue()
					Dim strMessage As String = System.Text.Encoding.UTF8.GetString(ea.Body)
					Dim routingKey As String = ea.RoutingKey
					' avisar
					Dim js As New JavaScriptSerializer
					Dim e As New MessageReceivedEventArgs
					e.Message = js.Deserialize(Of MQMessage)(strMessage)
					'e.DeliveryTag = ea.DeliveryTag
					RaiseEvent MessageReceived(Me, e)
					m_channel.BasicAck(ea.DeliveryTag, False)
				Catch ex As Exception
					m_listening = False
					Disconnect()
					Throw ex
				End Try
			End While
		End Sub


		Sub StopListening()
			m_listening = False
		End Sub


		Function GetMessage(sendAcknowledge As Boolean) As MessageReceivedEventArgs
			If Not m_conn.IsOpen Then
				CreateConnection()
			End If
			If Not m_channel.IsOpen Then
				m_channel = m_conn.CreateModel()
				m_conn.AutoClose = True
			End If
			m_channel.QueueDeclare(m_queueName, True, False, False, Nothing)
			Dim result As BasicGetResult
			result = m_channel.BasicGet(m_queueName, sendAcknowledge)
			If result Is Nothing Then
				Return Nothing
			End If

			Dim e As New MessageReceivedEventArgs
			Dim js As New JavaScriptSerializer
			Try
				e.Message = js.Deserialize(Of MQMessage)(System.Text.Encoding.UTF8.GetString(result.Body))
				e.DeliveryTag = result.DeliveryTag
			Catch ex As Exception
				e.Message = Nothing
			End Try
			If sendAcknowledge Then
				m_channel.BasicAck(result.DeliveryTag, False)
			Else
				e.DeliveryTag = result.DeliveryTag
			End If
			Return e
		End Function

		Function AcknowledgeMessage(deliveryTag As ULong) As Boolean
			m_channel.BasicAck(deliveryTag, False)
			Return True
		End Function


		Sub Disconnect()
			Try
				m_listening = False
				If m_channel.IsOpen Then
					m_channel.Close(200, "Goodbye")
				End If
				If m_conn.IsOpen Then
					m_conn.Close()
				End If
			Catch ex As Exception

			End Try
		End Sub



		Dim disposed As Boolean = False
		Public Sub Dispose() Implements IDisposable.Dispose
			' Dispose of unmanaged resources.
			Dispose(True)
			' Suppress finalization.
			GC.SuppressFinalize(Me)
		End Sub

		Protected Overridable Sub Dispose(disposing As Boolean)
			If disposed Then Return
			If disposing Then
				' Free any other managed objects here. 
				' 
			End If
			Disconnect()
			disposed = True
		End Sub

	End Class

End Namespace

Public Class MessageReceivedEventArgs
	Inherits EventArgs
	Public Property Message As MQMessage
	Public Property DeliveryTag As ULong
End Class

Public Class MQMessage
	Public tenantName As String
	Public messageType As String
	Public source As String
	Public data As String
End Class
