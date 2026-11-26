using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;


public class GetPartnerTransform : MonoBehaviour
{
    public static List<Transform> sharedEntitiesStatic = new List<Transform>();
    [SerializeField] private List<Transform> sharedEntitiesEditor = new List<Transform>();
    public Transform myTransform;

    // Start is called before the first frame update
    void Start()
    {
        myTransform = transform;
        sharedEntitiesStatic.Add(myTransform);
        Debug.Log(sharedEntitiesStatic.Count);
    }

    // Update is called once per frame
    void Update()
    {
        if (sharedEntitiesStatic.Count > 2)
        {
            return;
        }

        foreach (var entity in sharedEntitiesStatic)
        {
            if (entity != myTransform)
            {
                transform.GetChild(3).localPosition = entity.GetChild(0).localPosition;
                transform.GetChild(3).localRotation = entity.GetChild(0).localRotation;
                transform.GetChild(4).localPosition = entity.GetChild(1).localPosition;
                transform.GetChild(4).localRotation = entity.GetChild(1).localRotation;
                transform.GetChild(5).localPosition = entity.GetChild(2).localPosition;
                transform.GetChild(5).localRotation = entity.GetChild(2).localRotation;
            }
        }

        UpdateEditorList();
    }

    void OnDestroy()
    {
        sharedEntitiesStatic.Remove(myTransform);
        UpdateEditorList();
    }

    private void UpdateEditorList()
    {
        sharedEntitiesEditor.Clear();
        sharedEntitiesEditor.AddRange(sharedEntitiesStatic);
    }
}    



