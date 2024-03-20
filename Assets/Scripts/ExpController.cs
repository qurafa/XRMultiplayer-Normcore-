using Normal.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExpController : MonoBehaviour
{
    [Header("SCENE VARIABLES")]
    [SerializeField]
    private bool ToRun = false;
    [SerializeField]
    private Realtime m_Realtime;
    [SerializeField]
    private DataManager m_DataManager;
    [SerializeField]
    private Camera m_PlayerCamera;
    [SerializeField]
    private Transform m_ExpSetUp;
    [SerializeField]
    private Transform m_SortingCube;
    [SerializeField]
    private Transform m_CubeSpawn;
    [SerializeField]
    private Transform m_ShapeSpawn;
    [SerializeField]
    private AudioSource m_SaveAudio;
    [Header("EXPERIMENT VARIABLES")]
    /// <summary>
    /// Name of the shapes we'll be using, ensure they're in the Resource folder
    /// </summary>
    [SerializeField]
    private string[] m_Shapes = new string[] { };
    /// <summary>
    /// Whether the shape and the box should face towards the player's position or not
    /// </summary>
    [SerializeField]
    private bool m_FacePlayer = false;
    [SerializeField]
    private float m_MinBlankTimeLimit = 1.0f;
    [SerializeField]
    private float m_MaxBlankTimeLimit = 1.0f;
    [SerializeField]
    private float m_TrialTimeLimit = 2.0f;
    [SerializeField]
    private int m_NumberSmaller = 0;
    [SerializeField]
    private int m_NumberLarger = 0;
    [SerializeField]
    private string m_PID = "";
    [SerializeField]
    private string m_Condition = "";
    /// <summary>
    /// Number of repeats for each shape and size
    /// </summary>
    [SerializeField]
    private int m_Repeats = 0;
    /// <summary>
    /// Represents the difference in range of shapes between trials
    /// </summary>
    [SerializeField]
    private float m_ScaleDiff = 0.025f;
    /// <summary>
    /// Number of shapes to use, picks randon n number of shapes to use
    /// </summary>
    [SerializeField]
    private int m_NumOfShapes = 6;

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

    private Transform _playerTransform;

    /// <summary>
    /// to keep track of the objects being spawned
    /// </summary>
    private GameObject spawn = null;

    /// <summary>
    /// Time last stimuli is displayed
    /// </summary>
    private DateTime _dispTime = DateTime.Now;
    /// <summary>
    /// Time last entry was registered
    /// </summary>
    private DateTime _entryTime = DateTime.Now;

    //important variables
    private readonly static string LARGER_RESPONSE = "1";
    private readonly static string SMALLER_RESPONSE = "0";
    private readonly static string NO_RESPONSE = "-1";

    //soooo many flags.........lol
    //when we're ready to start flag
    private bool _ready = false;

    //next trial loading flag
    private bool _nextLoading = false;

    //blank timer and flags
    private float _blankTimer = 0;
    private bool _minBlankLoading = false;
    private bool _maxBlankLoading = false;

    //trial timer and flags
    private float _trialTimer = 0;
    private bool _trialLoading = true;

    private bool _savingEntry = false;
    private bool _savedEntry = false;

    private Dictionary<string, Vector3> _shapeToBoxRotation0;
    private Dictionary<string, Vector3[]> _shapeToBoxRotation1;

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
        //without LookAt vector, euler angles is....new Vector3(0, 180, 0)
        

        if (m_FacePlayer)
        {
            _shapeToBoxRotation1 = new Dictionary<string, Vector3[]>
            {
                //set from trying it out with the box in teh editor and setting it here,
                //you'll have to do that again on your end if you want to change the values for whatever reason
                { "Trapezoid", new[] { Vector3.up, new Vector3(0, -90, 0) } },
                { "Oval", new[] { Vector3.up, new Vector3(0, -90, 0) } },
                { "Diamond", new[] { Vector3.up, new Vector3(0, -90, 0) } },

                { "Quatrefoil", new[] { Vector3.down, new Vector3(90, 0, 0) } },
                { "Square", new[] { Vector3.down, new Vector3(90, 0, 0) } },
                { "Triangle", new[] { Vector3.down, new Vector3(90, 0, 0) } },

                { "Octagon", new[] { Vector3.up, new Vector3(90, 0, 0) } },
                { "Parallelogram", new[] { Vector3.up, new Vector3(90, 0, 0) } },
                { "Star", new[] { Vector3.up, new Vector3(90, 0, 0) } },

                { "Hexagon", new[] { Vector3.up, new Vector3(0, 90, 0) } },
                { "Rectangle", new[] { Vector3.up, new Vector3(0, 90, 0) } },
                { "Pentagon", new[] { Vector3.up, new Vector3(0, 90, 0) } }
            };
        }
        else
        {
            _shapeToBoxRotation0 = new Dictionary<string, Vector3>
            {
                //without LookAt vector, euler angles is....new Vector3(0, 180, 0)
                { "Trapezoid", new Vector3(0, 180, 0) },
                { "Oval", new Vector3(0, 180, 0) },
                { "Diamond", new Vector3(0, 180, 0) },

                //new Vector3 (-90, 0, 90)
                { "Quatrefoil", new Vector3(-90, 0, 90) },
                { "Square", new Vector3(-90, 0, 90) },
                { "Triangle", new Vector3(-90, 0, 90) },

                //new Vector3 (0, -90, 0)
                { "Octagon", new Vector3(0, -90, 0) },
                { "Parallelogram", new Vector3(0, -90, 0) },
                { "Star", new Vector3(0, -90, 0) },

                //new Vector3(0, 0, 0)
                { "Hexagon", new Vector3(0, 0, 0) },
                { "Rectangle", new Vector3(0, 0, 0) },
                { "Pentagon", new Vector3(0, 0, 0) }
            };
        }
        //see nothing when you join
        //m_PlayerCamera.cullingMask = 0;

        if (m_Realtime == null)
        {
            Debug.Log("Realtime not specified");
            return;
        }
        m_Realtime.didConnectToRoom += DidConnectToRoom;
    }

    void FixedUpdate()
    {
        if (_trialLoading)
        {
            _trialTimer += Time.deltaTime;
            if (_trialTimer > m_TrialTimeLimit)
            {
                _trialTimer = 0;
                _trialLoading = false;

                //see blank/nothing
                m_PlayerCamera.cullingMask = 0;
                _blankTimer = 0;
                _minBlankLoading = true;
                _maxBlankLoading = true;
            }
        }

        if (_minBlankLoading || _maxBlankLoading)
        {
            _blankTimer += Time.deltaTime;
            if (_blankTimer > m_MinBlankTimeLimit)
            {
                _minBlankLoading = false;
                if(_savedEntry) NextTrial();
            }

            if(_blankTimer > m_MaxBlankTimeLimit)
            {
                _blankTimer = 0;
                _minBlankLoading = false;
                _maxBlankLoading = false;

                SaveEntry(NO_RESPONSE);
                NextTrial();
            }
        }
    }

    private void DidConnectToRoom(Realtime realtime)
    {
        if(!ToRun) return;

        //don't do anything if it isn't the first user....
        if (m_Realtime.clientID != 0) return;
        
        //Shuffle the order of the shapes
        ShuffleShapes();
        //Set up experiment order
        CreateExpOrder();
        //Shuffle the order
        ShuffleOrder();

        //Create file path to store entries 
        CreateExpFile();

        _playerTransform = m_PlayerCamera.transform;

        _ready = true;

        //Start exp
        //NextTrial();
    }

    private void NextSetUp()
    {
        float xDiff = m_PlayerCamera.transform.position.x - m_ExpSetUp.transform.position.x;
        float yDiff = m_PlayerCamera.transform.position.y - m_ExpSetUp.transform.position.y;
        float zDiff = m_PlayerCamera.transform.position.z - m_ExpSetUp.transform.position.z;
        m_ExpSetUp.Translate(new Vector3(xDiff + 0.8f, yDiff, zDiff));

        string shape = GetNextTrial().Split('|')[0];
        m_SortingCube.position = m_CubeSpawn.position;
        if (m_FacePlayer)
        {
            m_SortingCube.LookAt(_playerTransform.position, _shapeToBoxRotation1[shape][0]);
            m_SortingCube.Rotate(_shapeToBoxRotation1[shape][1]);

            m_ShapeSpawn.transform.LookAt(_playerTransform.position, Vector3.up);
        }
        else
        {
            m_SortingCube.eulerAngles = _shapeToBoxRotation0[shape];
            m_ShapeSpawn.eulerAngles = new Vector3(90, 90, 0);
        }
    }

    public void SetPID(string id)
    {
        m_PID = id;
        Debug.Log($"set id to {id}");
    }

    /// <summary>
    /// Sets the number of random shapes to use, caps at the number of shapes we have
    /// </summary>
    /// <param name="num"></param>
    public void SetNumOfShapes(int num = int.MaxValue)
    {
        m_NumOfShapes = num > m_Shapes.Length ? m_Shapes.Length : num;
        Debug.Log($"using {m_NumOfShapes} shapes");
    }

    public void SetRepeats(int repeats)
    {
        m_Repeats = repeats;
        Debug.Log($"Set repeats to {repeats}");
    }

    public void SetScaleDiff(float diff)
    {
        m_ScaleDiff = diff;
        Debug.Log($"set scale diff to {diff}");
    }

    public void SetCondition(string cond)
    {
        m_Condition = cond;
        Debug.Log($"set condition to {cond}");
    }

    private bool CreateExpOrder()
    {
        if (_order != null) return true;

        _order = new List<string>();
        string entry;

        //for every shape
        for (int i = 0; i < m_NumOfShapes; i++)
        {
            //for every repeat for each shape
            for (int r = 0; r < m_Repeats; r++)
            {
                entry = $"{m_Shapes[i]}|{1}";//mid scale
                _order.Add(entry);

                //smaller sizes
                for (int zS = 1; zS <= m_NumberSmaller; zS++)
                {
                    entry = $"{m_Shapes[i]}|{1 - (m_ScaleDiff * zS)}";//smaller scales
                    _order.Add(entry);
                }

                //larger sizes
                for (int zL = 1; zL <= m_NumberLarger; zL++)
                {
                    entry = $"{m_Shapes[i]}|{1 + (m_ScaleDiff * zL)}";//larger scales
                    _order.Add(entry);
                }
            }
        }

        return true;
    }

    public List<string> GetOrder()
    { 
        return _order; 
    }

    /// <summary>
    /// In-place Shuffle based on the Fisher-Yates Algorithm 
    /// </summary>
    /// <remarks>
    /// Fisher-Yates: <see href="https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle">Wikipedia</see>
    /// <seealso cref="https://stackoverflow.com/questions/273313/randomize-a-listt"/>
    /// <see cref="https://docs.unity3d.com/ScriptReference/Random.Range.html">Unity Random</see>
    /// Pseudocode:
    ///     for i from 0 to n−2 do
    ///         j ← random integer such that i ≤ j < n
    ///         exchange a[i] and a[j]
    /// </remarks>
    private void ShuffleShapes()
    {
        for (int n = 0; n < m_Shapes.Length; n++)
        {
            int i = UnityEngine.Random.Range(n, m_Shapes.Length);

            string temp = m_Shapes[i];
            m_Shapes[i] = m_Shapes[n];
            m_Shapes[n] = temp;
        }
    }

    private void ShuffleOrder()
    {
        for (int n = 0; n < _order.Count; n++)
        {
            int i = UnityEngine.Random.Range(n, _order.Count);

            string temp = _order[i];
            _order[i] = _order[n];
            _order[n] = temp;
        }
    }

    private void SaveLargerResponse(InputAction.CallbackContext obj)
    {
        if (spawn != null) SaveEntry(LARGER_RESPONSE);
        NextTrial();
    }

    private void SaveSmallerResponse(InputAction.CallbackContext obj)
    {
        if (spawn != null) SaveEntry(SMALLER_RESPONSE);
        NextTrial();
    }

    private void CreateExpFile()
    {
        m_DataManager.CreateExpFile($"{m_PID}_{m_Condition}_{m_Repeats}Reps_{m_ScaleDiff}Range");
/*        FILE_PATH = $"{Application.persistentDataPath}/ExpEntry_{System.DateTime.Now:yyyy-MM-dd-HH_mm_ss}.csv";
        Debug.Log("fILE PATH IS: " + FILE_PATH);

        FILE_TEMP = new StringBuilder();
        FILE_TEMP.AppendLine(string.Join(SEPARATOR, HEADING));*/
    }

    private void SaveEntry(string response)
    {
        if (SaveWait()) return;

        _savingEntry = true;
        m_SaveAudio.Play();
        Debug.Log($"Saving entry {response}");
        string trial = GetCurTrial();
        string shape = trial.Split('|')[0];
        float size = float.Parse(trial.Split('|')[1]);

        _entryTime= DateTime.Now;
        TimeSpan diff = _entryTime - _dispTime;
        m_DataManager.UpdateExpFile(_nTrialNumber, shape, size, response, diff.ToString(), _entryTime.TimeOfDay.ToString());

        /*string entry = $"{_nTrialNumber},{shape}, {size}, {response}";
        FILE_TEMP.AppendLine(string.Join(SEPARATOR, entry));*/
        _savedEntry = true;
        _savingEntry = false;
    }

    private void SaveExpFile()
    {   
        m_DataManager.SaveExpFile();
    }

    public void NextTrial()
    {
        if (NextWait()) return;

        if (_nTrialNumber >= _order.Count)
        {
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

        //set all related flags
        _nextLoading = true;

        _minBlankLoading = false;
        _maxBlankLoading = false;

        _trialLoading = false;

        _savedEntry = false;

        //Set up experiment env
        NextSetUp();

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
            spawn.transform.localScale.y * 1,
            spawn.transform.localScale.z * size);
        if(m_FacePlayer)
            spawn.transform.Rotate(new Vector3(90,0,0));

        _dispTime = DateTime.Now;
        
        _nTrialNumber++;

        //see everything after setting up
        m_PlayerCamera.cullingMask = -1;
        //reset trial timer to go up to the set limit
        _trialTimer = 0;
        //start counting trial loading
        _trialLoading = true;

        //next trial done loading
        _nextLoading = false;
    }

    private bool NextWait()
    {
        return (!ToRun || !_ready || _nextLoading || _minBlankLoading || _trialLoading);
    }

    private bool SaveWait()
    {
        return (!_ready || _savedEntry || _savingEntry || !ToRun || _nextLoading /*|| _trialLoading*/);
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
