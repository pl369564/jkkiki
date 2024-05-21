using Accord.Math;
using Accord.Math.Distances;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using MathNet.Numerics.Data.Matlab;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CToool.DancePackTools
{
    //[Serializable]
    //public struct Dancestep
    //{
    //    //1维:飞机id,2维:帧数
    //    public double[,] x;
    //    public double[,] y;
    //    public double[,] z;
    //    public byte[,] r;
    //    public byte[,] g;
    //    public byte[,] b;
    //    public int[,] yaw;
    //    public byte[,] ctr;
    //}
    public static class DancePackTools
    {
        public const float DeltaTime = 0.033333f;
        public const float InWeight = 0.15f;
        public const float OutWeight = 0.15f;

        public static byte[] encript_key = new byte[16] { 0x02, 0x05, 0x00, 0x08, 0x01, 0x07, 0x00, 0x01, 0x01, 0x09, 0x09, 0x01, 0x00, 0x07, 0x02, 0x04 };

        public static void PacketMat(List<Dancestep> dancestepList, string directory, string tarName, string fileName)
        {
            MatUtil.PacketMat(dancestepList, directory, tarName, fileName);
        }
        public static string PacketB06Dac(Dictionary<string, Matrix<float>> dict, string directory, string tarName, string fileName, string exePath = "", string FragmentInfoJson = "")
        {
            Position position;
            Lighting lighting = default;
            Turn turn;

            dict.TryGetValue("x", out position.x);
            dict.TryGetValue("y", out position.y);
            dict.TryGetValue("z", out position.z);
            position = ResetOrigin(position);

            int totals = position.x.RowCount;
            int points = position.x.ColumnCount;

            if (!dict.TryGetValue("r", out lighting.r))
            {
                lighting.r = DenseMatrix.Create(totals, points, 0).ToSingle();
            }

            if (!dict.TryGetValue("g", out lighting.g))
            {
                lighting.g = DenseMatrix.Create(totals, points, 0).ToSingle();
            }

            if (!dict.TryGetValue("b", out lighting.b))
            {
                lighting.b = DenseMatrix.Create(totals, points, 0).ToSingle();
            }
            if (!dict.TryGetValue("ctr", out turn.ctr))
            {
                turn.ctr = DenseMatrix.Create(totals, points, 0).ToSingle();
            }

            return PacketB06Dac(position,lighting,turn,directory,tarName,fileName,exePath,FragmentInfoJson);

        }
        public static string PacketB06Dac(Position position, Lighting lighting, Turn turn, string directory, string tarName, string fileName, string exePath = "", string FragmentInfoJson = "")
        {
            directory += "/Dac/";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                File.Delete(file);
            }
            int totals = position.x.RowCount;
            int points = position.x.ColumnCount;


            List<string> md5List = new List<string>(new string[totals]);
            float[] range = GetRange(position);

            Parallel.For(0, totals, qn =>
            {
                IncrementalHash md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
                HeaderV200 headerV200 = new HeaderV200
                {
                    version = new byte[] { 2, 0, 0 },
                    qn = (byte)qn,
                    time = points * 0.033333f,
                    frameRate = 30f,
                    total = totals,
                    pars = 8,
                    range = range,
                };
                byte[] header = Structure2Bytes(headerV200);
                FileStream fileStream = new FileStream(directory + "/dancestep" + qn.ToString() + ".dac", FileMode.OpenOrCreate);
                fileStream.Write(header, 0, header.Length);
                //md5.ComputeHash(header);
                md5.AppendData(header);
                Velocity velocity = CalSpeed(position);
                for (int fr = 0; fr < points; fr++)
                {
                    PayloadV200 payloadV200 = new PayloadV200
                    {
                        x = (short)(position.x[qn, fr] * 100f),
                        y = (short)(position.y[qn, fr] * 100f),
                        z = (short)(position.z[qn, fr] * 100f),
                        r = (byte)lighting.r[qn, fr],
                        g = (byte)lighting.g[qn, fr],
                        b = (byte)lighting.b[qn, fr],
                        ctr = (byte)turn.ctr[qn, fr],
                        yaw = 0,
                    };

                    byte[] payload = Structure2Bytes(payloadV200);
                    fileStream.Write(payload, 0, payload.Length);

                    md5.AppendData(payload);
                }
                md5List[qn] = BitConverter.ToString(md5.GetHashAndReset()).Replace("-", string.Empty);
                fileStream.Flush();
                fileStream.Close();
            });

            StreamWriter sw = File.CreateText(directory + "/dancestep_header.dac");
            sw.WriteLine("V2.3.0");
            sw.WriteLine((points * 0.033333f).ToString("F6") + " 30.000000 " + totals.ToString() + " 8");
            sw.WriteLine(String.Join(" ", range.ToString("F6")));

            foreach (string hash in md5List)
            {
                sw.Write(hash + "\n");
            }
            sw.Close();

            sw = File.CreateText(directory + "/DanceStepInfo.txt");
            sw.WriteLine("***************************************************************************");
            if (tarName.Contains("\\"))
                sw.WriteLine(tarName.Substring(tarName.LastIndexOf('\\') + 1).Replace(".zip", "") + " Dance Step Info:");
            else if (tarName.Contains("/"))
                sw.WriteLine(tarName.Substring(tarName.LastIndexOf('/') + 1).Replace(".zip", "") + " Dance Step Info:");
            else
                sw.WriteLine(tarName.Replace(".zip", "") + " Dance Step Info:");
            sw.WriteLine("Quads:" + totals + ",Times:" + (points * 0.033333f).ToString("F6"));
            //sw.WriteLine("Max Vxy:    " + ve);
            sw.WriteLine("Max X:" + range[1].ToString("F6").PadLeft(12, ' ') + ",Max Y:" + range[3].ToString("F6").PadLeft(12, ' ') + ",Max Z:" + range[5].ToString("F6").PadLeft(12, ' '));
            sw.WriteLine("Min X:" + range[0].ToString("F6").PadLeft(12, ' ') + ",Min Y:" + range[2].ToString("F6").PadLeft(12, ' ') + ",Min Z:" + range[4].ToString("F6").PadLeft(12, ' '));
            sw.WriteLine("Min Dist:" + Mathf.Min(CalMinDist(position)).ToString("F6"));
            sw.WriteLine("***************************************************************************");
            sw.Close();

            AppJson json = new AppJson();
            json.danceType = 4;
            json.dronenum = (short)totals;
            json.duration = (int)(points * 0.033333f);
            json.fileId = DateTime.Now.ToString("yyyyMMddHHmmss");
            json.id = ushort.Parse(DateTime.Now.ToString("mmss"));
            json.max_height = range[5];
            json.name = fileName;
            json.type = 90;

            var jsonStr = JsonUtility.ToJson(json);

            sw = File.CreateText(directory + "/" + json.fileId + ".json");
            sw.Write(jsonStr);
            sw.Close();

            if (File.Exists(tarName))
            {
                File.Delete(tarName);
            }
            var dir = Path.GetDirectoryName(tarName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string jsonPath = directory + "/TrajectoryInfo.json";
            WriteToFile(jsonPath, FragmentInfoJson);
            if (tarName.Contains(".zip"))
            {
                return ZipUtil.Zip(directory, string.Format("{3}/B06DanceStep{0}-{1}({2}).zip", json.dronenum, json.fileId, fileName, dir));
            }
            return string.Empty;
        }


        public static string PacketB03Dac(Dictionary<string, Matrix<float>> dict, string directory, string tarName, string fileName, string exePath = "", string FragmentInfoJson = "")
        {
            Position position = default;
            Lighting lighting = default;
            Turn turn;
            dict.TryGetValue("x", out position.x);
            dict.TryGetValue("y", out position.y);
            dict.TryGetValue("z", out position.z);
            position = ResetOrigin(position);
            Velocity velocity = CalSpeed(position);

            int totals = position.x.RowCount;
            int points = position.x.ColumnCount;

            if (!dict.TryGetValue("r", out lighting.r))
            {
                lighting.r = DenseMatrix.Create(totals, points, 0).ToSingle();
            }
            if (!dict.TryGetValue("g", out lighting.g))
            {
                lighting.g = DenseMatrix.Create(totals, points, 0).ToSingle();
            }
            if (!dict.TryGetValue("b", out lighting.b))
            {
                lighting.b = DenseMatrix.Create(totals, points, 0).ToSingle();
            }
            if (!dict.TryGetValue("ctr", out turn.ctr))
            {
                turn.ctr = DenseMatrix.Create(totals, points, 0).ToSingle();
            }

            return PacketB03Dac(position, lighting, turn, directory, tarName, fileName, exePath, FragmentInfoJson);
            
        }
        public static string PacketB03Dac(List<Dancestep> danlist, string directory, string tarName, string fileName,string exePath = "", string FragmentInfoJson = "")
        {
            Position position = DancestepUtil.ToPositon(danlist);
            Lighting lighting = DancestepUtil.Tolighting(danlist);
            Turn turn = DancestepUtil.ToTurn(danlist);
            return PacketB03Dac(position, lighting, turn, directory, tarName, fileName, exePath, FragmentInfoJson);
        }
        public static string PacketB03Dac(Position position, Lighting lighting, Turn turn, string directory,string tarName, string fileName, string exePath = "", string FragmentInfoJson = "")
        {
            directory += "/Dac/";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string[] files = Directory.GetFiles(directory);
            foreach (string file in files)
            {
                File.Delete(file);
            }

            position = ResetOrigin(position);
            Velocity velocity = CalSpeed(position);

            SaveStartPosition(position, directory + "position.txt");

            int totals = position.x.RowCount;
            int points = position.x.ColumnCount;

            List<string> md5List = new List<string>(new string[totals]);
            float[] range = GetRange(position, 30);

            int len_head = Marshal.SizeOf(typeof(B03HeaderV200));
            int len_payload = Marshal.SizeOf(typeof(B03PayloadV200));
            int totalLen = len_head + len_payload * points;//数据总长度
            Parallel.For(0, totals, qn =>
            {
                byte[] buffer = new byte[totalLen];

                int offset = 0;

                B03HeaderV200 headerV200 = new B03HeaderV200
                {
                    version = new byte[] { 2, 0, 0 },
                    qn = (UInt16)qn,
                    time = points * 0.033333f,
                    frameRate = 30f,
                    total = totals,
                    pars = 8,
                    range = range,
                    headCrc = 0// (byte)(2 + qn + points * 0.033333f + 30f + totals + 8 + range[0] + range[1] + range[2] + range[3] + range[4] + range[5]),
                };
                byte[] header = Structure2Bytes(headerV200);

                header[header.Length - 1] = GetCrc(header);

                header.WriteTO(buffer, ref offset);//buffer.Concat(header).ToArray();

                FileStream fileStream = new FileStream(directory + "/dancestep" + qn.ToString() + ".dac", FileMode.OpenOrCreate);
                B03PayloadV200 payloadV200 = new B03PayloadV200();
                byte[] payload = new byte[Marshal.SizeOf((payloadV200).GetType())];
                for (int fr = 0; fr < points; fr++)
                {
                    payloadV200.stx = 0xFE;
                    payloadV200.x = position.x[qn, fr];
                    payloadV200.y = position.y[qn, fr];
                    payloadV200.z = position.z[qn, fr];
                    payloadV200.r = (byte)lighting.r[qn, fr];
                    payloadV200.g = (byte)lighting.g[qn, fr];
                    payloadV200.b = (byte)lighting.b[qn, fr];
                    payloadV200.w = 0;
                    payloadV200.light_ctr = 0;
                    payloadV200.mode_ctr = (byte)turn.ctr[qn, fr];
                    payloadV200.reserve0 = 0;
                    payloadV200.reserve1 = 0;
                    payloadV200.stepCrc = 0;

                    Structure2Bytes(payload, payloadV200);

                    payload[payload.Length - 1] = GetCrc(payload);

                    payload.WriteTO(buffer, ref offset);//buffer = buffer.Concat(payload).ToArray();
                }

                if (offset != buffer.Length)
                {
                    Debug.LogError(" buffer.Length error");
                }
                buffer = AES.Encrypt(buffer, encript_key);
                md5List[qn] = GetFileMD5(buffer);
                fileStream.Write(buffer, 0, buffer.Length);
                fileStream.Flush();
                fileStream.Close();
            });


            StreamWriter sw = File.CreateText(directory + "/dancestep_header.dac");
            sw.WriteLine("V2.0.0");
            sw.WriteLine((points * 0.033333f).ToString("F6") + " 30.000000 " + totals.ToString() + " 8");
            sw.WriteLine(String.Join(" ", range.ToString("F6")));


            foreach (string hash in md5List)
            {
                sw.Write(hash + "\n");
            }
            sw.Close();

            sw = File.CreateText(directory + "/Config.txt");
            sw.WriteLine("dancestep_sn=" + GetFileMD5(directory + "/dancestep_header.dac", false) + "5");
            sw.Close();

            sw = File.CreateText(directory + "/DanceStepInfo.txt");
            sw.WriteLine("***************************************************************************");
            if (tarName.Contains("\\"))
                sw.WriteLine(tarName.Substring(tarName.LastIndexOf('\\') + 1).Replace(".zip", "") + " Dance Step Info:");
            else if (tarName.Contains("/"))
                sw.WriteLine(tarName.Substring(tarName.LastIndexOf('/') + 1).Replace(".zip", "") + " Dance Step Info:");
            else
                sw.WriteLine(tarName.Replace(".zip", "") + " Dance Step Info:");
            sw.WriteLine("Quads:" + totals + ",Times:" + (points * 0.033333f).ToString("F6"));
            sw.WriteLine($"Max Vxy:{velocity.vxyMax,12:F6},Max Vz:{velocity.vzMax,12:F6},Min Vz:{velocity.vzMin,12:F6}");
            sw.WriteLine($"Max   X:{range[1],12:F6},Max  Y:{range[3],12:F6},Max  Z:{range[5],12:F6}");
            sw.WriteLine($"Min   X:{range[0],12:F6},Min  Y:{range[2],12:F6},Min  Z:{range[4],12:F6}");
            sw.WriteLine("Min Dist:" + Mathf.Min(CalMinDist(position)).ToString("F6"));
            sw.WriteLine("***************************************************************************");
            sw.Close();


            AppJson json = new AppJson();
            json.danceType = 4;
            json.dronenum = (short)totals;
            json.duration = (int)(points * 0.033333f);
            json.fileId = DateTime.Now.ToString("yyyyMMddHHmmss");
            json.id = ushort.Parse(DateTime.Now.ToString("mmss"));
            json.max_height = range[5];
            json.name = fileName;
            json.type = 90;

            var jsonStr = JsonUtility.ToJson(json);

            sw = File.CreateText(directory + "/" + json.fileId + ".json");
            sw.Write(jsonStr);
            sw.Close();

            if (File.Exists(tarName))
            {
                File.Delete(tarName);
            }
            var dir = Path.GetDirectoryName(tarName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (FragmentInfoJson != string.Empty)
            {
                string TrajectoryInfoPath = directory + "/TrajectoryInfo.json";
                WriteToFile(TrajectoryInfoPath, FragmentInfoJson);
            }

            if (tarName.Contains(".zip"))
            {
                return ZipUtil.Zip(directory, string.Format("{3}/EMO_Dance{0}-{1}({2}).zip", json.dronenum, json.fileId, fileName, dir));
            }
            else if (tarName.Contains(".tgz"))
            {
                return TgzUtil.Tgz7za(exePath, directory, string.Format("{3}/EMO_Dance{0}-{1}({2}).tgz", json.dronenum, json.fileId, fileName, dir));
            }
            return string.Empty;
        }

        public static Dictionary<string, Matrix<float>> UnPackZip(string destPath,string zipPath)
        {
            var path = ZipUtil.Unzip(destPath, zipPath);
            return LoadDac(path);
        }
        public static Dictionary<string, Matrix<float>> UnPackTgz(string destPath, string zipPath)
        {
            var path = TgzUtil.UnTgz(destPath, zipPath);
            return LoadDac(path);
        }

        public static Dictionary<string, Matrix<float>> LoadDac(string directory)
        {
            var lines = File.ReadLines(directory + "/dancestep_header.dac");
            int lineIndex = 0;
            int points = 0;
            int quads = 0;
            string version = "";
            string info = "";
            float times = 0;

            foreach (string line in lines)
            {
                Debug.Log(line);
                if (lineIndex == 0)
                {
                    version = line;
                }
                else if (lineIndex == 1)
                {
                    info = line;
                }
                lineIndex++;
            }

            Debug.Log("Version:" + version);
            int.TryParse(info.Split(' ')[2], out quads);
            float.TryParse(info.Split(' ')[0], out times);
            points = (int)((times + 0.01f) * 30);
            Debug.Log($"Quads:{quads}|times:{times}|points:{points}");
            float[,] tmp_x = new float[quads, points];
            float[,] tmp_y = new float[quads, points];
            float[,] tmp_z = new float[quads, points];
            float[,] tmp_r = new float[quads, points];
            float[,] tmp_g = new float[quads, points];
            float[,] tmp_b = new float[quads, points];
            Parallel.For(0, quads, qn =>
            {
                byte[] bytes = File.ReadAllBytes(directory + "/dancestep" + qn + ".dac");
                int index = 46;
                if (bytes.LongLength == points * 22 + 46)
                {
                    byte[] bytes1 = AES.Decryptor(bytes, encript_key);
                    for (int i = 0; i < points; i++)
                    {
                        index++;
                        tmp_x[qn, i] = BitConverter.ToSingle(bytes1, index); index += 4;
                        tmp_y[qn, i] = BitConverter.ToSingle(bytes1, index); index += 4;
                        tmp_z[qn, i] = BitConverter.ToSingle(bytes1, index); index += 4;
                        tmp_r[qn, i] = bytes1[index]; index++;
                        tmp_g[qn, i] = bytes1[index]; index++;
                        tmp_b[qn, i] = bytes1[index]; index += 3;
                        index += 4;
                    }
                }
                else if (bytes.LongLength == points * 12 + 44)
                {
                    index = 44;
                    for (int i = 0; i < points; i++)
                    {
                        tmp_x[qn, i] = BitConverter.ToInt16(bytes, index) / 100f; index += 2;
                        tmp_y[qn, i] = BitConverter.ToInt16(bytes, index) / 100f; index += 2;
                        tmp_z[qn, i] = BitConverter.ToInt16(bytes, index) / 100f; index += 2;
                        tmp_r[qn, i] = bytes[index]; index++;
                        tmp_g[qn, i] = bytes[index]; index++;
                        tmp_b[qn, i] = bytes[index]; index += 4;
                    }
                }
                else
                {
                    Debug.LogError("invalid byte");
                }
                Debug.Log(index);
            });

            Dictionary<string, Matrix<float>> dict = new Dictionary<string, Matrix<float>>();

            Matrix<float> matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfArray(tmp_x);
            dict.Add("x", matrix);
            matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfArray(tmp_y);
            dict.Add("y", matrix);
            matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfArray(tmp_z);
            dict.Add("z", matrix);
            matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfArray(tmp_r);
            dict.Add("r", matrix);
            matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfArray(tmp_g);
            dict.Add("g", matrix);
            matrix = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfArray(tmp_b);
            dict.Add("b", matrix);

            return dict;
        }

        #region [Tools]
        public static double CalMinDist(this List<Dancestep> list)
        {
            double[] mindist = list.CalMinDistList();
            return Math.Sqrt(mindist.Min());
        }
        public static double[] CalMinDistList(this List<Dancestep> list,double maxValue = double.MaxValue)
        {
            int totals = list[0].x.GetLength(0);
            int points = list[0].x.GetLength(1);

            for (int i = 1; i < list.Count; i++)
            {
                points += list[i].x.GetLength(1);
            }

            double[] mindist = new double[points];

            Parallel.For(0, points, (fr) =>
            {
                double min = double.MaxValue;
                mindist[fr] = double.MaxValue;
                var index = 0;
                var nfr = fr;
                for (int i = 0; i < list.Count; i++)
                {
                    var sfr = list[i].x.GetLength(1);
                    if (sfr > nfr)
                    {
                        index = i;
                        break;
                    }
                    else
                    {
                        nfr -= sfr;
                    }
                }
                var current = list[index];//.x[qn, fr];

                for (int qi = 0; qi < totals - 1; qi++)
                {
                    for (int qj = qi + 1; qj < totals; qj++)
                    {
                        var z1 = current.z[qi, nfr];
                        var z2 = current.z[qj, nfr];

                        if (z1 < 0.01 || z2 < 0.01)
                        {
                            continue;
                        }

                        var dx = current.x[qi, nfr] - current.x[qj, nfr];
                        dx *= dx;
                        if (dx > mindist[fr])
                            continue;

                        var dy = current.y[qi, nfr] - current.y[qj, nfr];
                        dy *= dy;
                        if (dy > mindist[fr])
                            continue;

                        var dz = z1 - z2;
                        dz *= dz;
                        if (dz > mindist[fr])
                            continue;

                        mindist[fr] = dx + dy + dz;

                        if (mindist[fr] < min)
                        {
                            min = mindist[fr];
                        }
                    }
                }
                if (min == double.MaxValue)
                    min = maxValue * maxValue;
                mindist[fr] = min;
            });

            return mindist;
        }
        public static void WriteTO(this byte[] data, byte[] buffer, ref int offset)
        {
            Array.Copy(data, 0, buffer, offset, data.Length);
            offset += data.Length;
            if (offset < data.Length)
            {
                throw new Exception("Offset OverMaxValue");
            }
        }

        public static string GetFileMD5(string path, bool isLower = true)
        {
            using var fs = File.OpenRead(path);
            using var cryto = MD5.Create();
            var md5bytes = cryto.ComputeHash(fs);
            var format = isLower ? "x2" : "X2";
            return md5bytes.Aggregate(string.Empty, (a, b) => { return a + b.ToString(format); });
        }
        public static string GetFileMD5(byte[] dancedata)
        {
            var md5data = getMD5Data(dancedata);
            return MD5ToString(md5data);
        }
        public static byte[] getMD5Data(byte[] bytes)
        {
            var md5 = new MD5CryptoServiceProvider();
            byte[] md5data = md5.ComputeHash(bytes);
            md5.Clear();

            return md5data;
        }
        public static string MD5ToString(byte[] md5data)
        {
            return md5data.Aggregate(string.Empty, (a, b) => { return a + b.ToString("x2"); });
        }

        public static Position ResetOrigin(Position position)
        {
            float originX = position.x[0, 0];
            float originY = position.y[0, 0];
            float originZ = position.z[0, 0];

            for (int qn = 0; qn < position.x.RowCount; qn++)
            {
                for (int fr = 0; fr < position.x.ColumnCount; fr++)
                {
                    position.x[qn, fr] -= originX;
                    position.y[qn, fr] -= originY;
                    position.z[qn, fr] -= originZ;
                }
            }

            return position;
        }

        public static float[] GetRange(Position position, float plusBound = 0)
        {
            int totals = position.x.RowCount;
            int points = position.x.ColumnCount;
            float[] range = new float[6];
            float[] xMins = new float[totals];
            float[] yMins = new float[totals];
            float[] zMins = new float[totals];
            float[] xMaxs = new float[totals];
            float[] yMaxs = new float[totals];
            float[] zMaxs = new float[totals];

            Parallel.For(0, totals, (qn) =>
            {
                xMins[qn] = position.x.Row(qn).Minimum();
                yMins[qn] = position.y.Row(qn).Minimum();
                zMins[qn] = position.z.Row(qn).Minimum();
                xMaxs[qn] = position.x.Row(qn).Maximum();
                yMaxs[qn] = position.y.Row(qn).Maximum();
                zMaxs[qn] = position.z.Row(qn).Maximum();
            });

            range[0] = Mathf.Min(xMins) - plusBound;
            range[1] = Mathf.Max(xMaxs) + plusBound;
            range[2] = Mathf.Min(yMins) - plusBound;
            range[3] = Mathf.Max(yMaxs) + plusBound;
            range[4] = Mathf.Min(zMins) - plusBound;
            range[5] = Mathf.Max(zMaxs) + plusBound;

            return range;
        }

        static byte[] Structure2Bytes(object structure)
        {
            int size = Marshal.SizeOf((structure).GetType());
            byte[] bytes = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }
        static void Structure2Bytes(byte[] bytes, object structure)
        {
            int size = Marshal.SizeOf((structure).GetType());
            if (bytes.Length < size)
                throw new Exception("Structure2Bytes Length Error");
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
        }
        public static Velocity CalSpeed(Position position)
        {
            Velocity velocity = new Velocity
            {
                vxy = DenseMatrix.Create(position.x.RowCount, position.x.ColumnCount, 0).ToSingle(),
                vz = DenseMatrix.Create(position.x.RowCount, position.x.ColumnCount, 0).ToSingle(),
                vxyMax = float.MinValue,
                vzMax = float.MinValue,
                vzMin = float.MaxValue,
            };

            for (int qn = 0; qn < position.x.RowCount; qn++)
            {
                for (int fr = 1; fr < position.x.ColumnCount; fr++)
                {
                    float vx = (position.x[qn, fr] - position.x[qn, fr - 1]) / DeltaTime;
                    float vy = (position.y[qn, fr] - position.y[qn, fr - 1]) / DeltaTime;
                    velocity.vxy[qn, fr] = Mathf.Sqrt(vx * vx + vy * vy);
                    velocity.vz[qn, fr] = (position.z[qn, fr] - position.z[qn, fr - 1]) / DeltaTime;

                    if (velocity.vxy[qn, fr] > velocity.vxyMax)
                    {
                        velocity.vxyMax = velocity.vxy[qn, fr];
                    }

                    if (velocity.vz[qn, fr] > velocity.vzMax)
                    {
                        velocity.vzMax = velocity.vz[qn, fr];
                    }

                    if (velocity.vz[qn, fr] < velocity.vzMin)
                    {
                        velocity.vzMin = velocity.vz[qn, fr];
                    }
                }
            }

            return velocity;
        }
        public static float[] CalMinDist(Position position, int totals = 0, int frames = 0)
        {
            if (totals == 0)
            {
                totals = position.x.RowCount;
            }

            if (frames == 0)
            {
                frames = position.x.ColumnCount;
            }

            float[] mindist = new float[frames];
            Parallel.For(0, frames, (fr) =>
            {
                float min = float.MaxValue;

                for (int qi = 0; qi < totals - 1; qi++)
                {
                    for (int qj = qi + 1; qj < totals; qj++)
                    {
                        //降落至地面的飞机不计入计算
                        if (position.z[qi, fr] < 0.01f || position.z[qj, fr] < 0.01f)
                            continue;
                        var foo = SquaredDistance(
                            position.x[qi, fr], position.z[qi, fr], position.y[qi, fr]
                            , position.x[qj, fr], position.z[qj, fr], position.y[qj, fr]
                            );
                        if (foo < min)
                        {
                            min = foo;
                        }
                    }
                }

                mindist[fr] = Mathf.Sqrt(min);
            });

            return mindist;
        }
        // 将字符串写入到文件
        public static void WriteToFile(string filePath, string content)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.Write(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void SaveStartPosition(Position position, string path)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < position.x.RowCount; i++)
            {
                sb.Append(i);
                sb.Append(" ");
                sb.Append(position.x[i, 0].ToString("0.000000"));
                sb.Append(" ");
                sb.Append(position.y[i, 0].ToString("0.000000"));
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        }

        public static float SquaredDistance(float ax, float ay, float az
        , float bx, float by, float bz)
        {
            float num = ax - bx;
            float num2 = ay - by;
            float num3 = az - bz;
            return num * num + num2 * num2 + num3 * num3;
        }

        private static byte GetCrc(byte[] data)
        {
            byte crc = 0;
            for (int i = 0; i < data.Length; i++)
            {
                crc += data[i];
            }
            return crc;
        } 

        #endregion

        #region 类

        public static class DancestepUtil
        {
            public static Position ToPositon(List<Dancestep> danlist)
            {
                var droneNum = danlist[0].x.GetLength(0);
                Position pos = new Position();
                foreach (var item in danlist)
                {
                    var points = item.x.GetLength(1);
                    if (pos.x == null)
                    {
                        pos.x = DenseMatrix.Create(droneNum, points, (row, column) => item.x[row, column]).ToSingle();
                        pos.y = DenseMatrix.Create(droneNum, points, (row, column) => item.y[row, column]).ToSingle();
                        pos.z = DenseMatrix.Create(droneNum, points, (row, column) => item.z[row, column]).ToSingle();
                    }
                    else
                    {
                        pos.x = pos.x.Append(DenseMatrix.Create(droneNum, points, (row, column) => item.x[row, column]).ToSingle());
                        pos.y = pos.y.Append(DenseMatrix.Create(droneNum, points, (row, column) => item.y[row, column]).ToSingle());
                        pos.z = pos.z.Append(DenseMatrix.Create(droneNum, points, (row, column) => item.z[row, column]).ToSingle());
                    }

                }
                return pos;
            }

            public static Lighting Tolighting(List<Dancestep> danlist)
            {
                var droneNum = danlist[0].x.GetLength(0);
                Lighting light = new Lighting();
                foreach (var item in danlist)
                {
                    var points = item.x.GetLength(1);
                    if (light.r == null)
                    {
                        light.r = DenseMatrix.Create(droneNum, points, (row, column) => item.r[row, column]).ToSingle();
                        light.g = DenseMatrix.Create(droneNum, points, (row, column) => item.g[row, column]).ToSingle();
                        light.b = DenseMatrix.Create(droneNum, points, (row, column) => item.b[row, column]).ToSingle();
                    }
                    else
                    {
                        light.r = light.r.Append(DenseMatrix.Create(droneNum, points, (row, column) => item.r[row, column]).ToSingle());
                        light.g = light.g.Append(DenseMatrix.Create(droneNum, points, (row, column) => item.g[row, column]).ToSingle());
                        light.b = light.b.Append(DenseMatrix.Create(droneNum, points, (row, column) => item.b[row, column]).ToSingle());
                    }

                }
                return light;
            }

            public static Turn ToTurn(List<Dancestep> danlist)
            {
                var droneNum = danlist[0].x.GetLength(0);
                Turn trun = new Turn();
                foreach (var item in danlist)
                {
                    var points = item.x.GetLength(1);
                    if (trun.ctr == null)
                    {
                        trun.ctr = DenseMatrix.Create(droneNum, points, (row, column) => item.ctr[row, column]).ToSingle();
                    }
                    else
                    {
                        trun.ctr = trun.ctr.Append(DenseMatrix.Create(droneNum, points, (row, column) => item.ctr[row, column]).ToSingle());
                    }

                }
                return trun;
            }

        }

        public static class MatUtil
        {
            //Packet
            public static void PacketMat(Dictionary<string, Matrix<float>> dict, string directory, string tarName, string fileName)
            {
                directory += "/Mat/";
                var filePath = directory + tarName;

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }

                MatlabWriter.Write(tarName, dict);
            }
            public static void PacketMat(List<Dancestep> dancestepList, string directory, string tarName, string fileName)
            {
                var dict = DancetpToMat(dancestepList);
                PacketMat(dict, directory, tarName, fileName);
            }

            public static Dictionary<string, Matrix<float>> UnpacketMat(string filePath)
            {
                if (!filePath.EndsWith(".mat"))
                {
                    return null;
                }
                if (!File.Exists(filePath))
                {
                    return null;
                }
                return MatlabReader.ReadAll<float>(filePath);
            }

            //tranform
            public static List<Dancestep> MatToDancetp(Dictionary<string, Matrix<float>> dict)
            {
                List<Dancestep> dancestepList = new List<Dancestep>();
                int totals = dict["x"].RowCount;
                int points = dict["x"].ColumnCount;
                for (int i = 0; i < totals; i++)
                {
                    Dancestep dancestep = new Dancestep(totals, points);
                    for (int j = 0; j < points; j++)
                    {
                        dancestep.x[i, j] = dict["x"][i, j];
                        dancestep.y[i, j] = dict["y"][i, j];
                        dancestep.z[i, j] = dict["z"][i, j];
                        dancestep.r[i, j] = (byte)dict["r"][i, j];
                        dancestep.g[i, j] = (byte)dict["g"][i, j];
                        dancestep.b[i, j] = (byte)dict["b"][i, j];
                        dancestep.ctr[i, j] = (byte)dict["ctr"][i, j];
                    }
                    dancestepList.Add(dancestep);
                }
                return dancestepList;
            }
            public static Dictionary<string, Matrix<float>> DancetpToMat(List<Dancestep> dancestepList)
            {
                int totals = dancestepList[0].x.GetLength(0);
                int points = dancestepList[0].x.GetLength(1);
                var dict = new Dictionary<string, Matrix<float>>();
                for (int i = 0; i < dancestepList.Count; i++)
                {
                    if (!dict.ContainsKey("x"))
                    {
                        dict.Add("x", DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].x[row, column]).ToSingle());
                        dict.Add("y", DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].y[row, column]).ToSingle());
                        dict.Add("z", DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].z[row, column]).ToSingle());
                        dict.Add("r", DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].r[row, column]).ToSingle());
                        dict.Add("g", DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].g[row, column]).ToSingle());
                        dict.Add("b", DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].b[row, column]).ToSingle());
                        dict.Add("ctr", DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].ctr[row, column]).ToSingle());
                    }
                    else
                    {
                        dict["x"] = dict["x"].Append(DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].x[row, column]).ToSingle());
                        dict["y"] = dict["y"].Append(DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].y[row, column]).ToSingle());
                        dict["z"] = dict["z"].Append(DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].z[row, column]).ToSingle());
                        dict["r"] = dict["r"].Append(DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].r[row, column]).ToSingle());
                        dict["g"] = dict["g"].Append(DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].g[row, column]).ToSingle());
                        dict["b"] = dict["b"].Append(DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].b[row, column]).ToSingle());
                        dict["ctr"] = dict["ctr"].Append(DenseMatrix.Create(totals, points, (row, column) => dancestepList[i].ctr[row, column]).ToSingle());
                    }
                }
                return dict;
            }
        }

        public static class ZipUtil
        {
            public static string Zip(string directory, string zipName)
            {
                zipName = zipName.Replace(".tgz", ".zip");
                string[] filenames = Directory.GetFiles(directory);
                ZipFile zipFile = ZipFile.Create(zipName);
                zipFile.NameTransform = new ZipNameTransform(directory);
                zipFile.BeginUpdate();
                foreach (string file in filenames)
                {
                    zipFile.Add(file);
                }
                zipFile.CommitUpdate();
                zipFile.Close();

                return zipName;
            }
            public static string Unzip(string targetFolderPath, string zipFilePath)
            {
                using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(zipFilePath)))
                {
                    ZipEntry entry;
                    while ((entry = zipInputStream.GetNextEntry()) != null)
                    {
                        string entryName = entry.Name;
                        string entryFolderPath = Path.Combine(targetFolderPath, Path.GetDirectoryName(entryName));
                        string entryFilePath = Path.Combine(entryFolderPath, Path.GetFileName(entryName));

                        // 如果是文件夹，创建文件夹
                        if (entry.IsDirectory)
                        {
                            Directory.CreateDirectory(entryFolderPath);
                            continue;
                        }

                        // 如果是文件，创建父文件夹并写入文件
                        Directory.CreateDirectory(entryFolderPath);
                        using (FileStream entryFile = File.Create(entryFilePath))
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = zipInputStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                entryFile.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
                return targetFolderPath;
            }
        }

        public static class TgzUtil
        {
            public static string Tgz7za(string exePath, string fileDir, string tgzName)
            {
                System.Diagnostics.ProcessStartInfo startinfo;
                System.Diagnostics.Process process;
                if (!tgzName.EndsWith(".tgz"))
                {
                    return null;
                }
                try
                {
                    StringBuilder cmd = new StringBuilder("a -ttar ");
                    var dir = Path.GetDirectoryName(tgzName);
                    var tarName = Path.ChangeExtension(tgzName, ".tar");
                    string[] files = Directory.GetFiles(fileDir);
                    cmd.Append(tarName);
                    cmd.Append(" ");
                    foreach (string file in files)
                    {
                        cmd.Append(file);
                        cmd.Append(" ");
                    }

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    startinfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = exePath,
                        Arguments = cmd.ToString(),
                    };
                    process = new System.Diagnostics.Process()
                    {
                        StartInfo = startinfo
                    };

                    process.Start();
                    process.WaitForExit();
                    process.Close();

                    cmd = new StringBuilder($"a -tgzip {tarName}.gz {tarName}");

                    startinfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = exePath,
                        Arguments = cmd.ToString(),
                    };
                    process = new System.Diagnostics.Process()
                    {
                        StartInfo = startinfo
                    };

                    process.Start();
                    process.WaitForExit();
                    process.Close();
                    File.Move(tarName + ".gz", tgzName);
                    File.Delete(tarName);
                    return tgzName;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            public static string Tgz(string directory, string tarName)
            {
                string[] files = Directory.GetFiles(directory);
                Stream targetStream = new GZipOutputStream(File.Create(tarName));
                TarArchive tarArchive = TarArchive.CreateOutputTarArchive(targetStream, TarBuffer.DefaultBlockFactor);
                tarArchive.RootPath = directory;

                foreach (string file in files)
                {
                    TarEntry entry = TarEntry.CreateEntryFromFile(file);
                    tarArchive.WriteEntry(entry, false);
                }
                targetStream.Flush();
                targetStream.Close();
                return tarName;
            }

            public static string UnTgz(string directory, string untarName)
            {
                FileInfo tarFileInfo = new FileInfo(untarName);
                DirectoryInfo targetDirectory = new DirectoryInfo(directory);
                if (!targetDirectory.Exists)
                {
                    targetDirectory.Create();
                }
                using (Stream sourceStream = new GZipInputStream(tarFileInfo.OpenRead()))
                {
                    using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(sourceStream, TarBuffer.DefaultBlockFactor))
                    {
                        tarArchive.ExtractContents(targetDirectory.FullName);
                    }
                }
                return directory;
            }
        }

        public struct Position
        {
            public Matrix<float> x;
            public Matrix<float> y;
            public Matrix<float> z;
        };

        public struct Lighting
        {
            public Matrix<float> r;
            public Matrix<float> g;
            public Matrix<float> b;
            public Matrix<float> w;
        };
        public struct Turn
        {
            public Matrix<float> ctr;
        };
        [StructLayout(LayoutKind.Sequential)]
        public class HeaderV200
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] version;
            public byte qn;
            public float time;
            public float frameRate;
            public float total;
            public float pars;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public float[] range;
        }
        [StructLayout(LayoutKind.Sequential)]
        public class PayloadV200
        {
            public short x;
            public short y;
            public short z;
            public byte r;
            public byte g;
            public byte b;
            public byte ctr;
            public short yaw;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class B03HeaderV200
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] version;
            public UInt16 qn;
            public float time;
            public float frameRate;
            public float total;
            public float pars;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public float[] range;
            public byte headCrc;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class B03PayloadV200
        {
            public byte stx;
            public float x;
            public float y;
            public float z;
            public byte r;
            public byte g;
            public byte b;
            public byte w;
            public byte light_ctr;
            public byte mode_ctr;
            public byte reserve0;
            public byte reserve1;
            public byte stepCrc;
        }
        [StructLayout(LayoutKind.Sequential)]
        public class AppJson
        {
            public short danceType;
            public short dronenum;
            public int duration;
            public string fileId;
            public ushort id;
            public float max_height;
            public string name;
            public short type;
        }
        //旧版
        //public class AppJson
        //{
        //    public ushort id;
        //    public string fileId;
        //    public short dronenum;
        //    public int duration;
        //    public string versionCode;
        //    public string name;
        //    public string nameEn;
        //    public string nameTw;
        //    public string nameKr;
        //    public short danceType;
        //    public short bookType;
        //    public float max_height;
        //} 
        #endregion
    }
}
 