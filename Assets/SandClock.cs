using System;
using System.Collections.Generic;
using UnityEngine;
using VectorInt2 = UnityEngine.Vector2Int;

public class SandClock
{
    private readonly VectorInt2[] _directions = new[]
    {
        new VectorInt2(1, 0),
        new VectorInt2(1, 1),
        new VectorInt2(0, 1),
        new VectorInt2(-1, 1),
        new VectorInt2(-1, 0),
        new VectorInt2(-1, -1),
        new VectorInt2(0, -1),
        new VectorInt2(1, -1),
    };

    readonly VectorInt2[] _buffer = new VectorInt2[4];

    readonly Map _map = new Map();
    public readonly Dictionary<Vector2Int, Cell> _cellsMap = new Dictionary<Vector2Int, Cell>();
    readonly List<Cell> _cells = new List<Cell>();
    private readonly HashSet<(VectorInt2, VectorInt2)> _closedTransitions = new HashSet<(VectorInt2, VectorInt2)>();
    
    private uint [,] _bakeData = null;

    private void BakeData()
    {
        if (_bakeData == null)
        {
            VectorInt2 min = GetBakeIndex(_map.Min);
            VectorInt2 max = GetBakeIndex(_map.Max);

            _bakeData = new uint[max.x - min.x + 1, max.y - min.y + 1];
        }
        
        for (int i = 0; i < _bakeData.GetLength(0); i++)
        {
            for (int j = 0; j < _bakeData.GetLength(1); j++)
            {
                _bakeData[i, j] = 0;
            }
        }
        
        foreach (var cell in _cells)
        {
            Vector2Int bakeIndex = GetBakeIndex(cell.Position);
            uint value = _bakeData[bakeIndex.x, bakeIndex.y];
            
            value |= (uint)(1 << GetShift(cell.Position));
            _bakeData[bakeIndex.x, bakeIndex.y] = value;
        }
    }

    private bool HasInBakeDataAt(Vector2Int pos)
    {
        Vector2Int bakeIndex = GetBakeIndex(pos);
        uint value = _bakeData[bakeIndex.x, bakeIndex.y];
        
        return (value & (1 << GetShift(pos))) != 0;
    }

    private int GetShift(Vector2Int pos) => pos.x % 8 + (pos.y % 4) * 8;

    private Vector2Int GetBakeIndex(Vector2Int pos) => new(pos.x / 8, pos.y / 4);
    
    bool IsClosedTransition(VectorInt2 posA, VectorInt2 posB)
    {
        foreach (var tuple in _closedTransitions)
        {
            if (tuple == (posA, posB) || tuple == (posB, posA))
                return true;
        }

        return false;
    }
    
    public Map Map => _map;

    public void SetTransitionEnabled(bool enabled)
    {
        var a = new VectorInt2(7, 8);
        var b = new VectorInt2(8, 7);
        
        if (enabled)
        {
            _closedTransitions.Remove((a, b));
        }
        else
        {
            _closedTransitions.Add((a, b));
        }
    }

