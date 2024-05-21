using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CToool.Helper
{
    public class HungaryHelper3
    {
        private int nrow;
        private int ncol;

        private int path_row_0;
        private int path_col_0;
        private int path_count;

        private int[] rowCover;
        private int[] colCover;

        private float[,] mat;
        private int[,] mark;
        uint[,] sortTable;
        private int[,] path;
        private int[] result;

        private ComputeShader calc_shader;
        private int main_kernel;

        private int step1_kernel;
        private int step2_kernel;
        private int step3_kernel;
        private int step4_kernel;
        private int step5_kernel;
        private int step6_kernel;
        private int step7_kernel;

        ComputeBuffer mat_buffer;
        ComputeBuffer rowCover_buffer;
        ComputeBuffer colCover_buffer;
        ComputeBuffer mark_buffer    ;
        ComputeBuffer path_buffer    ;
        ComputeBuffer sortTable_buffer;
        ComputeBuffer result_buffer;

        int[] size = new int[2];
        int[] pathInfo = new int[3];

        public HungaryHelper3(ComputeShader calc_shader)
        {
            this.calc_shader = calc_shader;
            main_kernel = calc_shader.FindKernel("Cmain");
            step1_kernel = calc_shader.FindKernel("step1");
            step2_kernel = calc_shader.FindKernel("step2");
            step3_kernel = calc_shader.FindKernel("step3");
            step4_kernel = calc_shader.FindKernel("step4");
            step5_kernel = calc_shader.FindKernel("step5");
            step6_kernel = calc_shader.FindKernel("step6");
            step7_kernel = calc_shader.FindKernel("step7");

        }

        public int[] Hungary(float[,] mat)
        {
            this.mat = mat;

            nrow = mat.GetLength(0);
            ncol = mat.GetLength(1);

            sortTable = new uint[nrow, ncol];
            for (int r = 0; r < nrow; r++)
            {
                for (uint c = 0; c < ncol; c++)
                {
                    sortTable[r,c] = c;
                }
            }

            size[0] = (ushort)nrow;
            size[1] = (ushort)ncol;
            calc_shader.SetInts("size", size);

            path_row_0 = -1;
            path_col_0 = -1;
            path_count = -1;

            pathInfo[0] = -1;
            pathInfo[1] = -1;
            pathInfo[2] = -1;
            calc_shader.SetInts("pathinfo", pathInfo);

            rowCover = new int[nrow];
            colCover = new int[ncol];
            mark = new int[nrow, ncol];
            path = new int[nrow, 2];
            result = new int[nrow];

            //var matary = mat.Cast<float>().ToArray();
            mat_buffer       = SetBuffer<float>("mat", mat);
            rowCover_buffer  = SetBuffer<int>("rowCover", rowCover);
            colCover_buffer  = SetBuffer<int>("colCover", colCover);
            mark_buffer      = SetBuffer<int>("colCover", mark);
            path_buffer      = SetBuffer<int>("path", path);
            sortTable_buffer = SetBuffer<uint>("sortTable", sortTable);
            result_buffer    = SetBuffer<int>("result", result);

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
            mat_buffer.Release();
            rowCover_buffer.Release();
            colCover_buffer.Release();
            mark_buffer.Release();
            path_buffer.Release();
            result_buffer.Release();
            return result;
        }

        private ComputeBuffer SetBuffer<T>(string name, Array data)
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, Marshal.SizeOf(typeof(T)));
            buffer.SetData(data);
            calc_shader.SetBuffer(main_kernel, name, buffer);
            return buffer;
        }

        private void SetStep(int step)
        {
            calc_shader.SetInt("step", step);
        }

        /// <summary>
        /// 每行减出一个0
        /// </summary>
        /// <param name="step"></param>
        private void Step1(ref int step)
        {
            SetStep(1);
            calc_shader.Dispatch(main_kernel,nrow,1,1);
            step = 2;
        }

        /// <summary>
        /// 标记0的位置,标记为1
        /// </summary>
        /// <param name="step"></param>
        private void Step2(ref int step)
        {
            SetStep(2);
            calc_shader.Dispatch(main_kernel, nrow, 1, 1);
            clear_covers();
            step = 3;
        }

        /// <summary>
        /// 寻找含0的列数,大于指派数则结束
        /// </summary>
        /// <param name="step"></param>
        private void Step3(ref int step)
        {
            SetStep(3);
            calc_shader.Dispatch(main_kernel, ncol, 1, 1);
            colCover_buffer.GetData(colCover);

            int colcount = 0;

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
            sortTable_buffer.GetData(sortTable);

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
            uint c;
            bool done = false;
            row = -1;
            col = -1;
            while (!done)
            {
                if (rowCover[r] != 0)
                    continue;
                index = 0;
                while (index < ncol && !done)
                {
                    c = sortTable[r, index];
                    if (colCover[c] == 0 )
                    {
                        if (mat[r, c] == 0)
                        {
                            row = r;
                            col = (int)c;
                            done = true;
                        }
                        return;

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
            for (int i = nrow - 1; i >= 0; i--)
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
            rowCover_buffer.SetData(rowCover);
            colCover_buffer.SetData(colCover);
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
                for (int c = 0; c < ncol; c++)
                {
                    if (rowCover[r] == 1 && colCover[c] == 0)
                        continue;
                    if (rowCover[r] == 1)
                        mat[r, c] += minval;
                    else if (colCover[c] == 0)
                        mat[r, c] -= minval;
                }
            }
            step = 4;
        }
        //ii
        private void find_smallest(ref float minval)
        {
            for (int r = 0; r < nrow; r++)
            {
                if (rowCover[r] == 0)
                {
                    for (int c = 0; c < ncol; c++)
                        if (colCover[c] == 0)
                            if (minval > mat[r, c])
                                minval = mat[r, c];

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

    }
}
