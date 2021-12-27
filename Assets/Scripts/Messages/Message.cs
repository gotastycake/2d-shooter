using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class Message
{
    public int id, pid;
    public string type;
    public Dictionary<string, string> data = new Dictionary<string, string>();

    public byte[] ToBytes() {
        BinaryFormatter converter = new BinaryFormatter();
        MemoryStream memoryStream = new MemoryStream();
        converter.Serialize(memoryStream, this);
        return memoryStream.ToArray();
    }

    public static Message FromBytes(byte[] bytes) {
        BinaryFormatter converter = new BinaryFormatter();
        MemoryStream memoryStream = new MemoryStream(bytes);
        memoryStream.Position = 0;
        return (Message)converter.Deserialize(memoryStream);
    }
}
