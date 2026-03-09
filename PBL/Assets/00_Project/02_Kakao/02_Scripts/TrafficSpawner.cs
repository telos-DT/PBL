using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;

public class TrafficSpawner : MonoBehaviour
{
    [System.Serializable] public struct PoolCfg { public string name; public GameObject prefab; public int size; }
    [System.Serializable] public struct LaneCfg { public SplineContainer spline; [Range(1, 100)] public int weight; }

    public List<PoolCfg> pools;
    public List<LaneCfg> lanes;
    public float interval = 1.5f;
    private Dictionary<string, Queue<NPCVehicleController>> _poolDict = new Dictionary<string, Queue<NPCVehicleController>>();
    private Dictionary<string, Queue<Self_Driving_NPCVehicleController>> _poolDict_2 = new Dictionary<string, Queue<Self_Driving_NPCVehicleController>>();
    private Dictionary<string, Queue<Taxi_AutonomousController>> _poolDict_3 = new Dictionary<string, Queue<Taxi_AutonomousController>>();
    private int _totalWeight;

    void Awake()
    {
        //foreach (var p in pools)
        //{
        //    var q = new Queue<NPCVehicleController>();
        //    for (int i = 0; i < p.size; i++)
        //    {
        //        var v = Instantiate(p.prefab, transform).GetComponent<NPCVehicleController>();
        //        v.gameObject.SetActive(false); q.Enqueue(v);
        //    }
        //    _poolDict.Add(p.name, q);
        //}

        //일반 자율주행차량 생성시 사용
        foreach (var p in pools)
        {
            var q = new Queue<Self_Driving_NPCVehicleController>();
            for (int i = 0; i < p.size; i++)
            {
                var v = Instantiate(p.prefab, transform).GetComponent<Self_Driving_NPCVehicleController>();
                v.gameObject.SetActive(false); q.Enqueue(v);
            }
            _poolDict_2.Add(p.name, q);
        }

        //foreach (var p in pools)
        //{
        //    var q = new Queue<Taxi_AutonomousController>();
        //    for (int i = 0; i < p.size; i++)
        //    {
        //        var v = Instantiate(p.prefab, transform).GetComponent<Taxi_AutonomousController>();
        //        v.gameObject.SetActive(false); q.Enqueue(v);
        //    }
        //    _poolDict_3.Add(p.name, q);
        //}
        foreach (var l in lanes) _totalWeight += l.weight;
        InvokeRepeating(nameof(Spawn), 1f, interval);
    }

    void Spawn()
    {
        var lane = GetLane();
        var cfg = pools[Random.Range(0, pools.Count)];
        //if (_poolDict[cfg.name].Count > 0)
        //{
        //    var v = _poolDict[cfg.name].Dequeue();
        //    v.transform.position = (Vector3)lane.EvaluatePosition(0);
        //    v.Initialize(lane, this, cfg.name); v.gameObject.SetActive(true);
        //}
        //

        //일반 자율주행차량 생성시 사용
        if (_poolDict_2[cfg.name].Count > 0)
        {
            var v = _poolDict_2[cfg.name].Dequeue();
            v.transform.position = (Vector3)lane.EvaluatePosition(0);
            v.Initialize(lane, this, cfg.name); v.gameObject.SetActive(true);
        }

        //if (_poolDict_3[cfg.name].Count > 0)
        //{
        //    var v = _poolDict_3[cfg.name].Dequeue();
        //    v.transform.position = (Vector3)lane.EvaluatePosition(0);
        //    v.Initialize(lane, this, cfg.name); v.gameObject.SetActive(true);
        //}
    }

    private SplineContainer GetLane()
    {
        int r = Random.Range(0, _totalWeight); int s = 0;
        foreach (var l in lanes) { s += l.weight; if (r < s) return l.spline; }
        return lanes[0].spline;
    }

    public void ReturnToPool(NPCVehicleController v, string n) { v.gameObject.SetActive(false); _poolDict[n].Enqueue(v); }
    public void ReturnToPool(Self_Driving_NPCVehicleController v, string n) { v.gameObject.SetActive(false); _poolDict_2[n].Enqueue(v); }
    public void ReturnToPool(Taxi_AutonomousController v, string n) { v.gameObject.SetActive(false); _poolDict_3[n].Enqueue(v); }


}