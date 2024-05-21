using com.hgfly.uavutils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class HungaryHelper
{
    private int nrow;
    private int ncol;

    private int path_row_0;
    private int path_col_0;
    private int path_count;

    private byte[] rowCover;
    private byte[] colCover;

    private float[,] mat;
    private byte[,] mark;
    private int[,] path;
    private int[] result;

    public int[] Hungary(float[,] start, float[,] goal)
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
        var alast = DateTime.Now.Millisecond;
        int dev = 0;
        int[] time = new int[8];
        int[] count = new int[8];
        while (!isDone)
        {
            Debug.Log("Step:"+step);
            dev = DateTime.Now.Millisecond - alast;
            if (dev < 0)
            {
                dev = DateTime.Now.Millisecond + (1000 - alast);
            }
            time[step] += dev;
            count[step]++;
            alast = DateTime.Now.Millisecond;

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
        for (int i = 0; i < time.Length; i++)
        {
            Debug.Log($"{i}|{count[i]}|{time[i]}");
        }
        return result;
    }

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
        step = 2;
    }
    private void Step2(ref int step)
    {
        for (int r = 0; r < nrow; r++)
            for (int c = 0; c < ncol; c++)
                if (mat[r, c] == 0 && rowCover[r] == 0 && colCover[c] == 0)
                {
                    mark[r, c] = 1;
                    rowCover[r] = 1;
                    colCover[c] = 1;
                }
        for (int r = 0; r < nrow; r++) rowCover[r] = 0;
        for (int c = 0; c < ncol; c++) colCover[c] = 0;
        step = 3;
    }

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
        }else
        {
            step = 4;
        }
    }

    private void Step4(ref int step)
    {
        int row = -1;
        int col = -1;
        bool done = false;

        while (!done)
        {
            FindZero(ref row,ref col);
            if (row == -1)
            {
                done = true;
                step = 6;
            }else
            {
                mark[row, col] = 2;
                if (Star_Row(row))
                {
                    FindStar_Row(row, ref col);
                    rowCover[row] = 1;
                    colCover[col] = 0;
                }else
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
        int c;
        bool done = false;
        row = -1;
        col = -1;
        while (!done)
        {
            c = 0;
            while (true)
            {
                if (mat[r, c] == 0 && rowCover[r] == 0 && colCover[c] == 0)
                {
                    row =r;
                    col = c;
                    done = true;
                }
                c++;
                if (c >= ncol || done)
                    break;
            }

            r ++;
            if (r >= nrow)
                done = true;
        }
    }
    private bool Star_Row(int row)
    {
        bool tmp = false;
        for (int c = 0; c < ncol; c++)
            if (mark[row, c] == 1)
                tmp = true;
        return tmp;
    }
    private void FindStar_Row(int row, ref int col)
    {
        col = -1;
        for (int c = 0; c < ncol; c++)
            if (mark[row, c] == 1)
                col = c;
    }

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
                find_prime_in_row(path[path_count - 1,0],ref c);
                path_count++;
                path[path_count - 1, 0] = path[path_count - 2, 0];
                path[path_count - 1, 1] = c;
            }

        }
        augment_path();
        clear_covers();
        erase_primes();
        step = 3;
    }
    private void find_star_in_col(int c, ref int r)
    {
        r = -1;
        for (int i = 0; i < nrow; i++)
        {
            if (mark[i, c] == 1)
                r = i;
        }
    }
    private void find_prime_in_row(int r, ref int c)
    {
        c = -1;
        for (int i = 0; i < nrow; i++)
        {
            if (mark[r, i] == 2)
                c = i;
        }
    }
    private void augment_path()
    {
        for (int p = 0; p < path_count; p++)
        {
            if (mark[path[p, 0], path[p, 1]] == 1)
                mark[path[p,0],path[p, 1]] = 0;
            else
                mark[path[p,0],path[p, 1]] = 1;
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

    private void Step6(ref int step)
    {
        var minval = float.MaxValue;
        find_smallest(ref minval);
        for (int r = 0; r < nrow; r++)
            for (int c = 0; c < ncol; c++)
            {
                if (rowCover[r] == 1)
                    mat[r, c] += minval;
                if (colCover[c] == 0)
                    mat[r, c] -= minval;
                step = 4;
            }
    }
    private void find_smallest(ref float minval)
    {
        for (int r = 0; r < nrow; r++)
            for (int c = 0; c < ncol; c++)
                if (rowCover[r] == 0 && colCover[c] == 0)
                    if (minval > mat[r, c])
                        minval = mat[r, c];
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
        var res = new HungaryHelper().Hungary(mat);

        return res;
    }
}
