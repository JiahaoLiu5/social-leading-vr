using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds the avatar that moves this one and mirrors its head and hand poses into
/// this avatar's "Partner" children, in this avatar's own root space.
///
/// Child order is part of the contract: 0 head, 1 left hand, 2 right hand (own, raw),
/// 3 head, 4 left hand, 5 right hand (partner copies).
/// </summary>
[DefaultExecutionOrder(-60)]
public class GetPartnerTransform : MonoBehaviour
{
    /// <summary>Every avatar that can take part in a link. Filled at runtime.</summary>
    public static List<Transform> sharedEntitiesStatic = new List<Transform>();

    [SerializeField] private List<Transform> sharedEntitiesEditor = new List<Transform>();
    public Transform myTransform;

    [Header("Link")]
    [Tooltip("Tag of the avatar that moves this one. Leave empty to pair with the only other registered avatar.")]
    public string sourceTag = "";

    [Tooltip("Turn off to let this avatar keep its own movement.")]
    public bool linkEnabled = true;

    private float nextSearchTime;

    /// <summary>The avatar that currently moves this one, or null.</summary>
    public Transform Source { get; private set; }

    /// <summary>True while a link is active. The modulators read this every frame.</summary>
    public bool HasSource => linkEnabled && Source != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => sharedEntitiesStatic = new List<Transform>();

    private void OnEnable()
    {
        myTransform = transform;
        if (!sharedEntitiesStatic.Contains(myTransform)) sharedEntitiesStatic.Add(myTransform);
    }

    private void OnDisable()
    {
        sharedEntitiesStatic.Remove(myTransform);
        Source = null;
        UpdateEditorList();
    }

    private void Update()
    {
        Source = FindSource();
        if (Source != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform from = Source.GetChild(i);
                Transform to = myTransform.GetChild(i + 3);
                to.localPosition = from.localPosition;
                to.localRotation = from.localRotation;
            }
        }
        UpdateEditorList();
    }

    private Transform FindSource()
    {
        if (!string.IsNullOrEmpty(sourceTag))
        {
            if (Source != null && Source.CompareTag(sourceTag)) return Source;
            if (Time.unscaledTime < nextSearchTime) return null;
            nextSearchTime = Time.unscaledTime + 0.5f;
            foreach (Transform entity in sharedEntitiesStatic)
                if (entity != null && entity != myTransform && entity.CompareTag(sourceTag)) return entity;
            return null;
        }

        // Default: exactly one other avatar is registered.
        if (sharedEntitiesStatic.Count != 2) return null;
        foreach (Transform entity in sharedEntitiesStatic)
            if (entity != null && entity != myTransform) return entity;
        return null;
    }

    private void UpdateEditorList()
    {
#if UNITY_EDITOR
        sharedEntitiesEditor.Clear();
        sharedEntitiesEditor.AddRange(sharedEntitiesStatic);
#endif
    }
}
