# FingertechTerminalService

Serviço Windows para captura remota de impressões digitais usando dispositivos biométricos Nitgen.

## 📋 Descrição

**FingertechTerminalService** é um serviço Windows (Windows Service) desenvolvido em .NET Framework 4.5.1 que funciona como um servidor TCP para captura de digitais. Ele permite que aplicações remotas solicitem a captura de impressões digitais através de conexão de rede, utilizando dispositivos biométricos **Nitgen**.

## 🎯 Características

- ✅ Serviço Windows com inicialização automática
- ✅ Servidor TCP na porta 13000
- ✅ Integração com SDK Nitgen para leitores biométricos
- ✅ Retorno de dados em formato TextFIR
- ✅ Registro de eventos no Event Log do Windows
- ✅ Comunicação via protocolo TCP/IP simples

## 🛠️ Tecnologias Utilizadas

- **.NET Framework 4.5.1**
- **C#**
- **Windows Services**
- **TCP/IP Sockets**
- **Nitgen SDK** (NITGEN.SDK.NBioBSP)

## 📋 Pré-requisitos

Antes de instalar e executar o serviço, certifique-se de ter:

1. **Windows** (7, 8, 10, 11 ou Server)
2. **.NET Framework 4.5.1** ou superior instalado
3. **SDK Nitgen** (NITGEN.SDK.NBioBSP) instalado
4. **Dispositivo biométrico Nitgen** conectado ao computador
5. Permissões de administrador para instalação do serviço

## 📦 Instalação

### 1. Compilar o Projeto

Abra o projeto no Visual Studio e compile em modo **Release**:

```
Build → Build Solution (Ctrl+Shift+B)
```

### 2. Configurar o Arquivo de IP

Crie o arquivo de configuração em `C:\Windows\fingertechts.ini` contendo o endereço IP onde o servidor irá escutar:

```
192.168.1.100
```

> **Nota:** Substitua pelo IP da máquina onde o serviço será executado.

### 3. Instalar o Serviço

Abra o **Prompt de Comando como Administrador** e navegue até a pasta onde está o executável compilado:

```cmd
cd D:\Envio\Devices\Hamster\Software\rdp\FingertechTerminalService-master\TsService\bin\Release
```

Instale o serviço usando o InstallUtil:

```cmd
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe TsService.exe
```

### 4. Iniciar o Serviço

Via linha de comando:

```cmd
net start TsService
```

Ou pelo **Gerenciador de Serviços** do Windows:
1. Pressione `Win + R` e digite `services.msc`
2. Localize o serviço **FingertechTs**
3. Clique com o botão direito e selecione **Iniciar**

## 🚀 Como Usar

### Protocolo de Comunicação

O serviço escuta conexões TCP na **porta 13000**. O protocolo é simples baseado em comandos ASCII:

| Comando | Descrição | Resposta |
|---------|-----------|----------|
| `1` | Captura impressão digital | String TextFIR com os dados da digital |

### Exemplo de Cliente (C#)

```csharp
using System;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        try
        {
            // Conecta ao servidor
            TcpClient client = new TcpClient("192.168.1.100", 13000);
            NetworkStream stream = client.GetStream();

            // Envia comando de captura
            byte[] comando = Encoding.ASCII.GetBytes("1");
            stream.Write(comando, 0, comando.Length);

            // Recebe a digital
            byte[] buffer = new byte[15000];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string digital = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            Console.WriteLine("Digital capturada:");
            Console.WriteLine(digital);

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro: " + ex.Message);
        }
    }
}
```

### Exemplo de Cliente (Python)

```python
import socket

def capturar_digital(ip, porta=13000):
    try:
        # Conecta ao servidor
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client.connect((ip, porta))
        
        # Envia comando de captura
        client.send(b'1')
        
        # Recebe a digital
        digital = client.recv(15000).decode('ascii')
        
        client.close()
        return digital
    except Exception as e:
        print(f"Erro: {e}")
        return None

# Uso
digital = capturar_digital('192.168.1.100')
print(f"Digital capturada: {digital}")
```

### Exemplo de Cliente (Node.js)

