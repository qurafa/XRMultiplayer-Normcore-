using Normal.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExpController : MonoBehaviour
{
    [SerializeField]
    private bool ToRun = false;
    [SerializeField]
    private Realtime m_Realtime;
    [SerializeField]
    private Image m_UIPanel;
    [SerializeField]
    private Camera m_Camera;
    [SerializeField]
    private float m_PanelTimeLimit = 10.0f;
    [SerializeField]
    private Transform m_ShapeSpawn;
    /// <summary>
    /// Name of the shapes we'll be using, ensure they're in the Resource folder
    /// </summary>
    [SerializeField]
    private string[] m_Shapes = new string[] { };
    [SerializeField]
    private int m_NumberSmaller = 0;
    [SerializeField]
    private int m_NumberLarger = 0;
    [SerializeField]
    private int m_Repeats = 0;
    [SerializeField]
    private float diff = 0.1f;

    [Header("INPUT ACTIONS")]
    [SerializeField]
    private InputAction largerButton;
    [SerializeField]
    private InputAction smallerButton;
    [SerializeField]
    private InputAction resetButton;

    private List<string> _order;
    private int _cTrialNumber = -1;

    private GameObject spawn;

    //important variables
    private readonly static string LARGER_RESPONSE = "Larger";
    private readonly static string SMALLER_RESPONSE = "Smaller";
    private static string FILE_PATH = "";
    private static StringBuilder FILE_TEMP;
    private static string SEPARATOR = ",";
    private static string[] HEADING = {"Shape", "Size", "Response"};

    //next trial loading flag
    private bool _nextLoading = false;

    //blank timer and flags
    private float _blankTimer = 0;
    private bool _blankLoading = false;

    void OnEnable()
    {
        largerButton.Enable(); largerButton.performed += SaveLargerResponse;
        smallerButton.Enable(); smallerButton.performed += SaveSmallerResponse;
        resetButton.Enable(); resetButton.performed += Reset;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (m_Realtime == null)
        {
            Debug.Log("Realtime not specified");
            return;
        }
        m_Realtime.didConnectToRoom += DidConnectToRoom;
    }

    void FixedUpdate()
    {
        if (!_blankLoading) return;
        _blankTimer += Time.deltaTime;
        if(_blankTimer > m_PanelTimeLimit)
        {
            //see everything
            m_Camera.cullingMask = -1;
            _blankTimer = 0;
            _blankLoading = false;
        }
    }

    private void DidConnectToRoom(Realtime realtime)
    {
        if(!ToRun) return;

        //Set up sexperiment order
        CreateExpOrder();
        ShuffleOrder();

        //Create file path to store entries 
        CreateExpFile();

        //Start exp
        NextTrial();
    }

    private bool CreateExpOrder()
    {
        if (_order != null) return true;

        _order = new List<string>();
        string entry = "";
        int s = 0;

        //for every shape
        foreach (string shape in m_Shapes)
        {
            //for every repeat for each shape
            for (int r = 0; r < m_Repeats; r++)
            {
                //mid size
                s = UnityEngine.Random.Range(0, m_Shapes.Length);
                entry = $"{shape}|{1}";//scale 1
                _order.Add(entry);

                //smaller sizes
                for (int zS = 1; zS <= m_NumberSmaller; zS++)
                {
                    s = UnityEngine.Random.Range(0, m_Shapes.Length);
                    entry = $"{shape}|{1 - (diff * zS)}";//smaller scales
                    _order.Add(entry);
                }

                //larger sizes
                for (int zL = 1; zL <= m_NumberLarger; zL++)
                {
                    s = UnityEngine.Random.Range(0, m_Shapes.Length);
                    entry = $"{shape}|{1 + (diff * zL)}";//larger scales
                    _order.Add(entry);
                }
            }
        }
        /*        for (int r = 0; r < m_Repeats; r++)
                {
                    s = UnityEngine.Random.Range(0, m_Shapes.Length);
                    entry = $"{m_Shapes[s]}|{1}";//scale 1
                    _order.Add(entry);

                    for (int zS = 1; zS <= m_NumberSmaller; zS++)
                    {
                        s = UnityEngine.Random.Range(0, m_Shapes.Length);
                        entry = $"{m_Shapes[s]}|{1 - (diff * zS)}";//smaller scales
                        _order.Add(entry);
                    }

                    for (int zL = 1; zL <= m_NumberLarger; zL++)
                    {
                        s = UnityEngine.Random.Range(0, m_Shapes.Length);
                        entry = $"{m_Shapes[s]}|{1 + (diff * zL)}";//larger scales
                        _order.Add(entry);
                    }
                }*/
        
        /*for(int s = 0; s < m_Shapes.Length; s++)
        {}*/

        return true;
    }

    public List<string> GetOrder()
    { 
        return _order; 
    }

    private void ShuffleOrder()
    {
        for (int n = 0; n < _order.Count; n++)
        {
            int i = UnityEngine.Random.Range(0, _order.Count - 1);

            string temp = _order[i];
            _order[i] = _order[n];
            _order[n] = temp;
        }
    }

    private void SaveLargerResponse(InputAction.CallbackContext obj)
    {
        if(!ToRun) return;
        SaveCurrentEntry(LARGER_RESPONSE);
        NextTrial();
    }

    private void SaveSmallerResponse(InputAction.CallbackContext obj)
    {
        if (!ToRun) return;
        SaveCurrentEntry(SMALLER_RESPONSE);
        NextTrial();
    }

    private void CreateExpFile()
    {
        FILE_PATH = $"{Application.persistentDataPath}/ExpEntry_{System.DateTime.Now:yyyy-MM-dd-HH_mm_ss}.csv";
        Debug.Log("fILE PATH IS: " + FILE_PATH);

        FILE_TEMP = new StringBuilder();
        FILE_TEMP.AppendLine(string.Join(SEPARATOR, HEADING));
    }

    private void SaveCurrentEntry(string response)
    {
        string trial = GetCurTrial();
        string shape = trial.Split('|')[0];
        float size = float.Parse(trial.Split('|')[1]);

        string entry = $"{shape}, {size}, {response}";
        FILE_TEMP.AppendLine(string.Join(SEPARATOR, entry));
    }

    private void SaveFile()
    {
        try
        {
            File.AppendAllText(FILE_PATH, FILE_TEMP.ToString());
            Debug.Log($"File saved to {FILE_PATH}");
        }
        catch (Exception e)
        {
            Debug.Log($"Data could not be written to csv file");
            Debug.Log(e.ToString());
            return;
        }
    }

    public void NextTrial()
    {
        if (!ToRun || _nextLoading || _blankLoading) return;

        _nextLoading = true;
        if(spawn != null) Realtime.Destroy(spawn);
        //Debug.Log($"trail number: {_cTrialNumber} vs order count: {_order.Count}");
        if (_cTrialNumber >= _order.Count-1)
        {
            SaveFile();

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                return;
            }  
#endif
            Application.Quit();
            return;
        }

        string trial = GetNextTrial();

        string shape = trial.Split('|')[0];
        float size = float.Parse(trial.Split('|')[1]);
        spawn = Realtime.Instantiate(shape, m_ShapeSpawn.position, m_ShapeSpawn.rotation, new Realtime.InstantiateOptions
        {
            ownedByClient = true,
            preventOwnershipTakeover = true,
            destroyWhenOwnerLeaves = true,
            destroyWhenLastClientLeaves = true,
            useInstance = m_Realtime,
        });
        spawn.transform.localScale = new Vector3(size, 1, size);
        
        _cTrialNumber++;
        
        //see blank/nothing
        m_Camera.cullingMask = 0;
        //start waiting for blank to finish showing
        _blankLoading = true;

        //next trial done loading
        _nextLoading = false;
    }

    /// <summary>
    /// Returns string representation of the current trial
    /// </summary>
    /// <returns></returns>
    public string GetCurTrial()
    {
        return _order[_cTrialNumber];
    }

    /// <summary>
    /// Returns string representation of the next trial
    /// </summary>
    /// <returns></returns>
    public string GetNextTrial()
    {
        return _order[_cTrialNumber+1];
    }

    /// <summary>
    /// Returns the string representation of the trial based on the given trialNumber
    /// </summary>
    /// <param name="trialNumber"></param>
    /// <returns></returns>
    public string GetTrial(int trialNumber)
    {
        return _order[trialNumber];
    }

    private void Reset(InputAction.CallbackContext obj)
    {

    }

    private void Restart(InputAction.CallbackContext obj)
    {

    }

    void OnDisable()
    {
        largerButton.performed -= SaveLargerResponse; largerButton.Disable();
        smallerButton.performed -= SaveSmallerResponse; smallerButton.Disable();
        resetButton.performed -= Reset; resetButton.Disable(); 
    }
}

public class ExpEntry
{
    public string Shape;
    public float Size;
    public string Response;

    public ExpEntry(string shape, float size, string response)
    {
        Shape = shape;
        Size = size;
        Response = response;
    }
}
