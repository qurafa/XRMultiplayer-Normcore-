using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
//using Normal.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [SerializeField]
    private int currentSceneIndex;
    [Header("EXPERIMENT")]
    [SerializeField]
    private bool m_RunExp;
    [SerializeField]
    private Dropdown m_ExpModeDropdown;
    [SerializeField]
    private Dropdown m_ExpRangeDropdown;

    private bool isLoading;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
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
    public void LoadScene(MyTransform playerTransform, int id)
    {
        if (isLoading) return;
        if (currentSceneIndex == id) return;

        Debug.Log($"Loading Scene {id}");

        isLoading = true;

        StartCoroutine(LoadSceneAdditive(playerTransform, id));
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

            if (m_ExpModeDropdown.value == 0) expCont.SetRepeats(1);
            else expCont.SetRepeats(3);

            float range = float.Parse(m_ExpRangeDropdown.options[m_ExpRangeDropdown.value].text)/100.0f;
            expCont.SetScaleDiff(range);
        }

        Debug.Log("getting realtime helper");
        realtimeHelper helper = FindObjectOfType<realtimeHelper>();

        if (helper) Debug.Log("Helper Name: " + helper.name);
        helper.JoinMainRoom(transform);

        currentSceneIndex = id;
        isLoading = false;
    }
}
