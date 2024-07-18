﻿using Newtonsoft.Json;
using Serilog;
using super_tic_tac_toe_api.Entities;
using super_tic_tac_toe_api.Helpers;
using super_tic_tac_toe_api.Logic.Enums;
using super_tic_tac_toe_api.Models;
using super_tic_tac_toe_api.Services.Interfaces;

namespace super_tic_tac_toe_api.Services
{
    public class LobbyService : ILobbyService
    {
        private static List<Lobby> lobbies = new List<Lobby>();

        public string CreateLobby()
        {
            Log.Information("Creating a new lobby");

            var lobby = new Lobby();
            lobbies.Add(lobby);

            Log.Information("Lobby created with ID {LobbyId}", lobby.LobbyId);
            return JsonConvert.SerializeObject(new { LobbyId = lobby.LobbyId });
        }

        public string JoinLobby(JoinLobbyRequest request)
        {
            Log.Information("Player {PlayerName} is attempting to join lobby {LobbyId}", request.PlayerName, request.LobbyId);

            var lobby = lobbies.FirstOrDefault(l => l.LobbyId == request.LobbyId);
            if (lobby == null)
            {
                Log.Warning("Lobby {LobbyId} not found", request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Lobby not found." });
            }

            if (lobby.Players.Where(p => p.Name == request.PlayerName).Any())
            {
                Log.Warning("Player {PlayerName} already exists in lobby {LobbyId}", request.PlayerName, request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = $"Player '{request.PlayerName}' already exist." });
            }

            CellType playerType;
            switch (lobby.Players.Count)
            {
                case 0:
                    playerType = CellType.X;
                    break;
                case 1:
                    playerType = CellType.O;
                    break;
                default:
                    playerType = CellType.None;
                    break;
            }

            var player = new Player(request.PlayerName, playerType);

            if (!lobby.AddPlayer(player))
            {
                Log.Warning("Lobby {LobbyId} is full", request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Lobby is full." });
            }

            Log.Information("Player {PlayerName} joined lobby {LobbyId} as {PlayerType}", request.PlayerName, request.LobbyId, player.PlayerType);
            return JsonConvert.SerializeObject(new { PlayerType = player.PlayerType });
        }

        public string MakeMove(MoveRequest request)
        {
            Log.Information("Player {PlayerName} is making a move in lobby {LobbyId}", request.PlayerName, request.LobbyId);

            var lobby = lobbies.FirstOrDefault(l => l.LobbyId == request.LobbyId);
            if (lobby == null)
            {
                Log.Warning("Lobby {LobbyId} not found", request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Lobby not found." });
            }

            var player = lobby.Players.FirstOrDefault(p => p.Name == request.PlayerName);
            if (player == null)
            {
                Log.Warning("Player {PlayerName} not found in lobby {LobbyId}", request.PlayerName, request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Player not found." });
            }

            if (lobby.CurrentGame.Turn != player.PlayerType)
            {
                Log.Warning("It's not {PlayerName}'s turn in lobby {LobbyId}", request.PlayerName, request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "It's not your turn now." });
            }

            bool moveSuccessful = lobby.CurrentGame.MakeMove(request.SectorRow, request.SectorCol, request.CellRow, request.CellCol);

            if (!moveSuccessful)
            {
                Log.Warning("Invalid move by {PlayerName} in lobby {LobbyId}", request.PlayerName, request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Incorrect move." });
            }

            Log.Information("Move by {PlayerName} completed successfully in lobby {LobbyId}", request.PlayerName, request.LobbyId);
            return JsonConvert.SerializeObject(new { Success = "The move was completed successfully." });
        }

        public string GetGameState(int lobbyId)
        {
            Log.Information("Getting game state for lobby {LobbyId}", lobbyId);

            var lobby = lobbies.FirstOrDefault(l => l.LobbyId == lobbyId);
            if (lobby == null)
            {
                Log.Warning("Lobby {LobbyId} not found", lobbyId);
                return JsonConvert.SerializeObject(new { Error = "Lobby not found." });
            }

            var gameState = new
            {
                Board = ArrayHelper.ConvertToNestedLists(lobby.CurrentGame.Board),
                Sectors = ArrayHelper.ConvertToNestedLists(lobby.CurrentGame.Sectors).Select(x => x.Select(y => ArrayHelper.ConvertToNestedLists(y.Board))),
                Turn = lobby.CurrentGame.Turn,
                Winner = lobby.CurrentGame.Winner,
                OpenSectors = ArrayHelper.ConvertToNestedLists(lobby.CurrentGame.OpenSectors)
            };

            Log.Information("Game state retrieved for lobby {LobbyId}", lobbyId);
            return JsonConvert.SerializeObject(gameState);
        }

        public string GetLobbyState(int lobbyId)
        {
            Log.Information("Getting lobby state for lobby {LobbyId}", lobbyId);

            var lobby = lobbies.FirstOrDefault(l => l.LobbyId == lobbyId);
            if (lobby == null)
            {
                Log.Warning("Lobby {LobbyId} not found", lobbyId);
                return JsonConvert.SerializeObject(new { Error = "Lobby not found." });
            }

            var playerStates = lobby.Players.Select(p => new Dictionary<string, CellType> { { p.Name, p.PlayerType } }).ToList();

            Log.Information("Lobby state retrieved for lobby {LobbyId}", lobbyId);
            return JsonConvert.SerializeObject(playerStates);
        }

        public string DeleteLobby(DeleteLobbyRequest request)
        {
            Log.Information("Deleting lobby {LobbyId}", request.LobbyId);

            Lobby? lobbyToRemove = lobbies.FirstOrDefault(l => l.LobbyId == request.LobbyId);
            if (lobbyToRemove == null)
            {
                Log.Warning("Lobby {LobbyId} not found", request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Lobby not found." });
            }

            lobbies.Remove(lobbyToRemove);

            Log.Information("Lobby {LobbyId} removed", request.LobbyId);
            return JsonConvert.SerializeObject(new { Success = $"Lobby {request.LobbyId} removed." });
        }

        public string DeletePlayer(DeletePlayerRequest request)
        {
            Log.Information("Deleting player {PlayerName} from lobby {LobbyId}", request.PlayerName, request.LobbyId);

            var lobby = lobbies.FirstOrDefault(l => l.LobbyId == request.LobbyId);
            if (lobby == null)
            {
                Log.Warning("Lobby {LobbyId} not found", request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Lobby not found." });
            }

            Player? playerToRemove = lobby.Players.FirstOrDefault(p => p.Name == request.PlayerName);
            if (playerToRemove == null)
            {
                Log.Warning("Player {PlayerName} not found in lobby {LobbyId}", request.PlayerName, request.LobbyId);
                return JsonConvert.SerializeObject(new { Error = "Player not found." });
            }

            lobby.Players.Remove(playerToRemove);

            Log.Information("Player {PlayerName} removed from lobby {LobbyId}", request.PlayerName, request.LobbyId);
            return JsonConvert.SerializeObject(new { Success = $"Player {request.PlayerName} removed from lobby {request.LobbyId}." });
        }
    }
}
