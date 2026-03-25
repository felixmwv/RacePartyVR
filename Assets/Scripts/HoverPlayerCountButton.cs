using UnityEngine;
using UnityEngine.EventSystems;

public class HoverPlayerCountButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private int previewPlayerCount;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayerPreviewManager.Instance.PreviewPlayers(previewPlayerCount);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayerPreviewManager.Instance.ClearPreview();
    }
}

