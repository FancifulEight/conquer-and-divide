using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class GameManager : MonoBehaviour {
    public static GameManager gm = null;
    [Header("Audio")]
    public AudioSource mainSrc;
    public AudioClip beat, song;

    public float beatTempo = 120;
    [Range(1, 8)]
    public int ticksPerBeat = 2;
    public int totalBeats;

    private double startDSP, tickLength, nextTickTime;
    private int tickNum = 0;
    public UnityEvent ticked = new UnityEvent();

    [Header("UI")]
    public Text readyText;
    public GameObject[] armyPaths = new GameObject[4];
    public Animator kingAnim;

    [Header("Gameplay")]
    public Color armyColour = Color.blue;
    private float startTime = 0;
    public Vector2[] armyGoals = new Vector2[4];
    public int armySplit = 1;

    public List<Human> army = new List<Human>();
    public List<Transform> allEntities = new List<Transform>();

    public Encounter firstEncounter;

    public bool[] lmbArr;
    public bool[] rmbArr;

    void Awake() {
        if (gm == null) {
            gm = this;
        } else {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start() {
        beatTempo /= 60;
        totalBeats = (int)(beatTempo * 48);

        tickLength = 1f / (beatTempo * ticksPerBeat);
        nextTickTime = AudioSettings.dspTime + tickLength;

        //ticked.AddListener(Tick);
        ticked.AddListener(BounceKing);
        
        lmbArr = new bool[totalBeats];
        rmbArr = new bool[totalBeats];

        lmbArr[7-1] = true;
        lmbArr[15-1] = true;
        rmbArr[11-1] = true;
        rmbArr[15-1] = true;

        mainSrc = GetComponent<AudioSource>();
        mainSrc.clip = beat;
        mainSrc.Play(); 
    }

    public void Tick() {
        Debug.Log("Tick");
    }

    private int kingBobCount = 0;
    public int beatsPerBob = 8;
    public void BounceKing() {
        //Debug.Log("Count: " + kingBobCount);

        if (kingBobCount < beatsPerBob / 2)
            kingAnim.SetBool("Bob", true);
        else
            kingAnim.SetBool("Bob", false);
        
        kingBobCount = (kingBobCount + 1) % beatsPerBob;
    }

    public void Conquer() {
        if (armySplit == 1) return;
        armySplit = 1;

        for (int i = 0;i < armyPaths.Length;i++) {
            armyPaths[i].SetActive(i == armySplit - 1);
        }

        ReorganizeArmy();
    }

    public void Divide() {
        if (armySplit == armyGoals.Length) return;
        armySplit++;

        armyPaths[armySplit - 1].SetActive(true);
        
        ReorganizeArmy();
    }

    public void ReorganizeArmy() {
        for(int i = 0;i < armySplit;i++) {
            for (int j = i * army.Count / armySplit;j < (i + 1) * army.Count / armySplit && j < army.Count;j++) {
                army[j].armyNum = i;
            }
        }
    }

    private bool startingUp = false, musicPlaying = false;
    // Update is called once per frame
    void Update() {
        //Ticks
        startDSP = AudioSettings.dspTime;
        startDSP += Time.deltaTime;

        while(startDSP > nextTickTime) {
            ticked.Invoke();
            nextTickTime += tickLength;
            tickNum++;
        }
        //End Ticks

        bool left = Input.GetMouseButtonDown(0);
        bool right = Input.GetMouseButtonDown(1);
        if ((left || right) && !musicPlaying && !startingUp) {
            if (firstEncounter.isCorrectAction(left)) {
                firstEncounter.Recruit();
            } else {
                firstEncounter.Kill();
            }
            mainSrc.loop = false;
            startingUp = true;
            tickNum = 0;

            readyText.text = "READY";

        }

        if (!mainSrc.isPlaying) {
            if (startingUp) {
                startingUp = false;
                readyText.text = "";

                StartMusic();
            } else {
                mainSrc.loop = false;
                mainSrc.clip = beat;
            }
            mainSrc.Play();

        } else if (musicPlaying) {
            UpdateInGame();
        }
    }

    public void UpdateInGame() {
        KingSemaphore();

        if (Input.GetMouseButtonDown(0))
            Conquer();
        if (Input.GetMouseButtonDown(1))
            Divide();

        //My Army
        foreach (Transform t in allEntities) {
            t.Translate(0, -2 * Time.deltaTime, 0);
        }
        // float yTarget = armyGoals[0].y + 2 * Time.deltaTime;
        // for (int i = 0;i < armyGoals.Length;i++)
        //     armyGoals[i] = new Vector2(armyGoals[i].x, yTarget);
        
        // float camPos = Mathf.Lerp(Camera.main.transform.position.y, yTarget, 10 * Time.deltaTime);
        // Camera.main.transform.position = new Vector3(0, camPos, -10);
    }

    public void KingSemaphore() {
        float second = Time.time - startTime;
        int beat = (int)(beatTempo * second);
        bool changedLMB = false, changedRMB = false;

        //King Semaphore
        if (beat + 2 < totalBeats) {
            for (int i = 2;i >= 0;i--) {
                if (lmbArr[beat + i]) {
                    kingAnim.SetInteger("LMB", 3 - i);
                    changedLMB = true;
                }

                if (rmbArr[beat + i]) {
                    kingAnim.SetInteger("RMB", 3 - i);
                    changedRMB = true;
                }
            }
        }

        if (!changedLMB)
            kingAnim.SetInteger("LMB", 0);
        if (!changedRMB)
            kingAnim.SetInteger("RMB", 0);
    }

    public void StartMusic() {
        musicPlaying = true;
        mainSrc.loop = false;
        mainSrc.clip = song;

        startTime = Time.time;
    }

    public void ButtonHit() {
        Debug.Log("Hit On Time");
    }

    public void ButtonMissed() {
        Debug.Log("Missed Note");
    }
}
