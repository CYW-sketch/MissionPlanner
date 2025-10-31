using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;

// dns, ip address
// tcplistner

namespace MissionPlanner.Comms
{
    public class UdpSerial : CommsBase, ICommsSerial, IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // 全局注册：记录本进程内已绑定的本地端口，便于在手动连接前释放，手动断开时统一释放
        private static readonly object _registrySync = new object();
        private static readonly Dictionary<int, List<UdpSerial>> _portToInstances = new Dictionary<int, List<UdpSerial>>();

        public readonly List<IPEndPoint> EndPointList = new List<IPEndPoint>();

        private bool _isopen;

        public bool CancelConnect = false;
        /// <summary>
        /// add to EndPointList if need when injecting
        /// </summary>
        public UdpClient client = new UdpClient();

        private MemoryStream rbuffer = new MemoryStream();

        /// <summary>
        ///     this is the remote endpoint we send messages too. this class does not support multiple remote endpoints.
        /// </summary>
        public IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public string ConfigRef { get; set; } = "";

        public UdpSerial()
        {
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Port = "14550";
            ReadTimeout = 500;
        }

        public UdpSerial(UdpClient client)
        {
            this.client = client;
            _isopen = true;
            ReadTimeout = 500;
        }

        public string Port { get; set; }
        public bool SuppressPrompts { get; set; }

        public int WriteBufferSize { get; set; }
        public int WriteTimeout { get; set; }
        public bool RtsEnable { get; set; }
        public Stream BaseStream => new UDPStream(this);

        public void toggleDTR()
        {
        }

        public int ReadTimeout
        {
            get; // { return client.ReceiveTimeout; }
            set; // { client.ReceiveTimeout = value; }
        }

        public int ReadBufferSize { get; set; }

        public int BaudRate { get; set; }

        public int DataBits { get; set; }

        public string PortName
        {
            get => "UDP" + Port;
            set { }
        }

        public int BytesToRead => (int)(client.Available + rbuffer.Length - rbuffer.Position);

        public int BytesToWrite => 0;

        public bool IsOpen
        {
            get
            {
                if (client?.Client == null) return false;
                return _isopen;
            }
            set => _isopen = value;
        }

        public bool DtrEnable { get; set; }

        public void Open()
        {
            // 确保client对象不为null
            if (client == null)
            {
                client = new UdpClient();
            }
            
            // 防御式判空：client 或 client.Client 可能因上一次 Close()/Dispose() 为 null
            if (((client != null && client.Client != null) && client.Client.Connected) || IsOpen)
            {
                log.Info("该端口套接字已开启");
                return;
            }

            client.Close();

            var dest = Port;

            if (!SuppressPrompts)
            {
                dest = OnSettings("UDP_port" + ConfigRef, dest);
                if (inputboxreturn.Cancel == OnInputBoxShow("Listern Port",
                        "Enter Local port (ensure remote end is already sending)", ref dest)) return;
                Port = dest;
                OnSettings("UDP_port" + ConfigRef, Port, true);
            }

            //######################################

            try
            {
                if (client != null) client.Close();
            }
            catch
            {
            }

            // 在绑定前，确保本进程内相同端口的其它 UDP 已释放（满足“手动连接前先检查目标端口是否绑定”）
            var localPort = int.Parse(Port);
            CloseExistingOnPort(localPort);

            client = new UdpClient(localPort);

            // 注册到全局映射
            RegisterInstance(localPort, this);

            // 绑定成功后即标记为已打开，避免上层误判连接失败
            _isopen = true;

            // 非阻塞地尝试获取远端端点（可选）
            try
            {
                if (client.Available > 0)
                {
                    RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var b = client.Receive(ref RemoteIpEndPoint);
                    EndPointList.Add(RemoteIpEndPoint);
                    // 将已读取的数据放回读缓存，避免丢包
                    lock (rbuffer)
                    {
                        var pos = rbuffer.Position;
                        rbuffer.Seek(0, SeekOrigin.End);
                        rbuffer.Write(b, 0, b.Length);
                        rbuffer.Seek(pos, SeekOrigin.Begin);
                    }
                    log.InfoFormat("UDPSerial connecting to {0} : {1}", RemoteIpEndPoint.Address, RemoteIpEndPoint.Port);
                }
            }
            catch
            {
            }
        }

        public int Read(byte[] readto, int offset, int length)
        {
            // 在切换或关闭阶段，可能被调用到，此时直接返回，避免抛异常打扰用户
            if (!IsOpen || client == null || client.Client == null)
                return 0;
            VerifyConnected();
            if (length < 1) return 0;

            var deadline = DateTime.Now.AddMilliseconds(ReadTimeout);

            lock (rbuffer)
            {
                if (rbuffer.Position == rbuffer.Length)
                    rbuffer.SetLength(0);

                var position = rbuffer.Position;

                while ((rbuffer.Length - rbuffer.Position) < length && DateTime.Now < deadline)
                {
                    // read more
                    while (client.Available > 0 && (rbuffer.Length - rbuffer.Position) < length)
                    {
                        var currentRemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        // assumes the udp packets are mavlink aligned, if we are receiving from more than one source
                        var b = client.Receive(ref currentRemoteIpEndPoint);
                        rbuffer.Seek(0, SeekOrigin.End);
                        rbuffer.Write(b, 0, b.Length);
                        rbuffer.Seek(position, SeekOrigin.Begin);

                        if (!EndPointList.Contains(currentRemoteIpEndPoint))
                            EndPointList.Add(currentRemoteIpEndPoint);
                    }

                    Thread.Yield();
                }

                // prevent read past end of array
                if (rbuffer.Length - rbuffer.Position < length)
                    length = (int)(rbuffer.Length - rbuffer.Position);

                return rbuffer.Read(readto, offset, length);
            }
        }

