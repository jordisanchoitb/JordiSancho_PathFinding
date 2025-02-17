using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Static Variables
    public static GameManager Instance;
    #endregion

    #region Constants
    private const float TIMEWAIT = 3f;
    #endregion

    #region Variables
    public int Size;
    public BoxCollider2D Panel;
    public GameObject token;
    public GameObject prefabPlayer;
    public GameObject prefabFinal;
    #endregion

    #region Private Variables
    //private int[,] GameMatrix; //0 not chosen, 1 player, 2 enemy de momento no hago nada con esto
    private GameObject player;
    private Node[,] NodeMatrix;
    private GameObject[,] TokenMatrix;
    private int startPosx, startPosy;
    private int endPosx, endPosy;
    private List<Node> OpenList;
    private List<Node> ClosedList;
    private List<Node> FinalPath;
    private Node CurrentNode;
    private Node StartNode;
    private Node EndNode;
    private Coroutine coroutine;
    #endregion

    void Awake()
    {
        Instance = this;
        //GameMatrix = new int[Size, Size];
        Calculs.CalculateDistances(Panel, Size);
    }
    private void Start()
    {
        /*for(int i = 0; i<Size; i++)
        {
            for (int j = 0; j< Size; j++)
            {
                GameMatrix[i, j] = 0;
            }
        }*/
        

        startPosx = UnityEngine.Random.Range(0, Size);
        startPosy = UnityEngine.Random.Range(0, Size);
        do
        {
            endPosx = UnityEngine.Random.Range(0, Size);
            endPosy = UnityEngine.Random.Range(0, Size);
        } while(endPosx== startPosx || endPosy== startPosy);

        /*GameMatrix[startPosx, startPosy] = 2;
        GameMatrix[startPosx, startPosy] = 1;*/
        NodeMatrix = new Node[Size, Size];
        TokenMatrix = new GameObject[Size, Size];

        CreateNodes();

        Instantiate(prefabPlayer, Calculs.CalculatePoint(startPosx, startPosy), Quaternion.identity);
        Instantiate(prefabFinal, Calculs.CalculatePoint(endPosx, endPosy), Quaternion.identity);

        player = GameObject.Find("Player(Clone)");

        StartNode = NodeMatrix[startPosx,startPosy];
        CurrentNode = StartNode;

        EndNode = NodeMatrix[endPosx, endPosy];

        List<Node> path = CalculateWayAStar(StartNode, EndNode);

        foreach (var node in path)
        {
            Debug.Log("FinalPath (" + node.PositionX + ", " + node.PositionY + ")");
        }

        coroutine = StartCoroutine(MovePlayer());
    }

    public List<Node> CalculateWayAStar(Node start, Node end)
    {
        OpenList = new List<Node>();
        ClosedList = new List<Node>();
        FinalPath = new List<Node>();
        OpenList.Add(start);
        while (OpenList.Count > 0)
        {
            OpenList.Sort((x, y) => (x.WayList[0].ACUMulatedCost + x.Heuristic).CompareTo(y.WayList[0].ACUMulatedCost + y.Heuristic));
            CurrentNode = OpenList[0];
            /*for (int i = 1; i < OpenList.Count; i++)
            {
                if (OpenList[i].Heuristic + OpenList[i].WayList[0].ACUMulatedCost < CurrentNode.Heuristic + CurrentNode.WayList[0].ACUMulatedCost)
                {
                    CurrentNode = OpenList[i];
                }
            }*/
            OpenList.Remove(CurrentNode);
            ClosedList.Add(CurrentNode);
            if (CurrentNode == end)
            {
                Node temp = CurrentNode;
                while (temp != start)
                {
                    FinalPath.Add(temp);
                    temp = temp.NodeParent;
                }
                FinalPath.Add(start);
                FinalPath.Reverse();
                return FinalPath;
            }

            foreach (var way in CurrentNode.WayList)
            {
                if (ClosedList.Contains(way.NodeDestiny))
                {
                    continue;
                }
                if (!OpenList.Contains(way.NodeDestiny))
                {
                    way.NodeDestiny.NodeParent = CurrentNode;
                    way.ACUMulatedCost = way.Cost + CurrentNode.WayList[0].ACUMulatedCost;
                    OpenList.Add(way.NodeDestiny);
                }
                else
                {
                    if (way.Cost + CurrentNode.WayList[0].ACUMulatedCost < way.NodeDestiny.WayList[0].ACUMulatedCost)
                    {
                        way.NodeDestiny.NodeParent = CurrentNode;
                        way.NodeDestiny.WayList[0].ACUMulatedCost = way.Cost + CurrentNode.WayList[0].ACUMulatedCost;
                    }
                }
            }
        }
        return null;
    }

    public IEnumerator MovePlayer()
    {
        OpenList.Reverse();
        yield return new WaitForSeconds(TIMEWAIT);
        Debug.Log("Start Closed List");
        foreach (var node in OpenList)
        {
            player.transform.position = node.RealPosition;
            TokenMatrix[node.PositionX, node.PositionY].GetComponent<SpriteRenderer>().color = Color.yellow;
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(TIMEWAIT);
        foreach (var node in FinalPath)
        {
            player.transform.position = node.RealPosition;
            TokenMatrix[node.PositionX, node.PositionY].GetComponent<SpriteRenderer>().color = Color.green;
            yield return new WaitForSeconds(1f);
        }
        yield return null;
        StopCoroutine(coroutine);
    }

    public void CreateNodes()
    {
        for(int i=0; i<Size; i++)
        {
            for(int j=0; j<Size; j++)
            {
                NodeMatrix[i, j] = new Node(i, j, Calculs.CalculatePoint(i,j));
                NodeMatrix[i,j].Heuristic = Calculs.CalculateHeuristic(NodeMatrix[i,j],endPosx,endPosy);
            }
        }
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                SetWays(NodeMatrix[i, j], i, j);
            }
        }
        DebugMatrix();
    }
    public void DebugMatrix()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                GameObject gO = Instantiate(token, NodeMatrix[i, j].RealPosition, Quaternion.identity);
                TokenMatrix[i, j] = gO;
                Debug.Log("Element (" + j + ", " + i + ")");
                Debug.Log("Position " + NodeMatrix[i, j].RealPosition);
                Debug.Log("Heuristic " + NodeMatrix[i, j].Heuristic);
                Debug.Log("Ways: ");
                foreach (var way in NodeMatrix[i, j].WayList)
                {
                    Debug.Log(" (" + way.NodeDestiny.PositionX + ", " + way.NodeDestiny.PositionY + ")");
                }
            }
        }
    }
    public void SetWays(Node node, int x, int y)
    {
        node.WayList = new List<Way>();
        if (x>0)
        {
            node.WayList.Add(new Way(NodeMatrix[x - 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if(x<Size-1)
        {
            node.WayList.Add(new Way(NodeMatrix[x + 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if(y>0)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y - 1], Calculs.LinearDistance));
        }
        if (y<Size-1)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y + 1], Calculs.LinearDistance));
            if (x>0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y + 1], Calculs.DiagonalDistance));
            }
            if (x<Size-1)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y + 1], Calculs.DiagonalDistance));
            }
        }
    }

}
