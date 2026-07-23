using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class DraggableAudio : MonoBehaviour
{
    public AudioClip pickupClip;
    public AudioClip placeClip;
    [SerializeField] private LayerMask draggableLayer;

    AudioSource audioSource;
    bool isBeingDragged;

    void Awake() => audioSource = GetComponent<AudioSource>();

    //void Update()
    //{
    //    var cam = Camera.main;
    //    if (cam == null) return;

    //    if (Mouse.current.leftButton.wasPressedThisFrame)
    //    {
    //        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
    //        //Debug.Log("Click detected, raycasting...");
    //        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, draggableLayer))
    //        {
    //            //Debug.Log("Hit: " + hit.collider.gameObject.name);
    //            var hitAudio = hit.collider.GetComponentInParent<DraggableAudio>();
    //            if (hitAudio == this)
    //            {
    //                //Debug.Log("Playing pickup sound, clip = " + pickupClip);
    //                isBeingDragged = true;
    //                audioSource.PlayOneShot(pickupClip);
    //            }
    //        }
    //    }

    //    if (Mouse.current.leftButton.wasReleasedThisFrame && isBeingDragged)
    //    {
    //        isBeingDragged = false;
    //        audioSource.PlayOneShot(placeClip);
    //    }
    //}
}