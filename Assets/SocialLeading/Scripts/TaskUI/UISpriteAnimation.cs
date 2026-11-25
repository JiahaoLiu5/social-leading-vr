using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Loops an image through a list of sprites, for the thumbstick hint.</summary>
public class UISpriteAnimation : MonoBehaviour
{
    public Image m_Image;
    public Sprite[] m_SpriteArray;

    [Tooltip("Seconds per sprite.")]
    public float m_Speed = .02f;

    private int m_IndexSprite;
    private Coroutine m_CorotineAnim;

    // Hiding a panel ends its coroutines, so the loop has to start again each time the panel is shown.
    private void OnEnable() => Func_PlayUIAnim();

    private void OnDisable() => Func_StopUIAnim();

    public void Func_PlayUIAnim()
    {
        if (m_CorotineAnim == null)
            m_CorotineAnim = StartCoroutine(Func_PlayAnimUI());
    }

    public void Func_StopUIAnim()
    {
        if (m_CorotineAnim != null)
        {
            StopCoroutine(m_CorotineAnim);
            m_CorotineAnim = null;
        }
    }

    private IEnumerator Func_PlayAnimUI()
    {
        while (true)
        {
            yield return new WaitForSeconds(m_Speed);

            if (m_Image == null || m_SpriteArray == null || m_SpriteArray.Length == 0)
                continue;

            if (m_IndexSprite >= m_SpriteArray.Length)
                m_IndexSprite = 0;

            m_Image.sprite = m_SpriteArray[m_IndexSprite];
            m_IndexSprite += 1;
        }
    }
}
