using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace TsService
{
    public partial class Service1 : ServiceBase
    {
        private TcpListener server;
        private Thread thread;
        private volatile bool _running;
        private utilsNitgen utils;
        private static readonly SemaphoreSlim _captureQueue = new SemaphoreSlim(1, 1);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr phToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName,
            string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,
            bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment,
            string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        private const int MAXIMUM_ALLOWED = 0x02000000;
        private const uint CREATE_NEW_CONSOLE = 0x00000010;
        private const uint INFINITE = 0xFFFFFFFF;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _running = true;
            utils = new utilsNitgen();
            EventLog.WriteEntry("Serviço Iniciando", EventLogEntryType.Warning);
            thread = new Thread(Server);
            thread.Start();
            EventLog.WriteEntry("Serviço Iniciado", EventLogEntryType.Warning);
        }

        protected override void OnStop()
        {
            _running = false;
            if (server != null) server.Stop();
            thread.Join(5000);
        }

        private void DetectarEGerarIP()
        {
            string iniPath = @"C:\Windows\fingertechts.ini";
            if (!File.Exists(iniPath) || string.IsNullOrWhiteSpace(File.ReadAllText(iniPath)))
            {
                try
                {
                    var addr = Dns.GetHostEntry("").AddressList
                        .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                    string ip = addr != null ? addr.ToString() : null;
                    if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
                    File.WriteAllText(iniPath, ip);
                    EventLog.WriteEntry("IP detectado e salvo: " + ip, EventLogEntryType.Information);
                }
                catch (Exception e)
                {
                    File.WriteAllText(iniPath, "127.0.0.1");
                    EventLog.WriteEntry("Falha ao detectar IP, usando localhost: " + e.Message, EventLogEntryType.Warning);
                }
            }
        }

        private void Server()
        {
            try
            {
                DetectarEGerarIP();
                Int32 port = 13000;
                IPAddress ip = IPAddress.Parse(File.ReadAllText(@"C:\Windows\fingertechts.ini"));
                server = new TcpListener(ip, port);
                server.Start();

                while (_running)
                {
                    TcpClient client = server.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(ProcessClient, client);
                }
            }
            catch (SocketException)
            {
                if (_running)
                    EventLog.WriteEntry("Erro de socket no servidor", EventLogEntryType.Error);
            }
            finally
            {
                if (server != null) server.Stop();
            }
        }

        private void ProcessClient(object state)
        {
            TcpClient client = (TcpClient)state;
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] bytes = new byte[15000];
                int bytesRead = stream.Read(bytes, 0, bytes.Length);
                string data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytesRead);

                string resultado = null;

                switch (data)
                {
                    case "0":
                        resultado = ExecutarEnroll();
                        break;
                    case "1":
                        resultado = CapturarComFila(() => utils.Capturar());
                        break;
                }

                if (resultado != null)
                {
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(resultado);
                    stream.Write(msg, 0, msg.Length);
                }
                else
                {
                    byte[] errMsg = System.Text.Encoding.ASCII.GetBytes("ERRO:Falha na captura");
                    stream.Write(errMsg, 0, errMsg.Length);
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("Erro no processamento: " + e.Message, EventLogEntryType.Error);
            }
            finally
            {
                client.Close();
            }
        }

        private string ExecutarEnroll()
        {
            _captureQueue.Wait();
            try
            {
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FingertechEnroll.exe");
                if (!File.Exists(exePath))
                {
                    EventLog.WriteEntry("FingertechEnroll.exe não encontrado: " + exePath, EventLogEntryType.Error);
                    return null;
                }

                uint sessionId = WTSGetActiveConsoleSessionId();
                if (sessionId == 0xFFFFFFFF)
                {
                    EventLog.WriteEntry("Nenhuma sessão de console ativa", EventLogEntryType.Error);
                    return null;
                }

                IntPtr userToken = IntPtr.Zero;
                if (!WTSQueryUserToken(sessionId, out userToken) || userToken == IntPtr.Zero)
                {
                    EventLog.WriteEntry("Falha ao obter token do usuário, erro: " + Marshal.GetLastWin32Error(), EventLogEntryType.Error);
                    return null;
                }

                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

                bool created = CreateProcessAsUser(userToken, null, exePath,
                    IntPtr.Zero, IntPtr.Zero, false, 0,
                    IntPtr.Zero, null, ref si, out pi);

                if (!created)
                {
                    EventLog.WriteEntry("Falha ao criar processo, erro: " + Marshal.GetLastWin32Error(), EventLogEntryType.Error);
                    CloseHandle(userToken);
                    return null;
                }

                EventLog.WriteEntry("Enroll iniciado, PID=" + pi.dwProcessId + " sessão=" + sessionId, EventLogEntryType.Information);

                string resultFile = @"C:\Windows\Temp\fingertech_enroll_result.txt";
                string resultado = null;

                Process proc = Process.GetProcessById((int)pi.dwProcessId);
                proc.WaitForExit();
                EventLog.WriteEntry("Enroll processo finalizou", EventLogEntryType.Information);

                if (File.Exists(resultFile))
                {
                    resultado = File.ReadAllText(resultFile);
                    EventLog.WriteEntry("Resultado lido: " + (resultado != null ? resultado.Length + " bytes" : "null"), EventLogEntryType.Information);
                    File.Delete(resultFile);
                }
                else
                {
                    EventLog.WriteEntry("Arquivo resultado não encontrado: " + resultFile, EventLogEntryType.Warning);
                }

                CloseHandle(pi.hProcess);
                CloseHandle(pi.hThread);
                CloseHandle(userToken);

                return resultado;
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("Erro no enroll: " + e.Message + "\n" + e.StackTrace, EventLogEntryType.Error);
                return null;
            }
            finally
            {
                _captureQueue.Release();
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private string CapturarComFila(Func<string> captura)
        {
            _captureQueue.Wait();
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var task = Task.Run(captura, cts.Token);

                if (task.Wait(TimeSpan.FromSeconds(60)))
                    return task.Result;

                EventLog.WriteEntry("Timeout na captura", EventLogEntryType.Warning);
                return null;
            }
            finally
            {
                _captureQueue.Release();
            }
        }
    }
}
