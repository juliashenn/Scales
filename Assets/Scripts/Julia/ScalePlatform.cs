using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class ScalePlatform : MonoBehaviour
{
    public float totalWeight = 0;
    void Start()
    {
        ResetWeight();
    }

    public void ResetWeight()
    {
        totalWeight = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        Draggable weight = other.GetComponentInParent<Draggable>();
        if (weight == null)
        {
            Debug.Log($"[ScalePlatform] {name}: trigger entered by '{other.name}' but no Draggable found in parents");
            return;
        }

        totalWeight += weight.weight.Value;
        Debug.Log($"[ScalePlatform] {name}: added weight {weight.weight.Value} from '{weight.name}', totalWeight={totalWeight}");
    }

    private void OnTriggerExit(Collider other)
    {
        Draggable weight = other.GetComponentInParent<Draggable>();
        if (weight == null)
        {
            Debug.Log($"[ScalePlatform] {name}: trigger exited by '{other.name}' but no Draggable found in parents");
            return;
        }

        totalWeight -= weight.weight.Value;
        Debug.Log($"[ScalePlatform] {name}: removed weight {weight.weight.Value} from '{weight.name}', totalWeight={totalWeight}");
    }

    private void Update()
    {
        
    }
}
