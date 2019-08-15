﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ReissNPCController : MonoBehaviour {
    //to remove later
    public string[] categories = { "Food: ", "Exploration: ", "Sleep: ", "Drink: " };

    public Text displayText;
    public GameObject idlePosition;

    public float speed;
    public GameObject desire;
    // put some of reiss's stuff here
    public float[] desires; //hunger, curiosity, sleepiness, thirst
    Vector3 movement;
    Affordances[] allAffordances;
    float[] weightmatrix;
    GameTime currTime;
    NavMeshAgent agent;

    //stuff used for calculating priorities
    public float currWeight;
    public float maxweight;
    int maxweightindex;
    float[] testAffordanceDesires;
    float[] postAffordanceDesires;
    float desireWeight;

    public float happiness = 80; //time-based progressive value for desires
    public float desireGoal = .7f; //desire weight, below generates unhappiness

    //PERSONALITY TRAITS
    public int attention_Span = 1; //affects rate of happiness changing
    public int extraversion = 50;
    public int energy = 50;

    bool inTask = false; //whether or not in the state of applying an affordance, currently not acutally used

    public float hunger=75;
    public float curiosity=75;
    public float sleepiness=75;
    public float thirst=75;

    private float farThreshold; //if item is too far, less desire to go to it. this is the threshold
    public Affordances lastAffordanceUsed; //last affordance used has less weight when deciding what afforadance to go to

    bool flag = false;

    public GameTime clock;

    //for example's sake, putting decays into public values instead of off of a .json
    public float hungerDecay;
    public float curiosityDecay;
    public float sleepinessDecay;
    public float thirstDecay;
    

    public float[] decays;

    //weights and thresholds for varioius states of desirability, not implemented yet
     public float[] normalThresholds = { 80, 60, 40 };
   /*/float[] preferredThresholds = { 70, 50, 30 };
    float[] dislikeThresholds = { 90, 70, 50 };*/

    float needWeight = 1;
    float wantWeight = .5F;
    float mehWeight = .3F;
    float neutralWeight = 0;
    public float[] getDesireDecay()
    {
        return decays;
    }
    public float getHappiness()
    {
        return happiness;
    }
    // Use this for initialization
    void Start()
    {
        //displayText.text = "";
        agent = GetComponent<NavMeshAgent>();
        clock = FindObjectOfType<GameTime>();
        farThreshold = FindObjectOfType<GameState>().farThreshold;

        //for now using random numbers for decays and initial values
        hungerDecay = Random.Range(0, .3f);
        curiosityDecay = Random.Range(0, .3f);
        sleepinessDecay = Random.Range(0, .3f);
        thirstDecay = Random.Range(0, .3f);

        hunger = Random.Range(0, 100);
        curiosity = Random.Range(0, 100);
        sleepiness = Random.Range(0, 100);
        thirst = Random.Range(0, 100);

        desires = new float[] { hunger, curiosity, sleepiness, thirst };
        idlePosition = GameObject.FindWithTag("Idle");
        //currTime = FindObjectOfType<GameTime>();

        //remove later
        decays = new float[] { hungerDecay, curiosityDecay, sleepinessDecay, thirstDecay };
        postAffordanceDesires = new float[4];
        
        if (CompareTag("Child"))
        {
            attention_Span = 1;
            extraversion = 100;
            energy = 100;
        }
        else if (CompareTag("Adult"))
        {
            attention_Span = 4;
            extraversion = 50;
            energy = 50;
        }
        else if (CompareTag("Elder"))
        {
            attention_Span = 4;
            extraversion = 25;
            energy = 30;
            
        }
        StartCoroutine(ProgressDesires(clock.timeSpeed));
        StartCoroutine(DecideAffordance(15*clock.timeSpeed));
        

    }
    IEnumerator ProgressDesires(float time) //progress desire changes as well as happiness
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(time);
        
        // to remove later, end up using tags to "differentiate" agents
        for (; ; )
        {
            for (int i = 0; i < desires.Length; i++)
            {;
                desires[i] -= decays[i];//+.001f*(100-desires[i]); //delete this later

                //postAffordanceDesires[i] = desires[i];
            }
            NormalizeDesires(ref desires);
            currWeight = FindDesireWeight(desires);
            currWeight = FindDesireWeight(desires);
            /*if (desireGoal>currWeight)
            {
                happiness += desireGoal - currWeight;
            }
            else
            {
                happiness += (desireGoal - currWeight) / attention_Span;
            }*/
            if (desireGoal-currWeight<0)
            {
                happiness += (desireGoal - currWeight) / attention_Span/3;
            }
            happiness += (desireGoal-currWeight)/attention_Span;
            if (happiness > 100)
                happiness = 100;
            else if (happiness < 0)
                happiness = 0;
            
            yield return waitForSeconds;
        }
    }
    void OnMouseEnter()
    {
        //If your mouse hovers over the GameObject with the script attached, output this message
        //displayText.text = "";
        
        //Debug.Log("Mouse is over GameObject.");
    }
    void OnMouseExit()
    {
        //displayText.text = "";
    }


    IEnumerator DecideAffordance(float time)
    {
        NavMeshAgent mNavMeshAgent = GetComponent<NavMeshAgent>();

       
        WaitForSeconds waitForSeconds = new WaitForSeconds(time);
        WaitForSeconds waitForIdleSeconds = new WaitForSeconds(10 * time*attention_Span);
        for (; ; )
        {
            while (happiness>50&&idlePosition!=null&&!inTask) //content, no need to decide on anything
            {
                if (Random.value * 100 < 100 - happiness)
                    break;
                agent.SetDestination(idlePosition.transform.position);
                yield return waitForIdleSeconds;
            }
            
            
            inTask = true;
            NormalizeDesires(ref desires);
            maxweight = 0;

            maxweightindex = -1;
            movement.Set(0, 0, 0);
            allAffordances = FindObjectsOfType<Affordances>(); //find all gameobjects that contain affordances
            weightmatrix = new float[allAffordances.Length];
            for (int i = 0; i < allAffordances.Length; i++) //logic goes here for calculating affordance weight
            {
                weightmatrix[i] = currWeight-FindDesireWeight(ApplyAffordance(allAffordances[i])); //see weight after applying affordance
               
                if (weightmatrix[i] >0)
                {
                   
                    movement = transform.position - allAffordances[i].transform.position;

                    if (allAffordances[i].duration > 35) //if too long, apply a penalty
                        weightmatrix[i] /= 1.3f;

                    //energy
                    if (movement.magnitude > farThreshold) //if too far, apply a penalty, to be slightly complicated further in future
                    {
                        if (CompareTag("Adult"))
                        {
                            weightmatrix[i] /= 2;
                   
                        }
                        else if (CompareTag("Elder"))
                        {
                            weightmatrix[i] /= 4;
                        }
                    }
                       // weightmatrix[i] *= energy/50;
                    if (allAffordances[i] == lastAffordanceUsed) //if last affordance used, apply a penalty, to be a buffer of N affordances in the future
                        weightmatrix[i] /= 4;

                    

                    //extraversion
                    if (Physics.OverlapSphere(allAffordances[i].transform.position, 7).Length>10) //consider a popular area
                    {
                        if (CompareTag("Adult"))
                        {
                            weightmatrix[i] /= 4;

                        }
                        else if (CompareTag("Elder"))
                        {
                            weightmatrix[i] /= 6;
                        }
                        /*if (extraversion>50)
                        {
                            weightmatrix[i] *= (extraversion - 50) / 25;
                        }
                        else if (extraversion<50)
                        {
                            weightmatrix[i] /= (50 - extraversion) / 25;
                        }*/
                    }


                    if (maxweight <weightmatrix[i])
                    {
                        maxweight = weightmatrix[i];
                        maxweightindex = i;
                        //desireAffordance
                    }
                }
            }
            //Debug.Log(maxweight + " " + currWeight);
            if (maxweightindex > -1) //if there is a affordance desired, work towards it
            {

                //Debug.Log(maxweight);
                //Debug.Log(weightmatrix[0]);
                desire = allAffordances[maxweightindex].gameObject;

                movement = transform.position - desire.transform.position;

                //afterwards, check if need to apply affordance, at the moment just sees if its a specific distance away
                /*Debug.Log(mNavMeshAgent.remainingDistance);
                if (mNavMeshAgent.remainingDistance<1.5)
                {

                    postAffordanceDesires = ApplyAffordance(desire.GetComponent<Affordances>());
                    Debug.Log("Used " + desire.name);

                    for (int i = 0; i < desires.Length; i++)
                    {
                        desires[i] = postAffordanceDesires[i];
                    }
                    flag = true;
                    desire.GetComponent<Affordances>().Use(gameObject);
                    inTask = true;
                    lastAffordanceUsed = desire.GetComponent<Affordances>();
                    yield return new WaitForSeconds(desire.GetComponent<Affordances>().duration * clock.timeSpeed);
                }
                else
                    agent.SetDestination(desire.transform.position); */


                if (movement.magnitude < 2.2)
                 {
                    


                     postAffordanceDesires = ApplyAffordance(desire.GetComponent<Affordances>());
                     Debug.Log("Used " + desire.name);

                     for (int i = 0; i < desires.Length; i++)
                     {
                         desires[i] = postAffordanceDesires[i];
                     }
                     flag = true;
                     desire.GetComponent<Affordances>().Use(gameObject);
                     inTask = true;
                     lastAffordanceUsed = desire.GetComponent<Affordances>();
                    inTask = false;
                     yield return new WaitForSeconds(desire.GetComponent<Affordances>().duration * clock.timeSpeed);

                     // postAffordanceDesires[0] = 151;

                     //NormalizeDesires(ref desires);
                     //Debug.Log(desires[0] + " " + desires[1] + " " + desires[2] + " " + desires[3]);
                 }
                 else
                     agent.SetDestination(desire.transform.position);
                //Debug.Log(gameObject.name);
                //Debug.Log(agent.destination);
            }
            yield return waitForSeconds;
        }
    }
    float FindDesireWeight (float[] desirematrix)
    {
        desireWeight = 0;
        foreach (float parameter in desirematrix)
        {
            if (parameter >= normalThresholds[0]) 
            {
                desireWeight += neutralWeight ;
            }
            else if (parameter >= normalThresholds[1])
            {
                desireWeight += mehWeight ;
            }
            else if (parameter >= normalThresholds[2])
            {
                desireWeight += wantWeight ;
            }
            else //need
                desireWeight += needWeight ;
        }
        return desireWeight;

    }

    void NormalizeDesires(ref float[] matrix) //if any desires above 100, set to 100, if below 0 set to 0
    {
        for (int i = 0; i < matrix.Length; i++)
        {
            if (matrix[i] > 100)
                matrix[i] = 100;
             else if (matrix[i] < 0)
                matrix[i] = 0;
        }
    }
    public float[] ApplyAffordance(Affordances affordance) //returns desire matrix with affordance applied
    {
        float[] affordances = affordance.getAffordances();
        if (affordances.Length!=desires.Length)
        {
            Debug.Log("Uh, something's wrong with desire lengths");
        }
        else
        {
            for (int i=0;i<affordances.Length;i++)
            {
                postAffordanceDesires[i] = desires[i]+affordances[i];


            }
            NormalizeDesires(ref postAffordanceDesires);
            //affordance.Use(gameObject);
            //Debug.Log(affordance.stock);
            
        }
        return postAffordanceDesires;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate() //physics calculations
    {
        //float moveHorizontal = Input.GetAxis("Horizontal");
        //float moveVertical = Input.GetAxis("Vertical");
        //Debug.Log("got here");
        /*if (movement.magnitude > 0) //perform momvement
        {
            //Debug.Log("got here");
            transform.position += -1*speed * movement;
            //transform.position += -1 * speed * movement.normalized;
        }*/
        // movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // GetComponent<Rigidbody>().AddForce(-1*speed * movement);

    }
}
