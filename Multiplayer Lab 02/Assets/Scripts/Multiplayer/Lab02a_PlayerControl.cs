using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Lab02a_PlayerControl : NetworkBehaviour {

	struct PlayerState
    {
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ;
    }

    [SyncVar]
    PlayerState state;

    [Server]
    void InitState()
    {
        state = new PlayerState
        {
            posX = -119f,
            posY = 165.08f,
            posZ = -924f,
            rotX = 0f,
            rotY = 0f,
            rotZ = 0f
        };
    }


    //Takes the sync state and sets the local transform to it.
    void SyncState()
    {
        transform.position = new Vector3(state.posX, state.posY, state.posZ);
        transform.rotation = Quaternion.Euler(state.rotX, state.rotY, state.rotZ);
    }

    void Start()
    {
        InitState();
        SyncState();
    }

    PlayerState Move(PlayerState previous, KeyCode newKey)
    {
        float deltaX = 0, deltaY = 0, deltaZ = 0;
        float deltaRotationY = 0;

        switch (newKey)
        {
            case KeyCode.Q:
                deltaX = -0.5f;
                break;
            case KeyCode.S:
                deltaZ = -0.5f;
                break;
            case KeyCode.E:
                deltaX = 0.5f;
                break;
            case KeyCode.W:
                deltaZ = 0.5f;
                break;
            case KeyCode.A:
                deltaRotationY = -1f;
                break;
            case KeyCode.D:
                deltaRotationY = 1f;
                break;
        }        return new PlayerState
        {
            posX = deltaX + previous.posX,
            posY = deltaY + previous.posY,
            posZ = deltaZ + previous.posZ,
            rotX = previous.rotX,
            rotY = deltaRotationY + previous.rotY,
            rotZ = previous.rotZ
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
            KeyCode[] possibleKeys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.W, KeyCode.Q, KeyCode.E, KeyCode.Space };
            foreach(KeyCode possibleKey in possibleKeys)
            {
                if (!Input.GetKey(possibleKey))
                {
                    continue;
                }

                CmdMoveOnServer(possibleKey);
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
        state = Move(state, pressedKey);
    }
}
