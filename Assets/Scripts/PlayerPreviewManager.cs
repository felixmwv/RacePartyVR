using UnityEngine;

public class PlayerPreviewManager : MonoBehaviour
{
    public static PlayerPreviewManager Instance;

    [SerializeField] private GameObject[] playerSlots; // wereld vakjes / lights / meshes

    private void Awake()
    {
        Instance = this;
    }

    public void PreviewPlayers(int count)
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i].SetActive(i < count);
        }
    }

    public void ClearPreview()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i].SetActive(false);
        }
    }
}

