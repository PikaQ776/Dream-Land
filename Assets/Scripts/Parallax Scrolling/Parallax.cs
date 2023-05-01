using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    // Start is called before the first frame update
    private float spriteXLen,spriteStartX;
    public GameObject cam;
    public float parallaxEffect;
    void Start()
    {
        spriteStartX=transform.position.x;
        spriteXLen=GetComponent<SpriteRenderer>().bounds.size.x;
    }
    
    // Update is called once per frame
    void Update()
    {
        float dist=(cam.transform.position.x*parallaxEffect);
        transform.position=new Vector3(spriteStartX+dist,cam.transform.position.y,transform.position.z);
    }
}
