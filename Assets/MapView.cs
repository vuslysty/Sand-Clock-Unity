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

    private float _nextUpdateTime;
    
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
    }

    private float _nextOpenTime;
    
    void Update()
    {
        float time = Time.time;
        
        if (time < _nextUpdateTime)
            return;

        _nextUpdateTime = time + DelaySeconds;

        if (time > _nextOpenTime)
        {
            float period = TimeInSeconds / _sandClock._cellsMap.Count;
            
            _nextOpenTime = time + period;

            _sandClock.SetTransitionEnabled(true);
        }
        
        float angle = Vector2.SignedAngle(transform.up, Vector2.up);
        angle -= 90;
        
        _sandClock.Simulate(SandClock.AngleToDirection(angle));
        
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