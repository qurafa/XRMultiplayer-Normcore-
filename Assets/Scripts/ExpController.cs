using Normal.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
    private Camera m_PlayerCamera;
    [SerializeField]
    private Transform m_ExpSetUp;
    [SerializeField]
    private Transform m_SortingCube;
    [SerializeField]
    private Transform m_ShapeSpawn;
    /// <summary>
    /// Name of the shapes we'll be using, ensure they're in the Resource folder
    /// </summary>
    [SerializeField]
    private string[] m_Shapes = new string[] { };
    [SerializeField]
    private float m_BlankTimeLimit = 1.0f;
    [SerializeField]
    private float m_TrialTimeLimit = 2.0f;
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
    [Header("-----------")]

    private List<string> _order;
    /// <summary>
    /// Next trial number, where trials go from 0 to n
    /// </summary>
    private int _nTrialNumber = 0;

    private GameObject spawn = null;

    //important variables
    private readonly static string LARGER_RESPONSE = "1";
    private readonly static string SMALLER_RESPONSE = "0";
    private readonly static string NO_RESPONSE = "-1";
    private static string FILE_PATH = "";
    private static StringBuilder FILE_TEMP;
    private static string SEPARATOR = ",";
    private static string[] HEADING = {"Trial","Shape", "Size", "Response"};

    //next trial loading flag
    private bool _nextLoading = false;

    //blank timer and flags
    private float _blankTimer = 0;
    private bool _blankLoading = false;

    //trial timer and flags
    private float _trialTimer = 0;
    private bool _trialLoading = false;

    private bool _savingEntry = false;

    private Dictionary<string, Vector3> _shapeToBoxRotation = new Dictionary<string, Vector3>();

    void OnEnable()
    {
        largerButton.Enable(); largerButton.performed += SaveLargerResponse;
        smallerButton.Enable(); smallerButton.performed += SaveSmallerResponse;
        resetButton.Enable(); resetButton.performed += Reset;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Add the shape to rotation
        _shapeToBoxRotation.Add("Trapezoid", new Vector3(0, 0, 0));
        _shapeToBoxRotation.Add("Oval", new Vector3(0, 0, 0));
        _shapeToBoxRotation.Add("Rhombus", new Vector3(0, 0, 0));

        _shapeToBoxRotation.Add("Quaterfoil-Separated", new Vector3 (90, -90, 0));
        _shapeToBoxRotation.Add("Square", new Vector3 (90, -90, 0));
        _shapeToBoxRotation.Add("Triangle", new Vector3 (90, -90, 0));

        _shapeToBoxRotation.Add("Octagon", new Vector3 (0, 90, 0));
        _shapeToBoxRotation.Add("Parallelogram", new Vector3 (0, 90, 0));
        _shapeToBoxRotation.Add("Star-Separated", new Vector3 (0, 90, 0));

        _shapeToBoxRotation.Add("Hexagon", new Vector3(0, 180, 0));
        _shapeToBoxRotation.Add("Rectangle", new Vector3(0, 180, 0));
        _shapeToBoxRotation.Add("Pentagon", new Vector3(0, 180, 0));

        if (m_Realtime == null)
        {
            Debug.Log("Realtime not specified");
            return;
        }
        m_Realtime.didConnectToRoom += DidConnectToRoom;
    }

    void FixedUpdate()
    {
        if (_blankLoading)
        {
            _blankTimer += Time.deltaTime;
            if (_blankTimer > m_BlankTimeLimit)
            {
                //see everything
                m_PlayerCamera.cullingMask = -1;
                _blankTimer = 0;
                _blankLoading = false;
                _trialTimer = 0;
                _trialLoading = true;//so we can start tracking the loading trial...
            }
        }

        if(_trialLoading)
        {
            _trialTimer += Time.deltaTime;
            if(_trialTimer > m_TrialTimeLimit)
            {
                //save no entry and go to the next trial
                _trialTimer = 0;
                _trialLoading = false;
                SaveCurrentEntry(NO_RESPONSE);
                NextTrial();
            }
        }

    }

    private void DidConnectToRoom(Realtime realtime)
    {
        if(!ToRun) return;

        //don't do anything if it isn't the first user....
        if (m_Realtime.clientID != 0) return;

        //Set up experiment order
        CreateExpOrder();
        ShuffleOrder();

        //Create file path to store entries 
        CreateExpFile();

        //Start exp
        //NextTrial();
    }

    private void SetUpNextLevel()
    {
        float diff = m_PlayerCamera.transform.position.y - m_ExpSetUp.transform.position.y;
        m_ExpSetUp.Translate(new Vector3(0, diff, 0));

        string shape = GetNextTrial().Split('|')[0];
        m_SortingCube.eulerAngles = _shapeToBoxRotation[shape];
    }

    private bool CreateExpOrder()
    {
        if (_order != null) return true;

        _order = new List<string>();
        string entry;
        int s = 0;

        //for every shape
        foreach (string shape in m_Shapes)
        {
            //for every repeat for each shape
            for (int r = 0; r < m_Repeats; r++)
            {
                entry = $"{shape}|{1}";//mid scale
                _order.Add(entry);

                //smaller sizes
                for (int zS = 1; zS <= m_NumberSmaller; zS++)
                {
                    entry = $"{shape}|{1 - (diff * zS)}";//smaller scales
                    _order.Add(entry);
                }

                //larger sizes
                for (int zL = 1; zL <= m_NumberLarger; zL++)
                {
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
        if (spawn != null) SaveCurrentEntry(LARGER_RESPONSE);
        NextTrial();
    }

    private void SaveSmallerResponse(InputAction.CallbackContext obj)
    {
        if (spawn != null) SaveCurrentEntry(SMALLER_RESPONSE);
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
        if (_savingEntry || Wait()) return;

        _savingEntry = true;
        Debug.Log($"Saving entry {response}");
        string trial = GetCurTrial();
        string shape = trial.Split('|')[0];
        float size = float.Parse(trial.Split('|')[1]);

        string entry = $"{_nTrialNumber},{shape}, {size}, {response}";
        FILE_TEMP.AppendLine(string.Join(SEPARATOR, entry));

        _savingEntry = false;
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
            Debug.Log($"Data could not be written to csv file due to exception: {e.ToString()}");
            return;
        }
    }

    public void NextTrial()
    {
        if (Wait()) return;

        if (_nTrialNumber >= _order.Count)
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
        Debug.Log($"Loading trial number {_nTrialNumber}");
        _nextLoading = true;
        
        //Set up experiment env
        SetUpNextLevel();

        if (spawn != null)
        {
            Realtime.Destroy(spawn);
            spawn = null;
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
        spawn.transform.localScale = new Vector3(spawn.transform.localScale.x * size,
            spawn.transform.localScale.x * 1,
            spawn.transform.localScale.x * size);
        
        _nTrialNumber++;
        
        //see blank/nothing
        m_PlayerCamera.cullingMask = 0;
        //start waiting for blank to finish showing
        _blankLoading = true;

        //next trial done loading
        _nextLoading = false;
    }

    private bool Wait()
    {
        return (!ToRun || _nextLoading || _blankLoading);
    }

    /// <summary>
    /// Returns string representation of the current trial
    /// </summary>
    /// <returns></returns>
    public string GetCurTrial()
    {
        return _order[_nTrialNumber - 1];
    }

    /// <summary>
    /// Returns string representation of the next trial
    /// </summary>
    /// <returns></returns>
    public string GetNextTrial()
    {
        return _order[_nTrialNumber];
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
