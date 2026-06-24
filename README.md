# FingertechTerminalService

Serviço Windows para captura remota de impressões digitais usando dispositivos biométricos Nitgen.

## Descrição

Serviço Windows (.NET Framework 4.5.1) que funciona como servidor TCP multithread para captura de digitais. Permite que aplicações remotas solicitem a captura de impressões digitais via rede, utilizando dispositivos biométricos **Nitgen**.

## Características

- Serviço Windows com inicialização automática
- Servidor TCP multithread na porta 13000
- Fila de requisições com acesso exclusivo ao leitor (SemaphoreSlim)
- Comandos: `"0"` = Enroll (registro com UI), `"1"` = Captura simples
- Retorno em formato TextFIR
- Registro de eventos no Event Log do Windows
- Detecção automática de IP da máquina
- Firewall configurado automaticamente pelo instalador

## Pré-requisitos

- Windows 7/8/10/11 ou Server
- .NET Framework 4.5.1 ou superior
- Dispositivo biométrico Nitgen conectado
- Permissões de administrador (para instalação)

## Instalação

### Via Instalador MSI (recomendado)

1. Execute `FingertechTS.msi` como Administrador
2. O instalador vai:
   - Copiar os arquivos para `C:\Program Files\FingertechTS\`
   - Detectar o IP da máquina automaticamente
   - Criar `C:\Windows\fingertechts.ini` com o IP
   - Instalar e iniciar o serviço
   - Instalar o VC++ 2010 Runtime se necessário
   - Adicionar regra de firewall para porta 13000

### Via linha de comando

```cmd
:: Compilar
msbuild TsService.sln /p:Configuration=Release

:: Instalar (como Administrador)
cd TsService\bin\Release
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe TsService.exe
net start TsService
```

## Desinstalação

- **Painel de Controle** → Programas → FingertechTS → Desinstalar
- **Linha de comando:** `msiexec /x FingertechTS.msi`
- **Manual:** `net stop TsService` + `InstallUtil /u TsService.exe`

## Protocolo de Comunicação

| Comando | Descrição | Resposta |
|---------|-----------|----------|
| `"0"` | Enroll (registro com UI do Nitgen) | String TextFIR |
| `"1"` | Captura simples | String TextFIR |

### Teste rápido

```powershell
# Captura simples
.\Test-Fingertech.ps1 -Server 192.168.1.100

# Enroll (abre UI no servidor)
.\Test-Fingertech.ps1 -Server 192.168.1.100 -Command "0"
```

### Exemplo de Cliente (C#)

```csharp
TcpClient client = new TcpClient("192.168.1.100", 13000);
NetworkStream stream = client.GetStream();
byte[] cmd = Encoding.ASCII.GetBytes("1");
stream.Write(cmd, 0, cmd.Length);
byte[] buffer = new byte[15000];
int bytesRead = stream.Read(buffer, 0, buffer.Length);
string digital = Encoding.ASCII.GetString(buffer, 0, bytesRead);
client.Close();
```

### Exemplo de Cliente (Python)

```python
import socket

client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect(("192.168.1.100", 13000))
client.send(b"1")
digital = client.recv(15000).decode("ascii")
client.close()
```

## Estrutura do Projeto

```
FingertechTerminalService/
├── TsService/
│   ├── Program.cs                    # Ponto de entrada
│   ├── Service1.cs                   # Servidor TCP multithread
│   ├── utilsNitgen.cs                # Wrapper do SDK Nitgen
│   ├── ProjectInstaller.cs           # Instalador do serviço
│   ├── FingertechEnroll/
│   │   ├── Program.cs                # Binário de enroll (UI do Nitgen)
│   │   └── FingertechEnroll.csproj
│   └── TsService.csproj
├── Installer/
│   ├── Product.wxs                   # Definição WiX v5
│   ├── FirewallHelper/               # Exe para regras de firewall
│   ├── SdkDlls/                      # DLLs do SDK Nitgen
│   │   ├── NITGEN.SDK.NBioBSP.dll    # Wrapper managed (AnyCPU)
│   │   ├── x86/                      # DLLs nativas 32-bit
│   │   └── x64/                      # DLLs nativas 64-bit
│   └── Redist/                       # VC++ 2010 Runtime
├── Test-Fingertech.ps1               # Script de teste TCP
├── TsService.sln
└── README.md
```

## Compilar o Instalador

```cmd
cd Installer
dotnet build CustomAction.csproj -c Release
wix build Product.wxs -o bin\Release\FingertechTS.msi -d ServiceDir="..\TsService\bin\Release" -d EnrollDir="..\TsService\FingertechEnroll\bin\Release" -d SdkDir="SdkDlls" -d RedistDir="Redist" -d FirewallDir="FirewallHelper\bin\Release"
```

## Arquitetura

```
Cliente TCP → Conecta :13000 → Envia "0" ou "1"
    ↓
ThreadPool → Fila (SemaphoreSlim)
    ↓
Comando "1": utilsNitgen.Capturar() → OpenDevice → Capture → CloseDevice → TextFIR
Comando "0": ExecutarEnroll() → CreateProcessAsUser → FingertechEnroll.exe (UI) → TextFIR
    ↓
TextFIR retornado ao cliente
```

## Logs e Diagnóstico

Event Viewer → Logs do Windows → Aplicativo → Origem: **service1**

## Configuração

- **Porta:** editar `Service1.cs` linha `Int32 port = 13000`
- **IP:** arquivo `C:\Windows\fingertechts.ini` (automático via MSI)
- **Timeout captura:** 60 segundos (comando "1")
- **Timeout enroll:** sem limite (comando "0", aguarda UI)

## Observações

- **Segurança:** sem autenticação — usar firewall para restringir acesso
- **Firewall:** adicionado automaticamente pelo instalador
- **Device ID:** 255 (abre qualquer dispositivo Nitgen disponível)
- **Buffer:** máximo 15000 bytes por resposta
- **Enroll:** requer interação do usuário na tela do servidor (janela UI do Nitgen)
- **Compatibilidade:** AnyCPU — inclui DLLs nativas x86 e x64 do SDK Nitgen, o runtime carrega automaticamente a arquitetura correta

## Versão

**1.0.0.0** — .NET Framework 4.5.1
