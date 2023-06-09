using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Actor))]
sealed class Player : MonoBehaviour, Controls.IPlayerActions
{
      private Controls controls;
    private UIManager uiManager;
    private Vector3 startPosition;
    private bool gameSaved;
     

    [SerializeField] private bool moveKeyDown; //read-only
    [SerializeField] private bool targetMode; //read-only
    [SerializeField] private bool isSingleTarget; //read-only
    [SerializeField] private GameObject targetObject;
 private void Awake()
    {
        controls = new Controls();
        uiManager = UIManager.instance;
        startPosition = transform.position;
        gameSaved = false; // Set the initial value to false
    }
     public void Respawn()
    {
        // Reset player state
        GetComponent<Actor>().IsAlive = true;
        targetMode = false;
        moveKeyDown = false;

        // Reset target object
        ToggleTargetMode(false);
        targetObject.SetActive(false);

        // Reset player position
        transform.position = startPosition;

        // Additional reset logic if needed
        // ...
    }


    private void OnEnable()
    {
        controls.Player.SetCallbacks(this);
        controls.Player.Enable();
    }

    

    private void OnDisable()
    {
        controls.Player.SetCallbacks(null);
        controls.Player.Disable();
    }

    void Controls.IPlayerActions.OnMovement(InputAction.CallbackContext context)
    {
        if (context.started && GetComponent<Actor>().IsAlive)
        {
            if (!gameSaved) // Check if the game has been saved
            {
                SaveManager.instance.SaveGame(false); // Save the game with false parameter
                gameSaved = true; // Set the flag to true to indicate the game has been saved
            }

            if (targetMode && !moveKeyDown)
            {
                moveKeyDown = true;
                Move();
            }
            else if (!targetMode)
            {
                moveKeyDown = true;
            }
        }
        else if (context.canceled)
        {
            moveKeyDown = false;
        }
        else if (context.canceled)
        {
            moveKeyDown = false;
        }

        if (!GetComponent<Actor>().IsAlive && !uiManager.IsEscapeMenuOpen)
        {
            uiManager.ToggleEscapeMenu();
        }
    }

