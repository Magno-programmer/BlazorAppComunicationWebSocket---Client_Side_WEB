using BlazorAppComunicationWebSocket.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlazorAppComunicationWebSocket.Services
{
    public class WebSocketService
    {
        
        private ClientWebSocket? _webSocket;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        public ClientWebSocket WebSocket => _webSocket!;
        public string? name;
        public int option;
        public bool isConnect = false;

        // Public list of messages
        public List<string> Messages { get; private set; } = [];


        // Public list of messages
        public List<string> ConsumerList { get; private set; } = [];

        public event Action? OnMessageReceived;

        private readonly NavigationManager _navigationManager;

        public WebSocketService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        private void NotifyMessageReceived()
        {
            try
            {
                OnMessageReceived?.Invoke(); // Fires the event if there are subscribers
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error no NotifyMessageReceived: {ex.Message}");
            }
        
        }

        public async Task ConnectAsync()
        {
            try
            { 
                // Obter o IP diretamente da URL base do navegador
                var baseUrl = new Uri(_navigationManager.BaseUri);
                string ipAddress = baseUrl.Host;

                // Construir a URL com o IP como parâmetro de consulta
                string _url = $"ws://{ipAddress}:5139/?ip={ipAddress}";
                Console.WriteLine($"Conectando ao WebSocket com IP: {ipAddress}");

                if (_webSocket?.State != WebSocketState.Open && name != string.Empty)
                {
                    _webSocket = new ClientWebSocket();
                    await _webSocket.ConnectAsync(new Uri(_url), CancellationToken.None);
                    isConnect = true;
                } 

            }
            catch (WebSocketException wex)
            {
                isConnect = false;
                Console.WriteLine($"Error no WebSocket ConnectAsync: {wex.Message}");
            }
            catch (Exception ex)
            {
                isConnect = false;
                Console.WriteLine($"Error no ConnectAsync: {ex.Message}");
            }
        }
        public async Task SendMessageAsync(string message)
        {
            try
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
                }

            }
            catch (WebSocketException wex)
            {
                Console.WriteLine($"Error no WebSocket SendMessageAsync: {wex.Message}");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error no SendMessageAsync: {ex.Message}");
            }
        }

        public async Task SendMessageInByteAsync(byte[] message)
        {
            try 
            { 

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true, _cancellationTokenSource.Token);
                }

            }
            catch (WebSocketException wex)
            {
                Console.WriteLine($"Error no WebSocket SendMessageInByteAsync: {wex.Message}");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error no SendMessageInByteAsync: {ex.Message}");
            }
        }

        public async Task ReceivingMessage()
        {
            try
            {
                var buffer = new byte[2048 * 1024];
                var result = await _webSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                List<Consumidor> consumidores;

                if (message.StartsWith("JSON: "))
                {

                    message = message.Replace("JSON: ", "");

                    // Desserializa a resposta JSON para a lista de consumidores
                    consumidores = JsonSerializer.Deserialize<List<Consumidor>>(message) ?? new List<Consumidor>();
                    if (consumidores.Any())
                    {
                        foreach(var consumidor in consumidores)
                        {
                            // Store received messages in the list
                            ConsumerList.Add($"{consumidor.nm_consumidor} - {consumidor.nr_documento}");
                            NotifyMessageReceived();
                        }
                    }
                    else
                    {
                        ConsumerList.Add("Nenhum consumidor encontrado.");
                        NotifyMessageReceived();
                    }
                }
                else if (IsBase64String(message))
                {
                    Console.WriteLine("***************************\nA Mensagem é base64\n*************************");
                    // Store received messages in the list
                    Messages.Add(message);
                    NotifyMessageReceived();
                }
                else
                {
                    // Store received messages in the list
                    Messages.Add(message);
                    NotifyMessageReceived();
                }


            }
            catch (WebSocketException wex)
            {
                Console.WriteLine($"Error no WebSocket ReceivingMessage: {wex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error no ReceivingMessage: {ex.Message}");
            }
        }
        internal static bool IsBase64String(string base64)
        {
            // Check if a string is Base64 encoded
            base64 = base64.Trim();
            return (base64.Length % 4 == 0) && Regex.IsMatch(base64, @"^[a-zA-Z0-9\+/]*={0,2}$", RegexOptions.None);
        }

        public async Task DisposeAsync()
        {
            try
            {
                await _webSocket!.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (WebSocketException wex)
            {
                Console.WriteLine($"Error no WebSocket DisposeAsync: {wex.Message}");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error no DisposeAsync: {ex.Message}");
            }
}
    }
}