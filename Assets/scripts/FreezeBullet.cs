using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeBullet : MonoBehaviour {

    [SerializeField]
    float speed;

    [HideInInspector]
    public Vector2 direction;


    private void Awake()
    {
        Destroy(gameObject, 4);
    }

    private void Update()
    {
        //transform.Translate(direction*speed*Time.deltaTime,Space.World);
        transform.Translate(Vector3.up * speed * Time.deltaTime, Space.Self);
    }

    public void Init(Vector2 shotDir)
    {
        direction = shotDir;

    }
}
