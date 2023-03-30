using System.Collections.Generic;
using UnityXiangqi;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityXiangqi.SquareUtil;

public class VisualPiece : MonoBehaviour
{
    //public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
    //public static event VisualPieceMovedAction VisualPieceMoved;

    //public delegate void VisualPieceSelectedAction(Square selectedSquare);
    //public static event VisualPieceSelectedAction VisualPieceSelected;

    public Side PieceColor;
    public Square CurrentSquare => StringToSquare(transform.parent.name);


    private const float SquareCollisionRadius = 9f;
    private Camera boardCamera;
    private Vector3 piecePositionSS;
    private SphereCollider pieceBoundingSphere;
    private List<GameObject> potentialLandingSquares;
    private Transform thisTransform;

    private void Start()
    {
        potentialLandingSquares = new List<GameObject>();
        thisTransform = transform;
        boardCamera = Camera.main;
    }

    public bool SameSide(VisualPiece piece)
    {
        return PieceColor == piece.PieceColor;
    }

    //public void OnMouseDown()
    //{
    //    Debug.Log($"mouse down on {CurrentSquare}");
    //    //VisualPieceSelected?.Invoke(CurrentSquare);
    //    //if (enabled)
    //    //{
    //    //    piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
    //    //    VisualPieceSelected?.Invoke(CurrentSquare);
    //    //}
    //}

    //private float LerpNeg(float begin, float end, float ratio)
    //{
    //    return begin + (end - begin) * ratio;
    //}
    //private Vector3 Lerp3DNeg(Vector3 begin, Vector3 end, float ratio)
    //{
    //    return new Vector3(
    //        LerpNeg(begin.x, end.x, ratio),
    //        LerpNeg(begin.y, end.y, ratio),
    //        LerpNeg(begin.z, end.z, ratio)
    //    );
    //}

    //private void OnMouseDrag()
    //{
    //    if (enabled)
    //    {
    //        //Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
    //        //thisTransform.position = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);

    //        //Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
    //        //Vector3 mouseWorld = boardCamera.ScreenToWorldPoint(Input.mousePosition);
    //        //Vector3 p0 = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);
    //        //thisTransform.position = Lerp3DNeg(p0, mouseWorld, (PickupYRaise - p0.y) / (mouseWorld.y - p0.y));
    //        // FIXME add select code

    //        //D_Mouse_Screen = Input.mousePosition;
    //        //D_Mouse_World = boardCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, boardCamera.transform.position.z));
    //        //D_Piece_Origin = piecePositionSS;
    //        //D_Piece_Now = thisTransform.position;
    //        //D_Piece_TmpScreen = nextPiecePositionSS;
    //        //         D_Piece_Tmp = p0;
    //        //D_Camera_pos = boardCamera.transform.position;

    //        //         Vector3 mouseWorld = boardCamera.ScreenToWorldPoint(Input.mousePosition);
    //        //thisTransform.position = mouseWorld; // Vector3.Lerp(transform.position, mouseWorld, PickupYRaise);

    //    }
    //}

    //public void OnMouseUp()
    //{
    //    Debug.Log($"mouse up on {CurrentSquare}");
    //    //if (enabled)
    //    //{
    //    //    potentialLandingSquares.Clear();
    //    //    BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);

    //    //    if (potentialLandingSquares.Count == 0)
    //    //    { // piece moved off board
    //    //        thisTransform.position = thisTransform.parent.position;
    //    //        return;
    //    //    }

    //    //    // determine closest square out of potential landing squares.
    //    //    Transform closestSquareTransform = potentialLandingSquares[0].transform;
    //    //    float shortestDistanceFromPieceSquared = (closestSquareTransform.transform.position - thisTransform.position).sqrMagnitude;
    //    //    for (int i = 1; i < potentialLandingSquares.Count; i++)
    //    //    {
    //    //        GameObject potentialLandingSquare = potentialLandingSquares[i];
    //    //        float distanceFromPieceSquared = (potentialLandingSquare.transform.position - thisTransform.position).sqrMagnitude;

    //    //        if (distanceFromPieceSquared < shortestDistanceFromPieceSquared)
    //    //        {
    //    //            shortestDistanceFromPieceSquared = distanceFromPieceSquared;
    //    //            closestSquareTransform = potentialLandingSquare.transform;
    //    //        }
    //    //    }

    //    //    VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);
    //    //}
    //}

    //public void OnPointerClick(PointerEventData eventData)
    //{
    //    Debug.Log($"clicked me {CurrentSquare}");
    //}
}