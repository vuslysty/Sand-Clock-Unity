using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapView : MonoBehaviour
{
    public RectTransform RectTransform;
    
    public PixelView PixelViewPrefab;
    public GridLayoutGroup GridLayoutGroup;

    public float DelaySeconds;
    public float TimeInSeconds;
    
    private readonly Dictionary<Vector2Int, PixelView> _pixels = new Dictionary<Vector2Int, PixelView>();
    private SandClock _sandClock;
    private Map _map;

    private float _prevUpdateTime;
    
    private void Start()
    {
        _sandClock = new SandClock();
        _map = _sandClock.Map;

        int width = _map.Max.x - _map.Min.x + 1;
        int height = _map.Max.y - _map.Min.y + 1;

        // GridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        // GridLayoutGroup.constraintCount = width;
        //
        // float sideLength = 
        
        for (int y = _map.Min.y - 1; y <= _map.Max.y + 1; y++)
        {
            for (int x = _map.Min.x - 1; x <= _map.Max.x + 1; x++)
            {
                PixelView pixelView = Instantiate(PixelViewPrefab, transform);
                pixelView.gameObject.SetActive(true);

                var pos = new Vector2Int(x, y);

                pixelView.name = pos.ToString();

                _pixels[pos] = pixelView;
            }
        }
        
        _sandClock.Simulate(Vector2.one);
    }

    private float _nextOpenTime;

    public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        // Спершу відобразимо вхідне значення на діапазон від 0 до 1
        float normalized = (value - fromMin) / (fromMax - fromMin);
        
        // Потім відобразимо його на вихідний діапазон
        return toMin + normalized * (toMax - toMin);
    }
    
    private float GetDelayBetweenUpdates(Vector3 acceleration)
    {
        float z = MathF.Abs(acceleration.z);
        z = Math.Clamp(z, 0, 1);

        if (z <= 0.6)
            return DelaySeconds;

        if (z > 0.6 && z <= 0.9)
            return Map(z, 0.6f, 0.9f, DelaySeconds, 0.5f);

        if (z > 0.9 && z <= 0.95)
            return Map(z, 0.9f, 0.95f, 0.5f, 1);
        
        return Map(z, 0.95f, 1, 1, 10);
    }
    
    void Update()
    {
        Vector3 acceleration = Quaternion.Euler(0, 0, 45) * Input.acceleration;
        
        float time = Time.time;

        float delay = GetDelayBetweenUpdates(acceleration);
        float diff = time - _prevUpdateTime;
        
        if (diff < delay)
            return;
        
        Debug.Log($"Diff: {diff}, Delay: {delay}");
        
        _prevUpdateTime = time;

        if (time > _nextOpenTime)
        {
            float period = TimeInSeconds / _sandClock._cellsMap.Count;
            
            _nextOpenTime = time + period;

            _sandClock.SetTransitionEnabled(true);
        }
        
        // float angle = Vector2.SignedAngle(transform.up, Vector2.up);
        // angle -= 90;
        //_sandClock.Simulate(SandClock.AngleToDirection(angle));
        
        _sandClock.Simulate(acceleration);
        Debug.Log(Input.acceleration);
        
        foreach (var pair in _pixels)
        {
            if (_sandClock._cellsMap.TryGetValue(pair.Key, out Cell cell))
            {
                pair.Value.Image.color = cell.Color;
            }
            else
            {
                pair.Value.Image.color = _map.IsMovable(pair.Key) ? Color.gray : Color.black;
            }
        }
        
        _sandClock.SetTransitionEnabled(false);
    }
}