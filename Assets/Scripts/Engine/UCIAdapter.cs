using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityXiangqi;

interface UCIAdapter
{
    public delegate void BestmoveCalculated(Movement move);
    public static event BestmoveCalculated OnBestmoveCalculated;

    // Start the engine
    void Start();
    void Stop();
    void GetBestmove(string fen);
}