        public int ReadByte()
        {
            if (!IsOpen || client == null || client.Client == null)
                return -1;
            VerifyConnected();
            var count = 0;
            while (BytesToRead == 0)
            {
                Thread.Sleep(1);
                if (count > ReadTimeout)
                    throw new Exception("NetSerial Timeout on read");
                count++;
            }

            var buffer = new byte[1];
            Read(buffer, 0, 1);
            return buffer[0];
        }

        public int ReadChar()
        {
            return ReadByte();
        }

        public string ReadExisting()
        {
            if (!IsOpen || client == null || client.Client == null)
                return string.Empty;
            VerifyConnected();
            var data = new byte[client.Available];
            if (data.Length > 0)
                Read(data, 0, data.Length);

            var line = Encoding.ASCII.GetString(data, 0, data.Length);

            return line;
        }

        public void WriteLine(string line)
        {
            if (!IsOpen || client == null || client.Client == null)
                return;
            VerifyConnected();
            line = line + "\n";
            Write(line);
        }

        public void Write(string line)
        {
            if (!IsOpen || client == null || client.Client == null)
                return;
            VerifyConnected();
            var data = new ASCIIEncoding().GetBytes(line);
            Write(data, 0, data.Length);
        }

        public void Write(byte[] write, int offset, int length)
        {
            if (!IsOpen || client == null || client.Client == null)
                return;
            VerifyConnected();
            // this is not ideal. but works
            foreach (var ipEndPoint in EndPointList)
                try
                {
                    client.Send(write, length, ipEndPoint);
                }
                catch
                {
                } //throw new Exception("Comport / Socket Closed"); }
        }

        public void DiscardInBuffer()
        {
            if (!IsOpen || client == null || client.Client == null)
                return;
            VerifyConnected();
            var size = client.Available;
            var crap = new byte[size];
            log.InfoFormat("UdpSerial DiscardInBuffer {0}", size);
            Read(crap, 0, size);
        }

        public string ReadLine()
        {
            var temp = new byte[4000];
            var count = 0;
            var timeout = 0;

            while (timeout <= 100)
            {
                if (!IsOpen) break;
                if (BytesToRead > 0)
                {
                    var letter = (byte) ReadByte();

                    temp[count] = letter;

                    if (letter == '\n') // normal line
                        break;

                    count++;
                    if (count == temp.Length)
                        break;
                    timeout = 0;
                }
                else
                {
                    timeout++;
                    Thread.Sleep(5);
                }
            }

            Array.Resize(ref temp, count + 1);

            return Encoding.ASCII.GetString(temp, 0, temp.Length);
        }

        public void Close()
        {
            _isopen = false;
            try
            {
                // 从全局映射注销
                UnregisterInstanceSafe();
            }
            catch { }

            if (client != null) client.Close();

            client = new UdpClient();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void VerifyConnected()
        {
            if (client == null || !IsOpen)
            {
                Close();
                throw new Exception("The socket/serialproxy is closed");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                Close();
                client = null;
            }

            // free native resources
        }

        // —— 端口占用管理 ——
        private static void RegisterInstance(int port, UdpSerial instance)
        {
            lock (_registrySync)
            {
                if (!_portToInstances.TryGetValue(port, out var list))
                {
                    list = new List<UdpSerial>();
                    _portToInstances[port] = list;
                }
                if (!list.Contains(instance))
                    list.Add(instance);
            }
        }

        private void UnregisterInstanceSafe()
        {
            try
            {
                if (int.TryParse(Port, out var p))
                {
                    lock (_registrySync)
                    {
                        if (_portToInstances.TryGetValue(p, out var list))
                        {
                            list.Remove(this);
                            if (list.Count == 0)
                                _portToInstances.Remove(p);
                        }
                    }
                }
            }
            catch { }
        }

        public static void CloseExistingOnPort(int port)
        {
            List<UdpSerial> toClose = null;
            lock (_registrySync)
            {
                if (_portToInstances.TryGetValue(port, out var list))
                {
                    toClose = new List<UdpSerial>(list);
                }
            }

            if (toClose != null)
            {
                foreach (var inst in toClose)
                {
                    try { inst.Close(); } catch { }
                }
            }
        }

        public static void ReleaseAll()
        {
            List<UdpSerial> all;
            lock (_registrySync)
            {
                var tmp = new List<UdpSerial>();
                foreach (var kv in _portToInstances)
                {
                    tmp.AddRange(kv.Value);
                }
                all = tmp;
            }

            foreach (var inst in all)
            {
                try { inst.Close(); } catch { }
            }

            lock (_registrySync)
            {
                _portToInstances.Clear();
            }
        }
    }
}