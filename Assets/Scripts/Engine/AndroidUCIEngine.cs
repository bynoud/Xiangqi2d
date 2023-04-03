
#if UNITY_ANDROID

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Timers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityXiangqi.Engine {
    public class AndroidUCIEngine : IUCIEngine {
        //private Process engineProcess;
        //private string exePath = Application.streamingAssetsPath + "/UCIEngines/fairy-stockfish_x86-64-bmi2.exe";
        private bool isReady;
        private Timer timer;
        private float timeMS;

        private FENSerializer fenSerializer = new FENSerializer();
        private bool isSearchingForBestMove;
        private Game game;

        private AndroidJavaObject engineProcess;
        private AndroidJavaObject stdoutBufferReader;   // response buffer from UCI
        private AndroidJavaObject stdinStream;  // input stream to UCI

        private BlockingCollection<string> uciout = new();

        public async void Start() {
            timer = new Timer(100);
            timer.Elapsed += (_, _) => timeMS += 100;

            //engineProcess = new Process();
            //engineProcess.StartInfo = new ProcessStartInfo(
            //    exePath
            //) {
            //    UseShellExecute = false,
            //    RedirectStandardInput = true,
            //    RedirectStandardOutput = true,
            //    CreateNoWindow = true
            //};
            //engineProcess.Start();
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var context = activity.Call<AndroidJavaObject>("getApplicationContext");

            // If you wanted it, this is how you get your filesDir
            //string filesDir = context.Call<AndroidJavaObject>("getFilesDir").Call<string>("getAbsolutePath");

            // This is the one you really want to execute your program from
            string nativeLibraryDir = context.Call<AndroidJavaObject>("getApplicationInfo").Get<string>("nativeLibraryDir");
            string exePath = $"{nativeLibraryDir}/libStockfish15-1.so";
            Debug.Log($"nativeLibDir {nativeLibraryDir}");
            Debug.Log($"exepath {exePath}");


            var pb = new AndroidJavaObject("java.lang.ProcessBuilder", new string[] { exePath });
            var nativeDir = new AndroidJavaObject("java.io.File", nativeLibraryDir);
            pb.Call<AndroidJavaObject>("directory", nativeDir);

            engineProcess = pb.Call<AndroidJavaObject>("start");
            Debug.Log("Executed");

            var ostream = engineProcess.Call<AndroidJavaObject>("getOutputStream");
            stdinStream = new AndroidJavaObject("java.io.PrintStream", ostream);
            Debug.Log("stdinStream");

            var istream = engineProcess.Call<AndroidJavaObject>("getInputStream");
            Debug.Log("istream");
            var isr = new AndroidJavaObject("java.io.InputStreamReader", istream);
            Debug.Log("isr");
            stdoutBufferReader = new AndroidJavaObject("java.io.BufferedReader", isr);
            System.Threading.Thread th = new(StartOutputHandler);
            th.Start();

            //// Write line to program
            //var javaString = new AndroidJavaObject("java.lang.String", "Send this to engine");
            //ostream.Call("write", javaString.Call<AndroidJavaObject>("getBytes"));
            //ostream.Call("flush");

            //// Read line from program - you could try bufferedReader.Call<string>("readLine") instead
            //// But I'm not sure it works. I can't remember but I think I did it this way for a reason
            //var readLine = br.Call<AndroidJavaObject>("readLine");

            //// Make sure line's not null and its pointer is not null. I think if readLine returns
            //// a null string, Unity will still return a non-null AndroidJavaObject representing it,
            //// but its pointer will be zero. So check for that.
            //string readLineString = null;
            //if (readLine != null && readLine.GetRawObject().ToInt32() != 0) {
            //    readLineString = AndroidJNI.GetStringChars(readLine.GetRawObject());
            //}


            //await foreach (string engineOutputLine in Receive()) {
            //    Debug.Log(engineOutputLine);
            //}

            Send("uci");
            foreach (string engineOutputLine in Receive("uciok")) {
                Debug.Log(engineOutputLine);
            }

            //await Send("setoption name UCI_Variant value xiangqi");
            //await Send("setoption name UCI_Elo value 2850");

            Send("isready");
            foreach (string engineOutputLine in Receive("readyok")) {
                Debug.Log(engineOutputLine);
            }
            isReady = true;
        }

        public void ShutDown() {
            Debug.Log("UCI shutting down");
            Send("quit");
            Debug.Log("UCI shutting down wait");
            engineProcess.Call("waitFor");
            Debug.Log("UCI shuted down");
            engineProcess.Dispose();
            stdoutBufferReader.Dispose();
            stdinStream.Dispose();
            //engineProcess.Close();
        }

        public void SetupNewGame(Game game) {
            this.game = game;

            while (!isReady) {
                System.Threading.Thread.Sleep(10); // kind of weird... but this not happend offent
            }

            Send("ucinewgame");
        }

        public Movement GetBestMove(int timeoutMS = -1) {
            game.ConditionsTimeline.TryGetCurrent(out GameConditions currentConditions);
            Side sideToMove = currentConditions.SideToMove;
            Send($"position fen {fenSerializer.Serialize(game)}");

            if (!isSearchingForBestMove) {
                isSearchingForBestMove = true;
                Send($"go movetime {timeoutMS}");
            }

            foreach (string line in Receive("bestmove")) {
                Debug.Log(line);
                if (line.StartsWith("bestmove")) {
                    isSearchingForBestMove = false;
                    return ParseUCIMove(line.Split(" ")[1], sideToMove);
                }
            }

            Send("stop");

            Movement result = null;
            foreach (string line in Receive("bestmove")) {
                Debug.Log(line);
                if (line.StartsWith("bestmove")) {
                    isSearchingForBestMove = false;
                    result = ParseUCIMove(line.Split(" ")[1], sideToMove);
                }
            }

            return result;
        }

        private static Movement ParseUCIMove(string uciMove, Side sideToMove) {
            Debug.Log($"Parse move {uciMove}");
            Match m = Regex.Match(uciMove, @"(\w\d+)(\w\d+)");
            if (!m.Success) {
                Debug.Log($"Error when parse move {uciMove}");
                return null;
            }
            Debug.Log($" -> {m.Groups[1].Value} {m.Groups[2].Value}");
            return new Movement(new Square(m.Groups[1].Value), new Square(m.Groups[2].Value));

            //Movement result;
            //if (uciMove.Length > 4)
            //{
            //    result = new PromotionMove(
            //        new Square(uciMove[..2]),
            //        new Square(uciMove[2..4])
            //    );

            //    ElectedPiece electedPiece = uciMove[4..5].ToLower() switch
            //    {
            //        "b" => ElectedPiece.Bishop,
            //        "n" => ElectedPiece.Knight,
            //        "q" => ElectedPiece.Queen,
            //        "r" => ElectedPiece.Rook,
            //        _ => ElectedPiece.None
            //    };

            //    ((PromotionMove)result).SetPromotionPiece(
            //        PromotionUtil.GeneratePromotionPiece(electedPiece, sideToMove)
            //    );
            //}
            //else
            //{
            //    result = new Movement(
            //        new Square(uciMove[..2]),
            //        new Square(uciMove[2..4])
            //    );
            //}

            //return result;
        }

        private void Send(string data) {
            Debug.Log($"UCI sending2 {data}");
            //var javaString = new AndroidJavaObject("java.lang.String", data+"\n");
            //await Task.Run(() => {
            //    //stdinStream.Call("write", javaString.Call<AndroidJavaObject>("getBytes"));
            //    stdinStream.Call("println", new AndroidJavaObject("java.lang.String", data));
            //    stdinStream.Call("flush");
            //});
            stdinStream.Call("println", data);
            Debug.Log($"UCI sending1 {data}");
            stdinStream.Call("flush");
            Debug.Log($"UCI send done {data}");
            //await engineProcess.StandardInput.WriteLineAsync($"{data}\n");
        }

        private IEnumerable<string> Receive(string responseBreak, int timeoutMS = -1) {
            string line = null;
            float startTime = timeMS;

            Debug.Log($"UCI checking {responseBreak}");

            while (!ResponseFinished() && (timeoutMS < 0 || timeMS - startTime < timeoutMS)) {
                //line = await engineProcess.StandardOutput.ReadLineAsync();
                //line = await Task.Run(readLine);
                line = uciout.Take();
                //line = stdoutBufferReader.Call<string>("readLine");
                Debug.Log($"UCI rec {line}");
                yield return line;
            }

            //bool ResponseFinished() => responseBreak switch {
            //    null => engineProcess.StandardOutput.Peek() == -1,
            //    _ => line?.StartsWith(responseBreak) ?? false
            //};
            //string readLine() => stdoutBufferReader.Call<string>("readLine");
            bool ResponseFinished() => line?.StartsWith(responseBreak) ?? false;
        }
        private void StartOutputHandler() {
            Debug.Log("UCI output handler starting");
            while (true) {
                try {
                    string line = stdoutBufferReader.Call<string>("readLine");
                    uciout.Add(line);
                } catch {
                    Debug.Log("UCI output stopped");
                    break;
                }
            }
        }
    }
}

#endif
