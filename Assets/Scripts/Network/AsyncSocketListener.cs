using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

// State object for reading client data asynchronously  
public class StateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

static class SocketExtensions
{
    public static bool IsConnected(this Socket socket)
    {
        try
        {
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch (SocketException) { return false; }
    }
}

public class AsyncSocketListener : MonoBehaviour
{
    // Thread signal.  
    //public static ManualResetEvent allDone = new ManualResetEvent(false);
    
    public static IEnumerator StartListening(NetworkConnection playerConn, int port)
    {
        // Establish the local endpoint for the socket.  
        // The DNS name of the computer  
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections
        listener.Bind(localEndPoint);
        listener.Listen(100);

        Debug.LogFormat("Now listening to player: {0}", playerConn.hostId);
        while (playerConn.isConnected)
        {
            // Only attempt to reconnect if disconnected.
            if (!listener.IsConnected()) {
                // Set the event to nonsignaled state.  
                //allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                Debug.LogFormat("Waiting for a connection ({0}:{1})...", ipAddress.ToString(), localEndPoint.Port.ToString());
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);
            }

            yield return null;
        }

        // Wait until a connection is made before continuing.  
        //allDone.WaitOne();
    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        //allDone.Set();
        Debug.Log("Connection found.");

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        StateObject state = new StateObject
        {
            workSocket = handler
        };
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.   
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read   
            // more data.  
            content = state.sb.ToString();
            Debug.LogFormat("Content received: {0}", content);
            if (content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the   
                // client. Display it on the console.  
                Debug.LogFormat("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);
                // Echo the data back to the client.  
                Send(handler, content);
            }
            else
            {
                // Not all data received. Get more.  
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
        }
    }

    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            Debug.LogFormat("Sent {0} bytes to client.", bytesSent);

            // Receieve more data
            // Create the state object.  
            StateObject state = new StateObject
            {
                workSocket = handler
            };
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);

            // End connection
            //handler.Shutdown(SocketShutdown.Both);
            //handler.Close();

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }
}
