using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityXiangqi;
using static UnityXiangqi.GlobalVar;
using static UnityXiangqi.SquareUtil;

public class BoardManager : MonoBehaviourSingleton<BoardManager>
{
    private const int BOARD_SIZE = FILE_MAX * RANK_MAX;

    private readonly GameObject[] allSquaresGO = new GameObject[BOARD_SIZE];
    private Dictionary<Square, GameObject> positionMap;

    //public GameObject HighlighPref = null;
    //private Queue<GameObject> HighlightsGO = new();
    private GameObject highlightEndGO;
    private GameObject highlightFromGO;
    private GameObject highlightSelectGO;
    private Queue<GameObject> HighlightPossibleMovesGO = new();

    private const string PieceModelPath = "Pieces/";
    //private const string PieceModelPath = "PieceSets/Marble/";
    private const float BoardFileExpectedLength = 16f;
    private float BoardFileSideStart = -1.386f; // measured square at File=1
    private float BoardFileSideEnd = 1.406f; // measured square at File=<last>
    private float BoardRankSideStart = -1.578f;
    private float BoardRankSideEnd = 1.585f;

    private float tileHalfSize;
    private Side currentMoveSide;

    private void Awake()
    {
        GameManager.NewGameStartedEvent += OnNewGameStarted;
        GameManager.GameResetToHalfMoveEvent += OnGameResetToHalfMove;

        positionMap = new Dictionary<Square, GameObject>(BOARD_SIZE);
        //Transform boardTransform = transform;
        //Vector3 boardPosition = boardTransform.position;

        Transform boardTransform = InstanceTheBoard();
        Vector3 boardPosition = boardTransform.position;

        tileHalfSize = (BoardFileSideEnd - BoardFileSideStart) / (FILE_MAX - 1);
        tileHalfSize /= 2f;
        Debug.Log($"tileHalfSize  {tileHalfSize} {BoardFileSideStart} {BoardFileSideEnd}");


        highlightEndGO = InstanceRes("Highlight", boardTransform);
        highlightEndGO.SetActive(false);
        highlightFromGO = InstanceRes("HighlightFrom", boardTransform);
        highlightFromGO.SetActive(false);
        highlightSelectGO = InstanceRes("CurrentSelected", boardTransform);
        highlightSelectGO.SetActive(false);

        for (int file = 1; file <= FILE_MAX; file++)
        {
            for (int rank = 1; rank <= RANK_MAX; rank++)
            {
                //GameObject squareGO = new GameObject(SquareToString(file, rank)) {
                //	transform = {
                //		position = new Vector3(
                //			boardPosition.x + FileToSidePosition(file),
                //			boardPosition.y + BoardHeight,
                //			boardPosition.z + RankToSidePosition(rank)),
                //		parent = boardTransform
                //	},
                //	tag = "Square"
                //};
                GameObject squareGO = new(SquareToString(file, rank))
                {
                    transform = {
                        position = new Vector3(
                            boardPosition.x + FileToSidePosition(file),
                            boardPosition.y + RankToSidePosition(rank),
                            boardPosition.z + 2), // make them behind, so I can click on the board
                    },
                    tag = "Square"
                };
                squareGO.transform.SetParent(boardTransform, false);

                positionMap.Add(new Square(file, rank), squareGO);
                allSquaresGO[(file - 1) * RANK_MAX + (rank - 1)] = squareGO;
            }
        }
    }

    private GameObject InstanceRes(String name, Transform parent=null) {
        if (parent) return Instantiate(Resources.Load(PieceModelPath + name) as GameObject, parent);
        else return Instantiate(Resources.Load(PieceModelPath + name) as GameObject);
    }

