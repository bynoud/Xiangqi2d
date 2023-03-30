
using UnityEngine;
using UnityXiangqi;

public class BoardClicker : MonoBehaviourSingleton<BoardClicker> {
    public delegate void VisualPieceSelectedAction(Square selectedSquare);
    public static event VisualPieceSelectedAction VisualPieceSelected;

    public delegate void VisualPieceMovedAction(VisualPiece piece, Transform closestBoardSquareTransform);
    public static event VisualPieceMovedAction VisualPieceMoved;

    private Camera boardCamera;
    private VisualPiece currentPiece = null;

    private void Start() {
        boardCamera = Camera.main;
    }

    public void DeselectPiece() {
        currentPiece = null;
    }

    public void OnMouseDown() {
        VisualPiece selectPiece = null;
        Ray cameraRay = boardCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] result = Physics2D.GetRayIntersectionAll(cameraRay);

        Vector3 mousePos = boardCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos = new(mousePos.x, mousePos.y, transform.position.y);

        for (int i = 0; i < result.Length; i++) {
            selectPiece = result[i].collider.gameObject.GetComponent<VisualPiece>();
            if (selectPiece) break;
        }

        Debug.Log($"Mousedown {mousePos} {selectPiece}");

        if (selectPiece) {
            Debug.Log($" -- {selectPiece.CurrentSquare} -- {currentPiece}");
            if (currentPiece == null || currentPiece.SameSide(selectPiece)) {
                if (BoardManager.Instance.IsMysideMove(selectPiece.PieceColor)) {
                    currentPiece = selectPiece;
                    Debug.Log($" Select {currentPiece.CurrentSquare}");
                    VisualPieceSelected?.Invoke(currentPiece.CurrentSquare);
                }
            } else {
                MovePiece(currentPiece, mousePos);
            }
        } else if (currentPiece) {
            MovePiece(currentPiece, mousePos);
        }

    }

    private void MovePiece(VisualPiece piece, Vector3 mousePos) {
        //Debug.Log($" Move {currentPiece.CurrentSquare} -> {mousePos}");
        GameObject squareGO = BoardManager.Instance.GetNearestSquare(mousePos);
        if (squareGO == null) {
            return;
        }
        VisualPieceMoved?.Invoke(piece, squareGO.transform);
    }

}
