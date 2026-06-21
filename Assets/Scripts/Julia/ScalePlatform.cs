using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class ScalePlatform : MonoBehaviour
{
    public float totalWeight = 0;
    void Start()
    {
        totalWeight = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Draggable weight))
        {
            Debug.Log("Adding Weight");
            totalWeight += weight.weight;
            Debug.Log(totalWeight);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Draggable weight))
        {
            Debug.Log("Remove weight");
            totalWeight -= weight.weight;
            Debug.Log(totalWeight);
        }
    }

    private void Update()
    {
        
    }
}
