using System.IO;
using Normal.Realtime;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEngine.UIElements.Experimental;

public class DataManager : MonoBehaviour
{
    [SerializeField]
    HashSet<GameObject> _toTrack = new HashSet<GameObject>();
    [SerializeField]
    private bool _canTrackPlayer;
    [SerializeField]
    private bool _canTrackObjects;
    [SerializeField]
    private Realtime _realTime;
    /// <summary>
    /// How often should data be read to the output files in seconds when specified
    /// </summary>
    [SerializeField]
    private float _timeInterval;

    //heading for csv's
    private string OBJECT_HEADING = "Object,Owner,Time,XPos,Ypos,ZPos,XRot,YRot,ZRot,Status\n";
    private string PLAYER_HEADING = "Bone,Time,XPos,YPos,ZPos,XRot,YRot,ZRot\n";
    private string EXP_HEADING = "Trial,Shape,Size,Response,ResponseTime,Time";
    private int id = 0;
    private bool objfileCreated = false;

    private float timeCounter = 0;


    //all file paths
    private Dictionary<int, string> PLAYER_FILE_PATH;
    private string OBJECT_FILE_PATH = "";
    private string EXP_FILE_PATH = "";
    //temporary file informations
    private static Dictionary<int, StringBuilder> PLAYER_FILE_TEMP;
    private static StringBuilder OBJECT_FILE_TEMP;
    private static StringBuilder EXP_FILE_TEMP;
    private static string SEPARATOR = ",";

    // Start is called before the first frame update
    void Start()
    {
        //create a file for the trial;
        CreateObjectsFile();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(timeCounter >= _timeInterval) {
            UpdateObjectFile();

            timeCounter = 0;
        }

        timeCounter += Time.deltaTime;
    }

    public void SetID(int id)
    {
        this.id = id;
        CreateObjectsFile();
    }

    public void AddObjectTrack(GameObject g)
    {
        _toTrack.Add(g);
    }

    public void RemoveObjectTrack(GameObject g)
    {
        _toTrack.Remove(g);
    }

