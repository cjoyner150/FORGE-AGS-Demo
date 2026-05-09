using UnityEngine;
using UnityEngine.UI;

namespace FORGE
{
    public class RestartButton : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button      button;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => gameManager.RestartSession());
        }
    }
}
