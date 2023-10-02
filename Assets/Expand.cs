using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Expand : MonoBehaviour
{
    float life=0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        life+=Time.deltaTime;
        float size = life*20;
        transform.localScale = new Vector3(size,size);
    }
}
