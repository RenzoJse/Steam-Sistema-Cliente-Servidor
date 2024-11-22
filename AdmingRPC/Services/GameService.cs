using AdmingRPC;
using Comunicacion.Dominio;
using Grpc.Core;
using ServerApp.Dominio;
using System;
using System.Collections.Generic;

public class GameManagementService : GameManagement.GameManagementBase
{
    private readonly ILogger<GameManagementService> _logger;

    public GameManagementService(ILogger<GameManagementService> logger)
    {
        _logger = logger;
    }

    public override async Task AddGameInteractive(
        IAsyncStreamReader<GameData> requestStream,
        IServerStreamWriter<ServerResponse> responseStream,
        ServerCallContext context)
    {
        var gameData = new Dictionary<string, string>();

        try
        {
            // Recolectar datos enviados por el cliente
            while (await requestStream.MoveNext())
            {
                var request = requestStream.Current;

                if (!string.IsNullOrEmpty(request.Key) && !string.IsNullOrEmpty(request.Value))
                {
                    gameData[request.Key.ToLower()] = request.Value;

                    // Enviar respuesta por cada dato recibido
                    if (!context.CancellationToken.IsCancellationRequested)
                    {
                        await responseStream.WriteAsync(new ServerResponse
                        {
                            Message = $"{request.Key} received successfully."
                        });
                    }
                }
            }

            // Validar que todos los datos requeridos estén presentes
            if (gameData.TryGetValue("name", out var name) &&
                gameData.TryGetValue("genre", out var genre) &&
                gameData.TryGetValue("releasedate", out var releaseDate) &&
                gameData.TryGetValue("platform", out var platform) &&
                gameData.TryGetValue("unitsavailable", out var unitsAvailableStr) &&
                gameData.TryGetValue("price", out var priceStr) &&
                gameData.TryGetValue("username", out var username) &&
                DateTime.TryParseExact(releaseDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var releaseDateParsed) &&
                int.TryParse(unitsAvailableStr, out var unitsAvailable) &&
                int.TryParse(priceStr, out var price))
            {
                // Validar si el usuario existe
                var user = UserManager.GetUserByUsername(username);
                if (user == null)
                {
                    await SafeWriteAsync(responseStream, "Error: User not found.");
                    
                }

                // Validar si el juego ya existe
                if (GameManager.DoesGameExist(name))
                {
                    await SafeWriteAsync(responseStream, "Error: That game already exists.");
                    return;
                }

                // Crear y publicar el juego
                var newGame = GameManager.CreateNewGame(name, genre, releaseDateParsed, platform, unitsAvailable, price, 0, user);
                UserManager.PublishGame(newGame, user);

                await SafeWriteAsync(responseStream, $"Game '{name}' added and published successfully by '{username}'.");
            }
            else
            {
                await SafeWriteAsync(responseStream, "Error: Invalid game data. Please ensure all fields are valid.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AddGameInteractive.");
            await SafeWriteAsync(responseStream, $"Error: {ex.Message}");
        }
    }

    // Método auxiliar para escribir en el flujo de respuesta de manera segura
    private static async Task SafeWriteAsync(IServerStreamWriter<ServerResponse> responseStream, string message)
    {
        try
        {
            await responseStream.WriteAsync(new ServerResponse { Message = message });
        }
        catch (InvalidOperationException)
        {
            // Ignorar la excepción si el flujo ya está completo
        }
    }

    public override async Task<RemoveGameResponse> RemoveGame(RemoveGameRequest request, ServerCallContext context)
    {
        try
        {
            // Intentar remover el juego
            var gameName = request.GameName;

            if (GameManager.DoesGameExist(gameName))
            {
                GameManager.RemoveGame(gameName);
                return new RemoveGameResponse
                {
                    Message = $"Game '{gameName}' removed successfully."
                };
            }
            else
            {
                return new RemoveGameResponse
                {
                    Message = $"Error: Game '{gameName}' not found."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RemoveGame.");
            return new RemoveGameResponse
            {
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<ModifyGameResponse> ModifyGame(ModifyGameRequest request, ServerCallContext context)
    {
        try
        {
            var game = GameManager.GetGameByName(request.GameName);
            if (game == null)
            {
                return new ModifyGameResponse
                {
                    Message = $"Error: Game '{request.GameName}' not found."
                };
            }

            switch (request.Field.ToLower())
            {
                case "name":
                    game.Name = request.NewValue;
                    break;
                case "genre":
                    game.Genre = request.NewValue;
                    break;
                case "release date":
                    if (DateTime.TryParse(request.NewValue, out var newReleaseDate))
                        game.ReleaseDate = newReleaseDate;
                    else
                        return new ModifyGameResponse
                        {
                            Message = "Error: Invalid date format."
                        };
                    break;
                case "platform":
                    game.Platform = request.NewValue;
                    break;
                case "units available":
                    if (int.TryParse(request.NewValue, out var newUnitsAvailable))
                        game.UnitsAvailable = newUnitsAvailable;
                    else
                        return new ModifyGameResponse
                        {
                            Message = "Error: Invalid number format for units available."
                        };
                    break;
                case "price":
                    if (int.TryParse(request.NewValue, out var newPrice))
                        game.Price = newPrice;
                    else
                        return new ModifyGameResponse
                        {
                            Message = "Error: Invalid number format for price."
                        };
                    break;
                default:
                    return new ModifyGameResponse
                    {
                        Message = "Error: Invalid field to modify."
                    };
            }

            return new ModifyGameResponse
            {
                Message = $"Game '{request.GameName}' modified successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ModifyGame.");
            return new ModifyGameResponse
            {
                Message = $"Error: {ex.Message}"
            };
        }
    }

}
