using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    // Use this for initialization
    CircleCollider2D circle;

	void Start () {
        //Debug.Log(Mathf.Sqrt(4));
        circle = GetComponent<CircleCollider2D>();
        Debug.Log(Mathf.Sin(Mathf.PI/6));
    }
	
	// Update is called once per frame
	void Update () {
        Vector2 center = circle.bounds.center;
        float radius = circle.bounds.extents.x;
        for (int i =0;i<7;i++)
        {
            float angle = Mathf.PI / 6 * i;
            Vector2 ray = center + new Vector2(radius * Mathf.Sin(angle), -radius * Mathf.Cos(angle));
            //Debug.Log(new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)));
            Debug.DrawLine(ray, ray + Vector2.right, Color.red);
        }
	}
}
