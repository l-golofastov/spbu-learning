using System.Net;
using System.Net.Sockets;

namespace P2PChat;

public class Chat : IDisposable
{
	private volatile bool _isStopped;

	private readonly Socket _acceptor;

	private readonly Thread _acceptorThread;
	private readonly List<Thread> _listeners = new();
	private readonly Dictionary<IPEndPoint, Connection> _participants = new();

	// Locker for participants and OnEvent execution
	private readonly object _locker = new();

	public delegate void EventHandler(ChatEvent e, IPEndPoint sender, string? payload);

	public event EventHandler? OnEvent;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="port">Port number associated with the IP address.</param>
	public Chat(int port)
	{
		var endPoint = new IPEndPoint(IPAddress.Any, port);

		_acceptor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_acceptor.Bind(endPoint);
		_acceptor.Listen();

		_acceptorThread = new Thread(AcceptIncoming);
		_acceptorThread.Start();
	}

	/// <summary>
	/// Adds a peer to a chat.
	/// </summary>
	/// <param name="endPoint">Chat address.</param>
	/// <param name="conn">Participant connection interface.</param>
	private void AddToChat(IPEndPoint endPoint, Connection conn)
	{
		var listener = new Thread(() => Listen(endPoint, conn));
		_listeners.Add(listener);
		listener.Start();

		lock (_locker)
		{
			_participants.Add(endPoint, conn);
		}
	}

	/// <summary>
	/// Accepts incoming traffic (new chat participants).
	/// </summary>
	private void AcceptIncoming()
	{
		while (!_isStopped)
		{
			try
			{
				var conn = new Connection(_acceptor.Accept());

				string port = conn.Receive();

				// Setting the right port
				var endPoint = IPEndPoint.Parse($"{conn.RemoteEndPoint!.Address}:{port}");

				lock (_locker)
				{
					conn.Send(_participants.Count > 0 ? string.Join(' ', _participants.Keys) : "NO");
				}

				AddToChat(endPoint, conn);


				OnEvent?.Invoke(ChatEvent.Connect, endPoint, null);
			}
			catch
			{
				if (!_isStopped) throw;
			}
		}
	}

    /// <summary>
    /// Handles incoming of messages.
    /// </summary>
    /// <param name="endPoint">Chat address.</param>
    /// <param name="conn">Participant connection interface.</param>
    private void Listen(IPEndPoint endPoint, Connection conn)
	{
		while (!_isStopped)
		{
			try
			{
				string received = conn.Receive();
				lock (_locker)
				{
					OnEvent?.Invoke(ChatEvent.Message, endPoint, received);
				}
			}
			catch (SocketException e)
			{
				lock (_locker)
				{
					switch (e.SocketErrorCode)
					{
						case SocketError.ConnectionReset:
							_participants.Remove(endPoint);
							OnEvent?.Invoke(ChatEvent.Disconnect, endPoint, null);
							break;
						default:
							if (!_isStopped)
								OnEvent?.Invoke(ChatEvent.Error, endPoint, e.Message);
							break;
					}
				}

				break;
			}
		}
	}

    /// <summary>
    /// Adds an endpoint chat participant to chat participants of this object.
    /// </summary>
    /// <param name="selfPort">Participant port.</param>
    /// <param name="endPoint">Chat address.</param>
    /// <returns></returns>
    private string InnerConnect(int selfPort, IPEndPoint endPoint)
	{
		var conn = new Connection(endPoint);
		conn.Send(selfPort.ToString());
		var received = conn.Receive();
		AddToChat(endPoint, conn);

		return received;
	}

    /// <summary>
    /// Connects a chat participant to a chat with the given endpoint.
    /// </summary>
    /// <param name="endPoint">Chat address to connect to.</param>
    /// <exception cref="Exception"></exception>
    public void Connect(IPEndPoint endPoint)
	{
		int selfPort = ((IPEndPoint) _acceptor.LocalEndPoint!).Port;

		lock (_locker)
		{
			if (_participants.Count > 0)
				throw new Exception("You're already connected to some chat");

			if (endPoint.Port == selfPort)
				throw new Exception("Can't connect to myself");

			if (_participants.Keys.Contains(endPoint))
				throw new Exception($"Already connected to {endPoint}");
		}

		string addresses = InnerConnect(selfPort, endPoint);

		// If somebody else is connected to a chat
		if (addresses != "NO")
		{
			foreach (var address in addresses.Split())
				InnerConnect(selfPort, IPEndPoint.Parse(address));
		}

		OnEvent?.Invoke(ChatEvent.Connect, endPoint, null);
	}

	/// <summary>
	/// Sends a message to chat participants
	/// </summary>
	/// <param name="msg">Message</param>
	public void Send(string msg)
	{
		lock (_locker)
		{
			OnEvent?.Invoke(ChatEvent.Message, (IPEndPoint) _acceptor.LocalEndPoint!, msg);

			foreach (var conn in _participants.Values)
				conn.Send(msg);
		}
	}

	/// <summary>
	/// Disables a chat participant.
	/// </summary>
	public void Dispose()
	{
		_isStopped = true;

		_acceptor.Close();
		_acceptorThread.Join();

		foreach (var conn in _participants.Values)
			conn.Close();

		foreach (var listener in _listeners)
			listener.Join();
	}
}