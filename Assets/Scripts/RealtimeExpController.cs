using Normal.Realtime;
using UnityEngine;

public class RealtimeExpController : ExpController
{
    [Header("REALTIME")]
    [SerializeField]
    private Realtime m_Realtime;
    // Start is called before the first frame update
    void Start()
    {
        if (m_Realtime == null)
        {
            Debug.Log("Realtime not specified");
            return;
        }
        m_Realtime.didConnectToRoom += Initialize;
    }

    private void Initialize(Realtime realtime)
    {
        if (!ToRun) return;

        //don't do anything if it isn't the first user....
        if (m_Realtime.clientID != 0) return;

        base.Initialize();
    }

    protected override void SpawnShape()
    {
        string trial = GetNextTrial();

        string shape = trial.Split('|')[1];
        float size = float.Parse(trial.Split('|')[2]);
        int loc = int.Parse(trial.Split('|')[3]);

        spawn = Realtime.Instantiate(shape, m_ShapeSpawn[loc].position, m_ShapeSpawn[loc].rotation, new Realtime.InstantiateOptions
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
        if (m_FacePlayer)
            spawn.transform.Rotate(new Vector3(90, 0, 0));
        if (spawn.TryGetComponent<Rigidbody>(out Rigidbody r))
        {
            r.mass = 1e+09f;
            r.constraints = RigidbodyConstraints.FreezeRotation;
            r.drag = 1000;
            r.angularDrag = 0;
        }
    }

    protected override void DestroyShape()
    {
        Realtime.Destroy(spawn);
        spawn = null;
    }

    new void OnDisable()
    {
        base.OnDisable();
        m_Realtime.didConnectToRoom -= Initialize;
    }
}
