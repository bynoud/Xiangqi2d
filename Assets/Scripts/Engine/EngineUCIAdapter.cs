using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityXiangqi;
using System;



public enum UCICommandType {
    Movetime,
    Multipv,
    Elo,
    Fen,
    Stop
}
public class UCICommand {
    public readonly UCICommandType type;
    public readonly String param;
    public UCICommand(UCICommandType type, String param="") {
        this.type = type;
        this.param = param;
    }
}

public enum UCIMessageType {
    Info,
    Fatal
}
public class UCIMessage {
    public readonly UCIMessageType type;
    public readonly String param;
    public UCIMessage(UCIMessageType type, String param="") {
        this.type = type;
        this.param = param;
    }
}

public class EngineMove {
    int multipv;
    int depth;
    int score;
    string scoreUnit;
    List<string> pv;


    public bool Match(string bestmove, string ponder) {
        if (pv.Count < 2) pv.Add(ponder);
        return (bestmove == pv[0]) && (ponder == "" || ponder == pv[1]);
    }

    public bool IsBetter(EngineMove other) {
        if (other==null) return true;
        if (scoreUnit == "mate") return true;
        if (other.scoreUnit == "mate") return false;
        if (other.depth == depth && other.score > score) return false;
        if (other.depth > depth && (other.score >= score)) return false;
        if (score < -100 && other.score > score) return false;
        return true;
    }

    public static EngineMove Parse(string[] items) {
        if (items.Length < 2 || items[0] != "info" || items[1] == "string") {
            return null;
        }
        EngineMove m = new();
        int i = 1;
        while (i < items.Length) {
            string name = items[i];
            i++;
            switch (name) {
                case "multipv":
                    m.multipv = int.Parse(items[i]);
                    break;
                case "depth":
                    m.depth = int.Parse(items[i]);
                    break;
                case "score":
                    m.scoreUnit = items[i];
                    m.score = int.Parse(items[i++]);
                    break;
                case "pv":
                    while (i<items.Length) {
                        m.pv.Add(items[i++]);
                    }
                    break;
            }
        }
        return m;
    }
}

public class EngineUCIAdapter {
    public delegate void EngineMessageEvent(UCIMessage msg);
    public static event EngineMessageEvent OnMessage;
    public delegate void EngineBestmoveCalculatedEvent(EngineMove move);
    public static event EngineBestmoveCalculatedEvent OnMoveCalculated;

    Process stockfish;
    StreamWriter stdin;
    Thread bgThread;
    BlockingCollection<UCICommand> cmdqueue = new();
    int movetime = 2000;
    List<EngineMove> movelist = new();

    private List<Tuple<int, int>> eloSetting = new() {
        new(200, 1000),  // movetime, elo
        new(500, 1500),
        new(1000, 1800),
        new(2000, 2200),
        new(3000, 2800),
    };

    // Start is called before the first frame update
    public async void Start() {
        stockfish = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = (Application.streamingAssetsPath + "\\fairy-stockfish_x86-64-bmi2.exe"),
                Arguments = "",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }

        };
        stockfish.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
        stockfish.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);
        stockfish.Start();
        
        stdin = stockfish.StandardInput;
        Write("uci\nsetoption name UCI_Variant value xiangqi\nsetoption name UCI_Elo value 2850\n");

        bgThread = new (new ThreadStart(CommandHandler));
        bgThread.Start();
    }

    public void Stop() {
        cmdqueue.Add(new UCICommand(UCICommandType.Stop));
    }

    void SetElo(int level) {
        if (level < 0 || level >= eloSetting.Count) {
            Log($"Wrong elo level {level}");
            return;
        }
        cmdqueue.Add(new UCICommand(UCICommandType.Movetime, eloSetting[level].Item1.ToString()));
        cmdqueue.Add(new UCICommand(UCICommandType.Elo, eloSetting[level].Item2.ToString()));
    }

    public void SetFen(string fen) {
        cmdqueue.Add(new UCICommand(UCICommandType.Fen, fen));
    }

    void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine) {
        OnMessage?.Invoke(new UCIMessage(UCIMessageType.Fatal, $"UCI Error {outLine.Data}"));
        Stop();
    }

    private EngineMove FilterBestMove(string bestmove, string ponder) {
        EngineMove sel = null;
        foreach (var move in movelist) {
            if (!move.Match(bestmove, ponder)) continue;
            if (move.IsBetter(sel)) sel = move;
        }
        Log($"Filter bestmove {sel}");
        if (sel == null) Log($"Error failed to filter movelist {bestmove} {ponder} {movelist}");
        return sel;
    }

    void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
        Log($"UCI out: {outLine.Data}");
        var items = outLine.Data.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (items.Length == 0) return;
        if (items[0] == "info") {
            EngineMove move = EngineMove.Parse(items);
            if (move != null) movelist.Add(move);
        } else if (items[0] == "bestmove") {
            string bestmove = items[1];
            string ponder = (items.Length < 2) ? "" : items[2];
            EngineMove selMove = FilterBestMove(bestmove, ponder);
            movelist.Clear();
            OnMoveCalculated?.Invoke(selMove);
        }
    }

    private void Write(string message) {
        Log($"UCI cmd: {message}");
        stdin.Write(message);
        stdin.Flush();
    }

    private void Log(string msg) {
        UnityEngine.Debug.Log($"UCI {msg}");
    }

    void CommandHandler() {
        Log("CMD hanlder started");

        

        while (true) {
            UCICommand cmd = cmdqueue.Take();
            switch (cmd.type) {

                case UCICommandType.Stop:
                    Log("UCI quiting");
                    Write("stop\nquit\n");
                    stockfish.WaitForExit();
                    Log("UCI exitted");
                    break;

                case UCICommandType.Movetime:
                    movetime = int.Parse(cmd.param);
                    Log($"Movetime = {cmd.param}");
                    break;

                case UCICommandType.Multipv:
                    Write($"setoption name MultiPV value {cmd.param}\n");
                    Log($"Multipv = {cmd.param}");
                    break;

                case UCICommandType.Elo:
                    Write($"setoption name UCI_Elo value {cmd.param}\n");
                    Log($"Elo = {cmd.param}");
                    break;

                case UCICommandType.Fen:
                    Write($"stop\nucinewgame\nposition fen {cmd.param}\ngo movetime {movetime}\n");
                    Log($"Start fen {cmd.param}");
                    break;

                default:
                    Log($"ERROR: Unknow cmdtype {cmd.type} {cmd.param}");
                    break;
            }
        }


    }
}
