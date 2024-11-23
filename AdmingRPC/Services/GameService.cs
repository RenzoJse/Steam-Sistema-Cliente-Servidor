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

    public override Task AddGameInteractive(
        IAsyncStreamReader<GameData> requestStream,
        IServerStreamWriter<ServerResponse> responseStream,
        ServerCallContext context)
    {
        return AddGameInteractiveInternal(requestStream, responseStream, context);
    }

    private async Task AddGameInteractiveInternal(
        IAsyncStreamReader<GameData> requestStream,
        IServerStreamWriter<ServerResponse> responseStream,
        ServerCallContext context)
    {
        var gameData = new Dictionary<string, string>();

        try
        {
            while (await requestStream.MoveNext())
            {
                var request = requestStream.Current;

                if (string.IsNullOrEmpty(request.Key) || string.IsNullOrEmpty(request.Value))
                {
                    await SafeWriteAsync(responseStream, "Error: Both key and value must be provided.");
                    return;
                }

                var key = request.Key.ToLower();
                var value = request.Value;

                switch (key)
                {
                    case "name":
                        if (GameManager.DoesGameExist(value))
                        {
                            await SafeWriteAsync(responseStream, "Error: That game already exists.");
                            return;
                        }
                        break;
                    case "releasedate":
                        if (!DateTime.TryParseExact(value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                        {
                            await SafeWriteAsync(responseStream, "Error: Invalid date format. Use dd/MM/yyyy.");
                            return;
                        }
                        break;
                    case "unitsavailable":
                    case "price":
                        if (!int.TryParse(value, out _))
                        {
                            await SafeWriteAsync(responseStream, $"Error: {key} must be a valid integer.");
                            return;
                        }
                        break;
                    case "username":
                        if (UserManager.GetUserByUsername(value) == null)
                        {
                            await SafeWriteAsync(responseStream, "Error: User not found. Please verify that the user is registered.");
                            return;
                        }
                        break;
                }

                gameData[key] = value;
                await SafeWriteAsync(responseStream, $"{key} received successfully.");
            }

            if (gameData.TryGetValue("name", out var name) &&
                gameData.TryGetValue("genre", out var genre) &&
                gameData.TryGetValue("releasedate", out var releaseDateStr) &&
                gameData.TryGetValue("platform", out var platform) &&
                gameData.TryGetValue("unitsavailable", out var unitsAvailableStr) &&
                gameData.TryGetValue("price", out var priceStr) &&
                gameData.TryGetValue("username", out var username) &&
                DateTime.TryParseExact(releaseDateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var releaseDate) &&
                int.TryParse(unitsAvailableStr, out var unitsAvailable) &&
                int.TryParse(priceStr, out var price))
            {
                var user = UserManager.GetUserByUsername(username);
                var newGame = GameManager.CreateNewGame(name, genre, releaseDate, platform, unitsAvailable, price, 0, user);
                UserManager.PublishGame(newGame, user);

                await SafeWriteAsync(responseStream, $"Game '{name}' added and published successfully.");
                _logger.LogInformation($"Game '{name}' was successfully added and published.");
            }
            else
            {
                await SafeWriteAsync(responseStream, "Error: Missing or invalid game data.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AddGameInteractive.");
            await SafeWriteAsync(responseStream, $"Error: {ex.Message}");
        }
    }

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

    public override Task<RemoveGameResponse> RemoveGame(RemoveGameRequest request, ServerCallContext context)
    {
        return Task.FromResult(RemoveGameInternal(request));
    }

    private RemoveGameResponse RemoveGameInternal(RemoveGameRequest request)
    {
        try
        {
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

    public override Task<ModifyGameResponse> ModifyGame(ModifyGameRequest request, ServerCallContext context)
    {
        return Task.FromResult(ModifyGameInternal(request));
    }

    private ModifyGameResponse ModifyGameInternal(ModifyGameRequest request)
    {
        try
        {
            if (request.Field.ToLower() == "check")
            {
                var gameExists = GameManager.DoesGameExist(request.GameName);
                if (!gameExists)
                {
                    return new ModifyGameResponse
                    {
                        Message = $"Error: Game '{request.GameName}' not found."
                    };
                }
                return new ModifyGameResponse
                {
                    Message = $"Game '{request.GameName}' exists."
                };
            }

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

    public override Task<GetGameReviewsResponse> GetGameReviews(GetGameReviewsRequest request, ServerCallContext context)
    {
        return Task.FromResult(GetGameReviewsInternal(request));
    }

    private GetGameReviewsResponse GetGameReviewsInternal(GetGameReviewsRequest request)
    {
        try
        {
            var game = GameManager.GetGameByName(request.GameName);

            if (game == null)
            {
                return new GetGameReviewsResponse
                {
                    Message = $"Error: Game '{request.GameName}' not found."
                };
            }

            if (game.Reviews.Count == 0)
            {
                return new GetGameReviewsResponse
                {
                    Message = $"No reviews found for the game '{request.GameName}'."
                };
            }

            var response = new GetGameReviewsResponse
            {
                Message = $"Reviews for the game '{request.GameName}':"
            };

            foreach (var review in game.Reviews)
            {
                response.Reviews.Add(new GameReview
                {
                    Valoration = review.Valoration,
                    Description = review.Description
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GetGameReviews.");
            return new GetGameReviewsResponse
            {
                Message = $"Error: {ex.Message}"
            };
        }
    }
}
