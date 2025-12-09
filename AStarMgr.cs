using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// A星寻路管理器，单例模式
/// </summary>
public class AStarMgr
{
    private static AStarMgr instance;
    public static AStarMgr Instance
    {
        get
        {
            if (instance == null)
                instance = new AStarMgr();
            return instance;
        }
    }

    // 构造函数
    private AStarMgr()
    {
        openlist = new List<Astarnode>();
        closelist = new List<Astarnode>();
    }

    //地图的宽高
    private int mapW;
    private int mapH;

    //地图相关的所有格子对象容器
    private Astarnode[,] nodes;
    //开启列表
    private List<Astarnode> openlist;
    //关闭列表
    private List<Astarnode> closelist;

    /// <summary>
    /// 初始化地图信息
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    public void InitMapInfo(int w, int h)
    {
        //根据宽高，创建格子 阻挡的问题 我们可以随机阻挡
        //因为我们现在没有地图相关的数据

        this.mapW = w;
        this.mapH = h;

        //申明容器可以装多少个格子
        nodes = new Astarnode[w, h];
        //申明格子 装进去
        for (int i = 0; i < w; ++i)
        {
            for (int j = 0; j < h; ++j)
            {
                //这里的随机阻挡 只是为了讲逻辑所以这样写
                //以后真正的项目中 这些阻挡信息应该是从地图配置文件中读取出来的 不应该是随机的
                Astarnode node = new Astarnode(i, j, Random.Range(0, 100) < 20 ? E_Node_Type.stop : E_Node_Type.walk);
                nodes[i, j] = node;
            }
        }
    }

    /// <summary>
    /// 寻路方法 提供给外部使用
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    public List<Astarnode> FindPath(Vector2 startPos, Vector2 endPos)
    {
        //实际项目中 传入的点往往是 坐标系中的位置
        //我们这里省略换算的步骤 直接认为它是传进来的格子坐标

        //首先判断传入的两个点是否合法
        //如果不合法 直接返回null 意味着不能寻路
        //1.首先要在地图范围内
        if (startPos.x < 0 || startPos.x >= mapW ||
            startPos.y < 0 || startPos.y >= mapH ||
            endPos.x < 0 || endPos.x >= mapW ||
            endPos.y < 0 || endPos.y >= mapH)
        {
            Debug.Log("开始或者结束点在地图格子范围外");
            return null;
        }

        //2.要不是阻挡
        Astarnode start = nodes[(int)startPos.x, (int)startPos.y];
        Astarnode end = nodes[(int)endPos.x, (int)endPos.y]; // 这里应该是endPos，不是startPos

        if (start.type == E_Node_Type.stop || end.type == E_Node_Type.stop)
        {
            Debug.Log("开始或者结束点是阻挡");
            return null;
        }

        //清空关闭和开启列表
        closelist.Clear();
        openlist.Clear();

        //把开始点放入关闭列表中
        start.father = null;
        start.f = 0;
        start.g = 0;
        start.h = 0;
        closelist.Add(start);

        while (true)
        {
            //从起点开始 找周围的点 并放入列表中
            //左上  x - 1  y - 1
            FindNearlyNodeToOpenList(start.x - 1, start.y - 1, 1.4f, start, end);
            //上  x  y - 1
            FindNearlyNodeToOpenList(start.x, start.y - 1, 1, start, end);
            //右上  x + 1  y - 1
            FindNearlyNodeToOpenList(start.x + 1, start.y - 1, 1.4f, start, end);
            //左  x - 1  y
            FindNearlyNodeToOpenList(start.x - 1, start.y, 1, start, end);
            //右  x + 1  y
            FindNearlyNodeToOpenList(start.x + 1, start.y, 1, start, end);
            //左下  x - 1  y + 1
            FindNearlyNodeToOpenList(start.x - 1, start.y + 1, 1.4f, start, end);
            //下  x  y + 1
            FindNearlyNodeToOpenList(start.x, start.y + 1, 1, start, end);
            //右下  x + 1  y + 1
            FindNearlyNodeToOpenList(start.x + 1, start.y + 1, 1.4f, start, end);

            //判断这些点是否是边界 是否是阻挡 是否在开启或关闭列表中 如果都不是 才放入开启列表

            //思路判断 开启列表为空 都还没有找到终点 就认为是死路
            if (openlist.Count == 0)
            {
                Debug.Log("死路");
                return null;
            }

            //选出开启列表中 寻路消耗最小的点
            openlist.Sort(SortOpenList); // 或使用: openlist.Sort((a, b) => a.f.CompareTo(b.f));

            //放入关闭列表中 然后再从开启列表中移除
            closelist.Add(openlist[0]);
            //找得这个点 又变成新的起点 进行下一次寻路计算了
            start = openlist[0];
            openlist.RemoveAt(0);

            //如果这个点已经是终点了 那么得到最终结果返回出去
            //如果这个点不是终点 那么继续寻路
            if (start == end)
            {
                //找完了 找到路径了
                List<Astarnode> path = new List<Astarnode>();
                path.Add(end);
                while (end.father != null)
                {
                    path.Add(end.father);
                    end = end.father;
                }
                //列表反转的API
                path.Reverse();

                return path;
            }
        }
    }

    /// <summary>
    /// 排序函数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private int SortOpenList(Astarnode a, Astarnode b)
    {
        if (a.f > b.f)
            return 1;
        else if (a.f < b.f)
            return -1;
        else
            return 0;
    }

    private void FindNearlyNodeToOpenList(int x, int y, float g, Astarnode father, Astarnode end)
    {
        //边界判断
        if (x < 0 || x >= mapW ||
            y < 0 || y >= mapH)
            return;

        //在范围内再去取点
        Astarnode node = nodes[x, y];

        //判断这些点是否是边界 是否是阻挡 是否在开启或关闭列表中 如果都不是 才放入开启列表
        if (node == null ||
            node.type == E_Node_Type.stop ||
            closelist.Contains(node) ||
            openlist.Contains(node))
            return;

        //计算f值
        //f = g + h
        //记录父对象
        node.father = father;
        //计算g
        node.g = father.g + g;
        node.h = Mathf.Abs(end.x - node.x) + Mathf.Abs(end.y - node.y);
        node.f = node.g + node.h;

        //如果通过了上面的合法验证 就存到openlist中
        openlist.Add(node);
    }
}