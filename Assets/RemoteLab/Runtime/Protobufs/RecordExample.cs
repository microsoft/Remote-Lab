using Google.Protobuf;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecordExample : MonoBehaviour
{
    public int objectsToGenerate = 10;
    public int secondsToGenerate = 60;
    public int samplingRate = 4;

    void Start()
    {
        Debug.Log("Writing and reading protobuf file");
        CreateAndWriteRecord();
        ReadRecord();
    }

    /// <summary>
    /// Remember to add "using Google.Protobuf;".
    /// To create and write the record to a protobuf, I've created a Record object that holds a List of Steps.
    /// A single Step is an entry in your spreadsheet, and it holds all the data you're tracking.
    /// 
    /// You first create a Record object, put it aside.
    /// Create a Step object, and fill all the parameters you need (name, pos, hierarchy, etc).
    /// Then Add() the Step into the Record_ field of the Record object.
    /// Repeat as many times as needed.
    /// 
    /// Once the Record object is filled with Step objects, use the WriteTo() function to write out to a file.
    /// </summary>
    private void CreateAndWriteRecord()
    {
        Record r = new Record();
        for (int i = 0; i < secondsToGenerate * samplingRate; i++)
        {
            for (int j = 0; j < objectsToGenerate; j++)
            {
                Step s = new Step
                {
                    FrameCount = i,
                    GameObject = $"Object{j}",
                    Status = Step.Types.StatusEnum.Changed,
                    PositionX = i,
                    PositionY = i,
                    PositionZ = i,
                    RotationX = i,
                    RotationY = i,
                    RotationZ = i,
                    ScaleX = i,
                    ScaleY = i,
                    ScaleZ = i,
                    Resource = $"Object{j}",
                    ID = $"Object{j}",
                    Hierarchy = $"Object{j}"
                };

                r.Record_.Add(s);
            }   
        }

        using (FileStream stream = File.Create(Path.Combine(Application.dataPath, "recordexample.bin")))
        {
            r.WriteTo(stream);
        }
    }

    /// <summary>
    /// Same deal but backwards.
    /// 
    /// Use the Parser.ParseFrom() function to read from a protobuffed file.
    /// Then access the Record_ member as a List and extract any datapoints from the Step.
    /// Feel free to edit the record.proto file as needed (under ReplaySystem/Scripts/Protobufs).
    /// The protobuffed file is saved at the root of the Assets folder.
    /// 
    /// Consider further optimizations, such as mapping the names of the tracked GameObjects into a table and coding
    /// the names into an int, or reducing the string size of the Hierarchy field, etc. Feel free to play around
    /// with the number of generated objects, the sampling rate, and seconds to store. Doing something ridiculous like
    /// tracking 100 objects for an hour at a 30Hz sampling rate gives us 952 MB. That's very well optimized vs. if we
    /// stored all as strings.
    /// </summary>
    private void ReadRecord()
    {
        Record r;
        using (FileStream stream = File.OpenRead(Path.Combine(Application.dataPath, "recordexample.bin")))
        {
            r = Record.Parser.ParseFrom(stream);
        }

        Debug.Log(r.CalculateSize());
        Debug.Log(r.Record_.ToString());
        Debug.Log(r.Record_[0].PositionX);
    }
}
