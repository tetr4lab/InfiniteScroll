using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BootLoader : MonoBehaviour {
    /// <summary>���[�h���Ď��g�ɒu��������</summary>
    private void Start () {
        var prefab = Resources.Load<GameObject> ("Prefabs/Canvas");
        var obj = Instantiate (prefab, transform.parent);
        obj.transform.SetSiblingIndex(transform.GetSiblingIndex ());
        Destroy (gameObject);
    }
}
