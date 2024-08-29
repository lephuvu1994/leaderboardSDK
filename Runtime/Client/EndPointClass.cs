using System;
using MyriaLeaderboard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyriaLeaderboard
{
    [Serializable]
    public class EndPointClass
    {
        public string endPoint { get; set; }
        public MyriaLeaderboardHTTPMethod httpMethod { get; set; }

        public EndPointClass() { }

        public EndPointClass(string endPoint, MyriaLeaderboardHTTPMethod httpMethod)
        {
            this.endPoint = endPoint;
            this.httpMethod = httpMethod;
        }
    }
}