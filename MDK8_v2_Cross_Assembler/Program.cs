using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MDK8_v2_Cross_Assembler
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

namespace Cross_Assy
{
    public static class Program
    {
        public static string jump_N;
        public static string jump_A;
        public static string BY_NUM;

        //変換部実装

        //コンパイラ内指定文字列
        /*
         *JUMPPOINT NAME → NAMEにJUMPラベルを指定
         * 
         * regA
         * regB
         * regC
         * 
         * REG1
         * REG2
         * REG3
         * REG4
         * 
         * ROM
         * 
         * IN1
         * IN2
         * 
         * OUT1
         * OUT2
         * OUT1&2
         * 
         * JUMP 指定文字
         * FGLD : フラグ値ロード
         * JALD : 上位8bitに値をロード
         * JPLD
         * NOT
         * SL
         * SR
         * CR
         * 
         * LD REGBに上位ジャンプADDRESS REGCに下位ADDRESS
         * QD REGB REGCに登録された値にジャンプ
         */
        public static byte[] op_Cross_Asse(string code_ASSEM)
        {
            byte[] binary_code = { 0xFF };

            int code_count = 0;
            List<string> jump_List = new List<string>();
            List<string> jump_ADDRESS = new List<string>();
            List<string> BK_jump_List = new List<string>();
            List<int> BK_count_L = new List<int>();
            List<int> BK_count_H = new List<int>();
            List<int> BK_Page_count = new List<int>();


            System.IO.StringReader rs = new System.IO.StringReader(code_ASSEM);

            //ストリームの末端まで繰り返す
            while (rs.Peek() > -1)
            {
                code_count++;
                //一行読み込んで処理
                string rsa = rs.ReadLine();
                //先頭の空白文字を削除する
                rsa = rsa.TrimStart();
                //末尾の空白文字を削除する
                rsa = rsa.TrimEnd();

                string[] arry = rsa.Split(' ');
                if (arry[0] == "") continue;
                if (arry[0].ToUpper().Contains("//")) continue;


                byte AC_data = 0;
                int sel_input = 0;
                int input_in_sel = 0;
                int sel_output_reg = 0;
                int hex_add = 0;
                bool set_JUMP_BK = false;

                switch (arry[0].ToUpper())
                {
                    case "JP:":
                        if (arry.Length < 2)
                        {
                            return System.Text.Encoding.UTF8.GetBytes("Error 701 JUMPPOINTの名前指定がありません" + code_count.ToString() + "行目");
                        }

                        if (jump_List.Contains(arry[1])) //リスト内検索
                        {
                            return System.Text.Encoding.UTF8.GetBytes("Error 702 JUMPPOINT 同一名のポイント名があります" + code_count.ToString() + "行目");
                        }
                        else
                        {
                            jump_List.Add(arry[1]);
                            if (((binary_code.Length / 2).ToString("X")).Length < 2)
                            {
                                jump_ADDRESS.Add("0X0" + (binary_code.Length / 2).ToString("X"));
                            }
                            else
                            {
                                jump_ADDRESS.Add("0X" + (binary_code.Length / 2).ToString("X"));
                            }
                        }
                        break;

                    case "JUMP":
                        if (arry.Length < 1)
                        {
                            return System.Text.Encoding.UTF8.GetBytes("Error 809 JUMP指定要素が足りていません" + code_count.ToString() + "行目");
                        }
                        if (arry[1].ToUpper() == "FGLD")//フラグロード
                        {
                            if (arry.Length < 2)
                            {
                                return System.Text.Encoding.UTF8.GetBytes("Error 810 FGLD 要素が多すぎます" + code_count.ToString() + "行目");
                            }
                            binary_code = Enumerable.Concat(binary_code, op_FGLD(0X00)).ToArray();
                            break;
                        }
                        if (arry.Length < 3)
                        {
                            return System.Text.Encoding.UTF8.GetBytes("Error 801 JUMP指定要素が足りていません" + code_count.ToString() + "行目");
                        }

                        switch (arry[2].ToUpper())//ロード要素
                        {
                            case "REGA":
                                sel_input = 0;
                                break;
                            case "REGB":
                                sel_input = 1;
                                break;
                            case "REGC":
                                sel_input = 2;
                                break;
                            case "JP:":
                                sel_input = 3;
                                break;
                            case "LD:":
                                sel_input = 3;
                                break;
                            case "QD:":
                                sel_input = 2;
                                break;

                            default:
                                if (arry[1].ToUpper() == "JP:" || arry[1].ToUpper() == "LD:" || arry[1].ToUpper() == "QD:")
                                {
                                    sel_input = 3;
                                    if(arry[1].ToUpper() == "QD:")
                                    {
                                        sel_input = 2;
                                    }
                                    Array.Resize(ref arry, arry.Length + 1);
                                    arry[3] = arry[2];
                                    arry[2] = arry[1];
                                    arry[1] = "0";
                                }
                                else
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 803 JUMP MODE 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }
                                break;
                        }

                        if(sel_input == 3)
                        {
                            if (arry.Length < 4)
                            {
                                return System.Text.Encoding.UTF8.GetBytes("Error 805 JUMP ROMデータ指定がありません" + code_count.ToString() + "行目");
                            }
                            if (arry.Length > 4)
                            {
                                return System.Text.Encoding.UTF8.GetBytes("Error 804 JUMP 指定要素が多すぎます" + code_count.ToString() + "行目");
                            }

                            //LD REGB REGCにジャンプ元アドレス設定
                            if (arry[2].ToUpper() == "LD:")
                            {
                                if (((binary_code.Length / 2) + 5) > 255) //実行アドレスが1byte以上
                                {
                                    //REGBに上位ADDRESS代入
                                    binary_code = Enumerable.Concat(binary_code, op_LDRG(7 ,0 ,2 , (byte)(((binary_code.Length / 2) + 5) >> 8))).ToArray();
                                }
                                else
                                {
                                    //REGBに0x00を代入
                                    binary_code = Enumerable.Concat(binary_code, op_LDRG(7, 0, 2, 0x00)).ToArray();
                                }
                                //REGCに下位ADDRESS代入
                                binary_code = Enumerable.Concat(binary_code, op_LDRG(7, 0, 3, (byte)(((binary_code.Length) / 2) + 5))).ToArray();
                            }

                            //JUMP要素検索
                            if (jump_List.Contains(arry[3]))
                            {
                                hex_add = Convert.ToInt32(jump_ADDRESS[jump_List.IndexOf(arry[3])], 16);
                                if(hex_add > 255) //実行アドレスが1byte以上
                                {
                                    hex_add = hex_add >> 8;
                                    binary_code = Enumerable.Concat(binary_code, op_JALD(3 ,(byte)hex_add)).ToArray();
                                }
                                else
                                {
                                    binary_code = Enumerable.Concat(binary_code, op_JALD(3, 0x00)).ToArray();
                                }

                                AC_data = (byte)Convert.ToInt32(jump_ADDRESS[jump_List.IndexOf(arry[3])], 16);
                            }
                            else
                            {
                                //予約として0X00を登録
                                binary_code = Enumerable.Concat(binary_code, op_JALD(3, 0X00)).ToArray();
                                AC_data = (byte)Convert.ToInt32("0X00", 16);
                                BK_jump_List.Add(arry[3]);
                                BK_count_H.Add(binary_code.Length);
                                BK_Page_count.Add(code_count);
                                set_JUMP_BK = true;
                            }
                        }

                        switch (arry[1].ToUpper())
                        {
                            case "!!":
                                binary_code = Enumerable.Concat(binary_code, op_JPAD(sel_input, AC_data)).ToArray();
                                break;

                            case "<<":
                                binary_code = Enumerable.Concat(binary_code, op_JPSL(sel_input, AC_data)).ToArray();
                                break;

                            case ">>":
                                binary_code = Enumerable.Concat(binary_code, op_JPSR(sel_input, AC_data)).ToArray();
                                break;

                            case "++":
                                binary_code = Enumerable.Concat(binary_code, op_JPCR(sel_input, AC_data)).ToArray();
                                break;

                            default:
                                if (sel_input == 3)
                                {
                                    binary_code = Enumerable.Concat(binary_code, op_JPLD(sel_input, AC_data)).ToArray();
                                }
                                else if (sel_input == 2 && arry[2].ToUpper() == "QD:" && arry[3].ToUpper() == "RETURN")
                                {
                                    //REGBとREGCの値にジャンプ
                                    binary_code = Enumerable.Concat(binary_code, op_JALD(1, 0x00)).ToArray();
                                    binary_code = Enumerable.Concat(binary_code, op_JPLD(2, 0x00)).ToArray();
                                }
                                else 
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 802 JUMP指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }
                                break;
                        }

                        if (set_JUMP_BK)
                        {
                            BK_count_L.Add(binary_code.Length);
                            set_JUMP_BK = false;
                        }
                        break;

                    case "NOP" :
                        if (arry.Length > 1)
                        {
                            if (arry[1].ToUpper().Contains("0X"))
                            {
                                try
                                {
                                    AC_data = (byte)Convert.ToInt32(arry[1].ToUpper(), 16);
                                }
                                catch (FormatException e)
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message +" "+ code_count.ToString() + "行目");//データ文字列が数字以外
                                }
                            }
                            else if (arry[1].ToUpper().Contains("0B"))
                            {
                                arry[1] = arry[1].ToUpper().Replace("0B","");
                                try {
                                AC_data = (byte)Convert.ToInt32(arry[1].ToUpper(), 2);
                                }
                                catch (FormatException e)
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                }
                            }
                            else
                            {
                                if (!arry[1].ToUpper().All(char.IsDigit))
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 501 データ文字列が数字以外" + code_count.ToString() +"行目");//データ文字列が数字以外
                                }
                                try {
                                AC_data = (byte)int.Parse(arry[1].ToUpper());
                                }
                                catch (FormatException e)
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                }
                            }
                            binary_code = Enumerable.Concat(binary_code, op_NKBT(AC_data)).ToArray();
                        }
                        else
                        {
                            binary_code = Enumerable.Concat(binary_code, op_NKBT(0x00)).ToArray();
                        }
                        break;

                    case "LDRG":
                        if (arry.Length < 3)
                        {
                            return System.Text.Encoding.UTF8.GetBytes("Error 601 LDGR 指定要素が少なすぎます" + code_count.ToString() + "行目");//LDGR 指定要素が少なすぎます
                        }

                        switch (arry[1].ToUpper()) 
                        {
                            case "REGA":
                                sel_input = 0;
                                input_in_sel = 0;
                                break;
                            case "REGB":
                                sel_input = 1;
                                input_in_sel = 0;
                                break;
                            case "REGC":
                                sel_input = 2;
                                input_in_sel = 0;
                                break;
                            case "REG1":
                                sel_input = 3;
                                input_in_sel = 0;
                                break;
                            case "REG2":
                                sel_input = 4;
                                input_in_sel = 0;
                                break;
                            case "REG3":
                                sel_input = 5;
                                input_in_sel = 0;
                                break;
                            case "REG4":
                                sel_input = 6;
                                input_in_sel = 0;
                                break;
                            case "ROM":
                                sel_input = 7;
                                input_in_sel = 0;
                                break;

                            case "IN1":
                                input_in_sel = 1;
                                break;
                            case "IN2":
                                input_in_sel = 2;
                                break;

                            default:
                                return System.Text.Encoding.UTF8.GetBytes("Error 603 LDRG指定文字以外が指定されました" + code_count.ToString() + "行目");
                        }

                        switch (arry[2].ToUpper())
                        {
                            case "REGA":
                                sel_output_reg = 1;
                                break;
                            case "REGB":
                                sel_output_reg = 2;
                                break;
                            case "REGC":
                                sel_output_reg = 3;
                                break;
                            case "REG1":
                                sel_output_reg = 4;
                                break;
                            case "REG2":
                                sel_output_reg = 5;
                                break;
                            case "REG3":
                                sel_output_reg = 6;
                                break;
                            case "REG4":
                                sel_output_reg = 7;
                                break;
                            default:
                                return System.Text.Encoding.UTF8.GetBytes("Error 604 LDRG指定文字以外が指定されました" + code_count.ToString() + "行目");
                        }

                        if(sel_input == 7)
                        {
                            if (arry.Length < 4)
                            {
                                return System.Text.Encoding.UTF8.GetBytes("Error 605 LDGR ROMデータ指定がありません" + code_count.ToString() + "行目");
                            }
                            if (arry.Length > 4)
                            {
                                return System.Text.Encoding.UTF8.GetBytes("Error 606 LDGR 指定要素が多すぎます" + code_count.ToString() + "行目");//LDGR 指定要素が多すぎます
                            }

                            if (arry[3].ToUpper().Contains("0X"))
                            {
                                try {
                                AC_data = (byte)Convert.ToInt32(arry[3].ToUpper(), 16);
                                }
                                catch (FormatException e)
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                }
                            }
                            else if (arry[3].ToUpper().Contains("0B"))
                            {
                                arry[3] = arry[3].ToUpper().Replace("0B", "");
                                try {
                                AC_data = (byte)Convert.ToInt32(arry[3].ToUpper(), 2);
                                }
                                catch (FormatException e)
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                }
                            }
                            else
                            {
                                if (!arry[3].ToUpper().All(char.IsDigit))
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 607 データ文字列が数字以外" + code_count.ToString() + "行目");//データ文字列が数字以外
                                }
                                try {
                                AC_data = (byte)int.Parse(arry[3].ToUpper());
                                }
                                catch (FormatException e)
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                }
                            }
                        }
                        else if (arry.Length > 3)
                        {
                            return System.Text.Encoding.UTF8.GetBytes("Error 602 LDGR 指定要素が多すぎます" + code_count.ToString() + "行目");//LDGR 指定要素が多すぎます
                        }

                        binary_code = Enumerable.Concat(binary_code, op_LDRG(sel_input , input_in_sel , sel_output_reg , AC_data)).ToArray();
                        break;

                    default:
                        if (arry.Length >= 4)
                        {
                            if (arry[1].ToUpper() == "=")//演算命令か?
                            {
                                if (arry.Length < 4 && arry[3].ToUpper() != "!")
                                {
                                    return System.Text.Encoding.UTF8.GetBytes("Error 1001 演算命令 指定要素が少なすぎます" + code_count.ToString() + "行目");//
                                }

                                switch (arry[2].ToUpper())
                                {
                                    case "REGA":
                                        sel_input = 0;
                                        break;

                                    case "REGB":
                                        sel_input = 1;
                                        break;

                                    case "REGC":
                                        sel_input = 2;
                                        break;

                                    case "IN1":
                                        sel_input = 3;
                                        break;

                                    default:
                                        return System.Text.Encoding.UTF8.GetBytes("Error 1002 第1指定 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }
                                if (arry[3].ToUpper() != "!") 
                                {
                                    switch (arry[4].ToUpper())
                                    {
                                        case "REG1":
                                            if (sel_input != 0)
                                            {
                                                return System.Text.Encoding.UTF8.GetBytes("Error 1003 CPUで処理できないレジスタの組み合わせです" + code_count.ToString() + "行目");
                                            }
                                            break;

                                        case "REG2":
                                            if (sel_input != 1)
                                            {
                                                return System.Text.Encoding.UTF8.GetBytes("Error 1003 CPUで処理できないレジスタの組み合わせです" + code_count.ToString() + "行目");
                                            }
                                            break;

                                        case "REG3":
                                            if (sel_input != 2)
                                            {
                                                return System.Text.Encoding.UTF8.GetBytes("Error 1003 CPUで処理できないレジスタの組み合わせです" + code_count.ToString() + "行目");
                                            }
                                            break;

                                        case "REG4":
                                            if (sel_input != 3)
                                            {
                                                return System.Text.Encoding.UTF8.GetBytes("Error 1003 CPUで処理できない入力&レジスタの組み合わせです" + code_count.ToString() + "行目");
                                            }
                                            break;

                                        default:
                                            return System.Text.Encoding.UTF8.GetBytes("Error 1004 第2指定 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                    }
                                }
                                switch (arry[3].ToUpper())
                                {
                                    case "+":
                                        input_in_sel = 0;
                                        break;

                                    case "&":
                                        input_in_sel = 1;
                                        break;

                                    case "|":
                                        input_in_sel = 2;
                                        break;

                                    case "!":
                                        input_in_sel = 3;
                                        break;

                                    default:
                                        return System.Text.Encoding.UTF8.GetBytes("Error 1002 第1指定 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }
                                switch (arry[0].ToUpper())
                                {
                                    case "0":
                                        sel_output_reg = 0;
                                        break;
                                    case "REGA":
                                        sel_output_reg = 1;
                                        break;
                                    case "REGB":
                                        sel_output_reg = 2;
                                        break;
                                    case "REGC":
                                        sel_output_reg = 3;
                                        break;
                                    case "REG1":
                                        sel_output_reg = 4;
                                        break;
                                    case "REG2":
                                        sel_output_reg = 5;
                                        break;
                                    case "REG3":
                                        sel_output_reg = 6;
                                        break;
                                    case "REG4":
                                        sel_output_reg = 7;
                                        break;
                                    default:
                                        return System.Text.Encoding.UTF8.GetBytes("Error 1004 出力レジスタが指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }
                                binary_code = Enumerable.Concat(binary_code, op_OPIM(sel_input, sel_output_reg, input_in_sel, AC_data)).ToArray();
                            }
                        }
                        else if(arry.Length > 2)
                        {
                            if (arry[1].ToUpper() == "=")//演算命令か?
                            {
                                sel_output_reg = 0;
                                switch (arry[0].ToUpper())
                                {
                                    case "OUT1":
                                        sel_output_reg = 1;
                                        break;

                                    case "OUT2":
                                        sel_output_reg = 2;
                                        break;

                                    case "OUT1&2":
                                        sel_output_reg = 3;
                                        break;
                                }
                                if (sel_output_reg != 0)
                                {
                                    switch (arry[2].ToUpper())
                                    {
                                        case "REGA":
                                            sel_input = 0;
                                            input_in_sel = 0;
                                            break;

                                        case "REGB":
                                            sel_input = 1;
                                            input_in_sel = 0;
                                            break;

                                        case "REGC":
                                            sel_input = 2;
                                            input_in_sel = 0;
                                            break;

                                        case "IN1":
                                            sel_input = 3;
                                            input_in_sel = 0;
                                            break;

                                        default:
                                            if (arry[2].ToUpper().Contains("0X"))
                                            {
                                                try {
                                                AC_data = (byte)Convert.ToInt32(arry[2].ToUpper(), 16);
                                                }
                                                catch (FormatException e)
                                                {
                                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                                }
                                                input_in_sel = 1;
                                            }
                                            else if (arry[2].ToUpper().Contains("0B"))
                                            {
                                                arry[2] = arry[2].ToUpper().Replace("0B", "");
                                                try {
                                                AC_data = (byte)Convert.ToInt32(arry[2].ToUpper(), 2);
                                                }
                                                catch (FormatException e)
                                                {
                                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                                }
                                                input_in_sel = 1;
                                            }
                                            else if(arry[2].ToUpper().All(char.IsDigit))
                                            {
                                                try {
                                                AC_data = (byte)int.Parse(arry[2].ToUpper());
                                                }
                                                catch (FormatException e)
                                                {
                                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                                }
                                                input_in_sel = 1;
                                            }
                                            else 
                                            {
                                                return System.Text.Encoding.UTF8.GetBytes("Error 2002 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                            }
                                            break;
                                    }
                                    binary_code = Enumerable.Concat(binary_code, op_LOUT(sel_input , input_in_sel , sel_output_reg , AC_data)).ToArray();
                                }
                                else
                                {
                                    switch (arry[0].ToUpper())
                                    {
                                        case "REGA":
                                            sel_output_reg = 1;
                                            break;

                                        case "REGB":
                                            sel_output_reg = 2;
                                            break;

                                        case "REGC":
                                            sel_output_reg = 3;
                                            break;

                                        case "REG1":
                                            sel_output_reg = 4;
                                            break;

                                        case "REG2":
                                            sel_output_reg = 5;
                                            break;

                                        case "REG3":
                                            sel_output_reg = 6;
                                            break;

                                        case "REG4":
                                            sel_output_reg = 7;
                                            break;

                                        default:
                                            return System.Text.Encoding.UTF8.GetBytes("Error 102 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                    }
                                    switch (arry[2].ToUpper())
                                    {
                                        case "REGA":
                                            sel_input = 0;
                                            input_in_sel = 0;
                                            break;
                                        case "REGB":
                                            sel_input = 1;
                                            input_in_sel = 0;
                                            break;
                                        case "REGC":
                                            sel_input = 2;
                                            input_in_sel = 0;
                                            break;
                                        case "REG1":
                                            sel_input = 3;
                                            input_in_sel = 0;
                                            break;
                                        case "REG2":
                                            sel_input = 4;
                                            input_in_sel = 0;
                                            break;
                                        case "REG3":
                                            sel_input = 5;
                                            input_in_sel = 0;
                                            break;
                                        case "REG4":
                                            sel_input = 6;
                                            input_in_sel = 0;
                                            break;

                                        case "IN1":
                                            input_in_sel = 1;
                                            break;
                                        case "IN2":
                                            input_in_sel = 2;
                                            break;

                                        default:
                                            if (arry[2].ToUpper().Contains("0X"))
                                            {
                                                try {
                                                AC_data = (byte)Convert.ToInt32(arry[2].ToUpper(), 16);
                                                }
                                                catch (FormatException e)
                                                {
                                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                                }
                                                sel_input = 7;
                                            }
                                            else if (arry[2].ToUpper().Contains("0B"))
                                            {
                                                arry[2] = arry[2].ToUpper().Replace("0B", "");
                                                try {
                                                AC_data = (byte)Convert.ToInt32(arry[2].ToUpper(), 2);
                                                }
                                                catch (FormatException e)
                                                {
                                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                                }
                                                sel_input = 7;
                                            }
                                            else if (arry[2].ToUpper().All(char.IsDigit))
                                            {
                                                try {
                                                AC_data = (byte)int.Parse(arry[2].ToUpper());
                                                }
                                                catch (FormatException e)
                                                {
                                                    return System.Text.Encoding.UTF8.GetBytes("Error 522 " + e.Message + " " + code_count.ToString() + "行目");//データ文字列が数字以外
                                                }
                                                sel_input = 7;
                                            }
                                            else
                                            {
                                                System.Text.Encoding.UTF8.GetBytes("Error 603 LDRG指定文字以外が指定されました" + code_count.ToString() + "行目");
                                            }
                                            break;
                                    }
                                    binary_code = Enumerable.Concat(binary_code, op_LDRG(sel_input, input_in_sel, sel_output_reg, AC_data)).ToArray();
                                }
                            }
                            else if(arry[1].ToUpper() == "=<<")
                            {
                                switch (arry[2].ToUpper())
                                {
                                    case "REG1":
                                        sel_input = 0;
                                        break;

                                    case "REG2":
                                        sel_input = 1;
                                        break;

                                    case "REG3":
                                        sel_input = 2;
                                        break;

                                    case "REG4":
                                        sel_input = 3;
                                        break;

                                    default:
                                        return System.Text.Encoding.UTF8.GetBytes("Error 3002 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }
                                switch (arry[0].ToUpper())
                                {
                                    case "0":
                                        sel_output_reg = 0;
                                        break;

                                    case "REGA":
                                        sel_output_reg = 1;
                                        break;

                                    case "REGB":
                                        sel_output_reg = 2;
                                        break;

                                    case "REGC":
                                        sel_output_reg = 3;
                                        break;

                                    case "REG1":
                                        sel_output_reg = 4;
                                        break;

                                    case "REG2":
                                        sel_output_reg = 5;
                                        break;

                                    case "REG3":
                                        sel_output_reg = 6;
                                        break;

                                    case "REG4":
                                        sel_output_reg = 7;
                                        break;

                                    default:
                                        return System.Text.Encoding.UTF8.GetBytes("Error 3005 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }

                                binary_code = Enumerable.Concat(binary_code, op_SIFT(1 , sel_input , sel_output_reg , AC_data)).ToArray();
                            }
                            else if (arry[1].ToUpper() == "=>>")
                            {
                                switch (arry[2].ToUpper())
                                {
                                    case "REG1":
                                        sel_input = 0;
                                        break;

                                    case "REG2":
                                        sel_input = 1;
                                        break;

                                    case "REG3":
                                        sel_input = 2;
                                        break;

                                    case "REG4":
                                        sel_input = 3;
                                        break;

                                    default:
                                        return System.Text.Encoding.UTF8.GetBytes("Error 3002 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }
                                switch (arry[0].ToUpper())
                                {
                                    case "0":
                                        sel_output_reg = 0;
                                        break;

                                    case "REGA":
                                        sel_output_reg = 1;
                                        break;

                                    case "REGB":
                                        sel_output_reg = 2;
                                        break;

                                    case "REGC":
                                        sel_output_reg = 3;
                                        break;

                                    case "REG1":
                                        sel_output_reg = 4;
                                        break;

                                    case "REG2":
                                        sel_output_reg = 5;
                                        break;

                                    case "REG3":
                                        sel_output_reg = 6;
                                        break;

                                    case "REG4":
                                        sel_output_reg = 7;
                                        break;

                                    default:
                                        return System.Text.Encoding.UTF8.GetBytes("Error 3005 指定文字以外が指定されました" + code_count.ToString() + "行目");
                                }

                                binary_code = Enumerable.Concat(binary_code, op_SIFT(0, sel_input, sel_output_reg, AC_data)).ToArray();
                            }
                            else
                            {
                                return System.Text.Encoding.UTF8.GetBytes("Error 102 指定文字以外が指定されました" + code_count.ToString() + "行目");
                            }
                        }
                        else
                        {
                            return System.Text.Encoding.UTF8.GetBytes("Error 102 指定文字以外が指定されました" + code_count.ToString() + "行目");
                        }
                        break;
                }


            }

            if(BK_jump_List.Count != 0)
            {
                for (int i_C = 0; BK_jump_List.Count > i_C; i_C++)
                {
                    if (jump_List.Contains(BK_jump_List[i_C])) //リスト内検索
                    {
                        if (Convert.ToInt32(jump_ADDRESS[jump_List.IndexOf(BK_jump_List[i_C])], 16) > 255) //実行アドレスが1byte以上
                        {
                            binary_code[BK_count_H[i_C] - 1] = (byte)(Convert.ToInt32(jump_ADDRESS[jump_List.IndexOf(BK_jump_List[i_C])], 16) >> 8);
                        }
                        binary_code[BK_count_L[i_C] - 1] = (byte)(Convert.ToInt32(jump_ADDRESS[jump_List.IndexOf(BK_jump_List[i_C])], 16) & 0XFF);
                    }
                    else
                    {
                        return System.Text.Encoding.UTF8.GetBytes("Error 8140 JUMP 予約 指定先ラベルが存在しません" + BK_Page_count[i_C].ToString() + "行目");
                    }
                }
            }

            binary_code = Enumerable.Concat(binary_code, op_FGLD(0X00)).ToArray();//最終ループ処理
            int com_add = binary_code.Length / 2;
            if (com_add > 255) //実行アドレスが1byte以上
            {
                com_add = com_add >> 8;
                binary_code = Enumerable.Concat(binary_code, op_JALD(3, (byte)com_add)).ToArray();
            }
            else
            {
                binary_code = Enumerable.Concat(binary_code, op_JALD(3, 0x00)).ToArray();
            }
            binary_code = Enumerable.Concat(binary_code, op_JPLD(3, (byte)(binary_code.Length / 2))).ToArray();

            for (int count_HEX = 0; binary_code.Length -1 > count_HEX ; count_HEX++)
            {
                binary_code[count_HEX] = binary_code[count_HEX + 1];
            }
            Array.Resize(ref binary_code, binary_code.Length - 1);

            jump_N = string.Join("\r\n", jump_List.ToArray());
            jump_A = string.Join("\r\n", jump_ADDRESS.ToArray());
            BY_NUM = ((binary_code.Length / 2) - 1).ToString();
            return binary_code;
        }


        //命令関係集合
        //特殊命令関係
        //MC_data : 意味のないデータ
        static byte[] op_NKBT(byte MC_data) //NOP命令実行
        {
            byte[] cross_data = new byte[2];

            cross_data[0] = 0x00; //NKBT
            cross_data[1] = MC_data;
            return cross_data;
        }

        //MC_data : 意味のないデータ
        static byte[] op_OPAB(byte MC_data) //OPAB命令実行
        {
            byte[] cross_data = new byte[2];

            cross_data[0] = 0x98; //OPAB
            cross_data[1] = MC_data;
            return cross_data;
        }

        //MC_data : 意味のないデータ
        static byte[] op_OPBA(byte MC_data) //OPBA命令実行
        {
            byte[] cross_data = new byte[2];

            cross_data[0] = 0x00; //OPBA
            cross_data[1] = MC_data;
            return cross_data;
        }

        //ジャンプ命令関係

        //MC_data : 意味のないデータ
        static byte[] op_FGLD(byte MC_data) //FGLD命令実行
        {
            byte[] cross_data = new byte[2];

            cross_data[0] = 0x06; //FGLD
            cross_data[1] = MC_data;
            return cross_data;
        }

        /*
         * sel_data : レジスタ指定用
         * 0 → Areg
         * 1 → Breg
         * 2 → Creg
         * 3 → ROM
         * 
         * AD_data : アドレス格納データ指定先がROMの時に使う
        */

        static byte[] op_JALD(int sel_data, byte AD_data) //JALD命令実行
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (sel_data <= 1)
            {
                Upper_bits = 0;
                if (sel_data == 1)
                {
                    Lower_bits = 8;
                }
            }
            else
            {
                Upper_bits = 1;
                if (sel_data == 3)
                {
                    Lower_bits = 8;
                }
            }

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            Lower_bits = (byte)((int)Lower_bits | 0x07); //JALD
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = AD_data;
            return cross_data;
        }

        static byte[] op_JPLD(int sel_data, byte AD_data) //JPLD命令実行
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (sel_data <= 1)
            {
                Upper_bits = 0;
                if (sel_data == 1)
                {
                    Lower_bits = 8;
                }
            }
            else
            {
                Upper_bits = 1;
                if (sel_data == 3)
                {
                    Lower_bits = 8;
                }
            }

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            Lower_bits = (byte)((int)Lower_bits | 0x05); //JPLD
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = AD_data;
            return cross_data;
        }

        static byte[] op_JPAD(int sel_data, byte AD_data) //JPAD命令実行
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (sel_data <= 1)
            {
                Upper_bits = 0;
                if (sel_data == 1)
                {
                    Lower_bits = 8;
                }
            }
            else
            {
                Upper_bits = 1;
                if (sel_data == 3)
                {
                    Lower_bits = 8;
                }
            }

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            Lower_bits = (byte)((int)Lower_bits | 0x01); //JPAD
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = AD_data;
            return cross_data;
        }

        static byte[] op_JPSL(int sel_data, byte AD_data) //JPSL命令実行
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (sel_data <= 1)
            {
                Upper_bits = 0;
                if (sel_data == 1)
                {
                    Lower_bits = 8;
                }
            }
            else
            {
                Upper_bits = 1;
                if (sel_data == 3)
                {
                    Lower_bits = 8;
                }
            }

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            Lower_bits = (byte)((int)Lower_bits | 0x03); //JPSL
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = AD_data;
            return cross_data;
        }

        static byte[] op_JPSR(int sel_data, byte AD_data) //JPSR命令実行
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (sel_data <= 1)
            {
                Upper_bits = 0;
                if (sel_data == 1)
                {
                    Lower_bits = 8;
                }
            }
            else
            {
                Upper_bits = 1;
                if (sel_data == 3)
                {
                    Lower_bits = 8;
                }
            }

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            Lower_bits = (byte)((int)Lower_bits | 0x02); //JPSR
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = AD_data;
            return cross_data;
        }

        static byte[] op_JPCR(int sel_data, byte AD_data) //JPCR命令実行
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (sel_data <= 1)
            {
                Upper_bits = 0;
                if (sel_data == 1)
                {
                    Lower_bits = 8;
                }
            }
            else
            {
                Upper_bits = 1;
                if (sel_data == 3)
                {
                    Lower_bits = 8;
                }
            }

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            Lower_bits = (byte)((int)Lower_bits | 0x04); //JPCR
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = AD_data;
            return cross_data;
        }

        //演算命令

        /*
         * sel_input_data : 演算レジスタ組み合わせ指定用
         * 0 → Areg OPIM REG1
         * 1 → Breg OPIM REG2
         * 2 → Creg OPIM REG3
         * 3 → IN1  OPIM REG4
         * 
         * sel_input_data : 演算結果保存レジスタ指定用
         * 0 → NONE
         * 1 → Areg
         * 2 → Breg
         * 3 → Creg
         * 4 → REG1
         * 5 → REG2
         * 6 → REG3
         * 7 → REG4
         * 
         * sel_op_im : 演算種類指定用
         * 0 → ADD
         * 1 → AND
         * 2 → OR
         * 3 → NOT
         * 
         * MC_data : 意味のないデータ
        */
        static byte[] op_OPIM(int sel_input_data, int sel_output_data, int sel_op_im, byte MC_data) //OPIM命令実行
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (sel_input_data <= 1)
            {
                if (sel_input_data == 1)
                {
                    Upper_bits = 4;
                }
                else
                {
                    Upper_bits = 0;
                }
            }
            else
            {
                if (sel_input_data == 3)
                {
                    Upper_bits = 0x0C;
                }
                else
                {
                    Upper_bits = 8;
                }
            }
            Upper_bits = (byte)((int)Upper_bits | 2); //OPIM

            if (sel_op_im <= 1)
            {
                if (sel_op_im == 1)
                {
                    Lower_bits = 8;
                }
                else
                {
                    Lower_bits = 0;
                }
            }
            else
            {
                Upper_bits = (byte)((int)Upper_bits | 1);
                if (sel_op_im == 3)
                {
                    Lower_bits = 8;
                }
            }

            sel_output_data = sel_output_data & 7;
            Lower_bits = (byte)((int)Lower_bits | sel_output_data);

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = MC_data;
            return cross_data;
        }

        //OUTPUT関連命令

        /*
         * sel_reg : 入力データselect
         * 0 → Areg
         * 1 → Breg
         * 2 → Creg
         * 3 → IN1
         * 
         * sel_ROM : 指定先をROMへ
         * 0 → Xreg sel_regで指定した値へ
         * 1 → ROM
         * 
         * sel_output_porte : アウトプット先へ
         * 0 → 追加命令用コード ふつうは指定しない
         * 1 → OUT1
         * 2 → OUT2
         * 3 → PUT1 & OUT2
         * 
         * ROM_data : ROM選択時に使用
        */
        static byte[] op_LOUT(int sel_reg ,int sel_ROM ,int sel_output_porte , byte ROM_data)//LOUT命令実行
        {
            byte[] cross_data = new byte[6];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if(sel_reg <= 1)
            {
                if (sel_reg == 1)
                {
                    Lower_bits = 8;
                }
            }
            else
            {
                Upper_bits = 1;
                if(sel_reg == 3)
                {
                    Lower_bits = 8;
                }
            }
            Upper_bits = (byte)((int)Upper_bits | 8);

            if(sel_ROM == 1)
            {
                Lower_bits = (byte)((int)Lower_bits | 4);
            }

            sel_output_porte = sel_output_porte & 3;
            Lower_bits = (byte)((int)Lower_bits | sel_output_porte);

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = ROM_data;
            cross_data[2] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[3] = ROM_data;
            cross_data[4] = 0x00;
            cross_data[5] = 0x00;
            return cross_data;
        }

        //シフト命令

        /*
         * sel_shift : 入力データselect
         * 0 → 右shift
         * 1 → 左shift
         * 
         * sel_input_reg : 入力データレジスタ指定
         * 0 → REG1
         * 1 → REG2
         * 2 → REG3
         * 3 → REG4
         * 
         * sel_output_reg : アウトプット先へ
         * 0 → フラグ検知用演算
         * 1 → Areg
         * 2 → Breg
         * 3 → Creg
         * 4 → REG1
         * 5 → REG2
         * 6 → REG3
         * 7 → REG4
         * 
         * MC_data : 意味のないデータ
        */

        static byte[] op_SIFT(int sel_shift ,int sel_input_reg ,int sel_output_reg ,byte MC_data)
        {
            byte[] cross_data = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if(sel_shift == 1)
            {
                Upper_bits = 8;
            }

            Upper_bits = (byte)((int)Upper_bits | 4); //SIFT

            if(sel_input_reg >= 2)
            {
                Upper_bits = (byte)((int)Upper_bits | 1);
            }

            if(sel_input_reg == 1 || sel_input_reg == 3)
            {
                Lower_bits = 8;
            }

            sel_output_reg = sel_output_reg & 7;
            Lower_bits = (byte)((int)Lower_bits | sel_output_reg);

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            cross_data[0] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[1] = MC_data;
            return cross_data;
        }

        //ロード命令

        /*
         * sel_input : データ入力場所指定
         * 0 → Areg
         * 1 → Breg
         * 2 → Creg
         * 3 → REG1
         * 4 → REG2
         * 5 → REG3
         * 6 → REG4
         * 7 → ROM
         * 
         * input_in_sel : データ入力場所をIN PORTEに強制的にする
         * 0 → IN PORTEにしない
         * 1 → IN1
         * 2 → IN2
         * 
         * sel_output_reg : アウトプット先へ
         * 0 → OPBA用予備コード ふつうは使用しない
         * 1 → Areg
         * 2 → Breg
         * 3 → Creg
         * 4 → REG1
         * 5 → REG2
         * 6 → REG3
         * 7 → REG4
         * 
         * ROM_data : ROMデータ用
        */
        static byte[] op_LDRG(int sel_input , int input_in_sel ,int sel_output_reg ,byte ROM_data)
        {
            byte[] cross_data = new byte[6];
            byte[] opba_code = new byte[2];
            byte Upper_bits = 0;
            byte Lower_bits = 0;

            if (input_in_sel == 0)
            {
                if(sel_input == 7) //ROM入力
                {
                    Upper_bits = 4;
                    Lower_bits = 0;
                }
                else if(sel_input <= 2) //Areg~Creg
                {
                    Upper_bits = 8;
                    if (sel_input == 1)
                    {
                        Lower_bits = (byte)((int)Lower_bits | 8);
                    }
                    else if (sel_input == 2)
                    {
                        Upper_bits = (byte)((int)Upper_bits | 1);
                    }
                }
                else //REG1~REG4
                {
                    Upper_bits = 0;
                    if(sel_input >= 5)
                    {
                        Upper_bits = (byte)((int)Upper_bits | 1);
                    }

                    if(sel_input == 4 || sel_input == 6)//REG2? REG4?
                    {
                        Lower_bits = (byte)((int)Lower_bits | 8);
                    }
                }
            }
            else if(input_in_sel == 1) //IN1指定
            {
                Upper_bits = 0x09;
                Lower_bits = 0X08;
            }
            else //IN2指定
            {
                Upper_bits = 0x06;
                Lower_bits = 0x00;
            }

            sel_output_reg = sel_output_reg & 7;
            Lower_bits = (byte)((int)Lower_bits | sel_output_reg);

            Upper_bits = (byte)((int)(Upper_bits) << 4);
            opba_code = op_OPAB(0x00);
            cross_data[0] = opba_code[0];
            cross_data[1] = opba_code[1];
            cross_data[2] = (byte)((int)Upper_bits | (int)Lower_bits);
            cross_data[3] = ROM_data;
            opba_code = op_OPBA(0x00);
            cross_data[4] = opba_code[0];
            cross_data[5] = opba_code[1];

            if (input_in_sel == 1)
            {
                //入力がINPUT1を使うコードはOPABとコード被りのため、フラグを立てないようにNOPを一回追加する
                Array.Resize(ref cross_data, cross_data.Length + 2);
                cross_data[6] = 0x00;
                cross_data[7] = 0x00;
            }

            return cross_data;
        }
    }
}