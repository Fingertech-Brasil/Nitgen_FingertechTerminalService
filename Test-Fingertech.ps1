<#
.SYNOPSIS
    Testa a conexao TCP com o FingertechTerminalService e solicita captura de digital.

.DESCRIPTION
    Script para testar se o servico FingertechTS esta respondendo corretamente
    na rede. Conecta ao servidor TCP, envia comando e exibe o resultado.

.PARAMETER Server
    Endereco IP ou hostname do servidor onde o servico esta rodando.

.PARAMETER Port
    Porta TCP do servico. Default: 13000.

.PARAMETER Command
    Comando a enviar: "0" para Enroll, "1" para Captura. Default: "1".

.EXAMPLE
    .\Test-Fingertech.ps1 -Server 192.168.1.100

.EXAMPLE
    .\Test-Fingertech.ps1 -Server 192.168.1.100 -Command "0"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    [int]$Port = 13000,
    [string]$Command = "1"
)

$ErrorActionPreference = "Stop"

$cmdLabel = if ($Command -eq "0") { "Enroll" } else { "Captura" }

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " FingertechTerminalService - Teste TCP" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Servidor : $Server" -ForegroundColor Yellow
Write-Host "Porta    : $Port" -ForegroundColor Yellow
Write-Host "Comando  : $Command ($cmdLabel)" -ForegroundColor Yellow
Write-Host ""

$client = $null
try {
    Write-Host "Conectando..." -NoNewline
    $client = New-Object System.Net.Sockets.TcpClient
    $client.Connect($Server, $Port)
    Write-Host " OK" -ForegroundColor Green

    $stream = $client.GetStream()
    $stream.ReadTimeout = [System.Threading.Timeout]::Infinite

    $sendBytes = [System.Text.Encoding]::ASCII.GetBytes($Command)
    $stream.Write($sendBytes, 0, $sendBytes.Length)
    Write-Host "Comando '$Command' enviado. Aguardando resposta..." -ForegroundColor Yellow

    $buffer = New-Object byte[] 15000
    $bytesRead = $stream.Read($buffer, 0, $buffer.Length)

    if ($bytesRead -eq 0) {
        Write-Host "Servidor fechou a conexao sem resposta." -ForegroundColor Red
        exit 1
    }

    $response = [System.Text.Encoding]::ASCII.GetString($buffer, 0, $bytesRead)

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host " Resposta recebida ($bytesRead bytes)" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host $response
    Write-Host ""

    Write-Host "Teste concluido com sucesso!" -ForegroundColor Green
    exit 0
}
catch [System.Net.Sockets.SocketException] {
    Write-Host " FALHOU" -ForegroundColor Red
    Write-Host "Erro de socket: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Verifique se o servico esta rodando e a porta $Port esta liberada." -ForegroundColor Yellow
    exit 1
}
catch {
    Write-Host " FALHOU" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    if ($client -ne $null) { $client.Close() }
}