```javascript
const net = require('net');

function capturarDigital(ip, porta = 13000) {
    return new Promise((resolve, reject) => {
        const client = new net.Socket();
        
        client.connect(porta, ip, () => {
            console.log('Conectado ao servidor');
            client.write('1');
        });
        
        client.on('data', (data) => {
            resolve(data.toString('ascii'));
            client.destroy();
        });
        
        client.on('error', (err) => {
            reject(err);
        });
    });
}

// Uso
capturarDigital('192.168.1.100')
    .then(digital => console.log('Digital:', digital))
    .catch(err => console.error('Erro:', err));
```

## 🔧 Estrutura do Projeto

```
FingertechTerminalService/
├── TsService/
│   ├── Program.cs                      # Ponto de entrada do serviço
│   ├── Service1.cs                     # Implementação do servidor TCP
│   ├── Service1.Designer.cs            # Designer do serviço
│   ├── utilsNitgen.cs                  # Classe para comunicação com Nitgen
│   ├── ProjectInstaller.cs             # Instalador do serviço
│   ├── ProjectInstaller.Designer.cs    # Designer do instalador
│   ├── App.config                      # Configuração da aplicação
│   └── Properties/
│       └── AssemblyInfo.cs             # Informações do assembly
└── README.md
```

## 📊 Fluxo de Funcionamento

```
┌─────────────┐                ┌──────────────┐                ┌─────────────────┐
│   Cliente   │───Conecta────▶ │  TsService   │────Comando────▶│ Leitor Nitgen   │
│   TCP/IP    │    Porta       │  (Servidor)  │    Captura     │   Biométrico    │
└─────────────┘    13000       └──────────────┘                └─────────────────┘
       ▲                               │                               │
       │                               │                               │
       └────────TextFIR────────────────┴───────────Digital─────────────┘
```

## 🔍 Logs e Diagnóstico

O serviço registra eventos no **Event Log** do Windows. Para visualizar:

1. Pressione `Win + R` e digite `eventvwr`
2. Navegue até **Logs do Windows → Aplicativo**
3. Procure por eventos da origem **TsService** ou **service1**

### Eventos Registrados:

- ✅ Inicialização do serviço
- ✅ Início do servidor TCP
- ✅ Conexões de clientes
- ✅ Capturas de digitais
- ❌ Erros de socket
- ❌ Interrupções do serviço

## 🛑 Parar e Desinstalar

### Parar o Serviço

```cmd
net stop TsService
```

### Desinstalar o Serviço

```cmd
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /u TsService.exe
```

## ⚙️ Configuração

### Alterar a Porta TCP

Edite o arquivo `Service1.cs`, linha onde está definida a porta:

```csharp
Int32 port = 13000;  // Altere para a porta desejada
```

### Alterar o Caminho do Arquivo de Configuração

Edite o arquivo `Service1.cs`, linha onde o IP é lido:

```csharp
IPAddress ip = IPAddress.Parse(File.ReadAllText(@"C:\Windows\fingertechts.ini"));
```

## ⚠️ Observações Importantes

- ⚠️ **Segurança:** Este serviço não possui autenticação. Qualquer cliente que se conecte pode solicitar captura de digitais
- ⚠️ **Firewall:** Certifique-se de liberar a porta 13000 no firewall do Windows
- ⚠️ **Processamento Serial:** O servidor processa uma requisição por vez
- ⚠️ **Device ID:** O código usa o device ID 255 (abre qualquer dispositivo Nitgen disponível)
- ⚠️ **Buffer:** Tamanho máximo de resposta é 15000 bytes

## 🔒 Recomendações de Segurança

Para ambientes de produção, considere:

1. Implementar autenticação de clientes
2. Usar SSL/TLS para criptografar a comunicação
3. Adicionar rate limiting para prevenir abuso
4. Restringir IPs permitidos via firewall
5. Implementar logging mais robusto

## 📧 Suporte

Para problemas ou dúvidas:

1. Verifique os logs do Event Viewer
2. Confirme que o dispositivo Nitgen está conectado
3. Verifique se o SDK Nitgen está instalado corretamente
4. Confirme que o arquivo `C:\Windows\fingertechts.ini` existe e contém um IP válido

## 🔄 Versão

**Versão Atual:** 1.0.0.0  
**Data:** 2019  
**Framework:** .NET Framework 4.5.1

---

**Desenvolvido para integração com dispositivos biométricos Nitgen em ambientes Windows**
