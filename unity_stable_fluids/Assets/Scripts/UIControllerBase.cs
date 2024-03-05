using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIControllerBase : MonoBehaviour
{
    public virtual void Init(UIIntent intent)
    {

    }

    public virtual void Close()
    {
        GameObject.Destroy(this.gameObject);
    }
}
