using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

public enum CharacterState
{
    Idle = 0,
    WalkingForward = 1,
    WalkingBackwards = 2,
    Jumping = 4,
    RunningFoward = 8,
    RunningBackward = 16
}

public class Lab02b_PlayerControlPrediction : NetworkBehaviour
{

    struct PlayerState
    {
        public int movementNumber;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ;
        public CharacterState animationState;
    }

    [SyncVar(hook = "OnServerStateChanged")]
    PlayerState serverState;

    PlayerState predictedState;

    public float speed = 0.1f;
    public float maxSpeed = 1;
    public float runThresh = 0.2f;



    Queue<KeyCode> pendingMoves;//This will represent the moves that the player is attempting
                                //That have not been acknowledged by the server yet!
                                //Remember: Queue is a first-in first-out list
    CharacterState characterAnimationState;
    public Animator animationController;

    void Start()
    {
        InitState();
        predictedState = serverState;

        if (isLocalPlayer)
        {
            pendingMoves = new Queue<KeyCode>();
            //UpdatePredictedState();
        }
        SyncState();
    }

    [Server]
    void InitState()
    {
        serverState = new PlayerState
        {
            movementNumber = 0,
            posX = -119f,
            posY = 165.08f,
            posZ = -924f,
            rotX = 0f,
            rotY = 0f,
            rotZ = 0f
        };
    }

    void OnServerStateChanged(PlayerState newState)
    {
        serverState = newState;
        if (pendingMoves != null)
        {
            while (pendingMoves.Count > (predictedState.movementNumber - serverState.movementNumber))
            {
                pendingMoves.Dequeue();
            }
            UpdatePredictedState();
        }
    }

    void UpdatePredictedState()
    {
        predictedState = serverState;
        foreach (KeyCode moveKey in pendingMoves)
        {
            predictedState = Move(predictedState, moveKey);
        }
    }

    //Takes the sync state and sets the local transform to it.
    void SyncState()
    {
        PlayerState stateToRender = isLocalPlayer ? predictedState : serverState;

        transform.position = new Vector3(stateToRender.posX, stateToRender.posY, stateToRender.posZ);
        transform.rotation = Quaternion.Euler(stateToRender.rotX, stateToRender.rotY, stateToRender.rotZ);
        animationController.SetInteger("CharacterState", (int)stateToRender.animationState);
    }

    CharacterState CalcAnimation(float dX, float dY, float dZ, float dRY)
    {
        if(dX == 0 && dY == 0 && dZ == 0)
        {
            return CharacterState.Idle;
        }

        if(dX != 0 || dZ != 0)
        {
            if(dX > 0 || dZ > 0)
            {
                if(dX > runThresh || dZ > runThresh)
                {
                    return CharacterState.RunningFoward;
                }
                return CharacterState.WalkingBackwards;
            }
            else
            {
                if(dX < -runThresh || dZ < -runThresh)
                {
                    return CharacterState.RunningBackward;
                }
                return CharacterState.WalkingForward;
            }
        }
        if(dY != 0)
        {
            return CharacterState.Jumping;
        }

        return CharacterState.Idle;
    }

    PlayerState Move(PlayerState previous, KeyCode newKey)
    {
        float deltaX = 0, deltaY = 0, deltaZ = 0;
        float deltaRotationY = 0;

        switch (newKey)
        {
            case KeyCode.Q:
                deltaX = -speed;
                break;
            case KeyCode.S:
                deltaZ = -speed;
                break;
            case KeyCode.E:
                deltaX = speed;
                break;
            case KeyCode.W:
                deltaZ = speed;
                break;
            case KeyCode.A:
                deltaRotationY = -1f;
                break;
            case KeyCode.D:
                deltaRotationY = 1f;
                break;
            case KeyCode.Space:
                //jump?
                break;
        }        return new PlayerState
        {
            movementNumber = 1 + previous.movementNumber,
            posX = deltaX + previous.posX,
            posY = deltaY + previous.posY,
            posZ = deltaZ + previous.posZ,
            rotX = previous.rotX,
            rotY = deltaRotationY + previous.rotY,
            rotZ = previous.rotZ,
            animationState = CalcAnimation(deltaX, deltaY, deltaZ, deltaRotationY)
        };
    }

    void Update()
    {
        /*
        -The script that is utilizing isLocalPlayer MUST extend a NetworkBehaviour
        -isLocalPlayer will only return true if it is called from a script that
            is attached to a game object that has a NetworkIdentity component 
            and Local Player Authority enabled
        -isLocalPlayer will always return false if it is called in the Awake() function*/
        if (isLocalPlayer)
        {
            Debug.Log("Pending moves: " + pendingMoves.Count);

            KeyCode[] possibleKeys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.W, KeyCode.Q, KeyCode.E, KeyCode.Space };

            bool somethingPressed = false;

            foreach (KeyCode possibleKey in possibleKeys)
            {
                if (!Input.GetKey(possibleKey))
                {
                    continue;
                }

                somethingPressed = true;
                pendingMoves.Enqueue(possibleKey);
                UpdatePredictedState();
                CmdMoveOnServer(possibleKey);
            }

            if (!somethingPressed)
            {
                pendingMoves.Enqueue(KeyCode.Alpha0);
                UpdatePredictedState();
                CmdMoveOnServer(KeyCode.Alpha0);
            }
        }

        SyncState();
    }

    /*
         A command will only run if isLocalPlayer would return true
         Any method that is tied to a Command MUST start with the Cmd prefix
    */
    [Command]
    void CmdMoveOnServer(KeyCode pressedKey)
    {
        serverState = Move(serverState, pressedKey);
    }
}
