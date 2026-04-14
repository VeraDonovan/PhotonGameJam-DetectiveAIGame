using UnityEngine;

public class InteractiveItem : MonoBehaviour {
    void OnMouseDown() {
        Debug.Log("Clicked on " + gameObject.name);
    }
}
