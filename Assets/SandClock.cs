using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VectorInt2 = UnityEngine.Vector2Int;

public class SandClock
{
    readonly VectorInt2[] _quadrant1 = { new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(1, -1) };
    readonly VectorInt2[] _quadrant2 = { new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(1, 1) };
    readonly VectorInt2[] _quadrant3 = { new(-1, 0), new(-1, -1), new(0, -1), new(1, -1), new(-1, 1) };
    readonly VectorInt2[] _quadrant4 = { new(0, -1), new(1, -1), new(1, 0), new(1, 1), new(-1, -1) };

    readonly VectorInt2[] _buffer = new VectorInt2[4];

    readonly Map _map = new Map();
    public readonly Dictionary<Vector2Int, Cell> _cellsMap = new Dictionary<Vector2Int, Cell>();
    readonly List<Cell> _cells = new List<Cell>();
    private readonly HashSet<(VectorInt2, VectorInt2)> _closedTransitions = new HashSet<(VectorInt2, VectorInt2)>();

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
        _map.BakeData(_cells);
        _cells.ForEach(cell => cell.CantMove = false);

        float angleInDegrees = DirectionToDegrees(direction);
        
        List<Cell> sortedCells = _cells.OrderByDescending(cell => cell.Position.x * direction.x + cell.Position.y * direction.y).ToList();

        foreach (var cell in sortedCells)
        {
            // Try stop falling
            if (cell.IsFalling)
            {
                VectorInt2 endFallPos = GetEndFallPos(cell.StartFallPosition, direction);

                if (endFallPos != cell.EndFallPosition)
                    cell.IsFalling = false;
            }

            VectorInt2 nextPos = cell.IsFalling
                ? GetNextPointWhenFalling(cell.Position, cell.StartFallPosition, direction)
                : GetNextPoint(cell.Position, direction);

            if (_map.IsMovable(nextPos) && !IsClosedTransition(cell.Position, nextPos))
            {
                if (_cellsMap.TryGetValue(nextPos, out Cell nextPosCell))
                {
                    TryMakeMove(cell, angleInDegrees, nextPos);
                }
                else if (_map.HasInBakeDataAt(nextPos))
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
                        cell.StartFallPosition = cell.Position;
                        cell.EndFallPosition = GetEndFallPos(cell.Position, direction);
                    }

                    cell.Position = nextPos;
                }
            }
            else
            {
                // Need add a logic for falling in right place
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

        Vector2 endFallCenter = startFallPosCenter + direction * 100;
        VectorInt2 endFallPos = new VectorInt2((int)endFallCenter.x, (int)endFallCenter.y);

        return endFallPos;
    }

    VectorInt2 GetNextPoint(VectorInt2 currentPoint, Vector2 direction)
    {
        foreach (var point in GetLinePointsByDirection(currentPoint, direction, 1000))
        {
            if (point == currentPoint)
                continue;

            return point;
        }

        throw new Exception();
    }

    VectorInt2 GetNextPointWhenFalling(VectorInt2 currentPoint, VectorInt2 startFallPoint, Vector2 direction)
    {
        float prevDistance = Single.MaxValue;

        foreach (VectorInt2 point in GetLinePointsByDirection(startFallPoint, direction, 1000))
        {
            float distance = Vector2.Distance(point, currentPoint);

            if (distance > prevDistance && distance > 0.5f)
                return point;

            prevDistance = distance;
        }

        throw new Exception();
    }

    static IEnumerable<VectorInt2> GetLinePointsByDirection(VectorInt2 start, Vector2 direction, int count)
    {
        direction = direction.normalized;

        int x0 = start.x;
        int y0 = start.y;

        int x1 = (int)(x0 + (count - 1) * direction.x);
        int y1 = (int)(y0 + (count - 1) * direction.y);

        foreach (VectorInt2 point in GetLinePoints(start, new VectorInt2(x1, y1)))
        {
            yield return point;
        }
    }

    static IEnumerable<VectorInt2> GetLinePoints(VectorInt2 start, VectorInt2 end)
    {
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
            yield return new VectorInt2(x0, y0);
            for (int x = x0 + sx, y = y0, i = 1; i <= dx; i++, x += sx)
            {
                if (d > 0)
                {
                    d += d2;
                    y += sy;
                }
                else
                    d += d1;

                yield return new VectorInt2(x, y);
            }
        }
        else
        {
            int d = (dx << 1) - dy;
            int d1 = dx << 1;
            int d2 = (dx - dy) << 1;
            yield return new VectorInt2(x0, y0);
            for (int y = y0 + sy, x = x0, i = 1; i <= dy; i++, y += sy)
            {
                if (d > 0)
                {
                    d += d2;
                    x += sx;
                }
                else
                    d += d1;

                yield return new VectorInt2(x, y);
            }
        }
    }

    VectorInt2[] GetQuadrantForAngle(float angleInDegrees)
    {
        if (angleInDegrees is >= 0 and < 90)
            return _quadrant1;
        if (angleInDegrees is >= 90 and < 180)
            return _quadrant2;
        if (angleInDegrees is >= 180 and < 270)
            return _quadrant3;
        if (angleInDegrees is >= 270 and < 360)
            return _quadrant4;

        throw new ArgumentException("Invalid angle. Angle must be in the range [0, 360).");
    }

    int GetSortedMovePoints(float angleInDegrees, VectorInt2[] buff)
    {
        const float N = 1;

        VectorInt2[] quadrant = GetQuadrantForAngle(angleInDegrees);
        float angleFrom0To90 = angleInDegrees % 90;

        float distToFirst = MathF.Abs(angleInDegrees - 0);
        float distToSecond = MathF.Abs(angleInDegrees - 45);
        float distToThird = MathF.Abs(angleInDegrees - 90);

        int amount;
        
        if (angleFrom0To90 is >= 0 and < N)
        {
            if (distToFirst < distToSecond)
            {
                buff[0] = quadrant[0];
                buff[1] = quadrant[1];
            }
            else
            {
                buff[0] = quadrant[1];
                buff[1] = quadrant[0];
            }

            amount = 2;
        }
        else if (angleFrom0To90 is >= 90 - N and < 90)
        {
            if (distToSecond < distToThird)
            {
                buff[0] = quadrant[1];
                buff[1] = quadrant[2];
            }
            else
            {
                buff[0] = quadrant[2];
                buff[1] = quadrant[1];
            }

            amount = 2;
        }
        else
        {
            if (angleFrom0To90 > 45)
            {
                if (distToSecond < distToThird)
                {
                    buff[0] = quadrant[1];
                    buff[1] = quadrant[2];
                }
                else
                {
                    buff[0] = quadrant[2];
                    buff[1] = quadrant[1];
                }

                buff[2] = quadrant[0];
            }
            else
            {
                if (distToFirst < distToSecond)
                {
                    buff[0] = quadrant[0];
                    buff[1] = quadrant[1];
                }
                else
                {
                    buff[0] = quadrant[1];
                    buff[1] = quadrant[0];
                }

                buff[2] = quadrant[2];
            }

            amount = 3;
        }

        if (angleFrom0To90 > 50)
        {
            buff[amount] = quadrant[3];
            amount++;
        }
        else if (angleFrom0To90 < 40)
        {
            buff[amount] = quadrant[4];
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

            if (_map.HasInBakeDataAt(checkPoint))
                return false;

            _cellsMap.Remove(cell.Position);
            _cellsMap[checkPoint] = cell;
            cell.Position = checkPoint;
            
            return true;
        }
        
        return true;
    }
}