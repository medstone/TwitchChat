using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class twitchChat : MonoBehaviour {
    public string oath;
    public string username;
    public string channel;
    public string address = "irc.chat.twitch.tv";
    public int port = 6667;
    public List<Text> outText;
    public Queue<string> messages;
    public Queue<string> outputQueue;
    public Queue<Queue<string>> eventQueue;
    public string input;
    public string output;
    public InputField channelField;

    private bool StopThreads = false;
    private System.Threading.Thread procIn, procOut;
    private System.Threading.ReaderWriterLock inLock, outLock;
    private System.Net.Sockets.NetworkStream netStream;
    private System.IO.StreamReader read;
    private System.IO.StreamWriter write;
    private string buffer;

    // Use this for initialization
    void Start () {
        System.Net.Sockets.TcpClient sock = new System.Net.Sockets.TcpClient();
        sock.Connect(address, port);
        if (!sock.Connected)
        {
            Debug.Log("Twitch contact failed");
            return;
        }
        eventQueue = new Queue<Queue<string>>();
        outputQueue = new Queue<string>();
        inLock = new System.Threading.ReaderWriterLock();
        outLock = new System.Threading.ReaderWriterLock();
        netStream = sock.GetStream();
        read = new System.IO.StreamReader(netStream);
        write = new System.IO.StreamWriter(netStream);

        write.WriteLine("PASS " + oath);
        write.WriteLine("NICK " + username);
        write.Flush();

        messages = new Queue<string>();

        procIn = new System.Threading.Thread(() => IRCInputProcedure());
        procIn.Start();
        procOut = new System.Threading.Thread(() => IRCOutputProcedure());
        procOut.Start();

        Debug.Log("We're in");

    }

    public void connectToChannel()
    {
        outLock.AcquireWriterLock(100);
        outputQueue.Enqueue("JOIN #" + channelField.text);
        outLock.ReleaseWriterLock();
    }
	
    private void IRCInputProcedure()
    {
        Queue<string> events;
        while (!StopThreads)
        {
            if (!netStream.DataAvailable)
            {
                System.Threading.Thread.Sleep(100);
                continue;
            }
            inLock.AcquireWriterLock(100);
            messages.Enqueue(read.ReadLine());
            input = messages.ToArray()[messages.Count-1];
            inLock.ReleaseWriterLock();

            //Debug.Log(input);
            if (input.Split(' ')[1] == "001")
            {
                outLock.AcquireWriterLock(100);
                outputQueue.Enqueue("JOIN #" + channel);
                outLock.ReleaseWriterLock();
            }
            else if(input.Split(' ')[0] == "PING")
            {
                input.Replace("PING", "PONG");
                outLock.AcquireWriterLock(100);
                outputQueue.Enqueue(buffer);
                outLock.ReleaseWriterLock();
            }
            else
            {
                if (input.Split(':').Length < 3)
                    continue;
                input = input.Split(':')[2];
                Debug.Log(input);
                if(input[0] == '!')
                {
                    events = new Queue<string>();
                    events.Enqueue("ChangeColor");
                    events.Enqueue(input.Substring(1).ToLower());
                    inLock.AcquireWriterLock(100);
                    eventQueue.Enqueue(events);
                    inLock.ReleaseWriterLock();
                }
            }
        }
    }

    private void IRCOutputProcedure()
    {
        while (!StopThreads)
        {
            if(outputQueue.Count < 1)
            {
                System.Threading.Thread.Sleep(100);
                continue;
            }
            outLock.AcquireReaderLock(100);
            output = outputQueue.Dequeue();
            outLock.ReleaseReaderLock();
            write.WriteLine(output);
            write.Flush();
            Debug.Log(output);
        }
    }

    void ChangeColor(string colorVal)
    {
        Color col = new Color();
        switch (colorVal)
        {
            case "red":
                col = Color.red;
                break;
            case "white":
                col = Color.white;
                break;
            case "blue":
                col = Color.blue;
                break;
            case "green":
                col = Color.green;
                break;
            case "yellow":
                col = Color.yellow;
                break;
            default:
                col = Color.black;
                break;
        }
        foreach(Text tex in outText)
        {
            tex.color = col;
        }
    }

// Update is called once per frame
void Update () {
        Queue<string> post = new Queue<string>();
        inLock.AcquireReaderLock(100);
        while (eventQueue.Count > 0)
        {
            post = eventQueue.Dequeue();
            gameObject.SendMessage(post.Dequeue(), post.Dequeue());
        }
        while(messages.Count > outText.Count)
        {
            messages.Dequeue();
        }
        for (int i = 0; i < messages.Count; ++i)
        {
            outText[i].text = messages.ToArray()[i];
        }
        inLock.ReleaseReaderLock();
    }

    void OnDestroy()
    {
        StopThreads = true;
    }
}
