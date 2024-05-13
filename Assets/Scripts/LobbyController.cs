using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [SerializeField]
    private int currentSceneIndex;
    [SerializeField]
    private int nextSceneIndex;
    [Header("EXPERIMENT")]
    [SerializeField]
    private bool m_RunExp;
    [SerializeField]
    private Canvas m_ExpCanvas;
    [SerializeField]
    private TextMeshProUGUI m_pID;
    [SerializeField]
    private Dropdown m_ExpCondition;
    [SerializeField]
    private Dropdown m_ExpMode;
    [SerializeField]
    private TextMeshProUGUI m_ExpRange;

    private bool isLoading;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        if (!m_RunExp)
        {
            m_ExpCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Load the next scene specified by "id"
    /// </summary>
    /// <param name="playerTransform"></param>
    /// <param name="id"></param>
    public void LoadScene(int id)
    {
        if (isLoading) return;
        if (currentSceneIndex == id) return;

        Debug.Log($"Loading Scene {id}");

        isLoading = true;
        StartCoroutine(LoadSceneAdditive(id));
    }

    /// <summary>
    /// Load the next scene placing the "player" with the given MyTransform information
    /// </summary>
    /// <param name="playerTransform"></param>
    /// <param name="id"></param>
    public void LoadScene(MyTransform playerTransform)
    {
        if (isLoading) return;
        if (currentSceneIndex == nextSceneIndex) return;

        Debug.Log($"Loading Scene {nextSceneIndex}");

        isLoading = true;

        StartCoroutine(LoadSceneAdditive(playerTransform, nextSceneIndex));// calls the coroutine once every frame till it finishes
    }

    /// <summary>
    /// Load the next scene with the given id only
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    IEnumerator LoadSceneAdditive(int id)
    {
        //get the current Realtime in the scene and disconnect
        //_realTime = GetComponent<Realtime>();
        //_realTime?.Disconnect();
        var loadAsync = SceneManager.LoadSceneAsync(id);

        while (!loadAsync.isDone) yield return null;

        //set the new values when we're done loading the scene
        currentSceneIndex = id;
        isLoading = false;
    }

    /// <summary>
    /// Load the next scene with the given id and player transform to set location
    /// using the realtimeHelper
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    IEnumerator LoadSceneAdditive(MyTransform transform, int id)
    {
        //get the current Realtime in the scene and disconnect
        //_realTime = GetComponent<Realtime>();
        //_realTime?.Disconnect();
        var loadAsync = SceneManager.LoadSceneAsync(id);

        while (!loadAsync.isDone) yield return null;

        if (m_RunExp)
        {
            ExpController expCont = FindObjectOfType<ExpController>();

            expCont.SetPID(m_pID.text);

            if (m_ExpCondition.value == 0)
                expCont.SetCondition("Monocular");
            else
                expCont.SetCondition("Binocular");

            if (m_ExpMode.value == 0)
            {
                expCont.SetRepeats(1);
                expCont.SetNumOfShapes(int.MaxValue);
            }
            else
            {
                expCont.SetRepeats(4);
                expCont.SetNumOfShapes(int.MaxValue);
            }

            float range = float.Parse(m_ExpRange.text) / 100.0f;
            expCont.SetScaleDiff(range);

            expCont.Initialize();
        }


        Debug.Log("getting realtime helper");
        var helper = FindObjectOfType<SceneHelper>();

        if (!helper)
        {
            Debug.Log("realtimehelper not present!!");
            LoadScene(0);
        }
        helper.JoinRoom(transform);
        currentSceneIndex = id;
        isLoading = false;
    }
}
