﻿using UnityEngine;
using System.Collections;

//This class requires a character controller attached to the CameraRig.
//This lets you hold down the touchpad to either walk, jog, or run in place.
public class VRTK_WalkInPlace : MonoBehaviour
{
    private CharacterController character;

    private SteamVR_TrackedObject leftControllerTrackedObject;
    private SteamVR_TrackedObject rightControllerTrackedObject;
    private SteamVR_Controller.Device leftControllerInput;
    private SteamVR_Controller.Device rightControllerInput;

    private SteamVR_TrackedObject controller;
    private SteamVR_Controller.Device controllerInput;
    private SteamVR_TrackedObject head;


    //This determines the amount of influence the slope of a hill has on speed.
    private const float slopeStrength = 10f;

    //Your speed
    private const float speed = 4;

    //Checks if the touchpad is held down.
    private bool isMoving;


    //boolean values to check if it's checking if you're running, etc and if you're actually running, etc
    private bool checkingRunning;
    private bool checkingJogging;
    private bool checkingWalking;
    private bool checkingStationary;

    //The time at which it begins checking what type of movement you're doing
    private float startTime;


    //boolean values indicating your current movement
    private bool isRunning;
    private bool isJogging;
    private bool isWalking;
    private bool isStationary;

    //How much your head has to move to walk, jog, or run
    private const float walkingThreshhold = .0005f;
    private const float joggingThreshhold = .0065f;
    private const float runningThreshhold = .011f;

    //How long the game will wait before it decides you're walking, jogging, or running
    private const float stationaryTime = .07f;
    private const float walkingTime = .12f;
    private const float joggingTime = .08f;
    private const float runningTime = .03f;

    //Jogging and running multiplies your speed
    private const float walkingMultiplier = .6f;
    private const float joggingMultiplier = 1.1f;
    private const float runningMultiplier = 1.7f;

    //Used to keep track of where your head was in the last frame and current frame
    private float prevY;
    private float currentY;

    //The direction of your movement.
    private float angle;

    //Important for knowing the slope of hills so you can slow down or up a hill.
    private Vector3 prevCharacterXZ;
    private Vector3 currentCharacterXZ;
    private float prevCharacterY;
    private float currentCharacterY;
    private float cosine;

    void Start()
    {
        character = GetComponent<CharacterController>();
        head = GetComponentInChildren<Camera>().gameObject.GetComponent<SteamVR_TrackedObject>();
        leftControllerTrackedObject = GetComponent<SteamVR_ControllerManager>().left.GetComponent<SteamVR_TrackedObject>();
        rightControllerTrackedObject = GetComponent<SteamVR_ControllerManager>().right.GetComponent<SteamVR_TrackedObject>();

    }

