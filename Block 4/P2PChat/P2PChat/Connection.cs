using System.Net;
using System.Net.Sockets;
using System.Text;

namespace P2PChat;

public class Connection
{
	private readonly Socket _socket;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="socket">Socket</param>
	public Connection(Socket socket)
	{
		_socket = socket;
	}

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="endPoint">Network endpoint as an IP address and a port number</param>
    public Connection(IPEndPoint endPoint)
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_socket.Connect(endPoint);
	}

	/// <summary>
	/// Sends data.
	/// </summary>
	/// <param name="msg">Message</param>
	public void Send(string msg) => _socket.Send(Encoding.UTF8.GetBytes(msg));

	/// <summary>
	/// Recieves data.
	/// </summary>
	/// <returns></returns>
	public string Receive()
	{
		//buffer to write in
		var buffer = new byte[256];

		var result = new StringBuilder();

		do
		{
			int size = _socket.Receive(buffer);
			result.Append(Encoding.UTF8.GetString(buffer, 0, size));
		} while (_socket.Available > 0);

		return result.ToString();
	}

	/// <summary>
	/// Returns a remote endpoint.
	/// </summary>
	public IPEndPoint? RemoteEndPoint => (IPEndPoint?) _socket.RemoteEndPoint;

    /// <summary>
    /// Closes the Socket connection and releases all associated resources.
    /// </summary>
    public void Close() => _socket.Close();
}