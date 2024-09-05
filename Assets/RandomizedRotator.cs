using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizedRotator : MonoBehaviour
{
    [SerializeField] private Vector3[] directions;
    [SerializeField] private Vector3 currentDirection;
    [SerializeField] private float directionChangeTimeInSeconds = 1f, rotateSpeed;

    private void Start()
    {
        Application.targetFrameRate = 120;
        Debug.Log("RanndomizedRotator => Started!");
        ChangeDirection();
    }

    private async void ChangeDirection()
    {
        while (Application.isPlaying)
        {
            Vector3 startDirection = currentDirection;
            Vector3 finalDirection = directions[Random.Range(0, directions.Length)];

            float t = 0f;
            while (t <= 1f)
            {
                t += Time.deltaTime / directionChangeTimeInSeconds;
                currentDirection = Vector3.Lerp(startDirection, finalDirection, t);
                await System.Threading.Tasks.Task.Yield();
            }

            await System.Threading.Tasks.Task.Yield();
        }
    }

    private void Update()
    {
        transform.rotation *= Quaternion.Euler(Camera.main.transform.TransformDirection(transform.forward + currentDirection) * Time.deltaTime * rotateSpeed);
    }
}