using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GameGraphic : MonoBehaviour
{
    public int selectedBottleIndex = -1;

    private GamePlayController game;

    public List<BottleGraphic> bottleGraphics;

    public BallGraphic prefabBallGraphic;

    public BallGraphic previewBall;

    public BottleGraphic prefabBottleGraphic;

    public Vector3 bottleStartPostion;
    
    public Vector3 bottleDistance;
    

    private void Start()
    {
        game = FindObjectOfType<GamePlayController>();
        selectedBottleIndex = -1;

        previewBall = Instantiate(prefabBallGraphic);
    }

    public void CreateBottleGraphic(List<GamePlayController.Bottle> bottles)
    {
        foreach (GamePlayController.Bottle b in bottles)
        {
            BottleGraphic bg = Instantiate(prefabBottleGraphic);
            
            bottleGraphics.Add(bg);

            List<int> ballTypes = new List<int>();

            foreach (var ball in b.balls)
            {
                ballTypes.Add(ball.type);
            }
            
            bg.SetGraphic(ballTypes.ToArray());
        }

        Vector3 pos = bottleStartPostion;

        for (int i = 0; i < bottleGraphics.Count; i++)
        {
            bottleGraphics[i].transform.position = pos;

            pos.x += bottleDistance.x;

            bottleGraphics[i].index = i;
        }
    }

    public void RefreshBottleGraphics(List<GamePlayController.Bottle> bottles)
    {
        for (int i = 0; i < bottles.Count; i++)
        {
            GamePlayController.Bottle gb = bottles[i];
            BottleGraphic bottleGraphic = bottleGraphics[i];

            List<int> ballTypes = new List<int>();

            foreach (var ball in gb.balls)
            {
                ballTypes.Add(ball.type);
            }

            bottleGraphic.SetGraphic(ballTypes.ToArray());
        }
    }

    public void OnClickBottle(int bottleIndex)
    {
        Debug.Log("Click bottle index: " + bottleIndex);
        if(isSwitchingBall) return;
        // trang thai mac dinh: -1
        // trang thai co ball: bottleIndex
        if (selectedBottleIndex == -1)
        {
            if(game.bottles[bottleIndex].balls.Count != 0)
            {
                selectedBottleIndex = bottleIndex;
                StartCoroutine(MoveBallUp(bottleIndex));
            }
        }
        // nếu k phải = -1 thì mình đã ấn vào 1 bottle
        // lúc này nếu ấn vòa 1 bottle khác có index
        // thì sẽ tiến hành switch ball
        else
        {
            if (selectedBottleIndex == bottleIndex)
            {
                StartCoroutine(MoveBallDown(bottleIndex));
                selectedBottleIndex = -1;
            }
            else
            {
                StartCoroutine(SwitchBallCoroutine(selectedBottleIndex, bottleIndex));
                // if (game.CheckWinCondition())
                // {
                //     Debug.Log("WIN");
                // }
            }
            // game.SwitchBall(selectedBottleIndex, bottleIndex);
            // selectedBottleIndex = -1;
            //
            // if (game.CheckWinCondition())
            // {
            //     Debug.Log("WIN");
            // }
        }
    }

    private IEnumerator MoveBallUp(int bottleIndex)
    {
        isSwitchingBall = true;
        
        Vector3 upPosition = bottleGraphics[bottleIndex].GetBottleUpPosition();

        List<GamePlayController.Ball> ballList = game.bottles[bottleIndex].balls;
        
        //lay ball tren cung
        GamePlayController.Ball b = ballList[ballList.Count - 1];

        Vector3 ballPosition = bottleGraphics[bottleIndex].GetBallPosition(ballList.Count - 1);

        bottleGraphics[bottleIndex].SetGraphicNone(ballList.Count - 1);
        
        previewBall.SetColor(b.type);
        
        previewBall.transform.position = ballPosition;
        
        previewBall.gameObject.SetActive(true);

        while (Vector3.Distance(previewBall.transform.position,upPosition) > 0.005f)
        {
            previewBall.transform.position = Vector3.MoveTowards(previewBall.transform.position,
                upPosition, 15 * Time.deltaTime);
            yield return null;
        }

        isSwitchingBall = false;
    }

    private IEnumerator MoveBallDown(int bottleIndex)
    {
        isSwitchingBall = true;
        
        List<GamePlayController.Ball> ballList = game.bottles[bottleIndex].balls;

        Vector3 downPostion = bottleGraphics[bottleIndex].GetBallPosition(ballList.Count - 1);

        Vector3 ballPosition = bottleGraphics[bottleIndex].GetBottleUpPosition();

        previewBall.transform.position = ballPosition;
        
        while (Vector3.Distance(previewBall.transform.position,downPostion) > 0.005f)
        {
            previewBall.transform.position = Vector3.MoveTowards(previewBall.transform.position,
                downPostion, 15 * Time.deltaTime);
            yield return null;
        }
        
        previewBall.gameObject.SetActive(false);

        GamePlayController.Ball b = ballList[ballList.Count - 1];

        bottleGraphics[bottleIndex].SetGraphic(ballList.Count - 1, b.type);
        
        isSwitchingBall = false;
    }

    private bool isSwitchingBall = false;

    private IEnumerator SwitchBallCoroutine(int fromBottleIndex, int toBottleIndex)
    {
        isSwitchingBall = true;
        List<GamePlayController.SwitchBallCommand> commands =
            game.CheckSwitchBall(fromBottleIndex, toBottleIndex);

        if (commands.Count == 0)
        {
            Debug.Log("Cant move");
        }
        else
        {
            pendingBalls = commands.Count;
            
            previewBall.gameObject.SetActive(false);
            
            for (int i = 0; i < commands.Count; i++)
            {
                GamePlayController.SwitchBallCommand command = commands[i];
                Queue<Vector3> moveQueue = GetCommandPath(command);

                if (i == 0)
                {
                    moveQueue.Dequeue();
                }

                StartCoroutine(SwitchBall(command, moveQueue));
                yield return new WaitForSeconds(0.06f);
            }

            while (pendingBalls > 0)
            {
                yield return null;
            }

            Debug.Log("Moving Completed!");
            game.SwitchBall(fromBottleIndex, toBottleIndex);
        }
        
        selectedBottleIndex = -1;
        isSwitchingBall = false;
    }

    private int pendingBalls = 0;

    private Queue<Vector3> GetCommandPath(GamePlayController.SwitchBallCommand command)
    {
        Queue<Vector3> queueMovement = new Queue<Vector3>();
        
        queueMovement.Enqueue(bottleGraphics[command.fromBottleIndex].GetBallPosition(command.fromBallIndex));
        queueMovement.Enqueue(bottleGraphics[command.fromBottleIndex].GetBottleUpPosition());
        queueMovement.Enqueue(bottleGraphics[command.toBottleIndex].GetBottleUpPosition());
        queueMovement.Enqueue(bottleGraphics[command.toBottleIndex].GetBallPosition(command.toBallIndex));

        return queueMovement;
    }

    private IEnumerator SwitchBall(GamePlayController.SwitchBallCommand command, Queue<Vector3> movement)
    {
        // tat graphic o vi tri from
        // tao 1 ball o vi tri from, co cung type
        // di chuyen ball theo dung duong
        // xoa ball di chuyen, bat graphic o vi tri TO
        bottleGraphics[command.fromBottleIndex].SetGraphicNone(command.fromBallIndex);

        Vector3 spawnPostion = movement.Peek();
        
        var ballObject = Instantiate(prefabBallGraphic, spawnPostion, Quaternion.identity);
        
        ballObject.SetColor(command.type);
        
        // Queue<Vector3> queueMovement = new Queue<Vector3>();
        //
        // queueMovement.Enqueue(bottleGraphics[command.fromBottleIndex].GetBallPosition(command.fromBallIndex));
        // queueMovement.Enqueue(bottleGraphics[command.fromBottleIndex].GetBottleUpPosition());
        // queueMovement.Enqueue(bottleGraphics[command.toBottleIndex].GetBottleUpPosition());
        // queueMovement.Enqueue(bottleGraphics[command.toBottleIndex].GetBallPosition(command.toBallIndex));

        while (movement.Count > 0)
        {
            Vector3 target = movement.Dequeue();

            while (Vector3.Distance(ballObject.transform.position,target) > 0.005f)
            {
                ballObject.transform.position = Vector3.MoveTowards(ballObject.transform.position,
                    target,6 * Time.deltaTime);
                yield return null;
            }
        }
        yield return null;
        
        Destroy(ballObject.gameObject);

        bottleGraphics[command.toBottleIndex].SetGraphic(command.toBallIndex, command.type);
        pendingBalls--;
    }
}