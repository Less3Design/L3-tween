using System.Collections.Generic;
using UnityEngine;

public class TweenTester : MonoBehaviour
{
    public GameObject prefab;

    public List<GameObject> instances = new List<GameObject>();
    public List<Tween> tweens = new List<Tween>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < 2048; i++)
        {
            GameObject instance = Instantiate(prefab, transform);
            instances.Add(instance);
            tweens.Add(new Tween());
        }
    }

    private void Update()
    {
        //on butotn press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Test();
        }

        int c = Random.Range(0, 20);
        /*
        for (int i = 0; i < c; i++)
        {
            int index = Random.Range(0, instances.Count);
            var pickRand = instances[index];
            Transform t = pickRand.transform;
            Vector3 startPos = pickRand.transform.position;
            Vector3 randPos = Random.insideUnitSphere * 32f;
            tweens[index].Cancel().New(Random.Range(.1f, 1f)).SetOnUpdate((v) =>
            {
                t.transform.position = Vector3.Lerp(startPos, randPos, v);
            });
        }
        */
        c = Random.Range(0, 90);
        for (int i = 0; i < c; i++)
        {
            Tween.Create(1f);
        }

    }

    public void Test()
    {
    }
}
