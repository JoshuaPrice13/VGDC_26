using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetBox : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        BallController ball = other.GetComponent<BallController>();
        if (ball != null)
            Debug.Log("Hit obstcle");
        ball.ResetBall();
    }
}
