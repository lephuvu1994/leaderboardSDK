
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyriaLeaderboard
{
    public class MyriaLeaderboardEndPoints
    {
        // Authentication
        [Header("GetScore")]
        public static EndPointClass MyriaEndPointGetScoreByLeaderboardId = new EndPointClass("v1/leaderboards/{0}/scores?page={1}&limit={2}&sortingField={3}&orderBy={4}", MyriaLeaderboardHTTPMethod.GET);

        [Header("PostScore")]
        public static EndPointClass MyriaEndPointPostScoreByLeaderboardId = new EndPointClass("v1/leaderboards/{0}/scores", MyriaLeaderboardHTTPMethod.POST);

    }
}
