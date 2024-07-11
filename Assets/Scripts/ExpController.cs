using com.perceptlab.armultiplayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using static UnityEngine.ParticleSystem;

public class ExpController : MonoBehaviour
{
    /// <summary>
    /// Whether to run the experiment or not
    /// </summary>
    [Header("SCENE VARIABLES")]
    [SerializeField]
    protected bool ToRun = false;
    /// <summary>
    /// DataManager to help monitor and save experiment data
    /// </summary>
    [SerializeField]
    protected DataManager m_DataManager;
    /// <summary>
    /// Player Camera
    /// </summary>
    [SerializeField]
    protected Camera m_PlayerCamera;
    /// <summary>
    /// Experiment set up Transform
    /// </summary>
    [SerializeField]
    protected Transform m_ExpSetUp;
    /// <summary>
    /// The sorting cube in the scene
    /// </summary>
    [SerializeField]
    protected Transform m_SortingCube;
    /// <summary>
    /// The spawn location for the sorting cube
    /// </summary>
    [SerializeField]
    protected Transform m_CubeSpawn;
    /// <summary>
    /// The spawn locations for the shapes
    /// </summary>
    [SerializeField]
    protected Transform[] m_ShapeSpawn = new Transform[] { };
    /// <summary>
    /// Used to highlight or draw attention to the hole/slot for different shapes 
    /// </summary>
    [SerializeField]
    protected GameObject[] m_ShapeSlotMarkers = new GameObject[] { };
    /// <summary>
    /// Audio to play after player inputs their response
    /// </summary>
    [SerializeField]
    protected AudioSource m_SaveAudio;

    /// <summary>
    /// The regular shapes that we can use, ensure to specify them if you plan on using them
    /// </summary>
    [Header("EXPERIMENT VARIABLES")]
    [SerializeField]
    protected GameObject[] m_RegularShapes = new GameObject[] { };
    /// <summary>
    /// The complex shapes that we can use, ensure to specify them if you plan on using them
    /// </summary>
    [SerializeField]
    protected GameObject[] m_ComplexShapes = new GameObject[] { };
    /// <summary>
    /// Whether the shapes and the box should face towards the player's position or not
    /// </summary>
    [SerializeField]
    protected bool m_FacePlayer = false;
    /// <summary>
    /// Whether to spawn the shapes in random locations or not
    /// </summary>
    [SerializeField]
    protected bool m_RandomShapeLocation = false;
    /// <summary>
    /// Whether we want to be able to grab the box or not
    /// </summary>
    [SerializeField]
    protected bool m_CanGrabBox = false;
    /// <summary>
    /// Whether we want to be able to grab the shapes or not
    /// </summary>
    [SerializeField]
    protected bool m_CanGrabShapes = false;
    /// <summary>
    /// Minimum amount of time to show blank/nothing to the participant
    /// </summary>
    [SerializeField]
    protected float m_MinBlankTimeLimit = 3.0f;
    /// <summary>
    /// Maximum amount of time to show blank/nothing to the participant
    /// </summary>
    [SerializeField]
    protected float m_MaxBlankTimeLimit = 300.0f;
    /// <summary>
    /// Time limit for each trial
    /// </summary>
    [SerializeField]
    protected float m_TrialTimeLimit = 5.0f;
    /// <summary>
    /// Number of sizes smaller from the original size for each shape
    /// </summary>
    [SerializeField]
    protected int m_NumberSmaller = 3;
    /// <summary>
    /// Number of sizes larger from the original size for each shape
    /// </summary>
    [SerializeField]
    protected int m_NumberLarger = 3;
    /// <summary>
    /// Particicpant ID
    /// </summary>
    [SerializeField]
    protected string m_PID = "0";
    /// <summary>
    /// What condition are we testing the participants on
    /// </summary>
    [SerializeField]
    protected string m_Condition = "test";
    /// <summary>
    /// Number of repeats for each shape and size
    /// </summary>
    [SerializeField]
    protected int m_Repeats = 1;
    /// <summary>
    /// Represents the difference in range of shapes between trials
    /// </summary>
    [SerializeField]
    protected float m_ScaleDiff = 0.025f;
    /// <summary>
    /// Number of shapes to use, picks random n number of shapes to use
    /// </summary>
    [SerializeField]
    protected int m_NumOfShapes = 4;
    /// <summary>
    /// Player's distance from the experiment set up in meters
    /// </summary>
    [SerializeField]
    protected float m_PlayerDistanceFromSetup = 0.8f;
    /// <summary>
    /// How fast the shape should rotate in DEG/S (Only set this if you're running ExpType 5)
    /// </summary>
    [SerializeField]
    protected float m_ShapeRotSpeed = 1f;
    /// <summary>
    /// Axis the shape should rotate on
    /// </summary>
    [SerializeField]
    protected Vector3 m_ShapeRotAxis = new(0, 1, 0);

