using Microsoft.International.Converters.PinYinConverter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var finder = new NameFinder();

            var commands = new List<ICommandExecuter>();
            commands.Add(new Initial());
            commands.Add(new Sex());
            commands.Add(new Count());
            commands.Add(new NotRepeat());
            commands.Add(new NotContains());
            commands.Add(new Pinyin());
            commands.Add(new Export());

            var cmd = "";
            while (true)
                for (var i = 0; i < commands.Count; i++)
                {
                    cmd = string.Empty;
                    if (!string.IsNullOrEmpty(commands[i].Desc()))
                    {
                        Console.WriteLine(commands[i].Desc());
                        cmd = Console.ReadLine();
                    }
                    commands[i].Start();
                    commands[i].CommandReceive(finder, cmd);
                    commands[i].Complete();
                    if (i == commands.Count - 1)
                    {
                        Console.WriteLine("----------------------------------");
                        Console.WriteLine("请检查default.txt文件，此为匹配后的结果");
                        Console.WriteLine("----------------------------------");
                    }
                }
        }
    }

    public interface ICommandExecuter
    {
        string Desc();

        void CommandReceive(NameFinder finder, string command);

        void Start();

        void Complete();
    }

    public class CommandExecuter : ICommandExecuter
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public virtual string Desc()
        {
            return string.Empty;
        }

        public virtual void CommandReceive(NameFinder finder, string command)
        {
        }

        public virtual void Start()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
        }


        public void Complete()
        {
            Console.WriteLine($"{GetType().Name} 完成耗时：{_stopwatch.ElapsedMilliseconds / 1000f}s");
            _stopwatch.Stop();
        }
    }

    public class NotRepeat : CommandExecuter
    {
        public override string Desc()
        {
            return "是否包含叠字：是0  否1 ";
        }

        public override void CommandReceive(NameFinder finder, string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                command = "0";
            }
            finder.AddCondition(name =>
            {
                var names = name.ToCharArray();

                if (names.Length == 1 || command.Trim() == "0")
                {
                    return true;
                }

                if (names.Length > 1)
                {
                    if (command.Trim() == "1")
                    {
                        return names[0] != names[1];
                    }
                }
                return true;
            });
        }
    }

    public class NotContains : CommandExecuter
    {
        public override string Desc()
        {
            return "排除字（enter跳过 默认使用block_text.txt内的屏蔽字库，随便输入个字符则为使用输入的屏蔽字）： ";
        }

        public override void Start()
        {
            base.Start();

            if (string.IsNullOrEmpty(content))
            {
                content = File.ReadAllText("../../../block_text.txt");
            }
        }

        public static string content = "";

        public override void CommandReceive(NameFinder finder, string command)
        {
            content = string.IsNullOrEmpty(command) ? content : command;
            Console.WriteLine($"排除关键字数量：{content.Length}");

            finder.AddCondition(name =>
            {
                var names = name.ToCharArray();

                for (int i = 0; i < names.Length; i++)
                {
                    if (content.Contains(names[i]))
                    {
                        return false;
                    }
                }
                return true;
            });
        }
    }

    public class Pinyin : CommandExecuter
    {
        public override string Desc()
        {
            return "0默认 多字名连续输入多个声调 如11 22 123 声调：1 2 3 4";
        }

        public static int Num;

        public override void CommandReceive(NameFinder finder, string command)
        {
            List<int> tons = new List<int>();
            int Num = 0;
            foreach (var tone in command)
            {
                Num = 0;
                if (int.TryParse(tone.ToString(), out Num))
                {
                    tons.Add(Num);
                }
            }
            //if (int.TryParse(command, out Num))
            //{
            //}

            finder.AddCondition(name =>
            {
                if (tons.Count == 0)
                {
                    return false;
                }

                if (name.Length != tons.Count)
                {
                    return false;
                }

                var nameChars = name.ToCharArray();

                for (int i = 0; i < tons.Count; i++)
                {
                    var cc = new ChineseChar(nameChars[i]);
                    var pinyins = cc.Pinyins.ToList();

                    for (int pingying_Index = 0; pingying_Index < pinyins.Count; pingying_Index++)
                    {
                        if (string.IsNullOrEmpty(pinyins[pingying_Index]))
                        {
                            continue;
                        }

                        if (int.Parse(pinyins[pingying_Index].LastOrDefault().ToString()) == tons[i])
                        {
                            return true;
                        }
                    }
                }
                //				Console.WriteLine($"{name} <--- ");
                return false;
            });
            //            var cc = new ChineseChar('鱼');
            //            var pinyins = cc.Pinyins.ToList();
            //            pinyins.ForEach(Console.WriteLine);
        }
    }


    public class Initial : CommandExecuter
    {
        public override string Desc()
        {
            return "";
        }

        public override void CommandReceive(NameFinder finder, string command)
        {
            finder.Initial(command);
        }
    }

    public class Export : CommandExecuter
    {
        public override string Desc()
        {
            return "";
        }

        public override void CommandReceive(NameFinder finder, string command)
        {
            finder.ExportData(command);
        }
    }



    public class Sex : CommandExecuter
    {
        public override string Desc()
        {
            return "筛选 0：女  1：男 (分类时间长，大概十几秒看电脑配置)";
        }

        public override void CommandReceive(NameFinder finder, string command)
        {
            finder.AddCondition(name =>
            {
                if (name.Trim().EndsWith("男") && command.Trim() == "1" ||
                    name.Trim().EndsWith("女") && command.Trim() == "0")
                    return true;
                else
                {
                    if (name.Trim().EndsWith("未知"))
                    {
                        return true;
                    }
                }

                return false;
            });
        }
    }

    public class Count : CommandExecuter
    {
        public static int Num = 0;
        public override string Desc()
        {
            return "字数 ：";
        }

        public override void CommandReceive(NameFinder finder, string command)
        {
            Num = 0;
            if (int.TryParse(command, out Num))
            {
            }

            finder.AddCondition(CheckName);
        }

        public bool CheckName(string name)
        {
            return name.Length == Num;
        }
    }


    public class NameFinder
    {
        private List<Func<string, bool>> Conditions;

        private List<string> filterData = new List<string>();

        private string loadPath = "../../../Chinese_Names.txt";
        private string outPath = "../../../default.txt";

        private bool record = true;

        public string[] RawData { get; private set; }

        public void AddCondition(Func<string, bool> condition)
        {
            if (filterData.Count == 0)
            {
                filterData = RawData.Distinct().ToList();
            }

            filterData = filterData.Where(name => condition(name)).Select(name =>
            {
                if (name.Contains(','))
                {
                    return name.Substring(1, name.IndexOf(',') - 1);
                }

                return name;
            }).Distinct().ToList();
            Console.WriteLine($"数量：{filterData.Count}");
        }


        public void Initial(string path = "")
        {
            filterData = new List<string>();
            Conditions = new List<Func<string, bool>>();
            loadPath = string.IsNullOrEmpty(path) ? loadPath : path;

            if (!File.Exists(loadPath))
            {
                Console.WriteLine($"can't find it: {loadPath}");
                return;
            }

            RawData = File.ReadAllLines(loadPath);
        }

        public void ExportData(string out_path = "")
        {
            outPath = string.IsNullOrEmpty(out_path) ? outPath : out_path;
            if (File.Exists(outPath)) File.Delete(outPath);
            File.WriteAllLines(outPath, filterData.ToArray());
        }
    }
}