    private void CreateObjectsFile()
    {
        if (!_canTrackObjects) return;

        OBJECT_FILE_PATH = $"{Application.persistentDataPath}/objectsData_{System.DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss")}.csv";//something to identify the participant

        OBJECT_FILE_TEMP = new StringBuilder();
        OBJECT_FILE_TEMP.AppendLine(string.Join(SEPARATOR, OBJECT_HEADING));
        //File.WriteAllText(OBJECT_FILE_PATH, _objectHeader);

        Debug.Log($"Object File Path is: {OBJECT_FILE_PATH}");

        objfileCreated = true;
    }

    private void UpdateObjectFile()
    {
        if (!_canTrackObjects) return;

        if (!objfileCreated) return;

        if (_toTrack.Count <= 0) return;

        string update = "";
        int ownerID = -1;
        foreach (GameObject track in _toTrack)
        {
            ownerID = track.TryGetComponent<RealtimeView>(out RealtimeView rV) ? rV.ownerIDInHierarchy : ownerID;
            string status = (track.TryGetComponent<NomcoreObject>(out NomcoreObject nO)) ? nO.GetStatus() : track.name;
            update += $"{track.name},{ownerID},{DateTime.Now.TimeOfDay}," +
                $"{track.transform.position.x},{track.transform.position.y},{track.transform.position.z}," +
                $"{track.transform.eulerAngles.x},{track.transform.eulerAngles.y},{track.transform.eulerAngles.z}," +
                $"{status}";
            OBJECT_FILE_TEMP.AppendLine(string.Join(SEPARATOR, update));
        }
        //File.AppendAllText(OBJECT_FILE_PATH, update);
    }

    private void SaveObjectsFile()
    {
        if (!_canTrackObjects) return;

        if (!objfileCreated) return;

        if (_toTrack.Count <= 0) return;

        try
        {
            File.WriteAllText(OBJECT_FILE_PATH, OBJECT_FILE_TEMP.ToString());
            OBJECT_FILE_TEMP.Clear();
            Debug.Log($"Objects file saved to {OBJECT_FILE_PATH}");
        }
        catch (Exception e)
        {
            Debug.Log($"Objects data could not be written to csv file due to exception: {e}");
            return;
        }
    }

    //create a file to store skeleton data for each player
    public void CreatePlayerFile(int pID)
    {
        if (!_canTrackPlayer) return;

        if (PLAYER_FILE_PATH == null)
            PLAYER_FILE_PATH = new Dictionary<int, string>();

        string playerFilePath = $"{Application.persistentDataPath}/p{pID}_skeletonData_{System.DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss")}.csv";

        if(PLAYER_FILE_TEMP == null)
            PLAYER_FILE_TEMP = new Dictionary<int, StringBuilder>();

        PLAYER_FILE_TEMP[pID] = new StringBuilder();
        PLAYER_FILE_TEMP[pID].AppendLine(string.Join(SEPARATOR, PLAYER_HEADING));

        //File.WriteAllText(playerFilePath, _playerHeader);
        PLAYER_FILE_PATH.Add(pID, playerFilePath);
    }

    public void UpdatePlayerFile(int pID, Transform trans)
    {
        if (!_canTrackPlayer || !PLAYER_FILE_PATH.ContainsKey(pID)) return;

        string update = $"{trans.name},{DateTime.Now.TimeOfDay}," +
            $"{trans.position.x},{trans.position.y},{trans.position.z}," +
            $"{trans.eulerAngles.x},{trans.eulerAngles.y},{trans.eulerAngles.z}";

        PLAYER_FILE_TEMP[pID].AppendLine(string.Join(SEPARATOR, update));

        //string path = PLAYER_FILE_PATH[playerID];
        //File.AppendAllText(path, update);
    }

    public void UpdatePlayerFile(int pID, Pose pose, string name)
    {
        if (!_canTrackPlayer || !PLAYER_FILE_PATH.ContainsKey(pID)) return;

        string update = $"{name},{DateTime.Now.TimeOfDay}," +
            $"{pose.position.x},{pose.position.y},{pose.position.z}," +
            $"{pose.rotation.eulerAngles.x},{pose.rotation.eulerAngles.y},{pose.rotation.eulerAngles.z}\n";

        PLAYER_FILE_TEMP[pID].AppendLine(string.Join(SEPARATOR, update));

        //string path = PLAYER_FILE_PATH[playerID];
        //File.AppendAllText(path, update);
    }

    public void SavePlayerFile(int pID)
    {
        if (!_canTrackPlayer) return;

        if (!PLAYER_FILE_PATH.ContainsKey(pID)) return;

        try
        {
            File.WriteAllText(PLAYER_FILE_PATH[pID], PLAYER_FILE_TEMP[pID].ToString());
            PLAYER_FILE_TEMP.Clear();
            Debug.Log($"Player {pID} file saved to {PLAYER_FILE_PATH[pID]}");
        }
        catch (Exception e)
        {
            Debug.Log($"Player {pID} data could not be written to file due to exception: {e}");
            return;
        }
    }

    public void CreateExpFile()
    {
        if(EXP_FILE_PATH != "")
        {
            Debug.LogError("Exp File already created");
            return;
        }

        EXP_FILE_PATH = $"{Application.persistentDataPath}/ExpEntry_{System.DateTime.Now:yyyy-MM-dd-HH_mm_ss}.csv";
        Debug.Log($"EXP FILE PATH IS: {EXP_FILE_PATH}");

        EXP_FILE_TEMP = new StringBuilder();
        EXP_FILE_TEMP.AppendLine(string.Join(SEPARATOR, EXP_HEADING));
    }

    public void CreateExpFile(string extraInfo)
    {
        if (EXP_FILE_PATH != "")
        {
            Debug.LogError("Exp File already created");
            return;
        }

        EXP_FILE_PATH = $"{Application.persistentDataPath}/ExpEntry_{extraInfo}_{System.DateTime.Now:yyyy-MM-dd-HH_mm_ss}.csv";
        Debug.Log($"EXP FILE PATH IS: {EXP_FILE_PATH}");

        EXP_FILE_TEMP = new StringBuilder();
        EXP_FILE_TEMP.AppendLine(string.Join(SEPARATOR, EXP_HEADING));
    }

    public void UpdateExpFile(int trial, string shape, float size, string response, string responseTime, string time)
    {
        string entry = $"{trial},{shape}, {size}, {response}, {responseTime}, {time}";
        Debug.Log($"Updating Entry: {entry}");
        EXP_FILE_TEMP.AppendLine(string.Join(SEPARATOR, entry));
    }

    public void SaveExpFile()
    {
        try
        {
            File.AppendAllText(EXP_FILE_PATH, EXP_FILE_TEMP.ToString());
            Debug.Log($"Exp file saved to {EXP_FILE_PATH}");
            EXP_FILE_TEMP.Clear();
        }
        catch (Exception e)
        {
            Debug.Log($"Exp data could not be written to file due to exception: {e}");
            return;
        }
    }

    public void SaveAllFiles()
    {
        SaveExpFile();
        SaveObjectsFile();
        foreach(int pID in PLAYER_FILE_PATH.Keys)
            SavePlayerFile(pID);
    }
}

public class PlayerEntry
{

}

public class ObjectEntry
{
    public string ObjectName;
    public int OwnerID;
    public string Time;
    public float XPos;
    public float YPos;
    public float ZPos;
    public float XRot;
    public float YRot;
    public float ZRot;
    public string Status;

    public ObjectEntry(string objectName, int ownerID, string time, float xPos, float yPos, float zPos, float xRot, float yRot, float zRot, string status)
    {
        ObjectName = objectName;
        OwnerID = ownerID;
        Time = time;
        XPos = xPos;
        YPos = yPos;
        ZPos = zPos;
        XRot = xRot;
        YRot = yRot;
        ZRot = zRot;
        Status = status;
    }
}