    /// <summary>
    /// Button for larger input response
    /// </summary>
    [Header("INPUT ACTIONS")]
    [SerializeField]
    protected InputAction largerButton;
    /// <summary>
    /// Button for smaller input response
    /// </summary>
    [SerializeField]
    protected InputAction smallerButton;
    [SerializeField]
    protected InputAction resetButton;

    public enum ExpType
    {
        NonInteractiveSimple,
        InteractiveSimple,
        NonInteractiveComplex,
        InteractiveComplex,
        RotatingComplex
    }

    /// <summary>
    /// Experiment types
    /// </summary>
    private ExpType _expType;
    
    protected ExpType expType{
        get { return _expType; }
        set 
        { 
            _expType = value;
            ExpTypeChanged();
        }
    }
    /// <summary>
    /// List of order of the experiment. Contains string in format "shapeNumber|shapeName|scale|spawnLocation";
    /// </summary>
    protected List<string> _order;
    /// <summary>
    /// Next trial number, where trials go from 0 to n
    /// </summary>
    protected int _nTrialNumber = 0;

    protected Transform _playerTransform;

    /// <summary>
    /// to keep track of the objects being spawned
    /// </summary>
    protected GameObject spawn = null;

    /// <summary>
    /// Time last stimuli is displayed
    /// </summary>
    protected DateTime _dispTime = DateTime.Now;
    /// <summary>
    /// Time last entry was registered
    /// </summary>
    protected DateTime _entryTime = DateTime.Now;

    //static variables
    protected readonly static string LARGER_RESPONSE = "1";//save "1" for larger responses
    protected readonly static string SMALLER_RESPONSE = "0";//save "0" for smaller responses 
    protected readonly static string NO_RESPONSE = "-1";//save "-1" if n response is recorded
    protected readonly static int DEFAULT_NO_SHAPE = 4;//4 shapes by default
    protected readonly static int DEFAULT_NO_LARGER = 3;//3 sizes bigger for each shape
    protected readonly static int DEFAULT_NO_SMALLER = 3;//3 sizes smaler for each shape
    protected readonly static float MAX_SHAPE_ROTATE_TIME = 1;//shape rotates every 1 second
    protected readonly static float SLOT_MARKER_MAX_TIME = 2;//time limit for the slot markers to show in in seconds

    //soooo many flags.........lol
    //when we're ready to start flag
    protected bool _ready = false;

    //next trial loading flag
    protected bool _nextLoading = false;

    //blank timer and flags
    protected float _blankTimer = 0;
    protected bool _minBlankLoading = false;
    protected bool _maxBlankLoading = false;

    //trial timer and flags
    protected float _trialTimer = 0;
    protected bool _trialLoading = true;

    //slot marker timer
    protected float _slotMarkerTimer = 0;
    protected bool _slotMarked = false;

    //shape rotate timer
    protected float _rotateTimer = 0;

    //saving flags
    protected bool _savingEntry = false;
    protected bool _savedEntry = false;

    protected Dictionary<string, Vector3> _shapeToBoxRotation0;
    protected Dictionary<string, Vector3[]> _shapeToBoxRotation1;
    protected Dictionary<string, GameObject> _shapeToMarker;

