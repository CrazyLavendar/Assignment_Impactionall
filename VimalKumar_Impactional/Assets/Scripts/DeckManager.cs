using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DeckManager : MonoBehaviour
{
    // Start is called before the first frame update

    [Header("Scene Dependencies")]
    [SerializeField] private NetworkManager networkManager;

    [Header("Buttons")]
    [SerializeField] private Button pickButton;
    [SerializeField] private Button restartButton;

    [Header("Texts")]
    [SerializeField] private Text resultText;
    [SerializeField] public Text connectionStatusText;

    [Header("Screen Gameobjects")]
    [SerializeField] private GameObject GameOverScreen;
    [SerializeField] private GameObject pickControl;


    public PhotonView photonView;
    public float x, y, z;
    public bool cardBackActive;
    public static bool allPlayerJoined;
    public int timer, currentVal ;
    public GameObject[] cards;
    private GameObject pickedCard;
 
    


    private void Awake()
    {
       
        OnGameLaunched();
    }
    public void OnGameLaunched()
    {
        allPlayerJoined = false;
        currentVal = -1;
        DisableAllScreens();
        OnConnect();
    }

    public void DisableAllScreens()
    {
        GameOverScreen.SetActive(false);
        pickControl.SetActive(false);
    }


    public void OnConnect()
    {
        networkManager.Connect();


    }


    public void afterMaxPlayersJoined()
    {
            pickControl.SetActive(true);
            allPlayerJoined = true;
            photonView.RPC("remoteCallMethod_PickActive", RpcTarget.Others);
    }

    [PunRPC]
    public void remoteCallMethod_PickActive()
    {
        pickControl.SetActive(true);
        allPlayerJoined = true;
    }

    public void pickCard()
    {

        //cardBack.SetActive(false);
        int rand = (int)Mathf.Ceil(Random.Range(0.1f, 10.0f));
        //Debug.Log(rand)
        foreach(GameObject card in cards)
        {
           card.SetActive(false);
        }
        cards[rand-1].SetActive(true);
        pickedCard = cards[rand - 1];
        pickedCard.SetActive(true);
        //Debug.Log(rand + " " + pickedCard.name );
        currentVal = rand - 1;
        photonView.RPC("remoteCallMethod", RpcTarget.Others, rand-1);
        
        StartFlip();

        
    }

    [PunRPC]
    void remoteCallMethod(int val)
    {
        if (currentVal == -1)
        {
            currentVal = val;
            Debug.Log("Remote val" + currentVal);
        }
        else
        {
            //Player 2 does
            GameOver(val);
            
        }
    }


    public void restart()
    {
        photonView.RPC("remoteRestart", RpcTarget.All);
        
    }

    [PunRPC]
    void remoteRestart()
    {
        pickedCard.transform.Rotate(new Vector3(0, 180, 0));
        OnGameLaunched();
        pickControl.SetActive(true);
    }

    void GameOver(int val)
    {
        GameOverScreen.SetActive(true);

        if (currentVal > val)
        {
            resultText.text = "You Won";
            photonView.RPC("remoteGameOver", RpcTarget.Others, 1);
        }
        else if (currentVal == val)
        {
            resultText.text = "Game Tied";
            photonView.RPC("remoteGameOver", RpcTarget.Others, 0);
        }
        else
        {
            resultText.text = "You Lost";
            photonView.RPC("remoteGameOver", RpcTarget.Others, -1);
        }
        
    }
    [PunRPC]
    void remoteGameOver(int score)
    {
        Debug.Log("Score " + score);
        GameOverScreen.SetActive(true);
        if(score == -1)
        {
            resultText.text = "You Won";
        }else if(score == 0)
        {
            resultText.text = "Game Tied";
        }
        else
        {
            resultText.text = "You Lost";
        }
        
    }



    public void StartFlip()
    {
        StartCoroutine(CalculateFlip());
    }
    public void Flip()
    {
        

        if (cardBackActive == true)
        {
            pickedCard.SetActive(false);
            cardBackActive = false;
        }
        else
        {
            pickedCard.SetActive(true);
            cardBackActive = true;
        }
    }

    IEnumerator CalculateFlip()
    {
        

        for (int i = 0; i < 180; i++)
        {
            yield return new WaitForSeconds(0.003f);
            pickedCard.transform.Rotate(new Vector3(x, y, z));
            timer++;
        }
        timer = 0;
    }

    public void SetConnectionStatusText(string status)
    {
        connectionStatusText.text = status;
    }

}
