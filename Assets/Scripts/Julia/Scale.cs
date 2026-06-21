using UnityEngine;

public class Scale : MonoBehaviour
{
    public ScalePlatform leftPan;
    public ScalePlatform rightPan;
    public GameObject topBar;
    public GameObject leftSide;
    public GameObject rightSide;

    [Header("Scale Movement")]
    [SerializeField] private float tiltFactor = 5f;
    [SerializeField] private float verticalFactor = 0.35f;
    [SerializeField] private float maxTilt = 30f;
    [SerializeField] private float maxVertical = 2.1f;

    private float currDifference;
    void Start()
    {
        topBar.transform.rotation = Quaternion.AngleAxis(90f, Vector3.forward);
    }

    void Update()
    {
        if (currDifference != rightPan.totalWeight - leftPan.totalWeight)
        {
            currDifference = rightPan.totalWeight - leftPan.totalWeight;
            topBar.transform.rotation = Quaternion.AngleAxis(90f - Mathf.Clamp(currDifference*tiltFactor, -maxTilt, maxTilt), Vector3.forward);
            leftSide.transform.position = new Vector3(0, Mathf.Clamp(currDifference *verticalFactor, -maxVertical, maxVertical), 0);
            rightSide.transform.position = new Vector3(0, Mathf.Clamp(-currDifference * verticalFactor, -maxVertical, maxVertical), 0);
        }
    }
}
