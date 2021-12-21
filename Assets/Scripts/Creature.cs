using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class Creature : MonoBehaviour
{
    /*
     *  general program structure:
     *
     *  each Update:
     *      check if starving
     * 
     *      if not busy with eating/mating --- Maybe not necessary due to Coroutines -> TODO: way to hold update in Coroutine?
     *          if not already have real target (not random pos)
     *              roll intention
     *              look for target for that intention
     *
     *          move To Intention
     *              when target found
     *                  set busy true
     *                  eat/mate/nothing if neither found (not busy then either)
     *
     *          repeat
     */

    [SerializeField] private float moveSpeed;
    [SerializeField] private Vector3 target;
    [SerializeField] private float awarenessRadius;

    [SerializeField]
    private float[] genes; // Search for food | Search for mate | Move speed // TODO: | awarenessRadius?

    [SerializeField] private int actionsTillStarve;
    [SerializeField] private int currentActionsTillStarve;

    private Creature targetCreature;
    private Food targetFood;

    private bool haveRealTarget, matingEatingOrDying; // True if target is not just a random position

    // Start is called before the first frame update    
    void Start()
    {
        haveRealTarget = false;
        genes = new float[4];
        currentActionsTillStarve = actionsTillStarve;
        GenerateRandomGenes();
        SetColour();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentActionsTillStarve <= 0)
        {
            StartCoroutine(Starve());
        }

        if (!matingEatingOrDying)
        {
            if (!haveRealTarget)
            {
                CheckForTargets(GetIntention());
            }

            MoveTowards(target, GetIntention());
        }
    }

    private String GetIntention()
    {
        String intention;

        float mateEatSum = genes[0] + genes[1];
        float randomNumber = Random.Range(0, mateEatSum);

        if (randomNumber > genes[0])
        {
            intention = "mate";
        }
        else
        {
            intention = "eat";
        }

        return intention;
    }

    private void CheckForTargets(String intention)
    {
        // TODO: The closer the more likely
        LayerMask creatureMask = LayerMask.GetMask("Creature");
        LayerMask foodMask = LayerMask.GetMask("Food");
        LayerMask chosenMask = intention.Equals("mate") ? creatureMask : foodMask;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(gameObject.transform.position, awarenessRadius, chosenMask);

        List<Collider2D> collidersList = new List<Collider2D>(colliders);
        collidersList.Remove(GetComponent<Collider2D>());

        if (collidersList.Count <= 0) // Get random point
        {
            if (target == Vector3.zero) // If just sent back from eat/mate
            {
                target = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), 0);
                currentActionsTillStarve--;
            }
            haveRealTarget = false;
        }
        else // Found a valid target
        {
            int randomIndex = Random.Range(0, collidersList.Count - 1);
            target = collidersList[randomIndex].gameObject.transform.position;
            targetCreature = collidersList[randomIndex].gameObject.GetComponent<Creature>();
            targetFood = collidersList[randomIndex].gameObject.GetComponent<Food>();

            haveRealTarget = true;
            currentActionsTillStarve--;
        }
    }

    private void MoveTowards(Vector3 target, String intention)
    {
        Vector3 moveDirection = target - gameObject.transform.position;
        float magnitudeMoveDirection = Vector3.Magnitude(moveDirection);
        Vector3 normalisedMoveDirection = moveDirection / magnitudeMoveDirection;

        if (math.abs(Vector3.Magnitude(target - gameObject.transform.position)) > moveSpeed * 0.5f) // If far enough away, move
        {
            gameObject.transform.position += normalisedMoveDirection * moveSpeed;
        }
        else // Else fulfill intention
        {
            if (haveRealTarget)
            {
                matingEatingOrDying = true;
                
                if (intention.Equals("eat"))
                {
                    StartCoroutine(Eat());
                }
                else if (intention.Equals("mate"))
                {
                    StartCoroutine(Mate());
                }

                matingEatingOrDying = false;
            }

            this.target = Vector3.zero;
            haveRealTarget = false;
        }
    }

    private IEnumerator Mate()
    {
        currentActionsTillStarve--;
        StartCoroutine(targetCreature.PauseForMate());
        yield return new WaitForSeconds(1);
        
        targetCreature = null;
        targetFood = null;
        haveRealTarget = false;
        target = Vector3.zero;
    }

    public IEnumerator PauseForMate()
    {
        matingEatingOrDying = true;

        yield return new WaitForSeconds(1);

        matingEatingOrDying = false;
    }

    private IEnumerator Eat()
    {
        targetFood.BeEaten();
        
        yield return new WaitForSeconds(1);
        
        currentActionsTillStarve = actionsTillStarve;
        transform.localScale = new Vector3(transform.localScale.x + 0.2f, transform.localScale.y + 0.2f,
            transform.localScale.z);


        // Set to null for next search
        targetCreature = null;
        targetFood = null;
        haveRealTarget = false;
        target = Vector3.zero;
    }

    public void SetColour()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color =
            new Color(genes[0] / 255 + genes[3] / 255, genes[1] / 255 + genes[3] / 255,
                genes[2] / 255 + genes[3] / 255);
    }

    public void GenerateRandomGenes()
    {
        int randomLimit1 = Random.Range(0, 100);
        int randomLimit2 = Random.Range(0, 100);
        int randomLimit3 = Random.Range(0, 100);
        List<int> numberList = new List<int>();
        numberList.Add(randomLimit1);
        numberList.Add(randomLimit2);
        numberList.Add(randomLimit3);
        numberList.Sort();

        genes[0] = numberList[0];
        genes[1] = numberList[1] - numberList[0];
        genes[2] = numberList[2] - numberList[1];
        genes[3] = 100 - numberList[2];
    }

    private IEnumerator Starve()
    {
        matingEatingOrDying = true;
        GetComponent<SpriteRenderer>().color = Color.red;
        yield return new WaitForSeconds(1);
        GameObject.Destroy(gameObject);
    }
}