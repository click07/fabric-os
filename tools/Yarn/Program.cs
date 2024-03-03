using System;
using System.Collections.Generic;
using System.Text;

public class Transpiler
{

public static void Main(string[] args)
{
    Console.WriteLine("Enter File Path to your Oxided Output File (out.urcl):");
    string path = Console.ReadLine();
    string inputIL = File.ReadAllText(path);
    string outputIL = Spread(inputIL);
    outputIL = Adjust(outputIL);
    outputIL = Syscalls(outputIL);
    outputIL = SplitIMM(outputIL);
    outputIL = AddZeros(outputIL);
    outputIL = ConvertLabels(outputIL);
    outputIL = Assemble(outputIL);
    Console.WriteLine(outputIL);

}

public static string Spread(string inputIL)
{
    string[] lines = inputIL.Split('\n');
    List<string> outputLines = new List<string>();

    int skippedLines = 0;
    foreach (string line in lines)
    {
        if (skippedLines < 5)
        {
            skippedLines++;
            continue;
        }

        string trimmedLine = line.Trim();
        if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("//"))
        {
            outputLines.Add(trimmedLine);
        }
    }

    List<string> newOutputLines = new List<string>();

    foreach (string line in outputLines)
    {
        string[] tokens = line.Split(' ');
        string instruction = tokens[0];
        string[] operands = new string[3];

        if (tokens.Length >= 2 && tokens[1][0] == '.')
        {
            newOutputLines.Add($"IMM R7 {tokens[1]}");
            newOutputLines.Add($"{instruction} R7 {string.Join(" ", tokens.Skip(2))}"); 
            continue;
        }

        if (tokens.Length <= 2)
        {
            newOutputLines.Add(line);
            continue;
        }
        operands[0] = tokens[1]; 

        if (tokens[2][0] != 'R')
        {
            newOutputLines.Add($"IMM R7 {tokens[2]}");
            operands[1] = "R7";
        }
        else
        {
            operands[1] = tokens[2];
        }

        if (tokens.Length > 3 && tokens[3][0] != 'R')
        {
            newOutputLines.Add($"IMM R8 {tokens[3]}");
            operands[2] = "R8";
        }
        else if (tokens.Length > 3)
        {
            operands[2] = tokens[3];
        }

        newOutputLines.Add($"{instruction} {string.Join(" ", operands)}");
    }

    outputLines = newOutputLines;

    return string.Join("\n", outputLines);
}

public static string Adjust(string inputIL)
{
    string[] lines = inputIL.Split('\n');
    List<string> outputLines = new List<string>();

    outputLines.Add("IMM R2 16");
    outputLines.Add("SYS MALLOC R2 R2");

    foreach (string line in lines)
    {
        string adjustedLine = line.Trim();
        if (adjustedLine.StartsWith("HLT"))
        {
            adjustedLine = "SYS HLT R0";
        }
        else if (adjustedLine.StartsWith("LSTR"))
        {
            string[] tokens = adjustedLine.Split(' ');
            string newLine = $"STR {tokens[1]} {tokens[3]} {tokens[2]}";
            adjustedLine = newLine;
        }
        else if (adjustedLine.StartsWith("STR"))
        {
            string[] tokens = adjustedLine.Split(' ');
            string newLine = $"DEC {tokens[1]} {tokens[1]}\nSTR R2 {tokens[1]} {tokens[2]}";
            adjustedLine = newLine;
        }

        outputLines.Add(adjustedLine);
    }

    return string.Join("\n", outputLines);
}

public static string Syscalls(string inputIL)
{
    string[] lines = inputIL.Split('\n');
    List<string> outputLines = new List<string>();

    bool[] syscalls = new bool[59];

    foreach (string line in lines)
    {
        string outputLine = line.Trim();

        outputLines.Add(outputLine);

        switch(outputLine)
        {
	    case "IMM R7 .oxided_exit":
		syscalls[0] = true;
		break;
	    case "IMM R7 .oxided_get":
		syscalls[4] = true;
		break;
	}
    }

    if(syscalls[0])
    {
	outputLines.Add(".oxided_exit");
	outputLines.Add("POP R3");
	outputLines.Add("POP R4");
	outputLines.Add("SYS HLT R4");
	outputLines.Add("PSH R3");
	outputLines.Add("RET");
    }
    if(syscalls[4])
    {
	outputLines.Add(".oxided_get");
	outputLines.Add("POP R3");
	outputLines.Add("POP R4");
	outputLines.Add("SYS GET R1 R4");
	outputLines.Add("PSH R3");
	outputLines.Add("RET");
    }

    return string.Join("\n", outputLines);
}


public static string SplitIMM(string inputIL)
{
    string[] lines = inputIL.Split('\n');
    List<string> outputLines = new List<string>();

    foreach (string line in lines)
    {
        string trimmedLine = line.Trim();

        if (trimmedLine.StartsWith("IMM"))
        {
            string[] tokens = trimmedLine.Split(' ');
            outputLines.Add($"{tokens[0]} {tokens[1]}");
            outputLines.Add(tokens[2]);
        }
        else
        {
            outputLines.Add(trimmedLine);
        }
    }

    return string.Join("\n", outputLines);
}

public static string AddZeros(string inputIL)
{
    string[] lines = inputIL.Split('\n');
    List<string> outputLines = new List<string>();

    foreach (string line in lines)
    {
        string trimmedLine = line.Trim();

        if (trimmedLine.Split(' ').Length < 4 && !(trimmedLine.StartsWith(".") || char.IsDigit(trimmedLine[0])))
        {
            string[] tokens = trimmedLine.Split(' ');
            while (tokens.Length < 4)
            {
                trimmedLine += " R0";
                tokens = trimmedLine.Split(' ');
            }
        }

        outputLines.Add(trimmedLine);
    }

    return string.Join("\n", outputLines);
}

