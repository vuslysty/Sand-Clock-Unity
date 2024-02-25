using UnityEngine;

public class Cell
{
    public Color Color;
    
    public bool IsFalling;
    public Vector2Int StartFallPosition;
    public Vector2Int EndFallPosition;

    public bool CantMove;
    
    public Vector2Int Position;
}