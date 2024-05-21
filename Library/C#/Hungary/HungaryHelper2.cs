using com.hgfly.uavutils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class HungaryHelper2
{
    private class SortedArray
    {
        public int nowIndex;
        public int[] array;
    }

    private int nrow;
    private int ncol;

    private int path_row_0;
    private int path_col_0;
    private int path_count;

    private byte[] rowCover;
    private byte[] colCover;

    private float[,] mat;
    private int[,] sortTable;
    private byte[,] mark;
    private int[,] path;
    private int[] result;

    private SortedArray[] saryList;
    private int[] minIndexList;

    public int[] Hungary(int[,] start, int[,] goal)
    {
        nrow = start.GetLength(0);
        ncol = goal.GetLength(0);

        var mat = new float[nrow, ncol];
        for (int i = 0; i < nrow; i++)
        {
            for (int j = 0; j < ncol; j++)
            {
                mat[i, j] = Mathf.Pow(start[i, 0] - goal[j, 0], 2)
                          + Mathf.Pow(start[i, 1] - goal[j, 1], 2)
                          + Mathf.Pow(start[i, 2] - goal[j, 2], 2);
            }
        }
        return Hungary(mat);
    }
    public int[] Hungary(float[,] mat)
    {
        this.mat = mat;

        nrow = mat.GetLength(0);
        ncol = mat.GetLength(1);

        path_row_0 = -1;
        path_col_0 = -1;
        path_count = -1;

        rowCover = new byte[nrow];
        colCover = new byte[ncol];

        mark = new byte[nrow, ncol];

        path = new int[nrow, 2];
        result = new int[nrow];

        int step = 1;
        bool isDone = false;

        while (!isDone)
        {
            switch (step)
            {
                case 1: Step1(ref step); break;
                case 2: Step2(ref step); break;
                case 3: Step3(ref step); break;
                case 4: Step4(ref step); break;
                case 5: Step5(ref step); break;
                case 6: Step6(ref step); break;
                case 7: Step7(ref step); isDone = true; break;
            }
        }
        return result;
    }

    /// <summary>
    /// 每行减出一个0
    /// </summary>
    /// <param name="step"></param>
    private void Step1(ref int step)
    {
        for (int r = 0; r < nrow; r++)
        {
            float min = mat[r, 0];
            for (int c = 1; c < ncol; c++)
            {
                if (mat[r, c] < min)
                    min = mat[r, c];
            }
            for (int c = 0; c < ncol; c++)
            {
                mat[r, c] -= min;
            }
        }
        DoSortTable();
        step = 2;
    }

    private void DoSortTable()
    {
        for (int r = 0; r < nrow; r++)
        {
            for (int c = 0; c < ncol; c++)
            {
                sortTable[r, c] = c;
            }
            ReSortTable(r);
        }
    }

    void ReSortTable(int r)
    {
        int left = 0;
        int right = ncol - 1;
        QuickSortTable(r, left, right);
    }

    void QuickSortTable(int row, int left, int right)
    {
        if (left < right)
        {
            int partition = Partition(row, left, right);
            QuickSortTable(row, left, partition - 1);
            QuickSortTable(row, partition + 1, right);
        }
    }

    int Partition(int r, int left, int right)
    {
        int index = left + 1;
        float pivot = mat[r, sortTable[r, left]];
        if (pivot == 0)
            return index;
        for (int i = index; i <= right; i++)
        {
            if (mat[r,sortTable[r, i]] < pivot)
            {
                Swap(r, i, index);
                index++;
            }
        }
        index--;
        if(left!=index)
            Swap(r,left,index);

        return index;
    }

    private void Swap(int r, int i, int index)
    {
        int tmp = sortTable[r, i];
        sortTable[r, i] = sortTable[r, index];
        sortTable[r, index] = tmp;
    }

    /// <summary>
    /// 标记0的位置,标记为1
    /// </summary>
    /// <param name="step"></param>
    private void Step2(ref int step)
    {
        for (int r = 0; r < nrow; r++)
        {
            for (int c = 0; c < ncol; c++)
            {
                if (mat[r, c] == 0 && rowCover[r] == 0 && colCover[c] == 0)
                {
                    mark[r, c] = 1;
                    rowCover[r] = 1;
                    colCover[c] = 1;
                    continue;
                }
            }
        }
        clear_covers();
        step = 3;
    }

    /// <summary>
    /// 寻找含0的列数,大于指派数则结束
    /// </summary>
    /// <param name="step"></param>
    private void Step3(ref int step)
    {
        int colcount = 0;
        for (int r = 0; r < nrow; r++)
        {
            for (int c = 0; c < ncol; c++)
            {
                if (mark[r, c] == 1)
                    colCover[c] = 1;
            }
        }
        for (int c = 0; c < ncol; c++)
        {
            if (colCover[c] == 1)
                colcount++;
        }
        if (colcount >= ncol || colcount >= nrow)
        {
            step = 7;
        }
        else
        {
            step = 4;
        }
    }

    /// <summary>
    /// 在未覆盖的行列中找到一个0,找不到则转到step6
    /// 找到的0标记为2,如该行有其他标记为1的0则覆盖该行,取消覆盖该列继续
    /// 最后将找到的0记录,转到step5
    /// </summary>
    /// <param name="step"></param>
    private void Step4(ref int step)
    {
        int row = -1;
        int col = -1;
        bool done = false;

        while (!done)
        {
            FindZero(ref row, ref col);
            if (row == -1)
            {
                done = true;
                step = 6;
            }
            else
            {
                mark[row, col] = 2;
                if (FindStar_Row(row, ref col))
                {
                    rowCover[row] = 1;
                    colCover[col] = 0;
                    DoSortTable();
                }
                else
                {
                    done = true;
                    step = 5;
                    path_row_0 = row;
                    path_col_0 = col;
                 }
            }
        }
    }
    private void FindZero(ref int row, ref int col)
    {
        int r = 0;
        int index;
        int c;
        bool done = false;
        row = -1;
        col = -1;
        while (!done)
        {
            index = 0;
            if (rowCover[r] != 0)
                continue;
            while (index < ncol && !done)
            {
                c = sortTable[r, index];
                if (mat[r, c] != 0)
                    break;
                if (colCover[c] == 0)
                {
                    row = r;
                    col = c;
                    done = true;
                }
                index++;
            }

            r++;
            if (r >= nrow)
                done = true;
        }
    }

    private bool FindStar_Row(int row, ref int col)
    {
        col = -1;

        for (int c = ncol - 1; c >= 0; c--)
        {
            if (mark[row, c] == 1)
            {
                col = c;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 循环查找标记1的同列标记2和标记2的同行标记1形成一个列表
    /// 将查找出的标记1清除,标记2变为标记1(标记号--)
    /// 将标记2清除
    /// 重置覆盖
    /// </summary>
    /// <param name="step"></param>
    private void Step5(ref int step)
    {
        bool done;
        int r = -1;
        int c = -1;
        path_count = 1;
        path[path_count - 1, 0] = path_row_0;
        path[path_count - 1, 1] = path_col_0;
        done = false;
        while (!done)
        {
            find_star_in_col(path[path_count - 1, 1], ref r);
            if (r > -1)
            {
                path_count++;
                path[path_count - 1, 0] = r;
                path[path_count - 1, 1] = path[path_count - 2, 1];
            }
            else
                done = true;
            if (!done)
            {
                find_prime_in_row(path[path_count - 1, 0], ref c);
                path_count++;
                path[path_count - 1, 0] = path[path_count - 2, 0];
                path[path_count - 1, 1] = c;
            }

        }
        // 将标记1清除,标记2变为标记1(标记号--)
        augment_path();
        // 将未查找出来的标记2清除
        erase_primes();
        // 重置覆盖
        clear_covers();
        step = 3;
    }
    private void find_star_in_col(int c, ref int r)
    {
        r = -1;
        for (int i = nrow - 1; i >=0; i--)
        {
            if (mark[i, c] == 1)
            {
                r = i;
                return;
            }
        }
    }
    private void find_prime_in_row(int r, ref int c)
    {
        c = -1;
        for (int i = nrow - 1; i >= 0; i--)
        {
            if (mark[r, i] == 2)
            {
                c = i;
                return;
            }
        }
    }
    private void augment_path()
    {
        for (int p = 0; p < path_count; p++)
        {
            if (mark[path[p, 0], path[p, 1]] == 1)
                mark[path[p, 0], path[p, 1]] = 0;
            else
                mark[path[p, 0], path[p, 1]] = 1;
        }
    }
    private void clear_covers()
    {
        for (int r = 0; r < nrow; r++)
            rowCover[r] = 0;
        for (int c = 0; c < ncol; c++)
            colCover[c] = 0;
    }
    private void erase_primes()
    {
        for (int r = 0; r < nrow; r++)
            for (int c = 0; c < ncol; c++)
                if (mark[r, c] == 2)
                    mark[r, c] = 0;
    }

    /// <summary>
    /// 未覆盖的行列中减去最小值,
    /// 行列同时覆盖的值则增加这个值
    /// </summary>
    /// <param name="step"></param>
    private void Step6(ref int step)
    {
        var minval = float.MaxValue;
        find_smallest(ref minval);
        for (int r = 0; r < nrow; r++)
        {
            if (rowCover[r] == 1)
            {
                for (int c = 0; c < ncol; c++)
                {
                    mat[r, c] += minval;
                }
            }
            else
            {
                for (int c = 0; c < ncol; c++)
                {
                    if (colCover[c] == 0)
                        mat[r, c] -= minval;
                }
            }
        }
        DoSortTable();
        step = 4;
    }
    private void find_smallest(ref float minval)
    {
        for (int r = 0; r < nrow; r++)
        {
            if (rowCover[r] == 0)
            {
                for (int index = 0; index < ncol; index++)
                {
                    int c = sortTable[r,index];
                    if (colCover[c] == 0)
                    {
                        var tmp = mat[r, c];
                        if (tmp == 0)
                        {
                            Debug.Log("0000000");
                        }
                        if (minval > tmp)
                            minval = tmp;
                        break;
                    }

                }

            }
        }

    }

    private void Step7(ref int step)
    {
        for (int r = 0; r < nrow; r++)
        {
            for (int c = 0; c < ncol; c++)
            {
                if (mark[r, c] == 1)
                {
                    result[r] = c;
                }
            }
        }
    }

    public static int[] CalTarget(UAVData[] start_position, UAVData[] target_position)
    {
        var nrow = start_position.Length;
        var ncol = target_position.Length;

        var mat = new float[nrow, ncol];

        for (int i = 0; i < nrow; i++)
        {
            for (int j = 0; j < ncol; j++)
            {
                mat[i, j] = Mathf.Pow(start_position[i].position.x - target_position[j].position.x, 2)
                          + Mathf.Pow(start_position[i].position.y - target_position[j].position.y, 2)
                          + Mathf.Pow(start_position[i].position.z - target_position[j].position.z, 2);
            }
        }
        var res = new HungaryHelper2().Hungary(mat);

        return res;
    }
}
