using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public NetworkMan networkMan;

    void Start()
    {
        networkMan = GameObject.Find("NetworkMan").GetComponent<NetworkMan>();
    }
    // Update is called once per frame
    void Update()
    {
        if (gameObject.name == networkMan.myAddress)
            PlayerMovement();
    }

    private void PlayerMovement()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(-0.5f, 0, 0);

        }


        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(0.5f, 0, 0);
        }


        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(0, 0, 0.5f);
            //transform.Translate(Vector3.forward * Time.deltaTime);
        }


        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(0, 0, -0.5f);
        }
    }
}
