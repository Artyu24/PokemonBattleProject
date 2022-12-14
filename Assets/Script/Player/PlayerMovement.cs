using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    [Header("Different Area")] 
    [SerializeField] private GameObject waterPokemon;
    [SerializeField] private GameObject mapIcone;
    private bool insideBuilding = false;
    private bool inGrass = false;
    public bool InGrass { get => inGrass; set => inGrass = value; }
    private bool fightDresseur = false;
    private HerbesHautes herbesHautes;
    public HerbesHautes Herbeshautes { get => herbesHautes; set => herbesHautes = value; }
    private Dresseur dresseur;
    private bool canWalkOnWater;
    private bool walkOnWater = false;
    public GameObject WaterPokemon => waterPokemon;
    public bool CanWalkOnWater { get => canWalkOnWater; set => canWalkOnWater = value; }
    public bool WalkOnWater { get => walkOnWater; set => walkOnWater = value; }

    [Header("Teleportation")]
    private bool isTP = false;
    private Collider2D actualDoor;
    private PotentialDirection lastDirEnum = PotentialDirection.DOWN;
    public PotentialDirection LastDirEnum => lastDirEnum;

    [Header("Movement")]
    public int moveSpeed = 20;
    private Vector3 endPos;
    private Vector2 inputDir;
    private BoxCenter boxCenter;

    [Header("Interaction")]
    public LayerMask wallLayer;
    public LayerMask waterLayer;
    public LayerMask interactLayer;
    public float raycastDistance = 2;

    public delegate void InteractionDelegate();
    private InteractionDelegate actualInteractionDelegate = null;
    public InteractionDelegate ActualInteractionDelegate { get => actualInteractionDelegate; set => actualInteractionDelegate = value; }

    [Header("Animation")]
    public Animator anim;
    private Animator animPokeWater;
    private PotentialDirection lastAnim = PotentialDirection.IDLE;
    private PlayerSmokeStep smokeStep;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        smokeStep = GetComponent<PlayerSmokeStep>();
    }

    private void Start()
    {
        boxCenter = GetComponent<BoxCenter>();
        if(waterPokemon != null)
            animPokeWater = waterPokemon.GetComponent<Animator>();

        if (SaveSystemManager.Instance != null)
        {
            if (SaveSystemManager.Instance.LastPosPlayer != Vector3.zero)
            {
                transform.position = SaveSystemManager.Instance.LastPosPlayer;
            }
        }
        endPos = boxCenter.CenterObject();

        actualInteractionDelegate = LaunchInteraction;
    }


    private void Update()
    {
        #region MOVEMENT

        #region Input

        if (GameManager.Instance.ActualGameState == GameState.Adventure && GameManager.Instance.ActualPlayerState == PlayerState.Idle)
        {
            MakeAMove();
        }

        #endregion

        #region End Movement

        if (transform.position == endPos && GameManager.Instance.ActualGameState == GameState.Adventure && (GameManager.Instance.ActualPlayerState == PlayerState.InMovement || GameManager.Instance.ActualPlayerState == PlayerState.ForcedMove))
        {
            endPos = boxCenter.CenterObject();
            transform.position = endPos;

            if(GameManager.Instance.ActualPlayerState != PlayerState.ForcedMove)
                GameManager.Instance.ActualPlayerState = PlayerState.Idle;

            isTP = false;
            if (herbesHautes != null)
            {
                //Debug.Log("Appel combat");
                herbesHautes.SpawnPokemon();
            }
            if (dresseur != null)
            {
                Debug.Log("Appel combat");
                dresseur.SpawnPokemon();
            }
        }

        #endregion

        #region In Movement

        if (GameManager.Instance.ActualGameState != GameState.Paused && (GameManager.Instance.ActualPlayerState == PlayerState.InMovement || GameManager.Instance.ActualPlayerState == PlayerState.ForcedMove))
        {
            transform.position = Vector3.MoveTowards(transform.position, endPos, moveSpeed * Time.deltaTime);
            if(waterPokemon != null)
                if(waterPokemon.activeInHierarchy && walkOnWater)
                    waterPokemon.transform.position = transform.position;

            if(mapIcone != null)
                if(!insideBuilding)
                    mapIcone.transform.position = transform.position;
        }

        #endregion

        #endregion
    }

    #region Done a Mouvement

    public void MakeAMove(PotentialDirection enumForced = PotentialDirection.NOTHING)
    {
        PotentialDirection dirEnum = PotentialDirection.IDLE;

        if (enumForced == PotentialDirection.NOTHING)
        {
            if (inputDir.y > 0)
                dirEnum = PotentialDirection.UP;
            else if (inputDir.y < 0)
                dirEnum = PotentialDirection.DOWN;
            else if (inputDir.x < 0)
                dirEnum = PotentialDirection.LEFT;
            else if (inputDir.x > 0)
                dirEnum = PotentialDirection.RIGHT;
        }
        else
            dirEnum = enumForced;

        DirectionData dirChoose = GameManager.Instance.DictDirection[dirEnum];
        AnimPlayer(dirChoose.dirEnum, dirChoose.animName);

        if (dirEnum != PotentialDirection.IDLE)
        {
            lastDirEnum = dirEnum;

            if (!Check(dirChoose.transformDir))
            {
                endPos = transform.position + dirChoose.mouv;

                if (enumForced == PotentialDirection.NOTHING)
                    GameManager.Instance.ActualPlayerState = PlayerState.InMovement;
            }
            else
            {
                //BONK
                if (FindObjectOfType<AudioManager>() != null)
                {
                    FindObjectOfType<AudioManager>().Play("BlockSound");
                }
            }
        }
    }

    #endregion

    #region Input Action
    public void OnMove(InputAction.CallbackContext ctx)
    {
        inputDir = ctx.ReadValue<Vector2>();        
        if (smokeStep != null)
            smokeStep.UpdateWalkingState(ctx.performed);

        if (ctx.started)
        {
            if (GameManager.Instance.ActualPlayerState == PlayerState.WaterInteraction && GameManager.Instance.ActualGameState == GameState.Adventure)
            {
                if(Math.Abs(inputDir.y) > 0)
                    WaterZone.Instance.SwitchText();
            }            

            if (GameManager.Instance.ActualPlayerState == PlayerState.Inventory && GameManager.Instance.ActualGameState == GameState.Adventure)
            {
                if (Math.Abs(inputDir.y) > 0)
                    PauseMenu.Instance.SwitchPauseSelection(inputDir.y);
            }
        }
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (actualInteractionDelegate != null)
            {
                DirectionData dirChoose = GameManager.Instance.DictDirection[PotentialDirection.IDLE];
                AnimPlayer(dirChoose.dirEnum, dirChoose.animName);

                actualInteractionDelegate();
            }
        }
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (GameManager.Instance.ActualPlayerState == PlayerState.Idle && GameManager.Instance.ActualGameState == GameState.Adventure)
            {
                PauseMenu.Instance.OpenPauseMenu();
            }
        }
    }

    #endregion

    #region Animation

    private void AnimPlayer(PotentialDirection actualDir, string animTrigger)
    {
        if (lastAnim != actualDir)
        {
            anim.SetTrigger(animTrigger);
            if (waterPokemon != null)
                if (waterPokemon.activeInHierarchy && animTrigger != "Idl" && walkOnWater)
                    animPokeWater.SetTrigger(animTrigger);
        }
        lastAnim = actualDir;
    }

    #endregion

    #region Check Collision

    private bool Check(Vector3 direction)
    {
        Debug.DrawRay(transform.position, direction * raycastDistance, Color.blue, 2f);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastDistance, wallLayer);
        if (hit.collider != null)
        {
            //if(FindObjectOfType<AudioManager>() != null)
            //    FindObjectOfType<AudioManager>().Play("BlockSound");
            
            return true;
        }

        RaycastHit2D hitWater = Physics2D.Raycast(transform.position, direction, raycastDistance, waterLayer);
        if (hitWater.collider != null)
        {
            if (walkOnWater)
            {
                WaterZone.Instance.ActivateExitAnimation();      
            }
            return true;
        }
        return false;
    }

    #endregion

    #region Interaction

    public void ResetInteractionFunction()
    {
        actualInteractionDelegate = LaunchInteraction;
    }

    private void LaunchInteraction()
    {
        if (GameManager.Instance.ActualPlayerState == PlayerState.Idle)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, GameManager.Instance.DictDirection[lastDirEnum].transformDir, raycastDistance, interactLayer);
            Debug.DrawRay(transform.position, GameManager.Instance.DictDirection[lastDirEnum].transformDir * raycastDistance, Color.red, 2f);

            if (hit.collider != null)
            {
                if (hit.collider.GetComponent<IInteractable>() != null)
                {
                    if (walkOnWater && hit.collider.gameObject.layer == 4)
                        return;

                    hit.collider.GetComponent<IInteractable>().Interact();

                    if (FindObjectOfType<AudioManager>() != null)
                    {
                        FindObjectOfType<AudioManager>().Play("SFXMenuClick");
                    }
                }
            }
        }
    }

    #endregion

    #region Enter in Door or Grass (Trigger)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("Door") && !isTP)
        {
            isTP = true;
            actualDoor = collision;

            insideBuilding = !insideBuilding;

            GameManager.Instance.ActualPlayerState = PlayerState.Teleportation;
            GameManager.Instance.ActivateFade(1);
            
            anim.SetTrigger("Idl");
            lastAnim = PotentialDirection.IDLE;

            if (FindObjectOfType<AudioManager>() != null)
                FindObjectOfType<AudioManager>().Play("DoorSound");
            
            StartCoroutine(WaitTP());
        }

        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("Grass"))
        {
            if (!inGrass)
            {
                inGrass = true;
                herbesHautes = collision.GetComponent<HerbesHautes>();
            }
            else
            {
                inGrass = false;
                herbesHautes = null;
            }
        }
        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("Dresseur"))
        {
            if (!fightDresseur)
            {
                fightDresseur = true;
                dresseur = collision.GetComponent<Dresseur>();
            }
            else
            {
                fightDresseur = false;
                dresseur = null;
            }
        }
    }

    #endregion

    #region Teleportation in Building

    private IEnumerator WaitTP()
    {
        yield return new WaitForSeconds(1);
        TeleportPlayer();
    }

    private void TeleportPlayer()
    {
        transform.position = TP_Manager.Instance.DictHouseDoor[actualDoor].transform.position;
        endPos = GetComponent<BoxCenter>().CenterObject() + GameManager.Instance.DictDirection[lastDirEnum].mouv;
        transform.position = GetComponent<BoxCenter>().CenterObject();
        GameManager.Instance.ActivateFade(2);
        GameManager.Instance.ActualPlayerState = PlayerState.InMovement;
    }

    #endregion

    #region Water Pokemon

    public void InitWaterPokemon()
    {
        DirectionData lastDirData = GetLastDirection();
        waterPokemon.transform.position = transform.position + lastDirData.mouv;
        waterPokemon.SetActive(true);
        animPokeWater.SetTrigger(lastDirData.animName);
    }

    #endregion
    
    public DirectionData GetLastDirection()
    {
        return GameManager.Instance.DictDirection[lastDirEnum];
    }
}