    public SandClock()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 8; y < 16; y++)
            {
                _map.AddData(new VectorInt2(x, y), true);
            }
        }

        for (int x = 8; x < 16; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                _map.AddData(new VectorInt2(x, y), true);
            }
        }

        for (int x = 0; x < 8; x++)
        {
            for (int y = 10; y < 14; y++)
            {
                var pos = new VectorInt2(x, y);
                
                Cell cell = new Cell()
                {
                    Color = Color.yellow,
                    Position = pos
                };
                _cellsMap.Add(pos, cell);
                _cells.Add(cell);
            }
        }

        float hueIncrement = 1f / _cells.Count;
        
        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].Color = Color.HSVToRGB(i * hueIncrement, 1f, 0.9f);
        }
    }

    public void Simulate(Vector2 direction)
    {
        BakeData();

        float angleInDegrees = DirectionToDegrees(direction);

        foreach (var cell in _cells)
        {
            // Try stop falling
            if (cell.IsFalling)
            {
                VectorInt2 endFallPos = GetEndFallPos(cell.StartFallPosition, direction);

                if (endFallPos != cell.EndFallPosition)
                    cell.IsFalling = false;
            }

            VectorInt2 nextPos = cell.IsFalling
                ? GetLinePointAtIteration(cell.StartFallPosition, direction, cell.FallingIndex + 1)
                : GetLinePointAtIteration(cell.Position, direction, 1);

            if (_map.IsMovable(nextPos) && !IsClosedTransition(cell.Position, nextPos))
            {
                if (_cellsMap.TryGetValue(nextPos, out Cell nextPosCell))
                {
                    if (TryMakeMove(cell, angleInDegrees, nextPos))
                        cell.IsFalling = false;
                }
                else if (HasInBakeDataAt(nextPos))
                {
                    continue;
                }
                else
                {
                    _cellsMap.Remove(cell.Position);
                    _cellsMap[nextPos] = cell;

                    if (!cell.IsFalling)
                    {
                        cell.IsFalling = true;
                        cell.FallingIndex = 1;
                        cell.StartFallPosition = cell.Position;
                        cell.EndFallPosition = GetEndFallPos(cell.StartFallPosition, direction);
                    }
                    else
                    {
                        cell.FallingIndex++;
                    }

                    cell.Position = nextPos;
                }
            }
            else
            {
                cell.IsFalling = false;

                TryMakeMove(cell, angleInDegrees, nextPos);
            }
        }
    }

    static float AnglesToRadians(float degrees) => degrees * (MathF.PI / 180);

    static float RadiansToAngles(float radians) => (radians / MathF.PI) * 180;

    public static Vector2 AngleToDirection(float degrees)
    {
        // Перетворення градусів в радіани
        float angleRadians = AnglesToRadians(degrees);

        // Визначення компонент вектора за допомогою тригонометричних функцій
        float x = MathF.Cos(angleRadians);
        float y = MathF.Sin(angleRadians);

        return new Vector2(x, y);
    }

    public static float DirectionToDegrees(Vector2 direction)
    {
        // Визначення арктангенса та конвертація радіан в градуси
        float angleInRadians = (float)Math.Atan2(direction.y, direction.x);
        float angleInDegrees = RadiansToAngles(angleInRadians);

        // Коригування від'ємних кутів
        if (angleInDegrees < 0)
        {
            angleInDegrees += 360;
        }

        return angleInDegrees;
    }

    VectorInt2 GetEndFallPos(VectorInt2 startFallPos, Vector2 direction)
    {
        Vector2 startFallPosCenter = startFallPos;
        startFallPosCenter += Vector2.one / 2;

        Vector2 endFallCenter = startFallPosCenter + direction * 10;
        VectorInt2 endFallPos = new VectorInt2((int)endFallCenter.x, (int)endFallCenter.y);

        return endFallPos;
    }
    
    VectorInt2 GetLinePointAtIteration(VectorInt2 start, Vector2 direction, int targetIteration)
    {
        int x0 = start.x;
        int y0 = start.y;

        int x1 = (int)(x0 + targetIteration * 10 * direction.x);
        int y1 = (int)(y0 + targetIteration * 10 * direction.y);

        return GetLinePointAtIteration(start, new VectorInt2(x1, y1), targetIteration);
    }
    
    VectorInt2 GetLinePointAtIteration(VectorInt2 start, VectorInt2 end, int targetIteration)
    {
        if (targetIteration <= 0)
            return start;

        int iterations = 0;
        
        int x0 = start.x;
        int y0 = start.y;

        int x1 = end.x;
        int y1 = end.y;

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        
        if (dy <= dx)
        {
            int d = (dy << 1) - dx;
            int d1 = dy << 1;
            int d2 = (dy - dx) << 1;
            
            for (int x = x0 + sx, y = y0, i = 1; i <= dx; i++, x += sx)
            {
                if (d > 0)
                {
                    d += d2;
                    y += sy;
                }
                else
                    d += d1;

                iterations++;
                
                if (iterations == targetIteration)
                    return new VectorInt2(x, y);
            }
        }
        else
        {
            int d = (dx << 1) - dy;
            int d1 = dx << 1;
            int d2 = (dx - dy) << 1;

            for (int y = y0 + sy, x = x0, i = 1; i <= dy; i++, y += sy)
            {
                if (d > 0)
                {
                    d += d2;
                    x += sx;
                }
                else
                    d += d1;

                iterations++;
                
                if (iterations == targetIteration)
                    return new VectorInt2(x, y);
            }
        }

        return end;
    }

    int GetIndexForAngle(float angleInDegrees)
    {
        angleInDegrees %= 360;
        
        if (angleInDegrees is >= 0 and < 90)
            return 1;
        if (angleInDegrees is >= 90 and < 180)
            return 3;
        if (angleInDegrees is >= 180 and < 270)
            return 5;
        if (angleInDegrees is >= 270 and < 360)
            return 7;

        throw new ArgumentException("Invalid angle. Angle must be in the range [0, 360).");
        
        // The same as above, but by formula. Leave a more readable version
        return (int)(angleInDegrees / 90) * 2 + 1;
    }

    int GetIndexOffset(int index, int offset) => ((index + offset) % 8 + 8) % 8;

    int GetSortedMovePoints(float angleInDegrees, VectorInt2[] buff)
    {
        const float N = 1;

        int quadrantIndex = GetIndexForAngle(angleInDegrees);
        float angleFrom0To90 = angleInDegrees % 90;

        float distToFirst = MathF.Abs(angleInDegrees - 0);
        float distToSecond = MathF.Abs(angleInDegrees - 45);
        float distToThird = MathF.Abs(angleInDegrees - 90);

        int amount;
        
        if (angleFrom0To90 is >= 0 and < N)
        {
            if (distToFirst < distToSecond)
            {
                buff[0] = _directions[GetIndexOffset(quadrantIndex, -1)];
                buff[1] = _directions[quadrantIndex];
            }
            else
            {
                buff[0] = _directions[quadrantIndex];
                buff[1] = _directions[GetIndexOffset(quadrantIndex, -1)];
            }

            amount = 2;
        }
        else if (angleFrom0To90 is >= 90 - N and < 90)
        {
            if (distToSecond < distToThird)
            {
                buff[0] = _directions[quadrantIndex];
                buff[1] = _directions[GetIndexOffset(quadrantIndex, 1)];
            }
            else
            {
                buff[0] = _directions[GetIndexOffset(quadrantIndex, 1)];
                buff[1] = _directions[quadrantIndex];
            }

            amount = 2;
        }
        else
        {
            if (angleFrom0To90 > 45)
            {
                if (distToSecond < distToThird)
                {
                    buff[0] = _directions[quadrantIndex];
                    buff[1] = _directions[GetIndexOffset(quadrantIndex, 1)];
                }
                else
                {
                    buff[0] = _directions[GetIndexOffset(quadrantIndex, 1)];
                    buff[1] = _directions[quadrantIndex];
                }

                buff[2] = _directions[GetIndexOffset(quadrantIndex, -1)];
            }
            else
            {
                if (distToFirst < distToSecond)
                {
                    buff[0] = _directions[GetIndexOffset(quadrantIndex, -1)];
                    buff[1] = _directions[quadrantIndex];
                }
                else
                {
                    buff[0] = _directions[quadrantIndex];
                    buff[1] = _directions[GetIndexOffset(quadrantIndex, -1)];
                }

                buff[2] = _directions[GetIndexOffset(quadrantIndex, 1)];
            }

            amount = 3;
        }

        if (angleFrom0To90 > 50)
        {
            buff[amount] = _directions[GetIndexOffset(quadrantIndex, 2)];
            amount++;
        }
        else if (angleFrom0To90 < 40)
        {
            buff[amount] = _directions[GetIndexOffset(quadrantIndex, -2)];
            amount++;
        }

        return amount;
    }

    bool TryMakeMove(Cell cell, float angleInDegrees, VectorInt2 exceptPoint)
    {
        int points = GetSortedMovePoints(angleInDegrees, _buffer);

        for (int i = 0; i < points; i++)
        {
            VectorInt2 checkPoint = cell.Position + _buffer[i];

            if (checkPoint == exceptPoint)
                continue;

            if (!_map.IsMovable(checkPoint) || IsClosedTransition(cell.Position, checkPoint))
                continue;

            if (_cellsMap.TryGetValue(checkPoint, out Cell nextCell))
                continue;

            if (HasInBakeDataAt(checkPoint))
                return false;

            _cellsMap.Remove(cell.Position);
            _cellsMap[checkPoint] = cell;
            cell.Position = checkPoint;
            
            return true;
        }
        
        return true;
    }
}