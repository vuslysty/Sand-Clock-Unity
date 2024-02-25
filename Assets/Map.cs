﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Map
{
    private readonly Dictionary<Vector2Int, bool> _data = new();
    private readonly Dictionary<Vector2Int, int> _bakeData = new();
    
    private Vector2Int _min;
    private Vector2Int _max;
    
    public Vector2Int Min => _min;
    public Vector2Int Max => _max;

    public void BakeData(List<Cell> cells)
    {
        _bakeData.Clear();
        
        foreach (var cell in cells)
        {
            var bakeIndex = new Vector2Int(cell.Position.x / 4, cell.Position.y / 4);
            int value = _bakeData.GetValueOrDefault(bakeIndex, 0);
            int shift = 4 * (cell.Position.x % 4) + cell.Position.y % 4;
            
            value |= 1 << shift;
            _bakeData[bakeIndex] = value;
        }
    }

    public bool HasInBakeDataAt(Vector2Int pos)
    {
        var bakeIndex = new Vector2Int(pos.x / 4, pos.y / 4);
        Int64 value = _bakeData.GetValueOrDefault(bakeIndex, 0);
        int shift = 4 * (pos.x % 4) + pos.y % 4;

        return (value & (1 << shift)) != 0;
    }
    
    public void AddData(Vector2Int position, bool isMovable)
    {
        if (_data.Count == 0)
        {
            _min = position;
            _max = position;
        }
        else
        {
            int minY = Math.Min(_min.y, position.y);
            int minX = Math.Min(_min.x, position.x);

            _min = new Vector2Int(minX, minY);
            
            int maxY = Math.Max(_max.y, position.y);
            int maxX = Math.Max(_max.x, position.x);

            _max = new Vector2Int(maxX, maxY);
        }
        
        _data[position] = isMovable;
    }

    public bool IsMovable(Vector2Int position)
    {
        return _data.GetValueOrDefault(position, false);
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        
        for (int y = _max.y + 1; y >= _min.y - 1; y--)
        {
            for (int x = _min.x - 1; x <= _max.x + 1; x++)
            {
                var pos = new Vector2Int(x, y);

                stringBuilder.Append(IsMovable(pos) ? ' ' : '#');
            }

            stringBuilder.Append('\n');
        }

        return stringBuilder.ToString();
    }
}