public static string ConvertLabels(string inputIL)
{
    string[] lines = inputIL.Split('\n');
    List<string> outputLines = new List<string>();

    Dictionary<string, int> labels = new Dictionary<string, int>();
    List<int> immIndexes = new List<int>();

    int lineCount = 0;
    foreach (string line in lines)
    {
        string trimmedLine = line.Trim();

        if (trimmedLine.StartsWith("."))
        {
            if (!outputLines.Any() || !outputLines.Last().StartsWith("IMM"))
            {
                string label = trimmedLine.Substring(1);
                if (!labels.ContainsKey(label))
                {
                    labels.Add(label, lineCount);
                }
            }
        }

        if (trimmedLine.StartsWith("IMM"))
        {
            immIndexes.Add(outputLines.Count);
        }

        outputLines.Add(trimmedLine);
        lineCount++;
    }

    foreach (int immIndex in immIndexes)
    {
        if (immIndex + 1 < outputLines.Count && outputLines[immIndex + 1].StartsWith("."))
        {
            string label = outputLines[immIndex + 1].Substring(1);
            if (labels.TryGetValue(label, out int matchedLineCount))
	    {
                outputLines[immIndex + 1] = matchedLineCount.ToString(); 
            }
        }
    }

    return string.Join("\n", outputLines);
}

public static string Assemble(string inputIL)
{
    Dictionary<string, string> instructionDictionary = new Dictionary<string, string>()
    {
        {"R0", "00"},
        {"R1", "01"},
        {"R2", "02"},
        {"R3", "03"},
        {"R4", "04"},
        {"R5", "05"},
        {"R6", "06"},
        {"R7", "07"},
        {"R8", "08"},
        {"SYS", "01"},
        {"IMM", "02"},
        {"ADD", "03"},
        {"RSH", "04"},
        {"LOD", "05"},
        {"STR", "06"},
        {"PSH", "07"},
        {"POP", "08"},
        {"NOR", "09"},
        {"JMP", "0C"},
        {"MOV", "0D"},
        {"CAL", "1C"},
        {"RET", "1D"},
        {"XOR", "0F"},
        {"INC", "10"},
        {"DEC", "11"},
        {"SUB", "12"},
        {"SETGE", "13"},
        {"MLT", "1E"},
        {"DIV", "1F"},
        {"MOD", "20"},
        {"LSH", "21"},
        {"SETG", "25"},
        {"SETE", "24"},
        {"SETLE", "26"},
        {"SETL", "27"},
        {"SETNE", "28"},
        {"BSR", "04"},
        {"BSL", "21"},
        {"HLT", "00"},
        {"WAIT", "01"},
        {"FTIME", "02"},
        {"RTIME", "03"},
        {"GET", "04"},
        {"SET", "05"},
        {"TICK", "06"},
        {"CLCK", "07"},
        {"EXEC", "08"},
        {"SUSP", "09"},
        {"PSTA", "0A"},
        {"FORK", "0B"},
        {"END", "0C"},
        {"TERM", "0D"},
        {"WFOR", "0E"},
        {"RESP", "0F"},
        {"RLOE", "10"},
        {"LSTP", "11"},
        {"MALLOC", "12"},
        {"FREE", "13"},
        {"OPEN", "14"},
        {"SWTC", "15"},
        {"CLOSE", "16"},
        {"READ", "17"},
        {"WRIT", "18"},
        {"NEWF", "19"},
        {"NEWD", "1A"},
        {"GETI", "1B"},
        {"SETI", "1C"},
        {"DEL", "1D"},
        {"RESN", "1E"},
        {"OUTN", "1F"},
        {"OUTC", "20"},
        {"OUTS", "21"},
        {"SEED", "22"},
        {"RAND", "23"},
        {"DRAW", "24"},
        {"CNVS", "25"},
        {"CNVST", "26"},
        {"CLMS", "27"},
        {"CLMG", "28"},
        {"FILS", "29"},
        {"DBOX", "2A"},
        {"DLINE", "2B"},
        {"SBUF", "2C"},
        {"GBUF", "2D"},
        {"COLR", "2E"},
        {"SSCR", "2F"},
        {"GSCR", "30"},
        {"ACHA", "31"},
        {"PLAY", "32"},
        {"KEYB", "33"},
        {"MPOS", "34"},
        {"MBUT", "35"},
        {"CTRL", "36"},
        {"MDXY", "37"},
        {"SETM", "38"},
        {"RECV", "39"},
        {"SEND", "3A"}
    };

    string[] lines = inputIL.Split('\n');
    List<string> outputLines = new List<string>();

    foreach (string line in lines)
    {
        string outputLine = line.Trim();
        if (outputLine.StartsWith("."))
        {
            outputLine = "00000000";
        }
        else if (int.TryParse(outputLine, out int lineNumber))
        {
            outputLine = lineNumber.ToString("X8");
        }
        else
        {
            string[] tokens = outputLine.Split(' ');
            StringBuilder hexLine = new StringBuilder();
            foreach (string token in tokens)
            {
                if (instructionDictionary.TryGetValue(token, out string hexValue))
                {
                    hexLine.Append(hexValue);
                }
                else
                {
                    hexLine.Append("00");
                }
            }
            outputLine = hexLine.ToString();
        }

        outputLine = outputLine.Replace(" ", "");
        outputLines.Add(outputLine);
    }

    return string.Join("\n", outputLines);
}



}