    /// <summary>
    /// The shapes being used in this run of the experiment
    /// </summary>
    protected GameObject[] m_Shapes;

    virtual protected void OnEnable()
    {
        largerButton.Enable(); largerButton.performed += SaveLargerResponse;
        smallerButton.Enable(); smallerButton.performed += SaveSmallerResponse;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Initialize();
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
                if (_savedEntry) NextTrial();
            }

            if (_blankTimer > m_MaxBlankTimeLimit)
            {
                _blankTimer = 0;
                _minBlankLoading = false;
                _maxBlankLoading = false;

                SaveEntry(NO_RESPONSE);
                NextTrial();
            }
        }

        if (_slotMarked)
        {
            _slotMarkerTimer += Time.deltaTime;
            if(_slotMarkerTimer > SLOT_MARKER_MAX_TIME)
            {
                HideAllSlotMarkers();
                _slotMarkerTimer = 0;
            }
        }

        if(spawn != null && expType == ExpType.RotatingComplex)
        {
            spawn.transform.parent.Rotate(m_ShapeRotAxis * m_ShapeRotSpeed * Time.deltaTime);
            _rotateTimer = 0;
        }
    }

    public void Initialize()
    {
        if (!ToRun || _ready) return;
        //Set the box rotation for the different shapes
        SetBoxRotations();
        //Shuffle the order of the shapes
        ShuffleShapes();
        //Set up experiment order
        CreateExpOrder();
        //Shuffle the order
        ShuffleOrder();
        //Setting up the shape markers and match them to their corresponding shapes
        AssignSlotMarkers();
        //Create file path to store entries 
        CreateExpFile();

        _playerTransform = m_PlayerCamera.transform;

        _ready = true;

        //Start exp
        //NextTrial();
    }

    protected void SetBoxRotations()
    {
        if (m_FacePlayer)
        {
            _shapeToBoxRotation1 = new Dictionary<string, Vector3[]>();
            foreach (GameObject s in m_Shapes)
            {
                if (s.name.ToLower().Contains("trapezoid") || s.name.ToLower().Contains("oval") || s.name.ToLower().Contains("diamond"))
                {
                    _shapeToBoxRotation1.Add(s.name, new[] { Vector3.up, new Vector3(0, -90, 0) });
                }
                else if (s.name.ToLower().Contains("quatrefoil") || s.name.ToLower().Contains("square") || s.name.ToLower().Contains("triangle"))
                {
                    _shapeToBoxRotation1.Add(s.name, new[] { Vector3.down, new Vector3(90, 0, 0) });
                }
                else if (s.name.ToLower().Contains("octagon") || s.name.ToLower().Contains("parallelogram") || s.name.ToLower().Contains("star"))
                {
                    _shapeToBoxRotation1.Add(s.name, new[] { Vector3.up, new Vector3(90, 0, 0) });
                }
                else if (s.name.ToLower().Contains("hexagon") || s.name.ToLower().Contains("rectangle") || s.name.ToLower().Contains("pentagon"))
                {
                    _shapeToBoxRotation1.Add(s.name, new[] { Vector3.up, new Vector3(0, 90, 0) });
                }
                else
                {
                    Debug.Log("SHAPE NAME NOT RECOGNIZED!!!");
                }
            }
            /*            _shapeToBoxRotation1 = new Dictionary<string, Vector3[]>
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
                        };*/
        }
        else
        {
            _shapeToBoxRotation0 = new Dictionary<string, Vector3>();
            foreach (GameObject s in m_Shapes)
            {
                Debug.Log($"Shape Name {s.name}");
                if (s.name.ToLower().Contains("trapezoid") || s.name.ToLower().Contains("oval") || s.name.ToLower().Contains("diamond"))
                {
                    _shapeToBoxRotation0.Add(s.name, new Vector3(0, 180, 0));
                }
                else if (s.name.ToLower().Contains("quatrefoil") || s.name.ToLower().Contains("square") || s.name.ToLower().Contains("triangle"))
                {
                    _shapeToBoxRotation0.Add(s.name, new Vector3(-90, 0, 90));
                }
                else if (s.name.ToLower().Contains("octagon") || s.name.ToLower().Contains("parallelogram") || s.name.ToLower().Contains("star"))
                {
                    _shapeToBoxRotation0.Add(s.name, new Vector3(0, -90, 0));
                }
                else if (s.name.ToLower().Contains("hexagon") || s.name.ToLower().Contains("rectangle") || s.name.ToLower().Contains("pentagon"))
                {
                    _shapeToBoxRotation0.Add(s.name, new Vector3(0, 0, 0));
                }
                else
                {
                    Debug.Log("SHAPE NAME NOT RECOGNIZED!!!");
                }
            }
            /*            _shapeToBoxRotation0 = new Dictionary<string, Vector3>
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
                        };*/
        }
    }

    protected void AssignSlotMarkers()
    {
        _shapeToMarker = new Dictionary<string, GameObject>();

        foreach(GameObject shape in m_Shapes)
        {
            _shapeToMarker.Add(shape.name, GetSlotMarker(shape));
        }
    }

    protected GameObject GetSlotMarker(GameObject s)
    {
        foreach(GameObject sm in m_ShapeSlotMarkers)
        {
            if (s.name.ToLower().Contains("trapezoid") && sm.name.ToLower().Contains("trapezoid"))
                return sm;
            if (s.name.ToLower().Contains("oval") && sm.name.ToLower().Contains("oval"))
                return sm;
            if (s.name.ToLower().Contains("diamond") && sm.name.ToLower().Contains("diamond"))
                return sm;
            if (s.name.ToLower().Contains("quatrefoil") && sm.name.ToLower().Contains("quatrefoil"))
                return sm;
            if (s.name.ToLower().Contains("square") && sm.name.ToLower().Contains("square"))
                return sm;
            if (s.name.ToLower().Contains("triangle") && sm.name.ToLower().Contains("triangle"))
                return sm;
            if (s.name.ToLower().Contains("octagon") && sm.name.ToLower().Contains("octagon"))
                return sm;
            if (s.name.ToLower().Contains("parallelogram") && sm.name.ToLower().Contains("parallelogram"))
                return sm;
            if (s.name.ToLower().Contains("star") && sm.name.ToLower().Contains("star"))
                return sm;
            if (s.name.ToLower().Contains("hexagon") && sm.name.ToLower().Contains("hexagon"))
                return sm;
            if (s.name.ToLower().Contains("rectangle") && sm.name.ToLower().Contains("rectangle"))
                return sm;
            if (s.name.ToLower().Contains("pentagon") && sm.name.ToLower().Contains("pentagon"))
                return sm;
        }

        return null;
    }

    private void HideAllSlotMarkers(SelectEnterEventArgs arg0)
    {
        HideAllSlotMarkers();
    }

    protected void HideAllSlotMarkers()
    {
        foreach(GameObject sm in m_ShapeSlotMarkers)
            sm.SetActive(false);
        _slotMarked = false;
    }

    protected void NextSetUp()
    {
        //if (NextWait()) return;

        float xDiff = m_PlayerCamera.transform.position.x - m_ExpSetUp.transform.position.x;
        float yDiff = m_PlayerCamera.transform.position.y - m_ExpSetUp.transform.position.y;
        float zDiff = m_PlayerCamera.transform.position.z - m_ExpSetUp.transform.position.z;
        m_ExpSetUp.Translate(new Vector3(xDiff + m_PlayerDistanceFromSetup, yDiff, zDiff));

        string trial = GetNextTrial();
        string shape = trial.Split('|')[1];
        int loc = int.Parse(trial.Split('|')[3]);

        m_SortingCube.position = m_CubeSpawn.position;

        if(m_SortingCube.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable g))
        {
            g.enabled = m_CanGrabBox;
        }
        if(m_SortingCube.TryGetComponent<Rigidbody>(out Rigidbody r)){
            r.constraints = m_CanGrabBox ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
        }

        if (m_FacePlayer)
        {
            m_SortingCube.LookAt(_playerTransform.position, _shapeToBoxRotation1[shape][0]);
            m_SortingCube.Rotate(_shapeToBoxRotation1[shape][1]);

            //m_ShapeSpawn[loc].transform.LookAt(_playerTransform.position, Vector3.up);
        }
        else
        {
            m_SortingCube.eulerAngles = _shapeToBoxRotation0[shape];
            //m_ShapeSpawn[loc].eulerAngles = new Vector3(90, 90, 0);
            //m_ShapeSpawn[loc].eulerAngles = new Vector3(90, -90, 0);
            //m_ShapeSpawn[loc].eulerAngles = new Vector3(90, -180, -180);
        }

        //activate the shape slot marker
        _shapeToMarker[shape].SetActive(true);
        _slotMarked = true;
    }

    public void SetPID(string id)
    {
        m_PID = id;
        Debug.Log($"set id to {id}");
    }

    public void SetExpType(int type)
    {
        expType = (ExpType)type;
    }

    protected void ExpTypeChanged()
    {
        //Non Interact Simple, Non Interact Complex
        if (expType == ExpType.NonInteractiveSimple || expType == ExpType.NonInteractiveComplex)
        {
            m_FacePlayer = false;
            m_RandomShapeLocation = false;
            m_CanGrabBox = false; m_CanGrabShapes = false;
            m_MinBlankTimeLimit = 3; m_MaxBlankTimeLimit = 300; m_TrialTimeLimit = 2.5f;
            m_NumberSmaller = DEFAULT_NO_SMALLER; m_NumberLarger = DEFAULT_NO_LARGER; m_NumOfShapes = DEFAULT_NO_SHAPE;

            if (expType == ExpType.NonInteractiveSimple)
                m_Shapes = m_RegularShapes;
            if (expType == ExpType.NonInteractiveComplex)
                m_Shapes = m_ComplexShapes;
        }
        //Interact Simple, Interact Complex
        else if (expType == ExpType.InteractiveSimple || expType == ExpType.InteractiveComplex)
        {
            m_FacePlayer = false;
            m_RandomShapeLocation = false;
            m_CanGrabBox = false; m_CanGrabShapes = true;
            m_MinBlankTimeLimit = 3; m_MaxBlankTimeLimit = 300; m_TrialTimeLimit = 10f;// 5f;
            m_NumberSmaller = DEFAULT_NO_SMALLER; m_NumberLarger = DEFAULT_NO_LARGER; m_NumOfShapes = DEFAULT_NO_SHAPE;

            if (expType == ExpType.InteractiveSimple)
                m_Shapes = m_RegularShapes;
            if (expType == ExpType.InteractiveComplex)
                m_Shapes = m_ComplexShapes;
        }
        //Rotating Complex
        else if (expType == ExpType.RotatingComplex)
        {
            m_FacePlayer = false;
            m_RandomShapeLocation = false;
            m_CanGrabBox = false; m_CanGrabShapes = false;
            m_MinBlankTimeLimit = 3; m_MaxBlankTimeLimit = 300; m_TrialTimeLimit = 10f;// 5f;
            m_NumberSmaller = DEFAULT_NO_SMALLER; m_NumberLarger = DEFAULT_NO_LARGER; m_NumOfShapes = DEFAULT_NO_SHAPE;

            m_Shapes = m_ComplexShapes;
        }
        else
        {
            RLogger.Log($"ExpType {expType.ToSafeString()} not valid");
        }
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

    private int randomShapeLoc
    {
        get
        {
            if (m_RandomShapeLocation)
            {
                Debug.Log("Random Shape Location");
                return UnityEngine.Random.Range(0, m_ShapeSpawn.Length);
            }
            else
            {
                Debug.Log("Non Random Shape Location");
                return 0;
            }
        }
    }

    protected bool CreateExpOrder()
    {
        if (_order != null) return true;

        _order = new List<string>();
        string entry;

        SetNumOfShapes(m_NumOfShapes);

        //for every shape
        for (int i = 0; i < m_NumOfShapes; i++)
        {
            //for every repeat for each shape
            for (int r = 0; r < m_Repeats; r++)
            {
                //smaller sizes
                for (int zS = 1; zS <= m_NumberSmaller; zS++)
                {
                    entry = $"{i}|{m_Shapes[i].name}|{1 - (m_ScaleDiff * zS)}|{randomShapeLoc}";//smaller scales
                    _order.Add(entry);
                }
                //mid size
                entry = $"{i}|{m_Shapes[i].name}|{1}|{randomShapeLoc}";
                _order.Add(entry);
                //larger sizes
                for (int zL = 1; zL <= m_NumberLarger; zL++)
                {
                    entry = $"{i}|{m_Shapes[i].name}|{1 + (m_ScaleDiff * zL)}|{randomShapeLoc}";//larger scales
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
    protected void ShuffleShapes()
    {
        for (int n = 0; n < m_Shapes.Length; n++)
        {
            int i = UnityEngine.Random.Range(n, m_Shapes.Length);

            GameObject temp = m_Shapes[i];
            m_Shapes[i] = m_Shapes[n];
            m_Shapes[n] = temp;
        }
    }

    protected void ShuffleOrder()
    {
        for (int n = 0; n < _order.Count; n++)
        {
            int i = UnityEngine.Random.Range(n, _order.Count);

            string temp = _order[i];
            _order[i] = _order[n];
            _order[n] = temp;
        }
        RLogger.Log("Order Shuffeled, the resuls is:");
        for (int n = 0; n < _order.Count; n++)
        {
            RLogger.Log(_order[n]);
        }
    }

    protected void SaveLargerResponse(InputAction.CallbackContext obj)
    {
        if (spawn != null) SaveEntry(LARGER_RESPONSE);
        NextTrial();
    }

    protected void SaveSmallerResponse(InputAction.CallbackContext obj)
    {
        if (spawn != null) SaveEntry(SMALLER_RESPONSE);
        NextTrial();
    }

    /// <summary>
    /// Creates the experiment file
    /// </summary>
    protected void CreateExpFile()
    {
        m_DataManager.CreateExpFile($"Exp{expType.ToSafeString()}_{m_PID}_{m_Condition}_{m_Repeats}Reps_{m_ScaleDiff}Range");
        /*FILE_PATH = $"{Application.persistentDataPath}/ExpEntry_{System.DateTime.Now:yyyy-MM-dd-HH_mm_ss}.csv";
        Debug.Log("fILE PATH IS: " + FILE_PATH);

        FILE_TEMP = new StringBuilder();
        FILE_TEMP.AppendLine(string.Join(SEPARATOR, HEADING));*/
    }

    /// <summary>
    /// Saves the current/last entry from the user
    /// </summary>
    /// <param name="response"></param>
    protected void SaveEntry(string response)
    {
        if (SaveWait()) return;

        _savingEntry = true;
        m_SaveAudio.Play();
        Debug.Log($"Saving entry {response}");
        string trial = GetCurTrial();
        string shape = trial.Split('|')[1];
        float size = float.Parse(trial.Split('|')[2]);

        _entryTime= DateTime.Now;
        TimeSpan diff = _entryTime - _dispTime;
        m_DataManager.UpdateExpFile(_nTrialNumber, shape, size, response, diff.ToString(), _entryTime.TimeOfDay.ToString());

        /*string entry = $"{_nTrialNumber},{shape}, {size}, {response}";
        FILE_TEMP.AppendLine(string.Join(SEPARATOR, entry));*/
        _savedEntry = true;
        _savingEntry = false;
    }

    /// <summary>
    /// Save the experiment file
    /// </summary>
    protected void SaveExpFile()
    {   
        m_DataManager.SaveExpFile();
    }

    /// <summary>
    /// Sets up the next trial
    /// </summary>
    protected void NextTrial()
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

        HideAllSlotMarkers();
        //Set up experiment env
        NextSetUp();

        if (spawn != null)
        {
            m_DataManager.RemoveObjectTrack(spawn);
            DestroySpawnShape();
            //deactivate the active slot markers
        }

        SpawnShape();
            
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

    /// <summary>
    /// Spawns the next "spawn" shape
    /// </summary>
    protected virtual void SpawnShape()
    {
        string trial = GetNextTrial();

        int index = int.Parse(trial.Split('|')[0]);
        
        int loc = int.Parse(trial.Split('|')[3]);

        spawn = Instantiate(m_Shapes[index], m_ShapeSpawn[loc].transform);
        spawn.name = $"{m_Shapes[index].name}_Trial{_nTrialNumber}";
        InitShapeSpawn();
    }

    /// <summary>
    /// Initializes values for the current "spawn" shape
    /// </summary>
    protected virtual void InitShapeSpawn()
    {
        string trial = GetNextTrial();
        float size = float.Parse(trial.Split('|')[2]);

        spawn.transform.localScale = new Vector3(spawn.transform.localScale.x * size,
            spawn.transform.localScale.y * 1,
            spawn.transform.localScale.z * size);
        if (m_FacePlayer)
        {
            spawn.transform.LookAt(_playerTransform.position, Vector3.up);
            spawn.transform.Rotate(new Vector3(90, 0, 0));
        }
        else
        {
            spawn.transform.eulerAngles = new Vector3(90, -180, -180);
        }
        if (spawn.TryGetComponent<Rigidbody>(out Rigidbody r))
        {
            r.mass = 1e+09f;
            r.constraints = RigidbodyConstraints.FreezeRotation;
            r.drag = 1000;
            r.angularDrag = 0;
        }

        if (spawn.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable g))
        {
            g.enabled = m_CanGrabShapes;
            //to turn of the slot marker after grabbing the shape
            if(m_CanGrabShapes)
                g.selectEntered.AddListener(HideAllSlotMarkers);
        }
    }

    /// <summary>
    /// Destroys the current "spawn" shape
    /// </summary>
    protected virtual void DestroySpawnShape()
    {
        Destroy(spawn);
        spawn = null;
    }

    /// <summary>
    /// Returns whether we can move to next trial or not
    /// </summary>
    /// <returns></returns>
    protected bool NextWait()
    {
        Debug.Log($"Unable to proceed to Next.......waiting \n " +
            $"ToRun: {ToRun}; _ready: {_ready}; _nextLoading: {_nextLoading}; _minBlankLoading: {_minBlankLoading}; _trialLoading: {_trialLoading};");
        return (!ToRun || !_ready || _nextLoading || _minBlankLoading || _trialLoading);
    }

    /// <summary>
    /// Returns whether we can save now or not
    /// </summary>
    /// <returns></returns>
    protected bool SaveWait()
    {
        Debug.Log("Unable to proceed to Save.......waiting");
        return (!_ready || _savedEntry || _savingEntry || !ToRun || _nextLoading /*|| _trialLoading*/);
    }

    /// <summary>
    /// Returns string representation of the current trial. In format "shapeNumber|shapeName|scale|spawnLocation";
    /// </summary>
    /// <returns></returns>
    public string GetCurTrial()
    {
        return _order[_nTrialNumber - 1];
    }

    /// <summary>
    /// Returns string representation of the next trial. In format "shapeNumber|shapeName|scale|spawnLocation";
    /// </summary>
    /// <returns></returns>
    public string GetNextTrial()
    {
        return _order[_nTrialNumber];
    }

    /// <summary>
    /// Returns the string representation of the trial based on the given trialNumber. In format "shapeNumber|shapeName|scale|spawnLocation";
    /// </summary>
    /// <param name="trialNumber"></param>
    /// <returns></returns>
    public string GetTrial(int trialNumber)
    {
        return _order[trialNumber];
    }

    protected void OnDisable()
    {
        largerButton.performed -= SaveLargerResponse; largerButton.Disable();
        smallerButton.performed -= SaveSmallerResponse; smallerButton.Disable();
    }
}