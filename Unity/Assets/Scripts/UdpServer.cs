using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UdpServer : IDisposable
{
    Socket socket;
    EndPoint epDst;
    byte[] bufferData = new byte[1024];

    // Start is called before the first frame update
    public void Start(int localPort, string dstIP, int dstPort)
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, localPort));

            epDst = (EndPoint)new IPEndPoint(IPAddress.Parse(dstIP), dstPort);

            //开始异步接收数据
            socket.BeginReceive(bufferData, 0, bufferData.Length,
                SocketFlags.None, new AsyncCallback(ReceiveData), null);

            Debug.Log("Pad UdpServer启动成功 端口号:" + localPort);
        }
        catch (Exception e)
        {
            EventCenter.Broadcast("Error", e.ToString());
            Debug.LogError("UdpServer启动失败 端口号:" + localPort);
            Debug.LogError(e.ToString());
        }
    }

    /// <summary>
    /// 接收消息并存放 
    /// </summary>
    /// <param name="iar"></param>
    private void ReceiveData(IAsyncResult iar)
    {
        try
        {
            if (socket != null)
            {
                int length = socket.EndReceive(iar);
                //继续监听
                socket.BeginReceive(bufferData, 0, bufferData.Length,
                    SocketFlags.None, new AsyncCallback(ReceiveData), null);

                if (length > 0)
                {
                    byte[] data = new byte[length];
                    Buffer.BlockCopy(bufferData, 0, data, 0, length);
                    string recvStr = Encoding.UTF8.GetString(data, 0, data.Length);
                    EventCenter.Broadcast("Receive", recvStr);
                    //Debug.Log($"收到PC端发过来的数据:{recvStr}");
                }
            }
            else
            {
                Debug.LogError("serverSocket == null");
                EventCenter.Broadcast("Error", "serverSocket == null");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("接收消息出错");
            //Debug.LogException(e);
        }
    }

    public void Send(string sendStr)
    {
        try
        {
            //发送给指定客户端
            byte[] sendData = Encoding.UTF8.GetBytes(sendStr);
            socket.SendTo(sendData, epDst);
        }
        catch(Exception e)
        {
            EventCenter.Broadcast("Error", e.ToString());
        }
    }

    public void Dispose()
    {
        // 关闭socket
        if (socket != null)
        {
            try
            {
                socket.Close();
                socket.Dispose();
                socket = null;
                GC.Collect();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        epDst = null;

        Debug.Log("关闭server");
    }
}