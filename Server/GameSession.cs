using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BlazorSignalRApp.Server.Hubs
{
  [Serializable]
  public class GameSession
  {
    public Int32 BoardSize { get; set; } = 19; // Always odd number. Never below 11

    public Char[][] EmptyBoard { get; set; }

    public Char[][] CurrentBoard { get; set; }

    public List<Int32> PlaysX { get; set; } = new List<Int32>();
    public List<Int32> PlaysY { get; set; } = new List<Int32>();

    private DateTime SessionUpdatedAt { get; set; }

    public GameSession()
    {
      EmptyBoard = new Char[BoardSize][];
      CurrentBoard = new Char[BoardSize][];

      // Default crosses
      for (Int32 j = 0; j < BoardSize; ++j)
      {
        EmptyBoard[j] = new Char[BoardSize];
        CurrentBoard[j] = new Char[BoardSize];
        for (Int32 i = 0; i < BoardSize; ++i)
          EmptyBoard[j][i] = '5';
      }
      // Straight Borders
      for (Int32 i = 0; i < BoardSize; ++i)
      {
        EmptyBoard[0][i] = '8';
        EmptyBoard[BoardSize - 1][i] = '2';
      }
      for (Int32 j = 0; j < BoardSize; ++j)
      {
        EmptyBoard[j][0] = '4';
        EmptyBoard[j][BoardSize - 1] = '6';
      }
      // Corners
      EmptyBoard[0][0] = '7';
      EmptyBoard[0][BoardSize - 1] = '9';
      EmptyBoard[BoardSize - 1][0] = '1';
      EmptyBoard[BoardSize - 1][BoardSize - 1] = '3';
      // Left dots
      EmptyBoard[3][3] = '+';
      EmptyBoard[BoardSize / 2][3] = '+';
      EmptyBoard[BoardSize - 4][3] = '+';
      // Center dots
      EmptyBoard[3][BoardSize / 2] = '+';
      EmptyBoard[BoardSize / 2][BoardSize / 2] = '+';
      EmptyBoard[BoardSize - 4][BoardSize / 2] = '+';
      // Right dots
      EmptyBoard[3][BoardSize - 4] = '+';
      EmptyBoard[BoardSize / 2][BoardSize - 4] = '+';
      EmptyBoard[BoardSize - 4][BoardSize - 4] = '+';

      for (Int32 j = 0; j < BoardSize; ++j)
        for (Int32 i = 0; i < BoardSize; ++i)
          CurrentBoard[j][i] = EmptyBoard[j][i];

      SessionUpdatedAt = DateTime.Now;
    }

    public Boolean OldGame() => SessionUpdatedAt < DateTime.Now - TimeSpan.FromMinutes(30);

    public String PrintCurrentBoard()
    {
      StringBuilder boardString = new StringBuilder();
      for (Int32 j = 0; j < BoardSize; ++j)
      {
        for (Int32 i = 0; i < BoardSize; ++i)
          boardString.Append(CurrentBoard[j][i]);
        boardString.AppendLine("");
      }
      SessionUpdatedAt = DateTime.Now;
      return boardString.ToString().Trim('\r', '\n').Replace("\r", "");
    }

    public Boolean PlaceStone(Int32 x, Int32 y)
    {
      if (CurrentBoard[y][x] != 'w' && CurrentBoard[y][x] != 'b')
      {
        CurrentBoard[y][x] = CurrentTurn();
        PlaysX.Add(x);
        PlaysY.Add(y);
        return true;
      }
      return false;
    }

    public void UndoStone()
    {
      if (PlaysX.Count > 0)
      {
        var lastCoordinateX = PlaysX.Last();
        var lastCoordinateY = PlaysY.Last();
        CurrentBoard[lastCoordinateY][lastCoordinateX] = EmptyBoard[lastCoordinateY][lastCoordinateX];
        PlaysX.RemoveAt(PlaysX.Count - 1);
        PlaysY.RemoveAt(PlaysY.Count - 1);
      }
    }

    public Char CurrentTurn(Int32 turn)
    {
      if (turn == 0)
        return 'b';
      return (((turn - 1) / 2) % 2 == 0) ? 'w' : 'b';
    }

    public Int32 CurrentTurnRemaining(Int32 turn) => (turn + 1) % 2 == 0 ? 2 : 1;


    public Char CurrentTurn() => CurrentTurn(PlaysX.Count);

    public Int32 CurrentTurnRemaining() => CurrentTurnRemaining(PlaysX.Count);
  }
}