    private Transform InstanceTheBoard() {
        GameObject boardGO = InstanceRes("BoardWrap", transform);
        Vector3 tr = boardGO.transform.Find("StartCorner").position;
        BoardFileSideStart = tr.x;
        BoardRankSideStart = tr.y;
        tr = boardGO.transform.Find("EndCorner").position;
        BoardFileSideEnd = tr.x;
        BoardRankSideEnd = tr.y;

        Transform ph = transform.Find("Placeholder");
        Transform board = boardGO.transform.Find("Chessboard");

        Bounds bound = ph.GetComponent<Renderer>().bounds;
        Bounds curbound = board.GetComponent<Renderer>().bounds;
        Debug.Log($" bound {bound.size} {curbound.size}");
        ph.gameObject.SetActive(false);
        float boardScale = bound.size.x / curbound.size.x;
        boardGO.transform.localScale = new Vector3(boardScale, boardScale, boardScale);

        return board;
    }


    private void OnNewGameStarted()
    {
        ClearBoard();

        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
        {
            CreateAndPlacePieceGO(piece, square);
        }

        EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }

    private void OnGameResetToHalfMove()
    {
        ClearBoard();

        foreach ((Square square, Piece piece) in GameManager.Instance.CurrentPieces)
        {
            CreateAndPlacePieceGO(piece, square);
        }

        GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove);
        if (latestHalfMove.CausedCheckmate || latestHalfMove.CausedStalemate) SetActiveAllPieces(false);
        else EnsureOnlyPiecesOfSideAreEnabled(GameManager.Instance.SideToMove);
    }

    public void CreateAndPlacePieceGO(Piece piece, Square position)
    {
        string modelName = $"{piece.Owner}{piece.GetType().Name}";
        //GameObject pieceGO = Instantiate(
        //    Resources.Load(PieceModelPath + modelName) as GameObject,
        //    positionMap[position].transform
        //);
        GameObject pieceGO = InstanceRes(modelName, positionMap[position].transform);
        pieceGO.AddComponent<VisualPiece>();
        pieceGO.GetComponent<VisualPiece>().enabled = true;
        pieceGO.GetComponent<VisualPiece>().PieceColor = piece.Owner;

    }

    //public void GetSquareGOsWithinRadius(List<GameObject> squareGOs, Vector3 positionWS, float radius)
    //{
    //    float radiusSqr = radius * radius;
    //    foreach (GameObject squareGO in allSquaresGO)
    //    {
    //        if ((squareGO.transform.position - positionWS).sqrMagnitude < radiusSqr)
    //            squareGOs.Add(squareGO);
    //    }
    //}
    public GameObject GetNearestSquare(Vector3 position) {
        //Debug.Log($"find near {position} {tileHalfSize}");
        float lastDistance = 1000000f;
        GameObject go = null;
        foreach (GameObject squareGO in allSquaresGO) {
            float dist = Vector2.Distance(position, squareGO.transform.position);
            if (dist < tileHalfSize && dist < lastDistance) {
                lastDistance = dist;
                go = squareGO;
                //Debug.Log($" near found {squareGO.transform.position}");
            }
        }
        //if (go) Debug.Log($" --> {go.transform.position}");
        return go;
    }

    public void SetActiveAllPieces(bool active)
    {
        VisualPiece[] visualPiece = GetComponentsInChildren<VisualPiece>(true);
        foreach (VisualPiece pieceBehaviour in visualPiece) pieceBehaviour.enabled = active;
    }

    public void EnsureOnlyPiecesOfSideAreEnabled(Side side)
    {
        currentMoveSide = side;
        //VisualPiece[] visualPiece = GetComponentsInChildren<VisualPiece>(true);
        ////Debug.Log($"enable check ${side} {visualPiece.Length}");
        //foreach (VisualPiece pieceBehaviour in visualPiece)
        //{
        //    Piece piece = GameManager.Instance.CurrentBoard[pieceBehaviour.CurrentSquare];

        //    //bool hasLegalMoves = GameManager.Instance.HasLegalMoves(piece);
        //    //pieceBehaviour.enabled = pieceBehaviour.PieceColor == side && hasLegalMoves;
        //    pieceBehaviour.enabled = pieceBehaviour.PieceColor == side; // just let the piece pickup, even without possible moves
        //}

    }
    public bool IsMysideMove(Side side) {
        return currentMoveSide == side;
    }

    //public void TryDestroyVisualPiece(Square position)
    //{
    //    VisualPiece visualPiece = positionMap[position].GetComponentInChildren<VisualPiece>();
    //    if (visualPiece != null) DestroyImmediate(visualPiece.gameObject);
    //}
    private void moveToSquare(Transform childTran, Square square) {
        childTran.parent = positionMap[square].transform;
        childTran.position = positionMap[square].transform.position;
    }
    public void updateMove(VisualPiece piece, Square toSquare) {
        Square fromSquare = piece.CurrentSquare;
        Debug.Log($" Update move {fromSquare} {toSquare}");

        VisualPiece visualPiece = positionMap[toSquare].GetComponentInChildren<VisualPiece>();
        if (visualPiece != null) DestroyImmediate(visualPiece.gameObject);

        Debug.Log($"HH {piece}");
        moveToSquare(piece.transform, toSquare);

        StopHighlight();

        highlightFromGO.transform.position = positionMap[fromSquare].transform.position;
        highlightEndGO.transform.position = positionMap[toSquare].transform.position;
        highlightFromGO.SetActive(true);
        highlightEndGO.SetActive(true);
        //moveToSquare(highlightFromGO.transform, fromSquare);
        //moveToSquare(highlightEndGO.transform, toSquare);
    }

    public GameObject GetPieceGOAtPosition(Square position)
    {
        GameObject square = GetSquareGOByPosition(position);
        return square.transform.childCount == 0 ? null : square.transform.GetChild(0).gameObject;
    }
    public VisualPiece GetVisualPieceAtPosition(Square position) {
        return positionMap[position].GetComponentInChildren<VisualPiece>();
    }

    public void HighlightSquares(Square sekectedSquare, ICollection<Movement> moves)
    {
        //Debug.Log("Higlighting not implemented yet");
        StopHighlight();
        highlightSelectGO.transform.position = positionMap[sekectedSquare].transform.position;
        highlightSelectGO.SetActive(true);
        //string msg = "";
        foreach (Movement move in moves) {
            //GameObject hlGO = Instantiate(HighlighPref, positionMap[move.End].transform);
            //hlGO.tag = "SquareHighlight";
            GameObject hlGO = InstanceRes("PossibleMove", positionMap[move.End].transform);
            HighlightPossibleMovesGO.Enqueue(hlGO);
            //msg += $" {move.End}";
        }
        //Debug.Log($"Higlighted {msg}");
    }

    private void StopHighlight()
    {
        Debug.Log("Stoping hight");
        highlightSelectGO.SetActive(false);
        while (HighlightPossibleMovesGO.Count > 0) {
            DestroyImmediate(HighlightPossibleMovesGO.Dequeue());
        }
    }

    // private static float FileOrRankToSidePosition(int index) {
    // 	float t = (index - 1) / 7f;
    // 	return Mathf.Lerp(-BoardPlaneSideHalfLength, BoardPlaneSideHalfLength, t);
    // }
    private float FileToSidePosition(int index)
    {
        float t = (index - 1) / (float)(FILE_MAX - 1);
        return Mathf.Lerp(BoardFileSideStart, BoardFileSideEnd, t);
    }
    private float RankToSidePosition(int index)
    {
        float t = (index - 1) / (float)(RANK_MAX - 1);
        return Mathf.Lerp(BoardRankSideStart, BoardRankSideEnd, t);
    }

    private void ClearBoard()
    {
        VisualPiece[] visualPiece = GetComponentsInChildren<VisualPiece>(true);

        foreach (VisualPiece pieceBehaviour in visualPiece)
        {
            DestroyImmediate(pieceBehaviour.gameObject);
        }
    }

    public GameObject GetSquareGOByPosition(Square position) => Array.Find(allSquaresGO, go => go.name == SquareToString(position));
}