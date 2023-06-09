using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Time")]
    [SerializeField] private float baseTime = 0.075f;
    [SerializeField] private float delayTime; // Read-only

    [Header("Entities")]
    [SerializeField] private bool isPlayerTurn = true; // Read-only
    [SerializeField] private int actorNum = 0; // Read-only
    [SerializeField] private List<Entity> entities;
    [SerializeField] private List<Actor> actors;

    [Header("Death")]
    [SerializeField] private Sprite deadSprite;

    public bool IsPlayerTurn => isPlayerTurn;
    public List<Entity> Entities => entities;
    public List<Actor> Actors => actors;
    public Sprite DeadSprite => deadSprite;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneState sceneState = SaveManager.instance.Save.Scenes.Find(x => x.FloorNumber == SaveManager.instance.CurrentFloor);

        if (sceneState != null)
        {
            LoadState(sceneState.GameState, true);
        }
        else
        {
            entities = new List<Entity>();
            actors = new List<Actor>();
        }
    }

    private void StartTurn()
    {
        if (actors[actorNum].GetComponent<Player>())
        {
            isPlayerTurn = true;

            // Set the player as the parent of the main camera
            Camera.main.transform.SetParent(actors[actorNum].transform, false);

            // Adjust the camera's position to center on the player while maintaining the z-axis position
            Vector3 cameraPosition = new Vector3(0f, 0f, -2f);
            Camera.main.transform.localPosition = cameraPosition;

            // Set the camera size to 10
            Camera.main.orthographicSize = 9f;
        }
        else
        {
            if (actors[actorNum].AI != null)
            {
                actors[actorNum].AI.RunAI();
            }
            else
            {
                Action.WaitAction();
            }
        }
    }

    public void EndTurn()
    {
        if (actors[actorNum].GetComponent<Player>())
        {
            isPlayerTurn = false;
        }

        if (actorNum == actors.Count - 1)
        {
            actorNum = 0;
        }
        else
        {
            actorNum++;
        }

        StartCoroutine(TurnDelay());
    }

    public IEnumerator TurnDelay()
    {
        yield return new WaitForSeconds(delayTime);
        StartTurn();
    }

    public void AddEntity(Entity entity)
    {
        if (!entity.gameObject.activeSelf)
        {
            entity.gameObject.SetActive(true);
        }
        entities.Add(entity);
    }

    public void InsertEntity(Entity entity, int index)
    {
        if (!entity.gameObject.activeSelf)
        {
            entity.gameObject.SetActive(true);
        }
        entities.Insert(index, entity);
    }

  public void RemoveEntity(Entity entity)
{
    if (entity != null && entity.gameObject != null)
    {
        entity.gameObject.SetActive(false);
        entities.Remove(entity);
    }
}

    public void AddActor(Actor actor)
    {
        actors.Add(actor);
        delayTime = SetTime();
    }

    public void InsertActor(Actor actor, int index)
    {
        actors.Insert(index, actor);
        delayTime = SetTime();
    }

    public void RemoveActor(Actor actor)
    {
        actors.Remove(actor);
        delayTime = SetTime();
    }

    public void RefreshPlayer()
    {
        actors[0].UpdateFieldOfView();
    }

    public Actor GetActorAtLocation(Vector3 location)
    {
        foreach (Actor actor in actors)
        {
            if (actor.BlocksMovement && actor.transform.position == location)
            {
                return actor;
            }
        }
        return null;
    }

    private float SetTime()
    {
        return baseTime / actors.Count;
    }

    public GameState SaveState()
    {
        foreach (Item item in actors[0].Inventory.Items)
        {
            AddEntity(item);
        }

        GameState gameState = new GameState(entities.ConvertAll(x => x.SaveState()));

        foreach (Item item in actors[0].Inventory.Items)
        {
            RemoveEntity(item);
        }

        return gameState;
    }

    public void LoadState(GameState state, bool canRemovePlayer)
    {
        isPlayerTurn = false; // Prevents player from moving during load
        Reset(canRemovePlayer);
        StartCoroutine(LoadEntityStates(state.Entities, canRemovePlayer));
    }

    private IEnumerator LoadEntityStates(List<EntityState> entityStates, bool canPlacePlayer)
    {
        int entityState = 0;
        while (entityState < entityStates.Count)
        {
            yield return new WaitForEndOfFrame();

            if (entityStates[entityState].Type == EntityState.EntityType.Actor)
            {
                ActorState actorState = entityStates[entityState] as ActorState;

                string entityName = entityStates[entityState].Name.Contains("Remains of")
                    ? entityStates[entityState].Name.Substring(entityStates[entityState].Name.LastIndexOf(' ') + 1)
                    : entityStates[entityState].Name;

                if (entityName == "Player" && !canPlacePlayer)
                {
                    actors[0].transform.position = entityStates[entityState].Position;
                    RefreshPlayer();
                    entityState++;
                    continue;
                }

                Actor actor = MapManager.instance.CreateEntity(entityName, actorState.Position).GetComponent<Actor>();

                actor.LoadState(actorState);
            }
            else if (entityStates[entityState].Type == EntityState.EntityType.Item)
            {
                ItemState itemState = entityStates[entityState] as ItemState;

                string entityName = entityStates[entityState].Name.Contains("(E)")
                    ? entityStates[entityState].Name.Replace(" (E)", "")
                    : entityStates[entityState].Name;

                if (itemState.Parent == "Player" && !canPlacePlayer)
                {
                    entityState++;
                    continue;
                }

                Item item = MapManager.instance.CreateEntity(entityName, itemState.Position).GetComponent<Item>();

                item.LoadState(itemState);
            }

            entityState++;
        }
        isPlayerTurn = true; // Allows player to move after load
    }

    public void Reset(bool canRemovePlayer)
    {
        if (entities.Count > 0)
        {
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                Entity entity = entities[i];
                if (!canRemovePlayer && entity.GetComponent<Player>())
                {
                    continue;
                }

                Destroy(entity.gameObject);
            }

            if (canRemovePlayer)
            {
                entities.Clear();
                actors.Clear();
            }
            else
            {
                entities.RemoveRange(1, entities.Count - 1);
                actors.RemoveRange(1, actors.Count - 1);
            }
        }
    }
}

[System.Serializable]
public class GameState
{
    [SerializeField] private List<EntityState> entities;

    public List<EntityState> Entities { get => entities; set => entities = value; }

    public GameState(List<EntityState> entities)
    {
        this.entities = entities;
    }
}
