using System;
using System.Collections.Generic;

public class Transpiler
{

public static void Main(string[] args)
{
    Console.WriteLine("Enter File Path to your Oxided Output File (out.urcl):");
    string path = Console.ReadLine();
    string inputIL = File.ReadAllText(path);
    string outputIL = Spread(inputIL);
    outputIL = Adjust(outputIL);
    Console.WriteLine(outputIL);
    outputIL = SplitIMM(outputIL);
    outputIL = AddZeros(outputIL);
    outputIL = ConvertLabels(outputIL);
    //Console.WriteLine(outputIL);
    // outputIL = Assemble();

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
            adjustedLine = "SYS EXIT R0";
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


}