    void Controls.IPlayerActions.OnExit(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (targetMode)
            {
                ToggleTargetMode();
            }
            else if (!uiManager.IsEscapeMenuOpen && !uiManager.IsMenuOpen)
            {
                uiManager.ToggleEscapeMenu();
            }
            else if (uiManager.IsMenuOpen)
            {
                uiManager.ToggleMenu();
            }
        }
    }

    public void OnView(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!uiManager.IsMenuOpen || uiManager.IsMessageHistoryOpen)
            {
                uiManager.ToggleMessageHistory();
            }
        }
    }

    public void OnPickup(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (CanAct())
            {
                Action.PickupAction(GetComponent<Actor>());
            }
        }
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (CanAct() || uiManager.IsInventoryOpen)
            {
                if (GetComponent<Inventory>().Items.Count > 0)
                {
                    uiManager.ToggleInventory(GetComponent<Actor>());
                }
                else
                {
                    uiManager.AddMessage("You have no items.", "#808080");
                }
            }
        }
    }

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (CanAct() || uiManager.IsDropMenuOpen)
            {
                if (GetComponent<Inventory>().Items.Count > 0)
                {
                    uiManager.ToggleDropMenu(GetComponent<Actor>());
                }
                else
                {
                    uiManager.AddMessage("You have no items.", "#808080");
                }
            }
        }
    }

    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (targetMode)
            {
                if (isSingleTarget)
                {
                    Actor target = SingleTargetChecks(targetObject.transform.position);

                    if (target != null)
                    {
                        Action.CastAction(GetComponent<Actor>(), target, GetComponent<Inventory>().SelectedConsumable);
                    }
                }
                else
                {
                    List<Actor> targets = AreaTargetChecks(targetObject.transform.position);

                    if (targets != null)
                    {
                        Action.CastAction(GetComponent<Actor>(), targets, GetComponent<Inventory>().SelectedConsumable);
                    }
                }
            }
            else if (CanAct())
            {
                Action.TakeStairsAction(GetComponent<Actor>());
            }
        }

        if (!GetComponent<Actor>().IsAlive && !uiManager.IsEscapeMenuOpen)
        {
            uiManager.ToggleEscapeMenu();
        }
    }

    public void OnInfo(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (CanAct() || uiManager.IsCharacterInformationMenuOpen)
            {
                uiManager.ToggleCharacterInformationMenu(GetComponent<Actor>());
            }
        }
    }

    public void ToggleTargetMode(bool isArea = false, int radius = 1)
    {
        targetMode = !targetMode;

        if (targetMode)
        {
            if (targetObject.transform.position != transform.position)
            {
                targetObject.transform.position = transform.position;
            }

            if (isArea)
            {
                isSingleTarget = false;
                targetObject.transform.GetChild(0).localScale = Vector3.one * (radius + 1); //+1 to account for the center
                targetObject.transform.GetChild(0).gameObject.SetActive(true);
            }
            else
            {
                isSingleTarget = true;
            }

            targetObject.SetActive(true);
        }
        else
        {
            if (targetObject.transform.GetChild(0).gameObject.activeSelf)
            {
                targetObject.transform.GetChild(0).gameObject.SetActive(false);
            }
            targetObject.SetActive(false);
            GetComponent<Inventory>().SelectedConsumable = null;
        }
    }

    private void FixedUpdate()
    {
        if (!uiManager.IsMenuOpen && !targetMode)
        {
            if (GameManager.instance.IsPlayerTurn && moveKeyDown && GetComponent<Actor>().IsAlive)
            {
                Move();
            }
        }
    }

    private void Move()
    {
        Vector2 direction = controls.Player.Movement.ReadValue<Vector2>();
        Vector2 roundedDirection = new Vector2(Mathf.Round(direction.x), Mathf.Round(direction.y));
        Vector3 futurePosition;

        if (targetMode)
        {
            futurePosition = targetObject.transform.position + (Vector3)roundedDirection;
        }
        else
        {
            futurePosition = transform.position + (Vector3)roundedDirection;
        }

        if (targetMode)
        {
            Vector3Int targetGridPosition = MapManager.instance.FloorMap.WorldToCell(futurePosition);

            if (MapManager.instance.IsValidPosition(futurePosition) && GetComponent<Actor>().FieldOfView.Contains(targetGridPosition))
            {
                targetObject.transform.position = futurePosition;
            }
        }
        else
        {
            moveKeyDown = Action.BumpAction(GetComponent<Actor>(), roundedDirection);

            // Flip sprite based on movement direction
            if (roundedDirection != Vector2.zero)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                spriteRenderer.flipX = roundedDirection.x < 0; // Flip horizontally if moving left
            }
        }
    }

    private bool CanAct()
    {
        if (targetMode || uiManager.IsMenuOpen || !GetComponent<Actor>().IsAlive)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private Actor SingleTargetChecks(Vector3 targetPosition)
    {
        Actor target = GameManager.instance.GetActorAtLocation(targetPosition);

        if (target == null)
        {
            uiManager.AddMessage("You must select an enemy to target.", "#FFFFFF");
            return null;
        }

        if (target == GetComponent<Actor>())
        {
            uiManager.AddMessage("You can't target yourself!", "#FFFFFF");
            return null;
        }

        return target;
    }

    private List<Actor> AreaTargetChecks(Vector3 targetPosition)
    {
        //Take away 1 to account for the center
        int radius = (int)targetObject.transform.GetChild(0).localScale.x - 1;

        Bounds targetBounds = new Bounds(targetPosition, Vector3.one * radius * 2);
        List<Actor> targets = new List<Actor>();

        foreach (Actor target in GameManager.instance.Actors)
        {
            if (targetBounds.Contains(target.transform.position))
            {
                targets.Add(target);
            }
        }

        if (targets.Count == 0)
        {
            uiManager.AddMessage("There are no targets in the radius.", "#FFFFFF");
            return null;
        }

        return targets;
    }
}