    void Update()
    {
        leftControllerInput = SteamVR_Controller.Input((int)leftControllerTrackedObject.index);
        rightControllerInput = SteamVR_Controller.Input((int)rightControllerTrackedObject.index);

        //This makes sure your character will fall due to gravity since SimpleMove forces you to fall due to gravity.
        character.SimpleMove(Vector3.forward * 0);

        //If the left controller clicks the touchpad, the left controller becomes the movement controller.
        if (leftControllerInput.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
        {
            controller = leftControllerTrackedObject;
            controllerInput = leftControllerInput;
        }

        //If the right contorller clicks the touchpad, the right controller becomes the movement controller.
        if (rightControllerInput.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
        {
            controller = rightControllerTrackedObject;
            controllerInput = rightControllerInput;
        }

        if (controllerInput.GetPress(SteamVR_Controller.ButtonMask.Touchpad))
        {
            isMoving = true;
        }
        if (controllerInput.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
        {
            isMoving = false;
        }


        //This just keeps track of the previous head height and current head height.
        prevY = currentY;
        currentY = head.transform.localPosition.y;

        //This is the different between head heights between frames
        var deltaY = Mathf.Abs(currentY - prevY);

        //This keeps track of your character's previous and current XZ positions, ignoring Y position.
        prevCharacterXZ = currentCharacterXZ;
        currentCharacterXZ = head.transform.position;
        Vector3 adjustedPreviousCharacterXZ = new Vector3(prevCharacterXZ.x, 0, prevCharacterXZ.z);
        Vector3 adjustedCurrentCharacterXZ = new Vector3(currentCharacterXZ.x, 0, currentCharacterXZ.z);

        //This keeps track of your character's previous and current Y position.
        prevCharacterY = currentCharacterY;
        currentCharacterY = head.transform.position.y;

        //This determines the change in XZ and change in Y.
        float deltaCharacterXZ = Vector3.Distance(adjustedPreviousCharacterXZ, adjustedCurrentCharacterXZ);
        float deltaCharacterY = currentCharacterY - prevCharacterY;

        //This calculates the slope of a hill the player is moving on.
        cosine = 1;
        if (deltaCharacterY > 0)
            cosine = Mathf.Cos(Mathf.Atan2(deltaCharacterY, deltaCharacterXZ));

        //This exaggerates the slope by a power of 10. This can be adjusted.
        cosine = Mathf.Pow(cosine, slopeStrength);

        //This is the part where you actually move.
        if (isMoving)
        {
            //You walk in the direction of your controller
            angle = controller.transform.rotation.eulerAngles.y / 180 * Mathf.PI;

            //If your head moves past the threshhold, your character moves at a certain speed.

            if (isRunning) character.Move(new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * cosine * speed * runningMultiplier * Time.deltaTime);
            if (isJogging) character.Move(new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * cosine * speed * joggingMultiplier * Time.deltaTime);
            if (isWalking) character.Move(new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * cosine * speed * walkingMultiplier * Time.deltaTime);


            //This checks if you are running, jogging, walking, or are stationary. If you pass the threshhold for a given time,
            //it will set your movement type.
            if (deltaY > runningThreshhold)
            {
                if (!isRunning)
                {
                    if (!checkingRunning)
                    {
                        UncheckAll();
                        checkingRunning = true;
                        startTime = Time.time;
                    }
                    else
                    {
                        if (Time.time - startTime > runningTime)
                        {
                            EndAllMotion();
                            isRunning = true;
                            checkingRunning = false;
                        }
                    }
                }
                else UncheckAll();
            }

            else if (deltaY > joggingThreshhold)
            {
                if (!isJogging)
                {
                    if (!checkingJogging)
                    {
                        UncheckAll();
                        checkingJogging = true;
                        startTime = Time.time;
                    }
                    else
                    {
                        if (Time.time - startTime > joggingTime) isJogging = true;
                        {
                            EndAllMotion();
                            isJogging = true;
                            checkingJogging = false;
                        }

                    }
                }
                else UncheckAll();
            }

            else if (deltaY > walkingThreshhold)
            {
                if (!isWalking)
                {
                    if (!checkingWalking)
                    {
                        UncheckAll();
                        checkingWalking = true;
                        startTime = Time.time;
                    }
                    else
                    {
                        if (Time.time - startTime > walkingTime)
                        {
                            EndAllMotion();
                            isWalking = true;
                            checkingWalking = false;
                        }
                    }
                }
                else UncheckAll();

            }
            else
            {
                if (!isStationary)
                {
                    if (!checkingStationary)
                    {
                        UncheckAll();
                        checkingStationary = true;
                        startTime = Time.time;
                    }
                    else
                    {
                        if (Time.time - startTime > stationaryTime)
                        {
                            EndAllMotion();
                            isStationary = true;
                            checkingStationary = false;
                        }

                    }
                }
                else UncheckAll();
            }

        }
        else
        {
            EndAllMotion();
            isStationary = true;
            UncheckAll();
        }

    }

    //This unchecks all the "checking" booleans.
    void UncheckAll()
    {
        checkingRunning = false;
        checkingJogging = false;
        checkingWalking = false;
        checkingStationary = false;
    }

    //This unchecks all the "movement" booleans.
    void EndAllMotion()
    {
        isStationary = false;
        isWalking = false;
        isJogging = false;
        isRunning = false;
    }





}
