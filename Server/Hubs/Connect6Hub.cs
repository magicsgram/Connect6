using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace BlazorSignalRApp.Server.Hubs
{
  public class Connect6Hub : Hub
  {
    public Connect6Hub() : base()
    {
      if (!initialized)
      {
        String totalSessionsFileName = Path.Combine(Directory.GetParent(".").FullName, "totalSessions.dat");
        if (File.Exists(totalSessionsFileName))
        {
          StreamReader sr = new StreamReader(totalSessionsFileName);
          totalSessions = UInt64.Parse(sr.ReadLine() as String);
          totalConnections = UInt64.Parse(sr.ReadLine() as String);
          totalMultiplayerGame = UInt64.Parse(sr.ReadLine() as String);
          sr.Close();
        }
        String gameSessionsFileName = Path.Combine(Directory.GetParent(".").FullName, "gameSessions.dat");
        if (File.Exists(gameSessionsFileName))
        {
          String dataRead = File.ReadAllText(gameSessionsFileName);
          gameSessions = JsonSerializer.Deserialize<Dictionary<String, GameSession>>(dataRead);
          foreach (var gameId in gameSessions.Keys)
            connections.Add(gameId, new HashSet<String>());
        }

        initialized = true;
      }
    }

    static Boolean initialized = false;

    static UInt64 totalSessions = 0;
    static UInt64 totalConnections = 0;
    static UInt64 totalMultiplayerGame = 0;
    static Dictionary<String, GameSession> gameSessions = new Dictionary<String, GameSession>();
    static Dictionary<String, HashSet<String>> connections = new Dictionary<String, HashSet<String>>();
    static Dictionary<String, String> reverseMapping = new Dictionary<String, String>();
    static Queue<String> serverLogsQueue = new Queue<String>();

    public async Task CreateNewGame()
    {
      var toRemove = gameSessions.Where(pair => pair.Value.OldGame()).Select(pair => pair.Key).ToList();
      foreach (String gameIdKey in toRemove)
      {
        try
        {
          gameSessions.Remove(gameIdKey);
          connections.Remove(gameIdKey);
          foreach (var keyValuePair in reverseMapping.ToList())
            if (keyValuePair.Value == gameIdKey)
              reverseMapping.Remove(keyValuePair.Key);
          await Report(gameIdKey, "Session destroyed");
        }
        catch { }
      }

      String gameId = "";
      do
      {
        Guid g = Guid.NewGuid();
        gameId = g.ToString().Substring(0, 8);
      } while (gameSessions.ContainsKey(gameId));
      gameSessions.Add(gameId, new GameSession());
      connections.Add(gameId, new HashSet<String>());
      ++totalSessions;
      await Clients.Caller.SendAsync("NewGameIdReceived", gameId);
      await Report(gameId, "New game made");
    }

    public async Task InitializeBoardAndConnection(String gameId)
    {
      if (await HandleNoGameFound(gameId))
        return;
      await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
      if (!connections[gameId].Contains(Context.ConnectionId))
      {
        connections[gameId].Add(Context.ConnectionId);
        reverseMapping.Add(Context.ConnectionId, gameId);
      }
      await SendCurrentStateAsync(gameId);
      await SendConnectionSize(gameId);
      ++totalConnections;
      if (connections[gameId].Count == 2)
        ++totalMultiplayerGame;
      await Report(gameId, "New user connected to game");
    }

    public async Task RegisterAdminConnection()
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, "AdminAdminAdmin");
      await Report("", "");
    }

    public async Task PlaceStone(String gameId, Int32 x, Int32 y)
    {
      if (await HandleNoGameFound(gameId))
        return;
      Boolean result = gameSessions[gameId].PlaceStone(x, y);
      await SendCurrentStateAsync(gameId, result ? "placeStone" : "");
      await Report(gameId, $"User placed stone ({x.ToString("D2")}, {y.ToString("D2")})");
    }

    public async Task UndoStone(String gameId)
    {
      if (await HandleNoGameFound(gameId))
        return;
      gameSessions[gameId].UndoStone();
      await SendCurrentStateAsync(gameId);
      await Report(gameId, "User undid");
    }

    public async Task NewGame(String gameId)
    {
      if (await HandleNoGameFound(gameId))
        return;
      try
      {
        if (gameSessions.ContainsKey(gameId))
        {
          gameSessions[gameId] = new GameSession();
          await SendCurrentStateAsync(gameId);
          await Report(gameId, "Board reset");
        }
      }
      catch { }
    }

    private async Task SendCurrentStateAsync(String gameId, String soundCue = "")
    {
      Dictionary<String, String> state = new Dictionary<String, String>();
      state.Add("currentTurn", gameSessions[gameId].CurrentTurn().ToString());
      state.Add("currentTurnRemaining", gameSessions[gameId].CurrentTurnRemaining().ToString());
      state.Add("boardString", gameSessions[gameId].PrintCurrentBoard());
      state.Add("soundCue", soundCue);

      if (gameSessions[gameId].PlaysX.Count > 0)
      {
        var lastPlayX = gameSessions[gameId].PlaysX.Last();
        var lastPlayY = gameSessions[gameId].PlaysY.Last();
        state.Add("lastPlayX", lastPlayX.ToString());
        state.Add("lastPlayY", lastPlayY.ToString());
        if (gameSessions[gameId].PlaysX.Count > 1)
        {
          Char lastTurn = gameSessions[gameId].CurrentTurn(gameSessions[gameId].PlaysX.Count - 1);
          Char lastLastTurn = gameSessions[gameId].CurrentTurn(gameSessions[gameId].PlaysX.Count - 2);
          if (lastTurn == lastLastTurn)
          {
            var lastLastPlayX = gameSessions[gameId].PlaysX[^2];
            var lastLastPlayY = gameSessions[gameId].PlaysY[^2];
            state.Add("lastLastPlayX", lastLastPlayX.ToString());
            state.Add("lastLastPlayY", lastLastPlayY.ToString());
          }
          else
          {
            state.Add("lastLastPlayX", (-1).ToString());
            state.Add("lastLastPlayY", (-1).ToString());
          }
        }
        else
        {
          state.Add("lastLastPlayX", (-1).ToString());
          state.Add("lastLastPlayY", (-1).ToString());
        }
      }
      else
      {
        state.Add("lastPlayX", (-1).ToString());
        state.Add("lastPlayY", (-1).ToString());
        state.Add("lastLastPlayX", (-1).ToString());
        state.Add("lastLastPlayY", (-1).ToString());
      }
      await Clients.Group(gameId).SendAsync("CurrentBoard", state);
    }

    private async Task SendConnectionSize(String gameId) => await Clients.Group(gameId).SendAsync("ConnectionSize", connections[gameId].Count);

    private async Task<Boolean> HandleNoGameFound(String gameId)
    {
      if (gameSessions.ContainsKey(gameId))
        return false;
      else
      {
        await Clients.Caller.SendAsync("NoGameFound", "");
        return true;
      }
    }

    public async override Task OnDisconnectedAsync(Exception exception)
    {
      if (reverseMapping.ContainsKey(Context.ConnectionId))
      {
        try
        {
          String gameId = reverseMapping[Context.ConnectionId];
          reverseMapping.Remove(Context.ConnectionId);
          connections[gameId].Remove(Context.ConnectionId);
          await SendConnectionSize(gameId);
          await Report(gameId, "User disconnected");
        }
        catch { }
      }
    }

    public void ParkAndExit(String adminKeyFromClient)
    {
      String adminKeyFileName = Path.Combine(Directory.GetParent(".").FullName, "adminKey.txt");
      if (File.Exists(adminKeyFileName))
      {
        StreamReader sr = new StreamReader(adminKeyFileName);
        String adminKey = sr.ReadLine() as String;
        sr.Close();

        if (adminKeyFromClient == adminKey)
        {
          File.WriteAllText(Path.Combine(Directory.GetParent(".").FullName, "totalSessions.dat"), $"{totalSessions}\n{totalConnections}\n{totalMultiplayerGame}");
          var jsonString = JsonSerializer.Serialize(gameSessions);
          File.WriteAllText(Path.Combine(Directory.GetParent(".").FullName, "gameSessions.dat"), jsonString);
          Environment.Exit(0);
        }
      }
    }

    private async Task Report(String gameId, String message)
    {
      if (gameId.Length > 0 && message.Length > 0)
      {
        String reportMessage = $"{DateTime.Now} [{totalSessions} TS, {totalConnections} TU, {totalMultiplayerGame} MUS, {gameSessions.Keys.Count} CS, {reverseMapping.Count} CU] {gameId} ({connections[gameId].Count}) : {message.PadRight(30)}{Context.ConnectionId}";
        while (serverLogsQueue.Count > 30)
          serverLogsQueue.Dequeue();
        serverLogsQueue.Enqueue(reportMessage);
      }
      await Clients.Group("AdminAdminAdmin").SendAsync("ServerLogReceived", serverLogsQueue.ToList());
    }
  }
}